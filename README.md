# Claude Account Switcher

A lightweight Windows system tray utility for managing multiple [Claude Code](https://claude.com/claude-code) CLI accounts side by side — for example, keeping a personal account and a work/freelance account fully isolated, without manually juggling environment variables or running `/login` and `/logout` every time you switch context.

It sits in the system tray and, on click, opens a small flyout panel listing your configured accounts. Each account gets its own isolated credentials folder (via Claude Code's `CLAUDE_CONFIG_DIR` environment variable), and opening a terminal for an account launches a new Windows Terminal window already scoped to that account.

## Features

- **Tray-resident, no visible main window** — click the icon to open a flyout panel, click elsewhere to dismiss it.
- **Multiple isolated accounts** — each account maps to its own `CLAUDE_CONFIG_DIR`, so credentials never mix.
- **One-click terminal launch** — opens a new Windows Terminal window running PowerShell with the right account already active, then starts `claude`.
- **Add / edit / remove accounts** from the flyout, with a name, color tag, and config folder.
- **Removing an account never deletes its credentials folder** — it only removes the entry from the list.
- **Starts with Windows** — registers itself in the Startup folder on first run.
- **Single instance** — reopening the app just brings the existing tray instance's panel back up instead of spawning a duplicate.
- **Self-contained executable** — no .NET runtime installation required on the target machine.

## Requirements

- Windows 10/11
- [Windows Terminal](https://apps.microsoft.com/detail/9n0dx20hk701) (ships by default on Windows 11)
- [Claude Code CLI](https://claude.com/claude-code) installed and available as `claude` on `PATH`
- .NET 8 SDK (only needed to build from source; the published executable is self-contained)

## Installation

### Build from source

```powershell
git clone https://github.com/<your-username>/claude-account-switcher.git
cd claude-account-switcher
dotnet publish ClaudeAccountSwitcher/ClaudeAccountSwitcher.csproj -c Release
```

The published executable will be at:

```
ClaudeAccountSwitcher/bin/Release/net8.0-windows/win-x64/publish/ClaudeAccountSwitcher.exe
```

It's a self-contained single file — copy it anywhere (e.g. `%LOCALAPPDATA%\ClaudeAccountSwitcher\`) and run it. No installer needed.

## Usage

1. Run `ClaudeAccountSwitcher.exe`. It adds an icon to the system tray (Windows may hide it under the "show hidden icons" chevron on first run).
2. Click the tray icon to open the accounts panel.
3. On first run, two example accounts are seeded automatically: **Pessoal** and **Work**, each pointing at its own folder under `%USERPROFILE%\.claude-accounts\`.
4. Click **Abrir terminal** ("Open terminal") next to an account — a new Windows Terminal window opens with `CLAUDE_CONFIG_DIR` set to that account's folder and `claude` already running. The first time you do this for a given account, Claude Code will prompt you to log in; that session is then persisted in the account's own folder.
5. Use **+ Adicionar conta** to add more accounts, or the edit/remove icons on each row to manage existing ones.

## How it works

Claude Code CLI stores its authentication credentials (`.credentials.json`) inside the directory pointed to by the `CLAUDE_CONFIG_DIR` environment variable (defaulting to `~/.claude` when unset). This app doesn't touch Claude Code internals at all — it simply keeps a small list of named accounts, each mapped to its own directory, and launches a terminal with that variable pre-set before starting `claude`. Every account therefore behaves as a fully independent, isolated login session.

Account metadata (name, color, config folder path) is stored locally in:

```
%APPDATA%\ClaudeAccountSwitcher\accounts.json
```

Logs (for troubleshooting terminal-launch or startup-registration issues) are written to:

```
%APPDATA%\ClaudeAccountSwitcher\log.txt
```

## Auto-start

On first run, the app creates a shortcut in your Startup folder (`shell:startup`) pointing at its own executable path, so it comes back up automatically after every login. This check is idempotent — it only touches the shortcut if it's missing or stale (e.g. after moving the executable).

## Project structure

```
ClaudeAccountSwitcher/
  Program.cs                  # Entry point, single-instance guard, global error handling
  TrayApplicationContext.cs   # NotifyIcon + context menu, wires everything together
  Models/                     # Account / AccountsFile data models
  Services/
    AccountStore.cs           # Atomic JSON persistence with corruption recovery
    TerminalLauncher.cs       # Builds and launches the Windows Terminal / PowerShell command
    StartupManager.cs         # Creates/verifies the Startup folder shortcut
    SingleInstanceGuard.cs    # Named mutex + named pipe activation signal
    Logger.cs
  UI/
    FlyoutForm.cs              # Borderless panel anchored near the cursor
    AddEditAccountForm.cs      # Add/edit account dialog
  Resources/
    app.ico                    # Tray/app icon (multi-resolution)
```

## Contributing

This started as a personal utility, but issues and pull requests are welcome — bug reports, feature suggestions, or improvements to the tray/flyout UX are all fair game.

## License

Licensed under the [MIT License](LICENSE).
