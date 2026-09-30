using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace FFShot.Capture;

/// <summary>HDR 有効時にディスプレイ設定の「SDR コンテンツの明るさ」を取得する。</summary>
internal static class SdrWhiteLevel
{
    /// <summary>
    /// GDI デバイス名（\\.\DISPLAY1 など）の出力について、SDR の白を scRGB 値（1.0 = 80 nits）で返す。
    /// 取得できなければ null。
    /// </summary>
    public static unsafe float? Get(string gdiDeviceName)
    {
        const QUERY_DISPLAY_CONFIG_FLAGS flags = QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS;
        if (PInvoke.GetDisplayConfigBufferSizes(flags, out var pathCount, out var modeCount) != WIN32_ERROR.ERROR_SUCCESS)
        {
            return null;
        }

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        if (PInvoke.QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes) != WIN32_ERROR.ERROR_SUCCESS)
        {
            return null;
        }

        for (var i = 0; i < pathCount; i++)
        {
            var path = paths[i];
            var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            source.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            source.header.size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
            source.header.adapterId = path.sourceInfo.adapterId;
            source.header.id = path.sourceInfo.id;
            if (PInvoke.DisplayConfigGetDeviceInfo(&source.header) != 0
                || !string.Equals(source.viewGdiDeviceName.ToString(), gdiDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var white = new DISPLAYCONFIG_SDR_WHITE_LEVEL();
            white.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL;
            white.header.size = (uint)sizeof(DISPLAYCONFIG_SDR_WHITE_LEVEL);
            white.header.adapterId = path.targetInfo.adapterId;
            white.header.id = path.targetInfo.id;
            if (PInvoke.DisplayConfigGetDeviceInfo(&white.header) != 0 || white.SDRWhiteLevel == 0)
            {
                return null;
            }
            // SDRWhiteLevel は 1000 が 80 nits（= scRGB の 1.0）
            return white.SDRWhiteLevel / 1000f;
        }
        return null;
    }
}
