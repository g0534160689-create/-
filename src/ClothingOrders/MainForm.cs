using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;

namespace ClothingOrders;

public sealed class MainForm : Form
{
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly string _dataDirectory;
    private SqliteStore? _store;

    public MainForm()
    {
        Text = "תומכי תורה | ישיבת דעת אהרן";
        Width = 1440;
        Height = 940;
        MinimumSize = new Size(1050, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 20, 36);
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClothingOrders");
        Controls.Add(_web);
        Shown += async (_, _) => await InitializeBrowserAsync();
        FormClosed += (_, _) => _store?.Dispose();
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            Directory.CreateDirectory(_dataDirectory);
            _store = new SqliteStore(Path.Combine(_dataDirectory, "clothing-orders.db"));
            var webViewDirectory = Path.Combine(_dataDirectory, "WebView2");
            Directory.CreateDirectory(webViewDirectory);
            var environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .CreateAsync(userDataFolder: webViewDirectory);
            await _web.EnsureCoreWebView2Async(environment);
            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _web.CoreWebView2.WebMessageReceived += HandleWebMessage;
            _web.CoreWebView2.NavigationCompleted += (_, _) => RestoreOrMigrateBrowserData();
            var page = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");
            if (!File.Exists(page))
                throw new FileNotFoundException("קובץ הממשק לא נמצא.", page);
            _web.Source = new Uri(page);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "לא ניתן להפעיל את התוכנה. ודא ש-WebView2 Runtime מותקן.\n\n" + ex.Message,
                "שגיאה בהפעלת התוכנה", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreOrMigrateBrowserData()
    {
        if (_store is null || _web.CoreWebView2 is null) return;
        var snapshot = _store.LoadSnapshot();
        _web.CoreWebView2.PostWebMessageAsString(
            snapshot is null ? "__export_local_data__" : JsonSerializer.Serialize(new
            {
                type = "restore",
                payload = snapshot
            }));
    }

    private void HandleWebMessage(object? sender,
        Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_store is null) return;
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "sqlite-snapshot")
                return;
            if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.String)
                return;
            var json = payload.GetString();
            if (string.IsNullOrWhiteSpace(json) || json.Length > 50_000_000) return;
            _store.SaveSnapshot(json);
        }
        catch (JsonException)
        {
            // Ignore malformed messages; the existing browser copy remains intact.
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            MessageBox.Show(this, "שמירת הנתונים למסד הנתונים נכשלה. מומלץ לייצא גיבוי.\n\n" + ex.Message,
                "שגיאת שמירה", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
