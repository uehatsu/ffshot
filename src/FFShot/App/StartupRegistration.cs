using Microsoft.Win32;

namespace FFShot.App;

/// <summary>HKCU\...\Run への登録でログオン時に自動起動する。</summary>
internal sealed class StartupRegistration
{
    public const string DefaultRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "ffshot";

    private readonly string _runKey;
    private readonly string _exePath;

    public StartupRegistration(string? runKey = null, string? exePath = null)
    {
        _runKey = runKey ?? DefaultRunKey;
        _exePath = exePath ?? Environment.ProcessPath ?? Application.ExecutablePath;
    }

    public string Command => $"\"{_exePath}\"";

    public bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_runKey, writable: false);
        return key?.GetValue(ValueName) is string s && string.Equals(s, Command, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>設定に合わせて登録／解除する。exe の場所が変わっていれば書き直す。</summary>
    public void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_runKey, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        else if (key.GetValue(ValueName) is not null)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
