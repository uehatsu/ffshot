using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;
using FFShot.Resources;

namespace FFShot.App;

/// <summary>
/// タスクスケジューラのログオントリガーで起動する。管理者権限（HighestAvailable）で
/// UAC ダイアログなしに常駐させるにはこちらが必要（Run キーは昇格アプリを起動しない）。
/// XML で登録することで「72 時間で停止」などの既定値を無効化している。
/// </summary>
internal sealed class StartupTask
{
    public const string DefaultTaskName = "ffshot";

    private readonly string _taskName;
    private readonly string _exePath;
    private readonly bool _highest;

    public StartupTask(bool highestPrivileges, string? taskName = null, string? exePath = null)
    {
        _highest = highestPrivileges;
        _taskName = taskName ?? DefaultTaskName;
        _exePath = exePath ?? Environment.ProcessPath ?? Application.ExecutablePath;
    }

    public string TaskName => _taskName;

    public bool Exists()
    {
        var (code, _) = RunSchtasks("/Query", "/TN", _taskName);
        return code == 0;
    }

    /// <exception cref="InvalidOperationException">schtasks が失敗した場合（権限不足など）。</exception>
    public void Create()
    {
        var xmlPath = Path.Combine(Path.GetTempPath(), $"ffshot-task-{Guid.NewGuid():N}.xml");
        try
        {
            // schtasks は UTF-16 (BOM 付き) の XML を要求する
            File.WriteAllText(xmlPath, BuildXml(), Encoding.Unicode);
            var (code, output) = RunSchtasks("/Create", "/TN", _taskName, "/XML", xmlPath, "/F");
            if (code != 0)
            {
                throw new InvalidOperationException(string.Format(Strings.Startup_TaskCreateFailed, code, output.Trim()));
            }
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    public void Delete()
    {
        var (code, output) = RunSchtasks("/Delete", "/TN", _taskName, "/F");
        if (code != 0 && Exists())
        {
            throw new InvalidOperationException(string.Format(Strings.Startup_TaskDeleteFailed, code, output.Trim()));
        }
    }

    internal string BuildXml()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = SecurityElement.Escape(identity.Name);
        var exe = SecurityElement.Escape(_exePath);
        var dir = SecurityElement.Escape(Path.GetDirectoryName(_exePath) ?? string.Empty);
        var runLevel = _highest ? "HighestAvailable" : "LeastPrivilege";
        var description = SecurityElement.Escape(Strings.Startup_TaskDescription);

        return $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Description>{description}</Description>
              </RegistrationInfo>
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <UserId>{user}</UserId>
                </LogonTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{user}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>{runLevel}</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <AllowHardTerminate>false</AllowHardTerminate>
                <StartWhenAvailable>false</StartWhenAvailable>
                <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                <IdleSettings>
                  <StopOnIdleEnd>false</StopOnIdleEnd>
                  <RestartOnIdle>false</RestartOnIdle>
                </IdleSettings>
                <AllowStartOnDemand>true</AllowStartOnDemand>
                <Enabled>true</Enabled>
                <Hidden>false</Hidden>
                <RunOnlyIfIdle>false</RunOnlyIfIdle>
                <WakeToRun>false</WakeToRun>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <Priority>7</Priority>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{exe}</Command>
                  <WorkingDirectory>{dir}</WorkingDirectory>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    private static (int ExitCode, string Output) RunSchtasks(params string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException(Strings.Startup_SchtasksMissing);
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout + stderr);
    }
}
