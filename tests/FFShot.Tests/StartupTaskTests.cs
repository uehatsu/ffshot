using FFShot.App;

namespace FFShot.Tests;

public class StartupTaskTests
{
    [Fact]
    public void BuildXml_ContainsCommandAndRunLevel()
    {
        var xml = new StartupTask(highestPrivileges: true, "ffshot-x", @"C:\apps\ff shot\ffshot.exe").BuildXml();
        Assert.Contains("<Command>C:\\apps\\ff shot\\ffshot.exe</Command>", xml);
        Assert.Contains("<WorkingDirectory>C:\\apps\\ff shot</WorkingDirectory>", xml);
        Assert.Contains("<RunLevel>HighestAvailable</RunLevel>", xml);
        Assert.Contains("<ExecutionTimeLimit>PT0S</ExecutionTimeLimit>", xml);
        Assert.Contains("<LogonType>InteractiveToken</LogonType>", xml);
    }

    [Fact]
    public void BuildXml_EscapesXml()
    {
        var xml = new StartupTask(highestPrivileges: false, "ffshot-x", @"C:\a&b\ffshot.exe").BuildXml();
        Assert.Contains("C:\\a&amp;b\\ffshot.exe", xml);
        Assert.Contains("<RunLevel>LeastPrivilege</RunLevel>", xml);
    }

    [Fact]
    public void Create_Exists_Delete_RoundTrip()
    {
        // 通常権限でも登録できる LeastPrivilege で実際にタスクスケジューラに登録する
        var name = "ffshot-test-" + Guid.NewGuid().ToString("N");
        var task = new StartupTask(highestPrivileges: false, name, @"C:\Windows\System32\cmd.exe");
        try
        {
            Assert.False(task.Exists());
            task.Create();
            Assert.True(task.Exists());
        }
        finally
        {
            task.Delete();
        }
        Assert.False(task.Exists());
    }
}
