namespace ClaudeAccountSwitcher.Models;

public sealed class AccountsFile
{
    public int SchemaVersion { get; set; } = 1;
    public List<Account> Accounts { get; set; } = new();
}
