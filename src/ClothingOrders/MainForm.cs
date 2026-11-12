using Microsoft.Web.WebView2.WinForms;

namespace ClothingOrders;

public sealed class MainForm : Form
{
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly string _dataDirectory;

    public MainForm()
    {
        Text = "דעת אהרן | מערכת ניהול הזמנות לבוש";
        Width = 1440;
        Height = 940;
        MinimumSize = new Size(1050, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 20, 36);
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClothingOrders", "WebView2");
        Controls.Add(_web);
        Shown += async (_, _) => await InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            Directory.CreateDirectory(_dataDirectory);
            var environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .CreateAsync(userDataFolder: _dataDirectory);
            await _web.EnsureCoreWebView2Async(environment);
            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
            var page = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");
            if (!File.Exists(page))
                throw new FileNotFoundException("קובץ הממשק לא נמצא.", page);
            _web.Source = new Uri(page);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "לא ניתן להפעיל את ממשק התוכנה. ודא ש-WebView2 Runtime מותקן.\n\n" + ex.Message,
                "שגיאה בהפעלת התוכנה", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}