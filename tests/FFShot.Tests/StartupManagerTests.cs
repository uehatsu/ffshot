using FFShot.App;
using Microsoft.Win32;

namespace FFShot.Tests;

public class StartupManagerTests : IDisposable
{
    private readonly string _key = @"Software\ffshot-tests\" + Guid.NewGuid().ToString("N");
    private readonly string _taskName = "ffshot-test-" + Guid.NewGuid().ToString("N");

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\ffshot-tests", throwOnMissingSubKey: false);
        new StartupTask(false, _taskName, @"C:\Windows\System32\cmd.exe").Delete();
    }

    private StartupManager Make(bool elevated) => new(
        elevated,
        new StartupRegistration(_key, @"C:\Windows\System32\cmd.exe"),
        new StartupTask(highestPrivileges: false, _taskName, @"C:\Windows\System32\cmd.exe"));

    [Fact]
    public void NotElevated_UsesRunKey()
    {
        var m = Make(elevated: false);
        Assert.Null(m.Apply(true));
        Assert.True(new StartupRegistration(_key, @"C:\Windows\System32\cmd.exe").IsRegistered());
        Assert.False(new StartupTask(false, _taskName).Exists());

        Assert.Null(m.Apply(false));
        Assert.False(new StartupRegistration(_key, @"C:\Windows\System32\cmd.exe").IsRegistered());
    }

    [Fact]
    public void Elevated_UsesTaskAndClearsRunKey()
    {
        new StartupRegistration(_key, @"C:\Windows\System32\cmd.exe").Apply(true);
        var m = Make(elevated: true);
        Assert.Null(m.Apply(true));
        Assert.True(new StartupTask(false, _taskName).Exists());
        Assert.False(new StartupRegistration(_key, @"C:\Windows\System32\cmd.exe").IsRegistered());

        Assert.Null(m.Apply(false));
        Assert.False(new StartupTask(false, _taskName).Exists());
    }

    [Fact]
    public void NotElevated_LeavesExistingTaskAlone()
    {
        new StartupTask(false, _taskName, @"C:\Windows\System32\cmd.exe").Create();
        var m = Make(elevated: false);
        Assert.Null(m.Apply(true));
        Assert.False(new StartupRegistration(_key, @"C:\Windows\System32\cmd.exe").IsRegistered());
        Assert.True(new StartupTask(false, _taskName).Exists());
    }
}
