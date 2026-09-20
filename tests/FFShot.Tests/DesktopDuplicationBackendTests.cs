using FFShot.Capture;

namespace FFShot.Tests;

[Trait("Category", "Screen")]
public class DesktopDuplicationBackendTests
{
    [Fact]
    public void Capture_ReturnsBitmapOfRequestedSize()
    {
        using var backend = new DesktopDuplicationBackend();
        var bounds = new Rectangle(Screen.PrimaryScreen!.Bounds.Location + new Size(10, 10), new Size(64, 32));
        using var bmp = backend.Capture(bounds);
        Assert.Equal(64, bmp.Width);
        Assert.Equal(32, bmp.Height);
    }

    [Fact]
    public void Capture_MatchesGdiForStaticRegion()
    {
        // 画面の一角（タスクバーやウィンドウが動かない限り同じはず）を両方式で撮って比較する
        var bounds = new Rectangle(Screen.PrimaryScreen!.Bounds.Location, new Size(48, 48));
        using var gdi = new GdiCaptureBackend();
        using var d3d = new DesktopDuplicationBackend();
        using var a = gdi.Capture(bounds);
        using var b = d3d.Capture(bounds);

        var matches = 0;
        var total = bounds.Width * bounds.Height;
        for (var y = 0; y < bounds.Height; y++)
        {
            for (var x = 0; x < bounds.Width; x++)
            {
                var pa = a.GetPixel(x, y);
                var pb = b.GetPixel(x, y);
                if (Math.Abs(pa.R - pb.R) <= 2 && Math.Abs(pa.G - pb.G) <= 2 && Math.Abs(pa.B - pb.B) <= 2)
                {
                    matches++;
                }
            }
        }
        // 動的な内容（時計など）で少数のピクセルが違っても許容する
        Assert.True(matches >= total * 0.9, $"一致ピクセル {matches}/{total}");
    }

    [Fact]
    public void Capture_ThrowsForRegionOutsideAnyOutput()
    {
        using var backend = new DesktopDuplicationBackend();
        var vs = SystemInformation.VirtualScreen;
        var outside = new Rectangle(vs.Right + 10000, vs.Bottom + 10000, 10, 10);
        Assert.Throws<CaptureException>(() => backend.Capture(outside));
    }
}
