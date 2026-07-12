using ClaudeAccountSwitcher.Services;
using ClaudeAccountSwitcher.UI;

namespace ClaudeAccountSwitcher;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly AccountStore _store;
    private readonly TerminalLauncher _launcher;
    private readonly FlyoutForm _flyout;
    private readonly SingleInstanceGuard _instanceGuard;

    public TrayApplicationContext(SingleInstanceGuard instanceGuard)
    {
        _instanceGuard = instanceGuard;
        _store = new AccountStore();
        _launcher = new TerminalLauncher();
        _flyout = new FlyoutForm(_store, _launcher);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Sobre", null, (_, _) => ShowAbout());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Sair", null, (_, _) => ExitApp());

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "Claude Account Switcher",
            Visible = true,
            ContextMenuStrip = contextMenu,
        };
        _notifyIcon.MouseUp += OnTrayIconMouseUp;

        StartupManager.EnsureRegistered();

        _instanceGuard.StartListening(() =>
        {
            // Chamado de uma thread de background; volta pra UI thread.
            if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
            {
                _notifyIcon.ContextMenuStrip.Invoke(ShowFlyoutAtDefaultLocation);
            }
            else
            {
                ShowFlyoutAtDefaultLocation();
            }
        });
    }

    private void OnTrayIconMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_flyout.Visible)
        {
            _flyout.Hide();
            return;
        }

        if ((DateTime.UtcNow - _flyout.LastHiddenAtUtc) < TimeSpan.FromMilliseconds(200))
        {
            // Este clique foi o mesmo que causou o Deactivate/Hide do flyout — não reabrir.
            return;
        }

        _flyout.ShowAtCursor(Cursor.Position);
    }

    private void ShowFlyoutAtDefaultLocation()
    {
        _flyout.ShowAtCursor(Cursor.Position);
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "Claude Account Switcher\nGerencia múltiplas contas do Claude Code CLI via CLAUDE_CONFIG_DIR.",
            "Sobre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _flyout.Dispose();
        _instanceGuard.Dispose();
        ExitThread();
    }
}
