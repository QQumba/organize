# Web UI

Rules and commands for work inside `web/`. The root `CLAUDE.md` still applies.

## Commands

Run in `web/` (Node 24+). `npm run dev` (proxies `/api` to the dev Core), `npm run build`, `npm run preview`, `npm run gen:api` (regenerates `src/lib/api/schema.d.ts` from `../docs/openapi.json`; `predev`, `prebuild`, `precheck` run it), `npm run check` (svelte-check, prettier, eslint, then `scripts/check-conventions.mjs`), `npm test` (vitest, plain TS modules only).

## Layout and naming

- `src/routes/`: screens (SvelteKit routes). A new screen is a folder with `+page.svelte` and, if it needs data, `+page.ts`.
- `src/lib/components/`: shared components, one `PascalCase.svelte` per component. A component used by one screen only may stay beside that screen.
- `src/lib/api/`: the API module: the `openapi-fetch` client (`client.ts`) and the generated `schema.d.ts` (git-ignored). Other plain TS (helpers, state) lives in `src/lib/` as `camelCase.ts` with a `.test.ts` beside it.
- Imports use the `#lib/*.ts` alias (SvelteKit 3 has no `$lib`).
- `src/routes/dev/` holds developer pages: the component gallery and the playground. Safe to deploy, not linked from the app.

## Calling the Core API

Only through the generated types and the `openapi-fetch` client in `#lib/api/client.ts`. Never hand-write request or response types, never use bare `fetch` for `/api`. After an API change, `docs/openapi.json` is regenerated on the Core side; run `npm run gen:api` here. No business logic in the UI: the Core owns the rules.

## Data pattern every screen follows

- Load data in the SvelteKit `load` function in `+page.ts`, not in `$effect`.
- Exception: the placeholder page `src/routes/(app)/+page.svelte` loads health in `$effect`; it goes away with the Inbox.
- After a change (capture, done, archive, edit), refresh the lists with `invalidate`.
- 401 and network errors are handled in one place, not per screen: the `openapi-fetch` middleware in `#lib/api/client.ts` (decisions in `api/guard.ts`). A 401 (except from `/api/session`) goes to `/login?return=<page>`, once for concurrent 401s; an unreachable Core (network error, 502/503/504) shows the `CoreBanner` (state in `coreStatus.svelte.ts`, rendered by the root layout) until the next successful response; Retry runs `invalidateAll()`. No offline mode.
- The root layout `load` asks `GET /api/session` once per page load (`session.ts`; `logout()` lives there too). `returnPath.ts` validates the login `return` path.
- A failed call shows its error where it happened: `errorText(...)` (`#lib/errorText.ts`) turns a failed result (`{ response, error }` or `{ thrown }`) into text (problem-details `detail` for 400/409, "Can't reach the Core", "Something went wrong (status N)").
- Show a loading indicator only when a load takes noticeably long (a short delay first), so quick loads do not flash.
- State: local `$state` in the component; shared state only when two distant parts need it, in a `.svelte.ts` module. No state library.

## Theme and styling

- Tokens are defined once in `src/app.css` (`@theme`): colors `surface`, `surface-raised`, `border`, `text`, `text-muted`, `accent`, `accent-contrast`, `focus-ring`, `danger`, `mark-important`, `mark-urgent`; the system font stack (`--font-sans`); radii `rounded-control` (4px), `rounded-panel` (6px), `rounded-full` (Tag chips); `h-control` (28px) and `h-row` (54px).
- Use tokens through Tailwind utilities (`bg-surface`, `text-text-muted`, `border-border`) or `var(--color-*)` in plain CSS. **Never raw color values** (hex, `rgb()`, Tailwind palette classes like `bg-red-500`, arbitrary `bg-[#...]`); `npm run check` fails on them in components and app screens.
- Dark only, `color-scheme: dark` (native date picker and scrollbars are dark). A light theme is one block of `--color-*` overrides in `app.css`; do not write components that need to know the theme.
- Base size 14px, line height 1.4, compact density: controls 28px high, gaps and padding in steps of 4px (mostly 8 and 12px).
- Tailwind is the default way to style; plain CSS is fine where it fits better. The CSP (`style-src 'self'`) forbids `style="..."` attributes; use classes or Svelte `style:` directives.
- Buttons: `btn btn-primary` (accent fill, `accent-contrast` text), `btn btn-secondary` (`border` outline on `surface-raised`), `btn btn-danger` (`danger` outline, fills on hover). Inputs: `input` (text and date); an invalid one has `aria-invalid="true"`. These classes are in `app.css`.
- Keyboard focus is always visible: one `focus-ring` outline from `app.css` on `:focus-visible`. Do not remove outlines. The keyboard shortcuts work (ATL-37) relies on it.
- User data may be Ukrainian (Cyrillic); the UI text is English only, no localization.

