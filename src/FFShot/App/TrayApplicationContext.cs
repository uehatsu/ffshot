using System.Diagnostics;
using FFShot.Settings;

namespace FFShot.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _store = new();
    private AppSettings _settings;
    private readonly NotifyIcon _tray;
    private readonly Icon _icon;

    public TrayApplicationContext()
    {
        _settings = _store.Load();
        _icon = TrayIconFactory.Create();

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "ffshot",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("全画面を撮影(&F)", null, (_, _) => { /* Phase 3 */ });
        menu.Items.Add("アクティブウィンドウを撮影(&A)", null, (_, _) => { /* Phase 3 */ });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("設定(&S)...", null, (_, _) => { /* Phase 2 */ });
        menu.Items.Add("保存先を開く(&O)", null, (_, _) => OpenSaveFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了(&X)", null, (_, _) => ExitThread());
        return menu;
    }

    private void OpenSaveFolder()
    {
        var folder = _settings.ResolvedSaveFolder;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
