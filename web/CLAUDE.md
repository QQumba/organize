# Web UI

Rules and commands for work inside `web/`. The root `CLAUDE.md` still applies. The conventions section (Tailwind theme, colors, light/dark, components) is added by ATL-23.

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
