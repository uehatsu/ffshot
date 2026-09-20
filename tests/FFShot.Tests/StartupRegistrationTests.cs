using FFShot.App;
using Microsoft.Win32;

namespace FFShot.Tests;

public class StartupRegistrationTests : IDisposable
{
    private readonly string _key = @"Software\ffshot-tests\" + Guid.NewGuid().ToString("N");

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(@"Software\ffshot-tests", throwOnMissingSubKey: false);

    [Fact]
    public void Apply_RegistersAndUnregisters()
    {
        var reg = new StartupRegistration(_key, @"C:\apps\ffshot.exe");
        Assert.False(reg.IsRegistered());

        reg.Apply(true);
        Assert.True(reg.IsRegistered());
        using (var k = Registry.CurrentUser.OpenSubKey(_key))
        {
            Assert.Equal("\"C:\\apps\\ffshot.exe\"", k!.GetValue(StartupRegistration.ValueName));
        }

        reg.Apply(false);
        Assert.False(reg.IsRegistered());
    }

    [Fact]
    public void IsRegistered_IsFalseWhenPathDiffers()
    {
        new StartupRegistration(_key, @"C:\old\ffshot.exe").Apply(true);
        var moved = new StartupRegistration(_key, @"C:\new\ffshot.exe");
        Assert.False(moved.IsRegistered());
        moved.Apply(true);
        Assert.True(moved.IsRegistered());
    }
}
