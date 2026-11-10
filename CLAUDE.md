# Organizer

Personal tool for capturing todos and tracking regular tasks, work and personal, in one place. Priorities: minimal-friction capture, ease of use, and speed with low memory use in every part (especially UI).

Vocabulary lives in `GLOSSARY.md`; use those terms exactly. Big decisions are recorded in `docs/adr/`; propose an ADR when a decision is hard to reverse, surprising and a real trade-off. ADR format: `000N-slug.md`, title states the decision, one short paragraph (decision, why, rejected alternatives); copy the existing ones.

Read when relevant, not every session:
- `docs/architecture.md`: before changing project structure, dependencies, hosting or deployment.
- `docs/adr/0001`, `0002`: before changing Core hosting, the API boundary, storage or the stack.
- `docs/linear-workflow.md`: only if your task involves Linear tickets. Otherwise ignore Linear completely.
- `README.md`: for human developers only. Do not read it unless asked to.

## Scope

**v1:** Core, CLI, tray desktop app. Items (text, notes, Category, Tags, optional due date, Important/Urgent flags), states open/done/archived (no deletion), Inbox plus Done and Archive views, text search, backup and restore, one-line Capture from every Client.

**Deferred:** web UI, Eisenhower matrix (UI only; fields exist from v1), Recurrence (design TBD), Activities, Reminders/notifications, extra views, phone, sync, multi-user, offline mode. Do not build these unless asked.

## Architecture

Core (ASP.NET Core minimal API, EF Core on SQLite) owns all data and rules; Clients (CLI, tray app, later web UI) are thin and share a C# client library. Read `docs/architecture.md` before changing project structure, dependencies, hosting or deployment.

## Rules

- No business logic or storage in Clients; they call the Core API.
- No offline mode: if the Core is unreachable, show a clear error.
- Test mainly through the Core's HTTP API (integration tests with a temporary SQLite database); light tests for clients; no WPF UI automation.
- Keep it fast and small: avoid heavy dependencies.
- User data (Item text, notes, Category and Tag names) must work in English and Ukrainian (Cyrillic): search, matching, uniqueness and sorting must be Unicode-aware, not ASCII-only. The UI stays English only; no localization.

## Definition of done

Acceptance criteria met, tests pass, and any hard-to-reverse decision recorded in an ADR. Keep this file's commands and layout current when you change them.

## Branches

`develop` is the working branch: commit and push directly to it. The user merges into `main` by hand to get a stable version. Never commit to `main`. Pull requests may replace this later.

## Layout

- `Organizer.sln`: the one solution, at the repo root. `Directory.Build.props` (shared settings) and `Directory.Packages.props` (central package versions) sit next to it; each project sets its own target framework.
- `core/Organizer.Core/`: the Core (ASP.NET Core minimal API, EF Core migrations in `Migrations/`). It also logs to `logs/core-<yyyyMMddTHHmmss>.log` files beside `core.json` (`FileLog.cs`; 1 MB each, newest 7 kept), including failed starts.
- `contracts/Organizer.Contracts/`: request and response types shared by the Core and the client library, no dependencies. Holds `CaptureRequest` and `ItemDto`; other DTOs still live in the Core until a Client needs them.
- `client/Organizer.Client/`: `OrganizerClient` (capture, inbox, done), `ClientConfig` loader, `OrganizerException`. AOT-compatible, source-generated JSON.
- `cli/Organizer.Cli/`: the `org` exe (Native AOT). `CliApp` holds parsing, output and exit codes (0 ok, 1 error, 2 usage). Config: `%APPDATA%/Organizer/cli.json` (`{ "Address", "Secret" }`, not auto-created), `--config <path>` overrides.
- `tests/Organizer.Core.Tests/`: xUnit integration tests, including the test that the Core references only Contracts.
- `tests/Organizer.Client.Tests/`, `tests/Organizer.Cli.Tests/`: light tests against a Core started by `CoreHarness`.
- `tests/Organizer.TestSupport/`: `CoreHarness` starts the Core in-process on a temp config and SQLite file.
- `docs/openapi.json`: the checked-in OpenAPI description of the Core API (the copy the future web UI generator reads); kept current by a Core test. Any API change must regenerate it.
- `nuget.config`: nuget.org only (the user-level config points at a missing local source).
- `dotnet-tools.json`: pins `dotnet-ef`.
- `publish.ps1`: builds the Core (framework-dependent, `WinExe` via `-p:CoreWinExe=true`, win-x64; needs the ASP.NET Core 10 runtime) into `dist/core/`, the CLI (Native AOT, win-x64) into `dist/cli/` and the tray app (framework-dependent, ReadyToRun, win-x64; needs the .NET 10 Desktop Runtime) into `dist/desktop/`; `dist/` is git-ignored. The CLI needs the Visual Studio C++ workload. It grows with each deliverable.
- `desktop/Organizer.Desktop/`: the tray app (WPF, `net10.0-windows`, WinForms `NotifyIcon`). `PopupWindow` (Capture popup with the Inbox list) is a thin view over `CapturePopupModel`; `TrayApp` holds the tray icon and the global hotkey (`Hotkey`). Config: `%APPDATA%/Organizer/desktop.json` (`{ "Address", "Secret", "Hotkey" }`, `Hotkey` optional, default `Alt+Q`; not auto-created), `--config <path>` overrides.
- `tests/Organizer.Desktop.Tests/`: light tests of the Desktop's plain classes (hotkey parsing, popup state); no WPF UI automation.
- `install.ps1`, `uninstall.ps1`, `dev.ps1`: per-user install (Task Scheduler logon task for the Core, `Run` key for the tray app, CLI on the user PATH), uninstall, and a throwaway dev run. `scripts/config.ps1` is the config-file function they share. Details in `docs/install.md`.
- Planned, not yet created: `web/`. The target layout is in `docs/architecture.md`.

