using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace FFShot.App;

internal static class Elevation
{
    public static bool IsElevated { get; } = ComputeIsElevated();

    private static bool ComputeIsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>現在の exe を UAC 昇格付きで起動する。ユーザーが UAC をキャンセルしたら false。</summary>
    public static bool StartElevatedCopy()
    {
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;
        try
        {
            Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exe) ?? string.Empty,
            });
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
        {
            return false;
        }
    }
}
