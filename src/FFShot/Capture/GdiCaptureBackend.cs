using System.Drawing.Imaging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FFShot.Resources;

namespace FFShot.Capture;

/// <summary>Graphics.CopyFromScreen（BitBlt）による通常キャプチャ。</summary>
internal sealed class GdiCaptureBackend : ICaptureBackend
{
    public string Name => "GDI";

    public Bitmap Capture(Rectangle screenBounds)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
        {
            throw new CaptureException(Strings.Capture_EmptyRegion);
        }

        var bmp = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size, CopyPixelOperation.SourceCopy);
            return bmp;
        }
        catch (Exception ex) when (ex is Win32Exception or ExternalException)
        {
            bmp.Dispose();
            throw new CaptureException(string.Format(Strings.Capture_GdiFailed, ex.Message), ex);
        }
    }

    public void Dispose() { }
}
