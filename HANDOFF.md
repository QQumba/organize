# Handoff: split v1 into individual issues

## Goal of the next session

Break v1 of Organizer into individual, independently buildable issues (vertical slices, each demoable and testable). Do not start implementing. Output issues in whatever tracker or format the user prefers; ask once if unclear. The repo has no code yet, only docs.

## Read first (the source of truth, not repeated here)

- `CLAUDE.md`: scope (v1 vs deferred), architecture, stack, working rules.
- `GLOSSARY.md`: vocabulary. Use these terms exactly in issue titles and bodies.
- `docs/adr/0001-core-as-http-service-with-thin-clients.md`
- `docs/adr/0002-dotnet-ef-core-sqlite-no-aot-core.md`

## Context that is not in those files

- The user is a single developer building a personal tool on Windows 11. They want several ways to capture (CLI, tray popup, web UI later) because they don't yet know which is easiest.
- The v1 focus is "building blocks first": Core, CLI, tray app, and easy capture. The user explicitly excluded all board features and the web UI from v1.
- Everything the user decided was deliberate; do not reopen: single Item type, no deletion (archive instead), separate views per state (Inbox, Done, Archive) implemented as filters, restore to open allowed, config files for auth and settings, no offline mode, Important/Urgent as manual flags.
- Deliberately left open (do not decide silently; make them explicit in issues):
  - Recurrence schedule design (TBD, user wants to pay attention to it during implementation; not in v1).
  - Natural-language parsing details for Capture (user said not to decide yet). v1 Capture can be plain text, and issues should note that parsing is a later refinement.
  - CLI command names and flags.
  - Backup file format (a single file produced and restored by the Core; shape not chosen).
  - How the Core is started at Windows login (a separate background process; mechanism not chosen).
  - Exact fields beyond those listed in `CLAUDE.md`, and the tray app's list view layout.
- Machine facts: Node 24, pnpm, Python 3.12, .NET 10 SDK and Rust 1.71 are installed. Go and Java were not confirmed.

## Suggested slicing (starting point, challenge it)

1. Core skeleton: project, config file, token auth, SQLite via EF Core, health endpoint, integration test harness with a temporary database.
2. Capture an Item and list the Inbox through the API (the first end-to-end slice).
3. Item edit: notes, Area, Tags, due date, Important/Urgent flags.
4. Item states: done, archived, restore; Done and Archive views.
5. Text search across Items.
6. Backup and restore commands in the Core.
7. Shared C# client library plus the CLI (Native AOT): capture, list, complete, edit, archive, search.
8. Tray app: global shortcut capture popup, tray icon, minimal list view.
9. Packaging and start-at-login for the Core and tray app.
10. OpenAPI description of the API (prerequisite for the later web UI).

Each issue should state acceptance criteria and which tests (Core API integration tests mainly) prove it. Flag dependencies between issues explicitly.

## Suggested skills

- `mattpocock-skills:grilling`: if slicing exposes unresolved decisions, put them to the user in rounds.
- `mattpocock-skills:domain-modeling`: when a new term or a hard-to-reverse decision appears, update `GLOSSARY.md` or add an ADR.
- `mattpocock-skills:codebase-design`: when shaping the Core's modules and the client library interface.
- `mattpocock-skills:tdd`: for the later implementation sessions, not this one.
- `mattpocock-skills:prototype`: if the tray popup or capture flow needs a throwaway experiment before committing.
