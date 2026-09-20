using FFShot.Capture;

namespace FFShot.Tests;

[Trait("Category", "Screen")]
public class GdiCaptureBackendTests
{
    [Fact]
    public void Capture_ReturnsBitmapOfRequestedSize()
    {
        using var backend = new GdiCaptureBackend();
        var bounds = new Rectangle(Screen.PrimaryScreen!.Bounds.Location, new Size(16, 8));
        using var bmp = backend.Capture(bounds);
        Assert.Equal(16, bmp.Width);
        Assert.Equal(8, bmp.Height);
    }

    [Fact]
    public void Capture_ThrowsOnEmptyBounds()
    {
        using var backend = new GdiCaptureBackend();
        Assert.Throws<CaptureException>(() => backend.Capture(Rectangle.Empty));
    }

    [Fact]
    public void ResolveRegion_FullScreen_IsNonEmpty()
    {
        var (bounds, label) = WindowInfo.ResolveRegion(CaptureTarget.FullScreen, allMonitors: false);
        Assert.Equal("full", label);
        Assert.True(bounds.Width > 0 && bounds.Height > 0);
    }

    [Fact]
    public void ResolveRegion_AllMonitors_CoversVirtualScreen()
    {
        var (bounds, _) = WindowInfo.ResolveRegion(CaptureTarget.FullScreen, allMonitors: true);
        Assert.Equal(SystemInformation.VirtualScreen, bounds);
    }
}
