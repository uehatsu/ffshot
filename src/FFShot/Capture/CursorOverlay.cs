using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace FFShot.Capture;

/// <summary>撮影した Bitmap にマウスカーソルを描き込む（どちらのバックエンドもカーソルを含まないため）。</summary>
internal static class CursorOverlay
{
    public static unsafe void Draw(Bitmap bitmap, Rectangle screenBounds)
    {
        var info = new CURSORINFO { cbSize = (uint)sizeof(CURSORINFO) };
        if (!PInvoke.GetCursorInfo(ref info) || (info.flags & CURSORINFO_FLAGS.CURSOR_SHOWING) == 0 || info.hCursor.IsNull)
        {
            return;
        }

        var hotspot = Point.Empty;
        ICONINFO iconInfo;
        if (PInvoke.GetIconInfo(new HICON(info.hCursor.Value), &iconInfo))
        {
            hotspot = new Point((int)iconInfo.xHotspot, (int)iconInfo.yHotspot);
            if (!iconInfo.hbmMask.IsNull) PInvoke.DeleteObject(iconInfo.hbmMask);
            if (!iconInfo.hbmColor.IsNull) PInvoke.DeleteObject(iconInfo.hbmColor);
        }

        var x = info.ptScreenPos.X - hotspot.X - screenBounds.X;
        var y = info.ptScreenPos.Y - hotspot.Y - screenBounds.Y;
        if (x >= screenBounds.Width || y >= screenBounds.Height || x < -64 || y < -64)
        {
            return;
        }

        using var g = Graphics.FromImage(bitmap);
        var hdc = g.GetHdc();
        try
        {
            PInvoke.DrawIconEx(new HDC(hdc), x, y, info.hCursor, 0, 0, 0, HBRUSH.Null, DI_FLAGS.DI_NORMAL);
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }
    }
}
