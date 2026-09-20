namespace FFShot.Settings;

public enum CaptureBackendKind
{
    Gdi,
    Direct3D,
}

public sealed class AppSettings
{
    public string HotkeyFullScreen { get; set; } = "Ctrl+Shift+F12";
    public string HotkeyActiveWindow { get; set; } = "Ctrl+Shift+F11";
    public CaptureBackendKind Backend { get; set; } = CaptureBackendKind.Gdi;
    public string SaveFolder { get; set; } = @"%USERPROFILE%\Pictures\ffshot";
    public string FileNamePattern { get; set; } = "ffshot_{yyyyMMdd_HHmmss}";
    public bool IncludeCursor { get; set; }
    public bool ShowNotification { get; set; } = true;
    public bool RunAtStartup { get; set; }
    /// <summary>全画面撮影で全モニターを結合するか（false ならアクティブウィンドウのあるモニターのみ）。</summary>
    public bool CaptureAllMonitors { get; set; }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public string ResolvedSaveFolder => Environment.ExpandEnvironmentVariables(SaveFolder);
}
