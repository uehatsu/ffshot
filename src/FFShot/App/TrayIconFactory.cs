using System.Drawing.Drawing2D;

namespace FFShot.App;

/// <summary>バイナリ資産を持たずに、実行時にトレイアイコンを描画する。</summary>
internal static class TrayIconFactory
{
    public static Icon Create(int size = 32)
    {
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // カメラ本体
            using var body = new SolidBrush(Color.FromArgb(40, 44, 52));
            var bodyRect = new Rectangle(2, size / 4, size - 4, size / 2 + size / 8);
            g.FillRectangle(body, bodyRect);
            // ファインダーの出っ張り
            g.FillRectangle(body, new Rectangle(size / 3, size / 6, size / 3, size / 6));
            // レンズ
            using var lens = new SolidBrush(Color.FromArgb(80, 200, 255));
            var r = size / 3;
            g.FillEllipse(lens, (size - r) / 2, bodyRect.Top + (bodyRect.Height - r) / 2, r, r);
        }

        var hIcon = bmp.GetHicon();
        try
        {
            // Icon.FromHandle は所有権を持たないため複製して自前管理する
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            Windows.Win32.PInvoke.DestroyIcon(new Windows.Win32.UI.WindowsAndMessaging.HICON(hIcon));
        }
    }
}
