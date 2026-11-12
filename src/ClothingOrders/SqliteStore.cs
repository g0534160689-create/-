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
            CREATE TABLE IF NOT EXISTS app_state (
                state_key TEXT PRIMARY KEY NOT NULL,
                payload TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER PRIMARY KEY NOT NULL,
                applied_utc TEXT NOT NULL
            );
            INSERT OR IGNORE INTO schema_migrations(version, applied_utc)
            VALUES (1, strftime('%Y-%m-%dT%H:%M:%fZ','now'));
            """;
        command.ExecuteNonQuery();
    }

    public string? LoadSnapshot()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT payload FROM app_state WHERE state_key = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", "webview-localstorage-v1");
        return command.ExecuteScalar() as string;
    }

    public void SaveSnapshot(string payload)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO app_state(state_key, payload, updated_utc)
            VALUES ($key, $payload, strftime('%Y-%m-%dT%H:%M:%fZ','now'))
            ON CONFLICT(state_key) DO UPDATE SET
                payload = excluded.payload,
                updated_utc = excluded.updated_utc;
            """;
        command.Parameters.AddWithValue("$key", "webview-localstorage-v1");
        command.Parameters.AddWithValue("$payload", payload);
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
