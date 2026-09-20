using FFShot.Capture;

namespace FFShot.Tests;

[Trait("Category", "Screen")]
public class CursorOverlayTests
{
    [Fact]
    public void Draw_DoesNotThrow_AndKeepsSize()
    {
        using var bmp = new Bitmap(100, 100);
        var bounds = new Rectangle(Cursor.Position.X - 50, Cursor.Position.Y - 50, 100, 100);
        CursorOverlay.Draw(bmp, bounds);
        Assert.Equal(100, bmp.Width);
    }

    [Fact]
    public void Draw_IsNoOp_WhenCursorOutside()
    {
        using var bmp = new Bitmap(10, 10);
        var far = new Rectangle(Cursor.Position.X + 5000, Cursor.Position.Y + 5000, 10, 10);
        CursorOverlay.Draw(bmp, far);
        Assert.Equal(0, bmp.GetPixel(5, 5).A);
    }
}
