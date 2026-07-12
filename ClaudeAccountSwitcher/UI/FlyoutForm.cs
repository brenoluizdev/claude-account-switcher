using System.Drawing.Drawing2D;
using ClaudeAccountSwitcher.Models;
using ClaudeAccountSwitcher.Services;

namespace ClaudeAccountSwitcher.UI;

public sealed class FlyoutForm : Form
{
    private const int WsExToolWindow = 0x80;
    private const int PanelWidth = 300;
    private const int RowHeight = 44;
    private const int FooterHeight = 44;

    private readonly AccountStore _store;
    private readonly TerminalLauncher _launcher;
    private readonly FlowLayoutPanel _accountsPanel;
    private readonly Panel _footer;

    private bool _suppressDeactivate;

    public DateTime LastHiddenAtUtc { get; private set; } = DateTime.MinValue;

    public FlyoutForm(AccountStore store, TerminalLauncher launcher)
    {
        _store = store;
        _launcher = launcher;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(32, 32, 36);
        Padding = new Padding(1);
        Width = PanelWidth;
        MinimumSize = new Size(PanelWidth, 80);

        var title = new Label
        {
            Text = "Claude Account Switcher",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Color.FromArgb(24, 24, 28),
        };

        _accountsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            Padding = new Padding(4, 4, 4, 4),
        };

        _footer = BuildFooter();

        Controls.Add(_accountsPanel);
        Controls.Add(title);
        Controls.Add(_footer);

        _footer.Dock = DockStyle.Top;
        title.BringToFront();
        _accountsPanel.BringToFront();
        _footer.BringToFront();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExToolWindow;
            return cp;
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (_suppressDeactivate)
        {
            return;
        }

