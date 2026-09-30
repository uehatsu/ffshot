namespace FFShot.Capture;

/// <summary>
/// HDR デスクトップ（R16G16B16A16_FLOAT, scRGB リニア）を 8bit sRGB の BGRA に変換する。
/// SDR の白（sdrWhite）を 1.0 として正規化し、それを超える明るさはクリップする。
/// </summary>
internal static class ScRgbConverter
{
    private const int LutSize = 16384;
    private static readonly byte[] EncodeLut = BuildLut();

    /// <summary>src（RGBA half × pixels）を dst（BGRA byte × pixels、アルファは 0xFF）へ変換する。</summary>
    public static unsafe void ConvertRow(Half* src, byte* dst, int pixels, float sdrWhite)
    {
        var scale = (LutSize - 1) / sdrWhite;
        for (var i = 0; i < pixels; i++)
        {
            var s = src + i * 4;
            var d = dst + i * 4;
            d[0] = Encode((float)s[2] * scale);
            d[1] = Encode((float)s[1] * scale);
            d[2] = Encode((float)s[0] * scale);
            d[3] = 0xFF;
        }
    }

    /// <summary>index は 0..LutSize-1 に正規化済みのリニア値。NaN・負値は黒、範囲超過は白。</summary>
    private static byte Encode(float index)
    {
        if (!(index > 0))
        {
            return 0;
        }
        return index >= LutSize - 1 ? (byte)255 : EncodeLut[(int)(index + 0.5f)];
    }

    private static byte[] BuildLut()
    {
        var lut = new byte[LutSize];
        for (var i = 0; i < LutSize; i++)
        {
            var linear = i / (double)(LutSize - 1);
            var srgb = linear <= 0.0031308 ? linear * 12.92 : 1.055 * Math.Pow(linear, 1 / 2.4) - 0.055;
            lut[i] = (byte)Math.Round(srgb * 255);
        }
        return lut;
    }
}
