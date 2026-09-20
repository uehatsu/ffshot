using FFShot.Capture;
using FFShot.Output;
using FFShot.Settings;

namespace FFShot.App;

internal sealed record CaptureResult(string Path, Rectangle Bounds, string BackendName, string? Warning);

/// <summary>撮影範囲の決定 → バックエンド実行（フォールバック付き） → PNG 保存 をまとめる。</summary>
internal sealed class CaptureService : IDisposable
{
    private readonly GdiCaptureBackend _gdi = new();
    private ICaptureBackend? _direct3D;

    public CaptureResult Capture(CaptureTarget target, AppSettings settings)
    {
        var (bounds, label) = WindowInfo.ResolveRegion(target, settings.CaptureAllMonitors);

        string? warning = null;
        Bitmap bitmap;
        string backendName;
        try
        {
            var backend = SelectBackend(settings.Backend);
            bitmap = backend.Capture(bounds);
            backendName = backend.Name;
        }
        catch (CaptureException ex) when (settings.Backend != CaptureBackendKind.Gdi)
        {
            warning = $"{settings.Backend} での取得に失敗したため GDI で撮影しました。\n{ex.Message}";
            bitmap = _gdi.Capture(bounds);
            backendName = _gdi.Name;
        }

        using (bitmap)
        {
            if (settings.IncludeCursor)
            {
                CursorOverlay.Draw(bitmap, bounds);
            }

            var folder = settings.ResolvedSaveFolder;
            var name = FileNamer.Expand(settings.FileNamePattern, DateTime.Now, label);
            var path = FileNamer.UniquePath(folder, name, ".png");
            PngWriter.Save(bitmap, path);
            return new CaptureResult(path, bounds, backendName, warning);
        }
    }

    private ICaptureBackend SelectBackend(CaptureBackendKind kind)
    {
        if (kind == CaptureBackendKind.Direct3D)
        {
            _direct3D ??= CreateDirect3DBackend();
            return _direct3D;
        }
        return _gdi;
    }

    private static ICaptureBackend CreateDirect3DBackend() => new DesktopDuplicationBackend();

    public void Dispose()
    {
        _direct3D?.Dispose();
        _gdi.Dispose();
    }
}
