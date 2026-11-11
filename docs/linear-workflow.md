# Working from Linear tickets

Read this only if your task involves Linear tickets. Otherwise ignore Linear completely and do not call any Linear tool.

Workspace `atlas-jab`, team `ATL`, project `Organize`. Use the Linear MCP tools (see "Linear tool notes" at the end).

## States

| State | Meaning | Moved by |
|---|---|---|
| Backlog | Not refined. May have unknowns. | |
| **Todo** | Refined: a self-contained spec, ready to implement. | Refinement session, when the user confirms |
| In Progress | Being implemented. | Implementation session |
| In Review | Implemented and on `develop`; the user verifies. | Implementation session |
| Done | | The user only |

A blocker is **implemented** when it is In Review or Done: its code is on `develop`. A ticket is **unblocked** when every `blockedBy` ticket is implemented.

A ticket with the `deferred` label is a placeholder for out-of-scope work. Work on it only when the user names it.

## Which session am I?

A session either refines one ticket or implements one ticket. The next ticket starts in a fresh session.

Fetch the ticket with `get_issue` and `includeRelations: true` (comments come from `list_comments`), then:

- The user says "refine next" or "implement next": pick the ticket first (Picking the next ticket).
- The user asks to refine it, or asks to implement a ticket in **Backlog**: refine it. Implement a Backlog ticket only if the user explicitly confirms that this ticket needs no refinement.
- The user asks to implement a ticket in **Todo**: implement it.
- Any other state: ask the user what they want.

## Picking the next ticket

1. List every ticket once: `list_issues` with project `Organize` and `fields: ["status", "priority", "labels"]`, no state filter. This gives the state of every ticket, which step 3 needs for blockers.
2. The candidates are the **Backlog** tickets for "refine next" and the **Todo** tickets for "implement next", minus those labelled `deferred`. Order them by Linear priority (Urgent, High, Medium, Low, then No priority), then by lowest ticket number. Priority is the user's way to steer the order; most tickets have none, so the number usually decides.
3. Walk the candidates in that order. For each, fetch its blockers with `get_issue` and `fields: ["relations"]`, look up each blocker's state in the step 1 list, and take the first unblocked ticket. If a blocker is Canceled, ask the user.
4. Tell the user the pick and the reason in one line (for example "ATL-27: lowest unblocked Backlog ticket, no blockers"), then start; the user redirects if they want another.
5. If no candidate is unblocked, stop and list the candidates with their open blockers.

## Blocked tickets the user names

- **Implement:** if the ticket is not unblocked, stop and name the open blockers and their states.
- **Refine:** say which blockers are not implemented, then refine anyway. Where the spec depends on code that does not exist yet, record what you assumed about it under "Assumptions" in the ticket, each naming the blocker it depends on.

## Refining a ticket

The ticket stays in Backlog while you work on it.

1. Read the ticket with its relations and comments, `GLOSSARY.md`, any ADR it touches, and the code it will touch, including the code its blockers produced.
2. Hunt for unknowns, including ones the ticket does not mention. Check at least: error paths and failure messages, config and where it lives, tests (what, and where), publish script and docs that must change (`CLAUDE.md` Layout and Commands), dependencies on other tickets, performance and memory limits, behaviour at the edges (empty input, second launch, Unicode), and anything the ticket's wording leaves open.
3. Ask the user about each open decision. Put recommendations, not surveys. For small gaps, propose a default and let the user confirm or change it. Treat every section outside the Backlog shape (Writing tickets), and any "proposed" wording, as open decisions to confirm.
4. Split the ticket if it is too big for one implementation session (Splitting a ticket). Raise new tickets for loose ends (Writing tickets).
5. Rewrite the ticket so it stands alone: the implementation agent must not have to guess.
6. When the user confirms the ticket is settled, move it to **Todo**. Stop.

A ticket is ready for Todo when:

- Acceptance criteria are specific and testable.
- Its sections are exactly the Todo shape (Writing tickets): no open decisions or proposed wording remain.
- The `blockedBy` relations are complete and the Dependencies section matches them.
- Tests and docs to change are stated.
- A ticket whose result is checked by hand states the manual demo to run.

## Implementing a ticket

1. Start only from a ticket that is in Todo and unblocked.
2. If it has any section outside the Todo shape, "proposed" wording, an Assumption that the blocker's real code contradicts, or anything you would have to guess: move it back to **Backlog**, add a short comment saying what is unclear, and stop.
3. Move it to **In Progress**.
4. Put the ticket ID in commit messages, e.g. `ATL-8: capture endpoint`.

## Finishing a ticket

1. All acceptance criteria met, tests pass, decisions recorded, work pushed to `develop`.
2. If the ticket states a manual demo, run it yourself against the dev setup (for UI, in a real browser against the dev Core). If you cannot run it, say so in the comment instead of claiming it.
3. Move the ticket to **In Review**.
4. Add one short comment: what changed, and what was verified and how (tests, manual demo, or not run).

## Splitting a ticket

Done when all of these hold:

- The new ticket is in Backlog, in the Backlog shape.
- `blockedBy` / `blocks` are set between the two tickets, and on any other ticket whose dependency moved.
- The old ticket's title, demo sentence, acceptance criteria and Dependencies section describe only what is left in it.
- Both tickets carry one line saying what moved and where.

## Writing tickets

Shape by state:

- **Todo (refined):** demo sentence, Acceptance criteria, Tests, Dependencies, Decisions, and Assumptions if it was refined ahead of a blocker.
- **Backlog:** demo sentence, Acceptance criteria, Tests, Dependencies, Open decisions. Refinement turns it into the Todo shape.

Rules for every ticket:

- New tickets go to Backlog. Check `list_issues` for duplicates before creating.
- The `blockedBy` relation is the source of truth for dependencies. The Dependencies section names the same tickets by ID and says why; when you change one, change the other in the same edit.
- A placeholder for out-of-scope work gets the `deferred` label.
- A ticket carries what its reader needs in its own text. Point only at files committed to the repo; for anything in `temp/` or another git-ignored path, write the content into the ticket.
- Put agent-only detail in a collapsible `+++ Agent notes` section at the end.

## Rules

- Never delete anything in Linear.
- Priority, assignee and every label except `deferred` are the user's.

## Linear tool notes

- `get_issue` returns relations only when asked: `includeRelations: true` with the full ticket, or `fields: ["relations"]` for the relations alone. A relation carries the other ticket's ID and title, not its state.
- `list_issues` returns no relations.
- `save_issue` relations (`blockedBy`, `blocks`) only add. Remove with `removeBlockedBy` / `removeBlocks`.
- Edit part of a description with `save_issue`'s `patch` rather than resending the whole text. Anchors match the description exactly as `get_issue` returns it, so copy them from a fresh `get_issue`. An anchor that matches twice, or not at all, fails the whole patch and saves nothing.
- The returned text differs from what you wrote in two ways, and anchors must use the returned form:
  - Collapsible notes are written `+++ Agent notes` ... `+++` and come back as `>>> Agent notes` ... `>>>`.
  - A ticket ID such as `ATL-24` comes back as an `<issue id="..." label="ATL-24" href="..." />` tag. Keep anchors clear of ticket IDs, or copy the whole tag.
- Add or remove a single label with `addLabels` / `removeLabels`; `labels` replaces the whole set.
