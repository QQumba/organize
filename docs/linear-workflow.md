# Working from Linear tickets

Read this only if your task involves Linear tickets. Otherwise ignore Linear completely and do not call any Linear tool.

Workspace `atlas-jab`, team `ATL`, project `Organize`. Use the Linear MCP tools (`get_issue`, `save_issue`, `save_comment`, `list_issues`).

## States

| State | Meaning | Moved by |
|---|---|---|
| Backlog | Not refined. May have unknowns. | |
| **Todo** | Refined: a self-contained spec, ready to implement. | Refinement session, when the user confirms |
| In Progress | Being implemented. | Implementation session |
| In Review | Implemented; the user verifies. | Implementation session |
| Done | | The user only |

A session either refines a ticket or implements it, never both. One ticket per session; do not pick up the next ticket in the same session.

## Which session am I?

Fetch the ticket with `get_issue` (it does not include comments; use `list_comments` if needed), then:

- The user asks to refine it, or asks to implement a ticket in **Backlog**: refine it (below). Do not implement it. Only implement a Backlog ticket if the user explicitly confirms that this ticket needs no refinement.
- The user asks to implement a ticket in **Todo**: implement it (below).
- Any other state: ask the user what they want.

## Refining a ticket

The ticket stays in Backlog while you work on it.

1. Read the ticket, `GLOSSARY.md`, any ADR it touches, and the code it will touch.
2. Hunt for unknowns, including ones the ticket does not mention. Check at least: error paths and failure messages, config and where it lives, tests (what, and where), publish script and docs that must change (`CLAUDE.md` Layout and Commands), dependencies on other tickets, performance and memory limits, behaviour at the edges (empty input, second launch, Unicode), and anything the ticket's wording leaves open.
3. Ask the user about each open decision. Put recommendations, not surveys. For small gaps, propose a default and let the user confirm or change it.
4. Split the ticket if it is too big for one implementation session. Raise new tickets for loose ends (see Writing tickets). Add an ADR only if a decision is hard to reverse, surprising and a real trade-off.
5. Rewrite the ticket so it stands alone. It has no "Open decisions" section and no "proposed" or "to be decided" wording left; decisions are folded into the acceptance criteria and listed under "Decisions". The implementation agent must not have to guess.
6. When the user confirms the ticket is settled, move it to **Todo**. Stop. Implementation starts in a fresh session.

A ticket is ready for Todo when:

- Acceptance criteria are specific and testable.
- No open decisions remain.
- Dependencies are set with `blockedBy`.
- Tests and docs to change are stated.

## Implementing a ticket

1. Only start from a ticket in Todo (see "Which session am I?").
2. If it still has an "Open decisions" section, "proposed" wording, or anything you would have to guess, do not implement. Move it back to **Backlog**, add a short comment saying what is unclear, and stop.
3. Move it to **In Progress**.
4. Work on `develop` (see Branches in `CLAUDE.md`). Put the ticket ID in commit messages, e.g. `ATL-8: capture endpoint`.

## Finishing a ticket

1. All acceptance criteria met, tests pass, decisions recorded.
2. Move the ticket to **In Review**. Never to Done: the user does that.
3. Add one short comment: what changed and what was verified (a few lines).

## Writing tickets

- Use the format of existing tickets: demo sentence, Acceptance criteria, Tests, Dependencies, then Decisions (refined) or Open decisions (not yet refined).
- New tickets go to Backlog.
- Name the dependency issues by ID and set them with `blockedBy`.
- Use `GLOSSARY.md` terms exactly.
- Put agent-only detail in a collapsible `+++ Agent notes` section at the end.
- Check `list_issues` for duplicates before creating.

## Rules

- Never delete anything in Linear.
- Do not change priority, assignee or labels unless asked.
- Do not mark a ticket Done.
- Do not implement a Backlog ticket without refinement or the user's explicit confirmation.
