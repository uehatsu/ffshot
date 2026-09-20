using FFShot.Output;

namespace FFShot.Tests;

public class FileNamerTests
{
    private static readonly DateTime Now = new(2026, 9, 20, 13, 4, 5);

    [Fact]
    public void Expand_ReplacesDateTokens()
    {
        Assert.Equal("ffshot_20260920_130405", FileNamer.Expand("ffshot_{yyyyMMdd_HHmmss}", Now, "full"));
    }

    [Fact]
    public void Expand_ReplacesTargetToken()
    {
        Assert.Equal("window-2026", FileNamer.Expand("{target}-{yyyy}", Now, "window"));
    }

    [Fact]
    public void Expand_SanitizesInvalidCharacters()
    {
        var name = FileNamer.Expand("a:b/c?{HH:mm}", Now, "full");
        Assert.DoesNotContain(':', name);
        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain('?', name);
        Assert.Equal("a_b_c_13_04", name);
    }

    [Fact]
    public void Expand_FallsBackWhenEmpty()
    {
        Assert.Equal("ffshot_20260920_130405", FileNamer.Expand("   ", Now, "full"));
    }

    [Fact]
    public void UniquePath_AppendsCounter()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine("C:\\x", "shot.png"),
            Path.Combine("C:\\x", "shot_1.png"),
        };
        var path = FileNamer.UniquePath("C:\\x", "shot", ".png", taken.Contains);
        Assert.Equal(Path.Combine("C:\\x", "shot_2.png"), path);
    }

    [Fact]
    public void UniquePath_ReturnsBaseWhenFree()
    {
        var path = FileNamer.UniquePath("C:\\x", "shot", ".png", _ => false);
        Assert.Equal(Path.Combine("C:\\x", "shot.png"), path);
    }
}
