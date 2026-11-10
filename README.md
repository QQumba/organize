# Organizer

Personal tool for capturing todos and tracking regular tasks. A Core (ASP.NET Core API on SQLite) owns the data; the CLI (`org`) and the tray app are thin Clients of it. See `docs/architecture.md` for the design.

## Run the dev environment

You need the .NET 10 SDK, and the runtimes listed in `docs/install.md`.

```powershell
dotnet tool restore       # once: installs the pinned dotnet-ef
powershell ./dev.ps1
```

`dev.ps1` does the following:

1. **Creates throwaway config in `.dev/`** (git-ignored): `core.json`, `cli.json` and `desktop.json`, all with one shared secret. Existing files are kept. This way dev never touches your real config or database in `%APPDATA%\Organizer`.
2. **Starts the Core** with the `Dev` launch profile on port **47822**. The installed Core uses 47821, so the two can run side by side. In Development the Core also serves an API explorer at `http://127.0.0.1:47822/scalar` with the secret prefilled.
3. **Starts the tray app** from source with `.dev/desktop.json`. Its hotkey is **Alt+Shift+Q**, because an installed copy already owns Alt+Q.
4. **Ctrl+C stops both.**

To use the CLI against the dev Core, point it at the dev config. Without `--config` it talks to the installed Core:

```powershell
org --config .dev/cli.json add "try this"
org --config .dev/cli.json ls
```

To start over, delete the `.dev/` folder.

## Build, test, install

```powershell
dotnet build Organizer.sln
dotnet test Organizer.sln
powershell ./publish.ps1   # builds dist/
```

Installing a built copy (and uninstalling it) is in `docs/install.md`.