## Component gallery

`src/routes/dev/components/+page.svelte` is the single source of truth for existing components: one section per component with its variants and states (default, hover, focus, disabled, error) and Ukrainian sample text.

- Before making a component, read the gallery and reuse what exists.
- A component is not done until it is in the gallery. `npm run check` fails if a file in `src/lib/components/` is not mentioned there.
- Every widget ticket adds its component to the gallery.

## Agreed look of the shared widgets

Source: the rows prototype (`/dev/playground/rows`, iteration 4). The shared widgets are built by the first screen ticket that needs them; the dialog and the Tags input are designed by their own tickets from the tokens.

**Item row** (a baseline, not final: the implementer may deviate with a reason stated in the ticket comment): 54px high (also without a bottom line) on `surface-raised` with a `border` divider, inside a rounded `border` container (`rounded-panel`); on hover or focus-within the row turns `surface`. Left to right: a checkbox (`accent-color`), the body, the Category, the hover actions.

- Body top line: the title on one line with an ellipsis.
- Body bottom line, only when there is something to show: Important marker `■` (`mark-important`), Urgent marker `▲` (`mark-urgent`), due date `DD.MM` (12px, tabular numbers, `text-muted`, `danger` when overdue), Tag chips.
- Category: 12px `text-muted`, vertically centered on the row.
- Actions (Archive, Edit): 12px `text-muted` text buttons, `text` on hover; the right-most element, vertically centered, shown only on hover or focus-within; the Category shifts left when they appear.

**Tag chip:** 1px `border` pill (`rounded-full`), 12px, `text-muted`.

The shell layout (ATL-31) and the row anatomy (ATL-32) are starting points; an implementer may deviate with a good reason stated in the ticket comment.

## Playground

`src/routes/dev/playground/<name>/` is an archive of prototypes (the index page lists them and repeats these rules).

- Prototypes are references only: never moved or copied into production code. Production code is written fresh, following the tokens and the gallery.
- Agents never delete or overwrite an existing prototype; a new idea gets a new folder. The user deletes old ones by hand.
- Nothing in `src/lib` imports from the playground.
- The playground is safe to deploy.

## Manual demos

For a web UI ticket with a manual demo, the implementing agent runs it in a real browser against the dev Core (next section) and reports what it saw in the ticket comment; the user verifies again in In Review. No browser tests are checked in. If no browser can be run, say so; never claim the demo ran.

## Checking a page in a real browser

Use `playwright-cli` (installed globally, 0.1.24; the `playwright-cli` skill in `.claude/skills/` has the full command list). Verified working on this machine with the Vite dev server.

```
cd web && npm run dev -- --port 5199     # run in the background; Vite prints the URL
playwright-cli open http://localhost:5199/<route>
playwright-cli resize 1280 800
playwright-cli screenshot --filename=../temp/screenshots/<name>.png
playwright-cli console                   # console messages
playwright-cli close
```

- Save screenshots in `temp/screenshots/` at the repo root (`temp/` is git-ignored; create the folder if missing). `playwright-cli` writes snapshots and console logs to `.playwright-cli/` (git-ignored); delete that folder when you are done.
- Stop the Vite server when you finish (it keeps the port).
- Drive pages with `snapshot`, `click`, `fill`, `goto` and `eval` (see the skill); `show --annotate` lets the user give feedback on the live page.
- Expected console noise without a Core: `404 /favicon.ico` and `502 /api/health` (Vite proxies `/api` to 127.0.0.1:47822). Start the dev Core (`powershell ./dev.ps1`, or `-Web` for Core, tray app and Vite together) when a page needs the API. Any other console error is a real failure.
- If you cannot run a browser, say so; never claim a manual demo ran.

## Before you finish

`npm run check` (svelte-check, prettier, eslint) and `npm test` must pass.
