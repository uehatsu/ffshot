using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace FFShot.Capture;

/// <summary>前面ウィンドウとモニターの矩形を求める。</summary>
internal static class WindowInfo
{
    public static HWND ForegroundWindow => PInvoke.GetForegroundWindow();

    /// <summary>DWM の実フレーム矩形（影を含まない）。取れなければ GetWindowRect。</summary>
    public static unsafe Rectangle? GetWindowBounds(HWND hwnd)
    {
        if (hwnd.IsNull || !PInvoke.IsWindowVisible(hwnd) || PInvoke.IsIconic(hwnd))
        {
            return null;
        }

        RECT rect;
        var hr = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, &rect, (uint)sizeof(RECT));
        if (hr.Failed || rect.right <= rect.left || rect.bottom <= rect.top)
        {
            if (!PInvoke.GetWindowRect(hwnd, out rect))
            {
                return null;
            }
        }

        var r = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
        return r.Width > 0 && r.Height > 0 ? r : null;
    }

    public static bool IsOwnedByCurrentProcess(HWND hwnd)
    {
        if (hwnd.IsNull)
        {
            return false;
        }
        uint pid;
        unsafe
        {
            PInvoke.GetWindowThreadProcessId(hwnd, &pid);
        }
        return pid == (uint)Environment.ProcessId;
    }

    /// <summary>前面ウィンドウが乗っているモニター。無ければカーソル位置のモニター。</summary>
    public static Rectangle GetActiveMonitorBounds()
    {
        var hwnd = ForegroundWindow;
        if (!hwnd.IsNull && !IsOwnedByCurrentProcess(hwnd))
        {
            return Screen.FromHandle(hwnd).Bounds;
        }
        return Screen.FromPoint(Cursor.Position).Bounds;
    }

    public static Rectangle VirtualScreenBounds => SystemInformation.VirtualScreen;

    /// <summary>撮影対象の矩形を決める。ActiveWindow が取れない場合はモニターにフォールバックする。</summary>
    public static (Rectangle Bounds, string Label) ResolveRegion(CaptureTarget target, bool allMonitors)
    {
        if (target == CaptureTarget.ActiveWindow)
        {
            var hwnd = ForegroundWindow;
            if (!IsOwnedByCurrentProcess(hwnd) && GetWindowBounds(hwnd) is { } wb)
            {
                // 画面外にはみ出た部分は切り落とす
                var clipped = Rectangle.Intersect(wb, VirtualScreenBounds);
                if (clipped.Width > 0 && clipped.Height > 0)
                {
                    return (clipped, "window");
                }
            }
        }

        return allMonitors ? (VirtualScreenBounds, "full") : (GetActiveMonitorBounds(), "full");
    }
}
