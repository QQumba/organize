# Install, upgrade, uninstall, dev run

Windows 10/11, per user, no admin. Plain folders plus scripts; there is no installer package.

## Prerequisites

- .NET 10 SDK (to build).
- Running the result: the **ASP.NET Core 10 runtime** (Core) and the **.NET 10 Desktop Runtime** (tray app).
- Building the CLI (Native AOT): the Visual Studio "Desktop development with C++" workload. The linker runs `vswhere.exe`, which the Visual Studio installer does not put on `PATH`; `publish.ps1` adds its folder for its own run, so nothing to do unless you publish the CLI by hand.

## Build

```powershell
dotnet tool restore   # once
powershell ./publish.ps1
```

Produces `dist/core/`, `dist/cli/` and `dist/desktop/` (git-ignored).

## Install

From a normal (non-elevated) PowerShell at the repo root. If the execution policy blocks the script:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

This affects only that process and needs no admin. The script refuses to run elevated, because the per-user settings would go to the wrong account.

It:

- copies `dist/*` to `%LOCALAPPDATA%\Organizer\core`, `\cli`, `\desktop`;
- registers the Task Scheduler task `Organizer Core` (at logon, current user, no time limit, restart on failure) and starts it;
- adds the `Organizer` value to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for the tray app, and starts it;
- adds `%LOCALAPPDATA%\Organizer\cli` to the user `PATH` (open a new terminal to use `org`);
- creates `core.json`, `cli.json` and `desktop.json` in `%APPDATA%\Organizer` if missing, with one shared secret. Existing files are never overwritten. A Client config whose secret differs from `core.json` gets a warning and is left alone.

## Upgrade

Build again and re-run `install.ps1`. It stops the running Core and tray app, replaces the files and starts them again. Config and data are untouched.

## Uninstall

```powershell
powershell ./uninstall.ps1             # keeps config and data
powershell ./uninstall.ps1 -RemoveData # also deletes %APPDATA%\Organizer
```

Removes the task, the `Run` value, the `PATH` entry and `%LOCALAPPDATA%\Organizer`. Works when parts are already gone.

## Where things live

| What | Where |
|---|---|
| Programs | `%LOCALAPPDATA%\Organizer\{core,cli,desktop}` |
| Configs | `%APPDATA%\Organizer\core.json`, `cli.json`, `desktop.json` |
| Database | `%APPDATA%\Organizer\organizer.db` (default) |

## The Core does not start

There is no log file yet. Run it in a console to see the error:

```powershell
cd $env:LOCALAPPDATA\Organizer\core
dotnet .\Organizer.Core.dll
```

(Run it from that folder: the working directory is where `appsettings.json` is found.) Common causes: the ASP.NET Core 10 runtime is missing, the port is taken, or `core.json` has no `Secret`. The tray app shows "Core unreachable" until the Core is up.

## Dev run

```powershell
powershell ./dev.ps1
```

Starts the Core (port **47822**) and the tray app (hotkey **Alt+Shift+Q**) from source, with throwaway config and data in `.dev/`. It does not touch the installed copy or `%APPDATA%\Organizer`, and works whether or not the installed copy is running. Ctrl+C stops both. Use the dev Core from the CLI with:

```powershell
org --config .dev/cli.json ls
```

## Manual checklist

No UI automation, so check by hand:

1. Fresh install: `install.ps1` finishes without errors and prints what it did.
2. Sign out and in: the Core (`Organizer Core` task) and the tray app are running.
3. Alt+Q opens the Capture popup and Enter captures.
4. In a new terminal, `org add test` and `org ls` work.
5. Re-run `install.ps1` while the Core is running: it upgrades without errors and the Core is running afterwards.
6. Kill `Organizer.Core.exe`: the task restarts it within about a minute.
7. `uninstall.ps1`: no `Organizer Core` task, no `Organizer` Run value, no `PATH` entry, no `%LOCALAPPDATA%\Organizer`; `%APPDATA%\Organizer` is kept.
8. `uninstall.ps1 -RemoveData` also removes `%APPDATA%\Organizer`.
9. With the installed copy running, `dev.ps1` starts a second Core and tray app; Alt+Q and Alt+Shift+Q each open their own popup; `org --config .dev/cli.json ls` shows only dev data.
