using System.Globalization;
using FFShot.Settings;

namespace FFShot.App;

/// <summary>設定の言語を CurrentUICulture に反映する。Auto は起動時の Windows 表示言語。</summary>
internal static class LanguageSelector
{
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;

    public static void Apply(UiLanguage language)
    {
        var culture = language switch
        {
            UiLanguage.Japanese => CultureInfo.GetCultureInfo("ja-JP"),
            UiLanguage.English => CultureInfo.GetCultureInfo("en-US"),
            _ => SystemUiCulture,
        };
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
