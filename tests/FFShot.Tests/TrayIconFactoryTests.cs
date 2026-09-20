using FFShot.App;

namespace FFShot.Tests;

public class TrayIconFactoryTests
{
    [Fact]
    public void EmbeddedIcon_IsLoadable()
    {
        using var stream = typeof(TrayIconFactory).Assembly.GetManifestResourceStream("ffshot.ico");
        Assert.NotNull(stream);
        using var icon = new Icon(stream, 16, 16);
        Assert.Equal(16, icon.Width);
    }

    [Fact]
    public void CreateTrayIcon_ReturnsIcon()
    {
        using var icon = TrayIconFactory.CreateTrayIcon();
        Assert.True(icon.Width >= 16);
        using var win = TrayIconFactory.CreateWindowIcon();
        Assert.Equal(48, win.Width);
    }
}
