# Working from Linear tickets

Read this only if your task involves Linear tickets. Otherwise ignore Linear completely and do not call any Linear tool.

Workspace `atlas-jab`, team `ATL`, project `Organize`. Use the Linear MCP tools (`get_issue`, `save_issue`, `save_comment`, `list_issues`).

## Starting a ticket

1. Fetch it with `get_issue` (it does not include comments; use `list_comments` if needed).
2. Move it to **In Progress**.
3. Check its "Open decisions" section. If it lists any, do a planning pass first (below). Otherwise implement.
4. Work on `develop` (see Branches in `CLAUDE.md`). Put the ticket ID in commit messages, e.g. `ATL-8: capture endpoint`.

One ticket per session. Do not pick up the next ticket in the same session.

## Planning pass (only for tickets with open decisions)

1. Read the ticket, `GLOSSARY.md` and any ADR the ticket touches.
2. Ask the user about each open decision. Put recommendations, not surveys.
3. Write the answers into the ticket (edit "Open decisions" into a "Decisions" section). Add an ADR only if the decision is hard to reverse, surprising and a real trade-off.
4. Stop. Implementation starts in a fresh session.

## Finishing a ticket

1. All acceptance criteria met, tests pass, decisions recorded.
2. Move the ticket to **In Review**. Never to Done: the user does that.
3. Add one short comment: what changed and what was verified (a few lines).

## Writing tickets

- Use the format of existing tickets: demo sentence, Acceptance criteria, Tests, Dependencies, Open decisions.
- Name the dependency issues by ID and set them with `blockedBy`.
- Use `GLOSSARY.md` terms exactly.
- Put agent-only detail in a collapsible `+++ Agent notes` section at the end.
- Check `list_issues` for duplicates before creating.

## Rules

- Never delete anything in Linear.
- Do not change priority, assignee or labels unless asked.
- Do not mark a ticket Done.
