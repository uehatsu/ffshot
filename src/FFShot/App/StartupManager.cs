using FFShot.Resources;

namespace FFShot.App;

/// <summary>
/// 自動起動の登録先を権限に応じて切り替える。
/// 管理者で動いていればタスクスケジューラ（最上位の特権）、通常権限なら Run キー。
/// </summary>
internal sealed class StartupManager
{
    private readonly StartupRegistration _runKey;
    private readonly StartupTask _task;
    private readonly bool _elevated;

    public StartupManager(bool elevated, StartupRegistration? runKey = null, StartupTask? task = null)
    {
        _elevated = elevated;
        _runKey = runKey ?? new StartupRegistration();
        _task = task ?? new StartupTask(highestPrivileges: true);
    }

    /// <summary>適用結果の説明（ユーザー通知用）。問題がなければ null。</summary>
    public string? Apply(bool enabled)
    {
        if (!enabled)
        {
            _runKey.Apply(false);
            if (_task.Exists())
            {
                try
                {
                    _task.Delete();
                }
                catch (InvalidOperationException ex)
                {
                    return string.Format(Strings.Startup_CannotDeleteElevatedTask, ex.Message);
                }
            }
            return null;
        }

        if (_elevated)
        {
            _task.Create();
            _runKey.Apply(false); // 二重起動を防ぐ
            return null;
        }

        if (_task.Exists())
        {
            // 既に管理者権限のタスクがあるならそちらに任せる
            _runKey.Apply(false);
            return null;
        }

        _runKey.Apply(true);
        return null;
    }
}
