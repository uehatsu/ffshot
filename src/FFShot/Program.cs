using FFShot.App;

namespace FFShot;

internal static class Program
{
    private const string MutexName = @"Local\ffshot-single-instance-7C1B2E1A";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
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
}
