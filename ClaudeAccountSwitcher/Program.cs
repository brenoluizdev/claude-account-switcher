using ClaudeAccountSwitcher.Services;

namespace ClaudeAccountSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) => HandleUnhandled(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            HandleUnhandled(e.ExceptionObject as Exception ?? new Exception("Erro desconhecido."));

        var instanceGuard = new SingleInstanceGuard();

        if (!instanceGuard.TryAcquire())
        {
            instanceGuard.NotifyExistingInstance();
            instanceGuard.Dispose();
            return;
        }

        try
        {
            Application.Run(new TrayApplicationContext(instanceGuard));
        }
        finally
        {
            instanceGuard.Dispose();
        }
    }

    private static void HandleUnhandled(Exception ex)
    {
        Logger.Log($"Exceção não tratada: {ex}");
        MessageBox.Show(
            $"Ocorreu um erro inesperado:\n{ex.Message}\n\nDetalhes em %APPDATA%\\ClaudeAccountSwitcher\\log.txt",
            "Claude Account Switcher",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
