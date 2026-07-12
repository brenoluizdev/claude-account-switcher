using System.Text.Json;
using ClaudeAccountSwitcher.Models;

namespace ClaudeAccountSwitcher.Services;

public sealed class AccountStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _appDataDir;
    private readonly string _filePath;

    public AccountStore()
    {
        _appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeAccountSwitcher");
        _filePath = Path.Combine(_appDataDir, "accounts.json");
    }

    public AccountsFile Load()
    {
        Directory.CreateDirectory(_appDataDir);

        if (!File.Exists(_filePath))
        {
            var seeded = CreateSeedFile();
            Save(seeded);
            return seeded;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var parsed = JsonSerializer.Deserialize<AccountsFile>(json, JsonOptions);
            if (parsed is null)
            {
                throw new InvalidDataException("accounts.json deserializou como null.");
            }
            return parsed;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException)
        {
            Logger.Log($"accounts.json corrompido, fazendo backup e recriando: {ex.Message}");
            var backupPath = Path.Combine(_appDataDir, $"accounts.json.bak.{DateTime.UtcNow:yyyyMMddHHmmss}");
            try
            {
                File.Copy(_filePath, backupPath, overwrite: true);
            }
            catch (Exception copyEx)
            {
                Logger.Log($"Falha ao criar backup de accounts.json: {copyEx.Message}");
            }

            var fresh = new AccountsFile();
            Save(fresh);
            return fresh;
        }
    }

    public void Save(AccountsFile file)
    {
        Directory.CreateDirectory(_appDataDir);
        var json = JsonSerializer.Serialize(file, JsonOptions);
        var tmpPath = _filePath + ".tmp";
        File.WriteAllText(tmpPath, json);

        if (File.Exists(_filePath))
        {
            File.Replace(tmpPath, _filePath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tmpPath, _filePath);
        }
    }

    private static AccountsFile CreateSeedFile()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new AccountsFile
        {
            Accounts = new List<Account>
            {
                new()
                {
                    Name = "Pessoal",
                    ColorHex = "#4C8BF5",
                    ConfigDir = Path.Combine(userProfile, ".claude-accounts", "pessoal"),
                },
                new()
                {
                    Name = "Work",
                    ColorHex = "#34A853",
                    ConfigDir = Path.Combine(userProfile, ".claude-accounts", "work"),
                },
            },
        };
    }
}
