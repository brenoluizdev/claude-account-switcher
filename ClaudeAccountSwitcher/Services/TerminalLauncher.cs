using System.ComponentModel;
using System.Diagnostics;
using ClaudeAccountSwitcher.Models;

namespace ClaudeAccountSwitcher.Services;

public sealed class TerminalLauncher
{
    public void OpenAccountTerminal(Account account)
    {
        Directory.CreateDirectory(account.ConfigDir);

        // Usa quebra de linha (não ';') como separador de statements: o wt.exe faz seu próprio
        // parsing de linha de comando e trata ';' fora de aspas como delimitador entre comandos,
        // o que quebrava a chamada (tentava rodar "claude" como um novo comando do wt).
        var psCommand = $"$env:CLAUDE_CONFIG_DIR='{EscapeSingleQuoted(account.ConfigDir)}'\nclaude";

        if (TryStart(BuildWindowsTerminalStartInfo(account.Name, psCommand)))
        {
            return;
        }

        Logger.Log("wt.exe indisponível, tentando fallback para powershell.exe direto.");

        if (TryStart(BuildPlainPowerShellStartInfo(psCommand)))
        {
            return;
        }

        throw new InvalidOperationException(
            "Não foi possível abrir um terminal (wt.exe e powershell.exe falharam). Veja o log para detalhes.");
    }

    private static ProcessStartInfo BuildWindowsTerminalStartInfo(string tabTitle, string psCommand)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = true,
        };

        psi.ArgumentList.Add("-w");
        psi.ArgumentList.Add("-1");
        psi.ArgumentList.Add("new-tab");
        psi.ArgumentList.Add("--title");
        psi.ArgumentList.Add(tabTitle);
        psi.ArgumentList.Add("powershell.exe");
        psi.ArgumentList.Add("-NoExit");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(psCommand);

        return psi;
    }

    private static ProcessStartInfo BuildPlainPowerShellStartInfo(string psCommand)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = true,
        };

        psi.ArgumentList.Add("-NoExit");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(psCommand);

        return psi;
    }

    private static bool TryStart(ProcessStartInfo psi)
    {
        try
        {
            using var process = Process.Start(psi);
            return process is not null;
        }
        catch (Win32Exception ex)
        {
            Logger.Log($"Falha ao iniciar '{psi.FileName}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log($"Erro inesperado ao iniciar '{psi.FileName}': {ex.Message}");
            return false;
        }
    }

    private static string EscapeSingleQuoted(string value) => value.Replace("'", "''");
}
