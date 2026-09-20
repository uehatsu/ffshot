using System.Drawing.Imaging;

namespace FFShot.Output;

internal static class PngWriter
{
    /// <summary>一時ファイルに書いてから rename し、途中で落ちても壊れた PNG を残さない。</summary>
    public static void Save(Bitmap bitmap, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tmp = path + ".part";
        try
        {
            bitmap.Save(tmp, ImageFormat.Png);
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }
    }
}
