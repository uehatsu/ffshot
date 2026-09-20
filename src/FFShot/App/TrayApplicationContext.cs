using System.Diagnostics;
using FFShot.Capture;
using FFShot.Hotkeys;
using FFShot.Settings;
using FFShot.UI;

namespace FFShot.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int HotkeyIdFullScreen = 1;
    private const int HotkeyIdActiveWindow = 2;

    private readonly SettingsStore _store = new();
    private AppSettings _settings;
    private readonly NotifyIcon _tray;
    private readonly Icon _icon;
    private readonly HotkeyManager _hotkeys = new();
    private readonly CaptureService _capture = new();
    private bool _capturing;
    private SettingsForm? _settingsForm;

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
        _tray.DoubleClick += (_, _) => ShowSettings();

        _hotkeys.HotkeyPressed += OnHotkey;
        ApplyHotkeys();
        ApplyStartupRegistration();
    }

    private void ApplyStartupRegistration()
    {
        try
        {
            new StartupRegistration().Apply(_settings.RunAtStartup);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            _tray.ShowBalloonTip(5000, "ffshot - 自動起動の設定に失敗しました", ex.Message, ToolTipIcon.Warning);
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("全画面を撮影(&F)", null, (_, _) => CaptureAfterMenuCloses(CaptureTarget.FullScreen));
        menu.Items.Add("アクティブウィンドウを撮影(&A)", null, (_, _) => CaptureAfterMenuCloses(CaptureTarget.ActiveWindow));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("設定(&S)...", null, (_, _) => ShowSettings());
        menu.Items.Add("保存先を開く(&O)", null, (_, _) => OpenSaveFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了(&X)", null, (_, _) => ExitThread());
        return menu;
    }

    private void OnHotkey(int id)
    {
        switch (id)
        {
            case HotkeyIdFullScreen:
                DoCapture(CaptureTarget.FullScreen);
                break;
            case HotkeyIdActiveWindow:
                DoCapture(CaptureTarget.ActiveWindow);
                break;
        }
    }

    /// <summary>メニューから撮る場合はメニューが閉じて画面が落ち着くまで少し待つ。</summary>
    private void CaptureAfterMenuCloses(CaptureTarget target)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 350 };
        timer.Tick += (_, _) =>
        {
            timer.Dispose();
            DoCapture(target);
        };
        timer.Start();
    }

    private void DoCapture(CaptureTarget target)
    {
        if (_capturing)
        {
            return;
        }
        _capturing = true;
        try
        {
            var result = _capture.Capture(target, _settings);
            if (result.Warning is not null)
            {
                _tray.ShowBalloonTip(5000, "ffshot - フォールバック", result.Warning, ToolTipIcon.Warning);
            }
            else if (_settings.ShowNotification)
            {
                _tray.ShowBalloonTip(2000, "ffshot - 保存しました",
                    $"{Path.GetFileName(result.Path)}\n{result.Bounds.Width}x{result.Bounds.Height} ({result.BackendName})", ToolTipIcon.Info);
            }
        }
        catch (Exception ex) when (ex is CaptureException or IOException or UnauthorizedAccessException)
        {
            _tray.ShowBalloonTip(5000, "ffshot - 撮影に失敗しました", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _capturing = false;
        }
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        // 設定中はキー入力がホットキーに横取りされないよう一時解除
        _hotkeys.UnregisterAll();
        using (_settingsForm = new SettingsForm(_settings))
        {
            if (_settingsForm.ShowDialog() == DialogResult.OK)
            {
                _settings = _settingsForm.Result;
                _store.Save(_settings);
                ApplyStartupRegistration();
            }
        }
        _settingsForm = null;
        ApplyHotkeys();
    }

    private void ApplyHotkeys()
    {
        var failed = new List<string>();
        RegisterOrReport(HotkeyIdFullScreen, _settings.HotkeyFullScreen, "全画面", failed);
        RegisterOrReport(HotkeyIdActiveWindow, _settings.HotkeyActiveWindow, "アクティブウィンドウ", failed);

        if (failed.Count > 0)
        {
            _tray.ShowBalloonTip(5000, "ffshot - ホットキーを登録できません",
                string.Join("\n", failed) + "\n他のアプリと競合していないか確認してください。", ToolTipIcon.Warning);
        }
    }

    private void RegisterOrReport(int id, string text, string label, List<string> failed)
    {
        if (!HotkeyBinding.TryParse(text, out var binding))
        {
            _hotkeys.Unregister(id);
            return; // 未設定は無効化扱い
        }
        if (!_hotkeys.Register(id, binding))
        {
            failed.Add($"{label}: {binding}");
        }
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
            _hotkeys.Dispose();
            _capture.Dispose();
            _tray.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
