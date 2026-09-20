using System.Globalization;
using System.Resources;

namespace FFShot.Resources;

/// <summary>
/// UI 文字列。ニュートラル（英語）は Strings.resx、日本語は Strings.ja.resx。
/// 表示言語は <see cref="CultureInfo.CurrentUICulture"/>（App.LanguageSelector が設定）で決まる。
/// このクラスは Strings.resx のキーから生成している。キーを足したら両方の resx とここに追加すること。
/// </summary>
internal static class Strings
{
    public static ResourceManager ResourceManager { get; } = new("FFShot.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>null なら CurrentUICulture。テストで固定したいときに使う。</summary>
    public static CultureInfo? Culture { get; set; }

    private static string Get(string key) => ResourceManager.GetString(key, Culture) ?? key;

    public static string App_AlreadyRunning => Get(nameof(App_AlreadyRunning));
    public static string App_ErrorTitle => Get(nameof(App_ErrorTitle));
    public static string Tray_TextElevated => Get(nameof(Tray_TextElevated));
    public static string Tray_TextNormal => Get(nameof(Tray_TextNormal));
    public static string Menu_CaptureFullScreen => Get(nameof(Menu_CaptureFullScreen));
    public static string Menu_CaptureActiveWindow => Get(nameof(Menu_CaptureActiveWindow));
    public static string Menu_Settings => Get(nameof(Menu_Settings));
    public static string Menu_OpenSaveFolder => Get(nameof(Menu_OpenSaveFolder));
    public static string Menu_RestartElevated => Get(nameof(Menu_RestartElevated));
    public static string Menu_RestartElevated_Tip => Get(nameof(Menu_RestartElevated_Tip));
    public static string Menu_Exit => Get(nameof(Menu_Exit));
    public static string Notify_FallbackTitle => Get(nameof(Notify_FallbackTitle));
    public static string Notify_SavedTitle => Get(nameof(Notify_SavedTitle));
    public static string Notify_SavedBody => Get(nameof(Notify_SavedBody));
    public static string Notify_CaptureFailedTitle => Get(nameof(Notify_CaptureFailedTitle));
    public static string Notify_HotkeyFailedTitle => Get(nameof(Notify_HotkeyFailedTitle));
    public static string Notify_HotkeyFailedHint => Get(nameof(Notify_HotkeyFailedHint));
    public static string Target_FullScreen => Get(nameof(Target_FullScreen));
    public static string Target_ActiveWindow => Get(nameof(Target_ActiveWindow));
    public static string Notify_StartupTitle => Get(nameof(Notify_StartupTitle));
    public static string Notify_StartupFailedTitle => Get(nameof(Notify_StartupFailedTitle));
    public static string Settings_Title => Get(nameof(Settings_Title));
    public static string Settings_HotkeyFullScreen => Get(nameof(Settings_HotkeyFullScreen));
    public static string Settings_HotkeyActiveWindow => Get(nameof(Settings_HotkeyActiveWindow));
    public static string Settings_Backend => Get(nameof(Settings_Backend));
    public static string Settings_Backend_Gdi => Get(nameof(Settings_Backend_Gdi));
    public static string Settings_Backend_Direct3D => Get(nameof(Settings_Backend_Direct3D));
    public static string Settings_SaveFolder => Get(nameof(Settings_SaveFolder));
    public static string Settings_Browse => Get(nameof(Settings_Browse));
    public static string Settings_FileNamePattern => Get(nameof(Settings_FileNamePattern));
    public static string Settings_FileNameHint => Get(nameof(Settings_FileNameHint));
    public static string Settings_AllMonitors => Get(nameof(Settings_AllMonitors));
    public static string Settings_IncludeCursor => Get(nameof(Settings_IncludeCursor));
    public static string Settings_ShowNotification => Get(nameof(Settings_ShowNotification));
    public static string Settings_RunAtStartup => Get(nameof(Settings_RunAtStartup));
    public static string Settings_StartupHintElevated => Get(nameof(Settings_StartupHintElevated));
    public static string Settings_StartupHintNormal => Get(nameof(Settings_StartupHintNormal));
    public static string Settings_Language => Get(nameof(Settings_Language));
    public static string Settings_Language_Auto => Get(nameof(Settings_Language_Auto));
    public static string Settings_Language_Japanese => Get(nameof(Settings_Language_Japanese));
    public static string Settings_Language_English => Get(nameof(Settings_Language_English));
    public static string Settings_LanguageHint => Get(nameof(Settings_LanguageHint));
    public static string Settings_OK => Get(nameof(Settings_OK));
    public static string Settings_Cancel => Get(nameof(Settings_Cancel));
    public static string Settings_FolderDialogTitle => Get(nameof(Settings_FolderDialogTitle));
    public static string Hotkey_Placeholder => Get(nameof(Hotkey_Placeholder));
    public static string Capture_FallbackWarning => Get(nameof(Capture_FallbackWarning));
    public static string Capture_EmptyRegion => Get(nameof(Capture_EmptyRegion));
    public static string Capture_GdiFailed => Get(nameof(Capture_GdiFailed));
    public static string Capture_D3DFailed => Get(nameof(Capture_D3DFailed));
    public static string Capture_NoOutput => Get(nameof(Capture_NoOutput));
    public static string Capture_RotatedUnsupported => Get(nameof(Capture_RotatedUnsupported));
    public static string Capture_FormatUnsupported => Get(nameof(Capture_FormatUnsupported));
    public static string Capture_DDUnsupported => Get(nameof(Capture_DDUnsupported));
    public static string Capture_DDNotAvailable => Get(nameof(Capture_DDNotAvailable));
    public static string Capture_DDAccessLost => Get(nameof(Capture_DDAccessLost));
    public static string Capture_DDTimeout => Get(nameof(Capture_DDTimeout));
    public static string Startup_TaskDescription => Get(nameof(Startup_TaskDescription));
    public static string Startup_TaskCreateFailed => Get(nameof(Startup_TaskCreateFailed));
    public static string Startup_TaskDeleteFailed => Get(nameof(Startup_TaskDeleteFailed));
    public static string Startup_SchtasksMissing => Get(nameof(Startup_SchtasksMissing));
    public static string Startup_CannotDeleteElevatedTask => Get(nameof(Startup_CannotDeleteElevatedTask));
}
