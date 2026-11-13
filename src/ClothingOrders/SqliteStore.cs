using System.Text.Json;
using System.IO.Compression;
using System.Text;
using Microsoft.Data.Sqlite;

namespace ClothingOrders;

internal sealed class SqliteStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteStore(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString());
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=FULL;
            PRAGMA foreign_keys=ON;
            PRAGMA busy_timeout=5000;
            CREATE TABLE IF NOT EXISTS app_state (
                state_key TEXT PRIMARY KEY NOT NULL,
                payload TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER PRIMARY KEY NOT NULL,
                applied_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS snapshot_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                payload TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            INSERT OR IGNORE INTO schema_migrations(version, applied_utc)
            VALUES (1, strftime('%Y-%m-%dT%H:%M:%fZ','now'));
            INSERT OR IGNORE INTO schema_migrations(version, applied_utc)
            VALUES (2, strftime('%Y-%m-%dT%H:%M:%fZ','now'));
            """;
        command.ExecuteNonQuery();

        using var check = _connection.CreateCommand();
        check.CommandText = "PRAGMA quick_check;";
        var integrity = Convert.ToString(check.ExecuteScalar());
        if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("בדיקת תקינות מסד הנתונים נכשלה: " + integrity);
    }

    public string? LoadSnapshot()
    {
        var current = ReadCurrentSnapshot();
        if (IsValidJson(current))
            return current;

        var previous = ReadLatestValidHistorySnapshot();
        if (previous is null)
            return null;

        // Restore the last valid snapshot after detecting a damaged current payload.
        SaveSnapshot(previous);
        return previous;
    }

    public void SaveSnapshot(string payload)
    {
        if (!IsValidJson(payload))
            throw new InvalidDataException("לא ניתן לשמור תמונת נתונים שאינה JSON תקין.");

        using var transaction = _connection.BeginTransaction();
        var previous = ReadCurrentSnapshot(transaction);
        if (string.Equals(previous, payload, StringComparison.Ordinal))
        {
            transaction.Rollback();
            return;
        }

        if (IsValidJson(previous))
        {
            using var history = _connection.CreateCommand();
            history.Transaction = transaction;
            history.CommandText = """
                INSERT INTO snapshot_history(payload, created_utc)
                SELECT $payload, strftime('%Y-%m-%dT%H:%M:%fZ','now')
                WHERE NOT EXISTS (
                    SELECT 1 FROM snapshot_history
                    WHERE created_utc >= strftime('%Y-%m-%dT%H:%M:%fZ','now','-5 minutes')
                );
                DELETE FROM snapshot_history
                WHERE id NOT IN (
                    SELECT id FROM snapshot_history ORDER BY id DESC LIMIT 30
                );
                """;
            history.Parameters.AddWithValue("$payload", EncodeSnapshot(previous!));
            history.ExecuteNonQuery();
        }

        using var save = _connection.CreateCommand();
        save.Transaction = transaction;
        save.CommandText = """
            INSERT INTO app_state(state_key, payload, updated_utc)
            VALUES ($key, $payload, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
            ON CONFLICT(state_key) DO UPDATE SET
                payload = excluded.payload,
                updated_utc = excluded.updated_utc;
            """;
        save.Parameters.AddWithValue("$key", "webview-localstorage-v1");
        save.Parameters.AddWithValue("$payload", EncodeSnapshot(payload));
        save.ExecuteNonQuery();
        transaction.Commit();
    }

    private string? ReadCurrentSnapshot(SqliteTransaction? transaction = null)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT payload FROM app_state WHERE state_key = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", "webview-localstorage-v1");
        return DecodeSnapshot(command.ExecuteScalar() as string);
    }

    private string? ReadLatestValidHistorySnapshot()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT payload FROM snapshot_history ORDER BY id DESC LIMIT 30;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var candidate = DecodeSnapshot(reader.GetString(0));
            if (IsValidJson(candidate))
                return candidate;
        }
        return null;
    }

    // Keep the database compact and reduce disk I/O for large multi-workspace snapshots.
    // The prefix preserves compatibility with databases containing older plain-JSON rows.
    private static string EncodeSnapshot(string value)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            gzip.Write(bytes, 0, bytes.Length);
        }
        return "gzip:v1:" + Convert.ToBase64String(output.ToArray());
    }

    private static string? DecodeSnapshot(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith("gzip:v1:", StringComparison.Ordinal))
            return value;

        try
        {
            var compressed = Convert.FromBase64String(value[8..]);
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or IOException)
        {
            return null;
        }
    }

    private static bool IsValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void Dispose() => _connection.Dispose();
}
