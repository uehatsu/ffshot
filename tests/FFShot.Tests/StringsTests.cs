using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using FFShot.Resources;

namespace FFShot.Tests;

public class StringsTests
{
    private static Dictionary<string, string> Load(CultureInfo culture)
    {
        var set = Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"resource set for {culture} not found");
        return set.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
    }

    [Fact]
    public void EnglishAndJapanese_HaveTheSameKeys()
    {
        var en = Load(CultureInfo.InvariantCulture);
        var ja = Load(CultureInfo.GetCultureInfo("ja"));
        Assert.NotEmpty(en);
        Assert.Empty(en.Keys.Except(ja.Keys));
        Assert.Empty(ja.Keys.Except(en.Keys));
    }

    [Fact]
    public void FormatPlaceholders_MatchBetweenLanguages()
    {
        var en = Load(CultureInfo.InvariantCulture);
        var ja = Load(CultureInfo.GetCultureInfo("ja"));
        foreach (var (key, value) in en)
        {
            var a = Regex.Matches(value, @"\{\d\}").Select(m => m.Value).OrderBy(x => x);
            var b = Regex.Matches(ja[key], @"\{\d\}").Select(m => m.Value).OrderBy(x => x);
            Assert.True(a.SequenceEqual(b), $"placeholder mismatch in {key}");
        }
    }

    [Fact]
    public void TypedAccessors_CoverEveryKey()
    {
        var keys = Load(CultureInfo.InvariantCulture).Keys;
        var props = typeof(Strings).GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => p.Name).ToHashSet();
        Assert.Empty(keys.Except(props));
    }

    [Fact]
    public void Culture_SelectsLanguage()
    {
        try
        {
            Strings.Culture = CultureInfo.GetCultureInfo("ja-JP");
            Assert.Equal("終了(&X)", Strings.Menu_Exit);
            Strings.Culture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("E&xit", Strings.Menu_Exit);
            Strings.Culture = CultureInfo.GetCultureInfo("de-DE"); // 未対応言語は英語にフォールバック
            Assert.Equal("E&xit", Strings.Menu_Exit);
        }
        finally
        {
            Strings.Culture = null;
        }
    }
}