        LastHiddenAtUtc = DateTime.UtcNow;
        Hide();
    }

    public void ShowAtCursor(Point cursorPosition)
    {
        RefreshAccounts();

        var workingArea = Screen.FromPoint(cursorPosition).WorkingArea;
        var x = Math.Min(cursorPosition.X, workingArea.Right - Width - 8);
        var y = Math.Min(cursorPosition.Y, workingArea.Bottom - Height - 8);

        // Ancora acima/à esquerda do cursor se a taskbar estiver embaixo (caso comum).
        y = Math.Max(workingArea.Top + 8, workingArea.Bottom - Height - 8);
        x = Math.Max(workingArea.Left + 8, x);

        Location = new Point(x, y);
        Show();
        Activate();
    }

    private void RefreshAccounts()
    {
        var file = _store.Load();
        _accountsPanel.SuspendLayout();
        _accountsPanel.Controls.Clear();

        foreach (var account in file.Accounts)
        {
            _accountsPanel.Controls.Add(BuildAccountRow(account));
        }

        _accountsPanel.ResumeLayout();
        Height = 34 + _accountsPanel.PreferredSize.Height + FooterHeight + 2;
    }

    private Control BuildAccountRow(Account account)
    {
        var row = new Panel
        {
            Width = PanelWidth - 12,
            Height = RowHeight,
            BackColor = Color.FromArgb(40, 40, 46),
            Margin = new Padding(0, 0, 0, 4),
        };

        var colorDot = new Panel
        {
            Size = new Size(10, 10),
            Location = new Point(10, (RowHeight - 10) / 2),
            BackColor = ColorTranslator.FromHtml(SafeColor(account.ColorHex)),
        };
        MakeRounded(colorDot);

        var nameLabel = new Label
        {
            Text = account.Name,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = false,
            Location = new Point(28, 0),
            Size = new Size(90, RowHeight),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var openButton = new Button
        {
            Text = "Abrir terminal",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 68),
            Size = new Size(100, 26),
            Location = new Point(row.Width - 168, (RowHeight - 26) / 2),
        };
        openButton.FlatAppearance.BorderSize = 0;
        openButton.Click += (_, _) => OnOpenTerminal(account);

        var editButton = new Button
        {
            Text = "✎",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 68),
            Size = new Size(28, 26),
            Location = new Point(row.Width - 62, (RowHeight - 26) / 2),
        };
        editButton.FlatAppearance.BorderSize = 0;
        editButton.Click += (_, _) => OnEditAccount(account);

        var removeButton = new Button
        {
            Text = "✕",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 68),
            Size = new Size(28, 26),
            Location = new Point(row.Width - 30, (RowHeight - 26) / 2),
        };
        removeButton.FlatAppearance.BorderSize = 0;
        removeButton.Click += (_, _) => OnRemoveAccount(account);

        row.Controls.Add(colorDot);
        row.Controls.Add(nameLabel);
        row.Controls.Add(openButton);
        row.Controls.Add(editButton);
        row.Controls.Add(removeButton);

        return row;
    }

    private Panel BuildFooter()
    {
        var footer = new Panel
        {
            Height = FooterHeight,
            BackColor = Color.FromArgb(24, 24, 28),
        };

        var addButton = new Button
        {
            Text = "+ Adicionar conta",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(24, 24, 28),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
        };
        addButton.FlatAppearance.BorderSize = 0;
        addButton.Click += (_, _) => OnAddAccount();

        footer.Controls.Add(addButton);
        return footer;
    }

    private void OnOpenTerminal(Account account)
    {
        try
        {
            _launcher.OpenAccountTerminal(account);

            var file = _store.Load();
            var stored = file.Accounts.FirstOrDefault(a => a.Id == account.Id);
            if (stored is not null)
            {
                stored.LastUsedUtc = DateTime.UtcNow;
                _store.Save(file);
            }

            Hide();
            LastHiddenAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Logger.Log($"Falha ao abrir terminal para '{account.Name}': {ex.Message}");
            ShowChildDialogSafe(() => MessageBox.Show(
                this, $"Não foi possível abrir o terminal:\n{ex.Message}", "Claude Account Switcher",
                MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    private void OnAddAccount()
    {
        ShowChildDialogSafe(() =>
        {
            using var form = new AddEditAccountForm(existing: null);
            if (form.ShowDialog(this) != DialogResult.OK || form.Result is null)
            {
                return;
            }

            var file = _store.Load();
            file.Accounts.Add(form.Result);
            _store.Save(file);
        });

        RefreshAccounts();
    }

    private void OnEditAccount(Account account)
    {
        ShowChildDialogSafe(() =>
        {
            using var form = new AddEditAccountForm(existing: account);
            if (form.ShowDialog(this) != DialogResult.OK || form.Result is null)
            {
                return;
            }

            var file = _store.Load();
            var stored = file.Accounts.FirstOrDefault(a => a.Id == account.Id);
            if (stored is null)
            {
                return;
            }

            stored.Name = form.Result.Name;
            stored.ColorHex = form.Result.ColorHex;
            _store.Save(file);
        });

        RefreshAccounts();
    }

    private void OnRemoveAccount(Account account)
    {
        ShowChildDialogSafe(() =>
        {
            var confirm = MessageBox.Show(
                this,
                $"Remover a conta '{account.Name}' da lista?\n\nA pasta de credenciais NÃO será apagada do disco.",
                "Claude Account Switcher",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var file = _store.Load();
            file.Accounts.RemoveAll(a => a.Id == account.Id);
            _store.Save(file);
        });

        RefreshAccounts();
    }

    private void ShowChildDialogSafe(Action action)
    {
        _suppressDeactivate = true;
        try
        {
            action();
        }
        finally
        {
            _suppressDeactivate = false;
        }
    }

    private static string SafeColor(string colorHex)
    {
        try
        {
            ColorTranslator.FromHtml(colorHex);
            return colorHex;
        }
        catch
        {
            return "#4C8BF5";
        }
    }

    private static void MakeRounded(Panel panel)
    {
        var path = new GraphicsPath();
        path.AddEllipse(0, 0, panel.Width, panel.Height);
        panel.Region = new Region(path);
    }
}
