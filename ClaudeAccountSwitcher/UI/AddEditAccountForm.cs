using ClaudeAccountSwitcher.Models;

namespace ClaudeAccountSwitcher.UI;

public sealed class AddEditAccountForm : Form
{
    private static readonly string[] PaletteHex =
    {
        "#4C8BF5", "#34A853", "#EA4335", "#FBBC05", "#9C27B0", "#00ACC1",
    };

    private readonly Account? _existing;
    private readonly TextBox _nameBox;
    private readonly TextBox _folderBox;
    private readonly Button _browseButton;
    private readonly FlowLayoutPanel _colorPanel;
    private string _selectedColorHex;

    public Account? Result { get; private set; }

    public AddEditAccountForm(Account? existing)
    {
        _existing = existing;
        _selectedColorHex = existing?.ColorHex ?? PaletteHex[0];

        Text = existing is null ? "Adicionar conta" : "Editar conta";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, existing is null ? 230 : 150);
        Font = new Font("Segoe UI", 9f);

        var nameLabel = new Label { Text = "Nome", Location = new Point(16, 16), AutoSize = true };
        _nameBox = new TextBox
        {
            Location = new Point(16, 36),
            Width = 328,
            Text = existing?.Name ?? string.Empty,
        };

        var colorLabel = new Label { Text = "Cor", Location = new Point(16, 68), AutoSize = true };
        _colorPanel = BuildColorPanel();
        _colorPanel.Location = new Point(16, 88);

        var folderLabel = new Label { Text = "Pasta de config (CLAUDE_CONFIG_DIR)", Location = new Point(16, 124), AutoSize = true };
        _folderBox = new TextBox
        {
            Location = new Point(16, 144),
            Width = 250,
            ReadOnly = existing is not null,
            Text = existing?.ConfigDir ?? SuggestDefaultFolder(),
        };
        _browseButton = new Button
        {
            Text = "...",
            Location = new Point(272, 143),
            Width = 32,
            Enabled = existing is null,
        };
        _browseButton.Click += (_, _) => BrowseFolder();

        var okButton = new Button
        {
            Text = "Salvar",
            DialogResult = DialogResult.OK,
            Location = new Point(168, existing is null ? 190 : 110),
            Width = 80,
        };
        okButton.Click += (_, _) => OnSave();

        var cancelButton = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new Point(256, existing is null ? 190 : 110),
            Width = 88,
        };

        Controls.Add(nameLabel);
        Controls.Add(_nameBox);
        Controls.Add(colorLabel);
        Controls.Add(_colorPanel);

        if (existing is null)
        {
            Controls.Add(folderLabel);
            Controls.Add(_folderBox);
            Controls.Add(_browseButton);
        }

        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private FlowLayoutPanel BuildColorPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Size = new Size(328, 28),
            WrapContents = false,
        };

        foreach (var hex in PaletteHex)
        {
            var swatch = new Button
            {
                Size = new Size(24, 24),
                BackColor = ColorTranslator.FromHtml(hex),
                FlatStyle = FlatStyle.Flat,
                Tag = hex,
                Margin = new Padding(2),
            };
            swatch.FlatAppearance.BorderSize = hex == _selectedColorHex ? 3 : 1;
            swatch.FlatAppearance.BorderColor = Color.White;
            swatch.Click += (_, _) =>
            {
                _selectedColorHex = hex;
                foreach (Button b in panel.Controls)
                {
                    b.FlatAppearance.BorderSize = (string?)b.Tag == hex ? 3 : 1;
                }
            };
            panel.Controls.Add(swatch);
        }

        return panel;
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecione (ou crie) a pasta de config isolada desta conta",
            SelectedPath = _folderBox.Text,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private void OnSave()
    {
        var name = _nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Informe um nome para a conta.", "Claude Account Switcher",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var configDir = _folderBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(configDir))
        {
            MessageBox.Show(this, "Informe uma pasta de config.", "Claude Account Switcher",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Result = new Account
        {
            Id = _existing?.Id ?? Guid.NewGuid().ToString("N"),
            Name = name,
            ColorHex = _selectedColorHex,
            ConfigDir = configDir,
            CreatedUtc = _existing?.CreatedUtc ?? DateTime.UtcNow,
            LastUsedUtc = _existing?.LastUsedUtc,
        };
    }

    private static string SuggestDefaultFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".claude-accounts", "nova-conta-" + DateTime.Now.Ticks % 10000);
    }
}
