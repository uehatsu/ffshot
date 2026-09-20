using FFShot.App;
using FFShot.Capture;
using FFShot.Settings;
using FFShot.UI;
using Windows.Win32.Foundation;

namespace FFShot.Tests;

/// <summary>
/// README 用スクリーンショットの生成。通常のテスト実行では何もしない。
/// 実行: FFSHOT_DOCS=1 dotnet test tests/FFShot.Tests --filter DocsScreenshots
/// </summary>
[Trait("Category", "Screen")]
public class DocsScreenshots
{
    private static bool Enabled => Environment.GetEnvironmentVariable("FFSHOT_DOCS") == "1";

    private static string DocsDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "ffshot.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return Path.Combine(dir ?? throw new InvalidOperationException("ffshot.sln が見つかりません"), "docs");
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var t = new Thread(() =>
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (error is not null)
        {
            throw error;
        }
    }

    private static void Pump(int ms)
    {
        var until = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < until)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }
    }

    [Fact]
    public void SettingsDialog()
    {
        if (!Enabled) return;

        RunSta(() =>
        {
            using var form = new SettingsForm(new AppSettings());
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(200, 200);
            form.Show();
            form.Activate();
            Pump(800);

            var bounds = WindowInfo.GetWindowBounds(new HWND(form.Handle))
                ?? throw new InvalidOperationException("ウィンドウ矩形が取れません");
            using var backend = new GdiCaptureBackend();
            using var bmp = backend.Capture(bounds);
            var path = Path.Combine(DocsDir(), "settings.png");
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            form.Close();
        });
    }

    [Fact]
    public void Notification()
    {
        if (!Enabled) return;

        RunSta(() =>
        {
            using var icon = TrayIconFactory.CreateTrayIcon();
            using var tray = new NotifyIcon { Icon = icon, Text = "ffshot", Visible = true };
            Pump(300);
            tray.ShowBalloonTip(4000, "ffshot - 保存しました", "ffshot_20260921_120000.png\n3840x2160 (Direct3D)", ToolTipIcon.Info);
            Pump(1500);

            // Windows 11 のトースト通知は作業領域の右下に出る（余白込みで切り出す）
            var wa = Screen.PrimaryScreen!.WorkingArea;
            var scale = wa.Width / 1920f;
            var w = (int)(200 * scale);
            var h = (int)(80 * scale);
            var bounds = new Rectangle(wa.Right - w, wa.Bottom - h, w, h);
            using var backend = new GdiCaptureBackend();
            using var bmp = backend.Capture(bounds);
            var path = Path.Combine(DocsDir(), "notification.png");
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            tray.Visible = false;
        });
    }
}
