using System.Drawing.Imaging;
using FFShot.Resources;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using MapFlags = Vortice.Direct3D11.MapFlags;
using ResultCode = Vortice.DXGI.ResultCode;

namespace FFShot.Capture;

/// <summary>
/// DXGI Desktop Duplication（Direct3D 11）によるキャプチャ。
/// モニター出力をテクスチャとして取得するため、GDI で黒くなる DirectX 全画面アプリも撮れる。
/// 要求矩形にかかるすべての出力を撮影し、1 枚の Bitmap に合成する。
/// </summary>
internal sealed class DesktopDuplicationBackend : ICaptureBackend
{
    private const uint FrameTimeoutMs = 250;
    private const int MaxAcquireAttempts = 8;

    public string Name => "Direct3D";

    public Bitmap Capture(Rectangle screenBounds)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
        {
            throw new CaptureException(Strings.Capture_EmptyRegion);
        }

        var result = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
        var covered = false;
        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint a = 0; factory.EnumAdapters1(a, out var adapter).Success; a++)
            {
                using (adapter)
                {
                    covered |= CaptureAdapterOutputs(adapter, screenBounds, result);
                }
            }
        }
        catch (SharpGenException ex)
        {
            result.Dispose();
            throw new CaptureException(string.Format(Strings.Capture_D3DFailed, ex.Message), ex);
        }
        catch
        {
            result.Dispose();
            throw;
        }

        if (!covered)
        {
            result.Dispose();
            throw new CaptureException(Strings.Capture_NoOutput);
        }

        return result;
    }

    private static bool CaptureAdapterOutputs(IDXGIAdapter1 adapter, Rectangle screenBounds, Bitmap target)
    {
        var covered = false;
        ID3D11Device? device = null;
        ID3D11DeviceContext? context = null;
        try
        {
            for (uint o = 0; adapter.EnumOutputs(o, out var output).Success; o++)
            {
                using (output)
                {
                    var desc = output.Description;
                    if (!desc.AttachedToDesktop)
                    {
                        continue;
                    }

                    var outputRect = Rectangle.FromLTRB(
                        desc.DesktopCoordinates.Left, desc.DesktopCoordinates.Top,
                        desc.DesktopCoordinates.Right, desc.DesktopCoordinates.Bottom);
                    var overlap = Rectangle.Intersect(outputRect, screenBounds);
                    if (overlap.Width <= 0 || overlap.Height <= 0)
                    {
                        continue;
                    }

                    if (desc.Rotation != ModeRotation.Identity && desc.Rotation != ModeRotation.Unspecified)
                    {
                        throw new CaptureException(Strings.Capture_RotatedUnsupported);
                    }

                    if (device is null)
                    {
                        // 出力を所有するアダプター上にデバイスを作る必要がある
                        D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport,
                            [FeatureLevel.Level_11_1, FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0],
                            out device, out context).CheckError();
                    }

                    using var output1 = output.QueryInterface<IDXGIOutput1>();
                    CaptureOutput(output1, device, context!, outputRect, overlap, screenBounds.Location, target);
                    covered = true;
                }
            }
        }
        finally
        {
            context?.Dispose();
            device?.Dispose();
        }
        return covered;
    }

    private static void CaptureOutput(IDXGIOutput1 output, ID3D11Device device, ID3D11DeviceContext context,
        Rectangle outputRect, Rectangle overlap, Point targetOrigin, Bitmap target)
    {
        using var duplication = CreateDuplication(output, device);
        var format = duplication.Description.ModeDescription.Format;
        if (format != Format.B8G8R8A8_UNorm && format != Format.B8G8R8A8_UNorm_SRgb)
        {
            throw new CaptureException(string.Format(Strings.Capture_FormatUnsupported, format));
        }

        using var frame = AcquireFrame(duplication);
        try
        {
            var srcDesc = frame.Description;
            var stagingDesc = new Texture2DDescription
            {
                Width = srcDesc.Width,
                Height = srcDesc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = srcDesc.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None,
            };
            using var staging = device.CreateTexture2D(stagingDesc);
            context.CopyResource(staging, frame);

            var mapped = context.Map(staging, 0, MapMode.Read, MapFlags.None);
            try
            {
                CopyRows(mapped, outputRect, overlap, targetOrigin, target);
            }
            finally
            {
                context.Unmap(staging, 0);
            }
        }
        finally
        {
            duplication.ReleaseFrame();
        }
    }

    private static IDXGIOutputDuplication CreateDuplication(IDXGIOutput1 output, ID3D11Device device)
    {
        try
        {
            return output.DuplicateOutput(device);
        }
        catch (SharpGenException ex) when (ex.ResultCode == ResultCode.Unsupported)
        {
            throw new CaptureException(Strings.Capture_DDUnsupported, ex);
        }
        catch (SharpGenException ex) when (ex.ResultCode == ResultCode.NotCurrentlyAvailable)
        {
            throw new CaptureException(Strings.Capture_DDNotAvailable, ex);
        }
    }

    private static ID3D11Texture2D AcquireFrame(IDXGIOutputDuplication duplication)
    {
        for (var attempt = 0; attempt < MaxAcquireAttempts; attempt++)
        {
            var hr = duplication.AcquireNextFrame(FrameTimeoutMs, out var info, out var resource);
            if (hr == ResultCode.WaitTimeout)
            {
                continue;
            }
            if (hr == ResultCode.AccessLost)
            {
                throw new CaptureException(Strings.Capture_DDAccessLost);
            }
            hr.CheckError();

            // DuplicateOutput 直後の最初のフレームはデスクトップ内容が未反映（真っ黒）で返ることがある。
            // 実際に present されたフレームが来るまで解放して待ち直す。
            if (info.LastPresentTime == 0 && info.AccumulatedFrames == 0)
            {
                resource.Dispose();
                duplication.ReleaseFrame();
                continue;
            }

            using (resource)
            {
                return resource.QueryInterface<ID3D11Texture2D>();
            }
        }
        throw new CaptureException(Strings.Capture_DDTimeout);
    }

    /// <summary>マップ済みテクスチャの overlap 部分を target（原点 targetOrigin）へ行単位でコピーする。BGRA → Format32bppArgb は同一レイアウト。</summary>
    private static unsafe void CopyRows(MappedSubresource mapped, Rectangle outputRect, Rectangle overlap, Point targetOrigin, Bitmap target)
    {
        var data = target.LockBits(new Rectangle(Point.Empty, target.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var srcBase = (byte*)mapped.DataPointer;
            var dstBase = (byte*)data.Scan0;
            var rowBytes = overlap.Width * 4;
            var srcX = overlap.X - outputRect.X;
            var srcY = overlap.Y - outputRect.Y;
            var dstX = overlap.X - targetOrigin.X;
            var dstY = overlap.Y - targetOrigin.Y;

            for (var y = 0; y < overlap.Height; y++)
            {
                var src = srcBase + (srcY + y) * (long)mapped.RowPitch + srcX * 4L;
                var dst = dstBase + (dstY + y) * (long)data.Stride + dstX * 4L;
                Buffer.MemoryCopy(src, dst, rowBytes, rowBytes);
                // デスクトップ面のアルファは不定なので不透明に揃える
                for (var x = 3; x < rowBytes; x += 4)
                {
                    dst[x] = 0xFF;
                }
            }
        }
        finally
        {
            target.UnlockBits(data);
        }
    }

    public void Dispose() { }
}
