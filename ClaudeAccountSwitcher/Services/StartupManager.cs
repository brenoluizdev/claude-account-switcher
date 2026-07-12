using System.Runtime.InteropServices;

namespace ClaudeAccountSwitcher.Services;

public static class StartupManager
{
    private const string ShortcutName = "ClaudeAccountSwitcher.lnk";

    public static void EnsureRegistered()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                Logger.Log("Environment.ProcessPath vazio, não é possível registrar auto-start.");
                return;
            }

            var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupDir, ShortcutName);

            if (File.Exists(shortcutPath) && TargetMatches(shortcutPath, exePath))
            {
                return;
            }

            CreateShortcut(shortcutPath, exePath);
            Logger.Log($"Atalho de auto-start criado/atualizado em {shortcutPath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Falha ao registrar auto-start: {ex.Message}");
        }
    }

    private static bool TargetMatches(string shortcutPath, string expectedTarget)
    {
        try
        {
            dynamic? shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            dynamic shortcut = shell!.CreateShortcut(shortcutPath);
            string targetPath = shortcut.TargetPath;
            Marshal.FinalReleaseComObject(shell);
            return string.Equals(targetPath, expectedTarget, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void CreateShortcut(string shortcutPath, string exePath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell não disponível.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Description = "Claude Account Switcher";
            shortcut.Save();
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
