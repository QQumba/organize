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

## Run the web UI

The web UI lives in `web/` (SvelteKit, Node project, not in the solution). You need **Node 24 or later** (`web/.nvmrc`).

Easiest, together with the Core and tray app:

```powershell
powershell ./dev.ps1 -Web
```

It checks the Node version, runs `npm install` if `web/node_modules` is missing, and starts the Vite dev server next to the Core. Vite prints its URL (default `http://localhost:5173`; another port if that one is taken). Ctrl+C stops everything. Until browser login exists, the placeholder page shows "Core: up, login required".

Or by hand, with a dev Core already running (`powershell ./dev.ps1`, or `dotnet run --project core/Organizer.Core --launch-profile Dev`):

```powershell
cd web
npm install     # once
npm run dev     # proxies /api to the dev Core on 127.0.0.1:47822
```

Other commands, from `web/`:

```powershell
npm run check     # svelte-check, prettier, eslint
npm test          # vitest
npm run build     # static files in web/build
npm run preview   # serve the build locally (check the browser console for CSP errors)
npm run gen:api   # regenerate src/lib/api/schema.d.ts from docs/openapi.json (dev, build and check do it for you)
```

The dev Core's secret is in `.dev/core.json`, not in `%APPDATA%`.

## Build, test, install

```powershell
dotnet build Organizer.sln
dotnet test Organizer.sln
powershell ./publish.ps1   # builds dist/
```

Installing a built copy (and uninstalling it) is in `docs/install.md`.
