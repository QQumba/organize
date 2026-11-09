# Architecture overview

Read this before changing project structure, dependencies, hosting or deployment. Rules that apply to every change (no business logic in Clients, no offline mode, and so on) stay in `CLAUDE.md`. The reasons behind single decisions are in `docs/adr/`.

The layout below is the target. `desktop/` and `web/` do not exist yet; `CLAUDE.md` Layout lists what exists now.

## Components

- **Core**: sole owner of data and rules. ASP.NET Core minimal API (HTTP/JSON, token auth), EF Core on one SQLite file. Config via file. Runs as a separate background process started at login, and may later move to a remote server. Listens on localhost by default. Uses only portable .NET APIs. Not Native AOT (ADR 0002).
- **Contracts**: the API's request and response types (DTOs) and nothing else. No dependencies. Shared by the Core and the client library, so the API has one source of truth.
- **Client library**: typed C# wrapper over the Core API. Holds no business logic or storage. Shared by the CLI and the desktop app.
- **CLI** (`org`): .NET Native AOT, thin Client over the client library.
- **Desktop app**: WPF tray app, thin Client over the client library. Global shortcut opens a capture popup; minimal list view.
- **Web UI** (later): Svelte + TypeScript, built to static files served by the Core. Its API types are generated from the Core's OpenAPI description.
- Each Client has its own config file with the Core address and secret. Clients are Windows 10/11 only.

## Layout

One repo, one solution. The web UI is a Node project and is not in the solution.

```
Organizer.sln
Directory.Build.props, Directory.Packages.props   shared settings, central package versions
core/       Organizer.Core
contracts/  Organizer.Contracts
client/     Organizer.Client
cli/        Organizer.Cli
desktop/    Organizer.Desktop
web/        Svelte app (own package.json)
tests/      Core.Tests, Client.Tests, Cli.Tests, TestSupport (CoreHarness)
docs/       adr/, architecture.md, linear-workflow.md
```

Target frameworks are set per project: plain `net10.0` for everything except the desktop app, which is `net10.0-windows`. Anything the Core or the shared projects use must not need the Windows target.

## Dependency rule

```
Contracts <- Core
Contracts <- Client <- Cli
                    <- Desktop
TestSupport -> Core, Contracts      (starts a real Core for tests)
```

The Core never references Client, CLI or Desktop. A test reads `Organizer.Core.csproj` and fails if it references anything except Contracts. This keeps the Core deployable on its own, locally and on a remote server.

Contracts and the client library set `IsAotCompatible=true`, so the AOT and trim analyzers flag problems in the normal build, not only when the CLI is published. The client uses a source-generated `JsonSerializerContext`.

## Build, publish and run

- Core builds and publishes never need Node. The web UI is added by a separate step that builds `web/` and copies `web/dist` into the Core's publish output. The Core serves `wwwroot` only if that folder exists.
- One script, `publish.ps1` at the repo root, builds the deliverables into the git-ignored `dist/`: Core (framework-dependent), CLI (Native AOT single exe), Desktop. It grows as each deliverable appears.
- Install is a manual copy to `%LOCALAPPDATA%/Organizer/` plus adding the CLI folder to PATH. There is no installer in v1.
- Locally the Core runs as a Task Scheduler "at logon" task for the current user: no admin, hidden window, restart on failure, and `%APPDATA%` is the right config location. It is not a Windows Service, which would run as another account. The tray app never starts the Core.
- A remote Core is a separate deployment story, to be designed when needed. Nothing in the layout above blocks it.

## Version drift

The Core and the Clients are updated together. A mismatch shows up as an ordinary API error. There is no API version or version check. Revisit when the Core actually moves to a remote server.
