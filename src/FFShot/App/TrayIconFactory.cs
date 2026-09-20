using System.Drawing.Drawing2D;
using System.Reflection;

namespace FFShot.App;

/// <summary>埋め込みの ffshot.ico からアイコンを取り出す。読めなければ実行時描画にフォールバックする。</summary>
internal static class TrayIconFactory
{
    /// <summary>トレイ用（小アイコンサイズ。DPI に応じて 16〜32px）。</summary>
    public static Icon CreateTrayIcon() => LoadEmbedded(SystemInformation.SmallIconSize) ?? DrawFallback(32);

    /// <summary>ウィンドウ用（タイトルバー・タスクバー）。</summary>
    public static Icon CreateWindowIcon() => LoadEmbedded(new Size(48, 48)) ?? DrawFallback(48);

    private static Icon? LoadEmbedded(Size size)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ffshot.ico");
            return stream is null ? null : new Icon(stream, size);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        {
            return null;
        }
    }

    private static Icon DrawFallback(int size)
    {
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var body = new SolidBrush(Color.FromArgb(40, 44, 52));
            var bodyRect = new Rectangle(2, size / 4, size - 4, size / 2 + size / 8);
            g.FillRectangle(body, bodyRect);
            g.FillRectangle(body, new Rectangle(size / 3, size / 6, size / 3, size / 6));
            using var lens = new SolidBrush(Color.FromArgb(80, 200, 255));
            var r = size / 3;
            g.FillEllipse(lens, (size - r) / 2, bodyRect.Top + (bodyRect.Height - r) / 2, r, r);
        }

        var hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            Windows.Win32.PInvoke.DestroyIcon(new Windows.Win32.UI.WindowsAndMessaging.HICON(hIcon));
        }
    }
}
