namespace FFShot.Capture;

/// <summary>スクリーン座標（物理ピクセル）の矩形を Bitmap として取得する。</summary>
internal interface ICaptureBackend : IDisposable
{
    string Name { get; }

    /// <exception cref="CaptureException">取得に失敗した場合。呼び出し側でフォールバックする。</exception>
    Bitmap Capture(Rectangle screenBounds);
}

public sealed class CaptureException : Exception
{
    public CaptureException(string message, Exception? inner = null) : base(message, inner) { }
}
