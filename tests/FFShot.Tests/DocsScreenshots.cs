using FFShot.Capture;
using FFShot.Settings;
using FFShot.UI;
using Windows.Win32.Foundation;

namespace FFShot.Tests;

/// <summary>
/// README 用スクリーンショットの生成。通常のテスト実行では何もしない。
/// 実行: FFSHOT_DOCS=1 dotnet test tests/FFShot.Tests --filter DocsScreenshots
/// FFSHOT_DOCS_LANG=en を付けると英語 UI で docs/settings_en.png に出力する（README_EN 用）。
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
                var lang = Environment.GetEnvironmentVariable("FFSHOT_DOCS_LANG");
                if (!string.IsNullOrEmpty(lang))
                {
                    Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(lang);
                }
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
            var lang = Environment.GetEnvironmentVariable("FFSHOT_DOCS_LANG");
            var path = Path.Combine(DocsDir(), string.IsNullOrEmpty(lang) ? "settings.png" : $"settings_{lang}.png");
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            form.Close();
        });
    }

    // 通知バルーン (docs/notification.png) はテストプロセスから出すとアプリ名が testhost になるため、
    // 本物の ffshot.exe を起動してホットキーを送り、作業領域の右下を切り出して撮る。
}
