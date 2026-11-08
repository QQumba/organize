# Organizer

Personal tool for capturing todos and tracking regular tasks, work and personal, in one place. Priorities: minimal-friction capture, ease of use, and speed with low memory use in every part (especially UI).

Vocabulary lives in `GLOSSARY.md`; use those terms exactly. Big decisions are recorded in `docs/adr/`; propose an ADR when a decision is hard to reverse, surprising and a real trade-off. ADR format: `000N-slug.md`, title states the decision, one short paragraph (decision, why, rejected alternatives); copy the existing ones.

Read when relevant, not every session:
- `docs/adr/0001`, `0002`: before changing Core hosting, the API boundary, storage or the stack.
- `docs/linear-workflow.md`: only if your task involves Linear tickets. Otherwise ignore Linear completely.

## Scope

**v1:** Core, CLI, tray desktop app. Items (text, notes, Category, Tags, optional due date, Important/Urgent flags), states open/done/archived (no deletion), Inbox plus Done and Archive views, text search, backup and restore, one-line Capture from every Client.

**Deferred:** web UI, Eisenhower matrix (UI only; fields exist from v1), Recurrence (design TBD), Activities, Reminders/notifications, extra views, phone, sync, multi-user, offline mode. Do not build these unless asked.

## Architecture

- **Core**: sole owner of data and rules. ASP.NET Core minimal API (HTTP/JSON, token auth), EF Core on one SQLite file. Config via file. Runs as a separate background process started at login; it may later move to a remote server. Listens on localhost by default. Uses only portable .NET APIs.
- **CLI**: .NET Native AOT, thin Client.
- **Desktop app**: WPF tray app, thin Client. Global shortcut opens a capture popup; minimal list view.
- **Web UI** (later): Svelte + TypeScript, built to static files served by the Core; types generated from the Core's OpenAPI description.
- CLI and desktop app share a C# client library for the API. Each Client has its own config file (Core address and secret).
- Clients are Windows 10/11 only.

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

- `core/Organizer.sln`: solution.
- `core/Organizer.Core/`: the Core (ASP.NET Core minimal API, EF Core migrations in `Migrations/`).
- `core/Organizer.Core.Tests/`: xUnit integration tests (`CoreHarness` starts the Core in-process on a temp config and SQLite file).
- `nuget.config`: nuget.org only (the user-level config points at a missing local source).
- `dotnet-tools.json`: pins `dotnet-ef`.
- Planned, not yet created: `cli/`, `desktop/`, `web/`, shared client library.

## Commands

Run from the repo root, after `dotnet tool restore` once.

- Build: `dotnet build core/Organizer.sln`
- Test: `dotnet test core/Organizer.sln`
- Run the Core: `dotnet run --project core/Organizer.Core [-- --config <path>]`. Settings layer: `core/Organizer.Core/appsettings.json` (defaults, ships with the app) < `core.json` (user overrides; default `%APPDATA%/Organizer/core.json`, created on first start containing only a random secret) < environment variables < command line. Auth: `Authorization: Bearer <secret>`. Health: `GET /health`.
- Run the Core for development: `dotnet run --project core/Organizer.Core --launch-profile Dev` (or the `Dev` profile in the IDE). Uses a throwaway config and database in the git-ignored `.dev/` folder at the repo root, not your real `%APPDATA%` one. The profile deliberately sets no `applicationUrl`, so address and port still come from the config. In Development only, the Core also serves the OpenAPI description at `/openapi/v1.json` and the Scalar UI at `/scalar` (both skip token auth; the dev secret is prefilled, so "Test request" works). `DevApiDocs.cs` holds this.
- New migration: `dotnet dotnet-ef migrations add <Name> --project core/Organizer.Core --output-dir Migrations -- --config <temp path>/core.json` (the config argument keeps design-time startup out of AppData).