## Commands

Run from the repo root, after `dotnet tool restore` once.

- Build: `dotnet build Organizer.sln`
- Test: `dotnet test Organizer.sln`
- CLI: `powershell ./publish.ps1`, then `dist/cli/org.exe add <text>`, `ls [--category <name>] [--tag <name>]...`, `done <id>`.
- Install: `powershell ./publish.ps1`, then `powershell -ExecutionPolicy Bypass -File ./install.ps1` from a non-elevated PowerShell (re-run to upgrade). `./uninstall.ps1 [-RemoveData]` removes it; data in `%APPDATA%/Organizer` is kept unless `-RemoveData`. See `docs/install.md`.
- Dev run: `powershell ./dev.ps1` starts a throwaway Core (port 47822) and tray app (hotkey Alt+Shift+Q) from source with data in `.dev/`, next to an installed copy. Use the dev Core from the CLI with `org --config .dev/cli.json <command>`.
- Tray app: after `publish.ps1`, run `dist/desktop/Organizer.Desktop.exe [--config <path>]`. Alt+Q (or the `Hotkey` config key) opens the Capture popup; Enter captures, Esc dismisses. Startup errors show as the tray tooltip and one balloon.
- Run the Core: `dotnet run --project core/Organizer.Core [-- --config <path>]`. Settings layer: `core/Organizer.Core/appsettings.json` (defaults, ships with the app) < `core.json` (user overrides; default `%APPDATA%/Organizer/core.json`, created on first start containing only a random secret) < environment variables < command line. Auth: `Authorization: Bearer <secret>`. Health: `GET /health`.
- Run the Core for development: `dotnet run --project core/Organizer.Core --launch-profile Dev` (or the `Dev` profile in the IDE). Uses a throwaway config and database in the git-ignored `.dev/` folder at the repo root, not your real `%APPDATA%` one. The profile deliberately sets no `applicationUrl`, so address and port still come from the config. In Development only, the Core also serves the Scalar UI at `/scalar`, and both it and `/openapi/v1.json` skip token auth (the dev secret is prefilled, so "Test request" works). `DevApiDocs.cs` holds this.
- OpenAPI description: the Core serves it at `GET /openapi/v1.json` in every environment, behind the bearer token. After any API change, regenerate the checked-in copy: `$env:UPDATE_OPENAPI=1; dotnet test tests/Organizer.Core.Tests --filter Served_description_equals_docs_openapi_json` (PowerShell), then commit `docs/openapi.json`. Without the variable, that test fails when the file is stale.
- New migration: `dotnet dotnet-ef migrations add <Name> --project core/Organizer.Core --output-dir Migrations -- --config <temp path>/core.json` (the config argument keeps design-time startup out of AppData).
