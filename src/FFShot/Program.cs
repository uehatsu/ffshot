using FFShot.App;

namespace FFShot;

internal static class Program
{
    private const string MutexName = @"Local\ffshot-single-instance-7C1B2E1A";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew && !WaitForPreviousInstance(mutex))
        {
            MessageBox.Show("ffshot は既に起動しています。", "ffshot",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "ffshot - エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

        using var context = new TrayApplicationContext();
        Application.Run(context);
    }

    /// <summary>「管理者として再起動」の直後は前のインスタンスが終了中なので、少しだけ待つ。</summary>
    private static bool WaitForPreviousInstance(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.FromSeconds(5));
        }
        catch (AbandonedMutexException)
        {
            return true; // 前のプロセスが終了して所有権が移った
        }
    }
}
