using FFShot.Capture;

namespace FFShot.Tests;

public class ScRgbConverterTests
{
    private static unsafe byte[] Convert(float r, float g, float b, float sdrWhite)
    {
        var src = new[] { (Half)r, (Half)g, (Half)b, (Half)1f };
        var dst = new byte[4];
        fixed (Half* s = src)
        fixed (byte* d = dst)
        {
            ScRgbConverter.ConvertRow(s, d, 1, sdrWhite);
        }
        return dst;
    }

    [Fact]
    public void SdrWhite_BecomesWhite()
    {
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, Convert(2.5f, 2.5f, 2.5f, 2.5f));
    }

    [Fact]
    public void OutputIsBgraWithOpaqueAlpha()
    {
        // R だけ SDR の白 → BGRA の 3 バイト目が 255
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, Convert(1f, 0f, 0f, 1f));
    }

    [Fact]
    public void MidGray_IsSrgbEncoded()
    {
        // リニア 0.214 ≒ sRGB 128
        var px = Convert(0.214f, 0.214f, 0.214f, 1f);
        Assert.InRange(px[0], 127, 129);
    }

    [Fact]
    public void HighlightsAndNegatives_AreClipped()
    {
        Assert.Equal(new byte[] { 0, 255, 255, 255 }, Convert(10f, 4f, -0.5f, 2f));
    }
}
