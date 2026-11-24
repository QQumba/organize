# Organizer tray app

A small Windows tray app (WPF) for capturing todos without leaving what you are doing. It is a thin Client of the Core: it stores nothing itself, and shows an error if the Core is unreachable.

## What it does

- Sits in the tray; the tray menu has **Open** and **Quit**.
- A global hotkey opens the Capture popup with the Inbox list below the input.
- Type a line and press **Enter** to capture it as an Item.
- Move down into the Inbox list, then press **Enter** or **Space** on a row to mark it done.

## Hotkey and keys

| Key | Action |
|---|---|
| **Alt+Q** | Open the popup (default; change with `Hotkey` in the config) |
| Enter (in the input) | Capture the text and close |
| Down / Up | Move between the input and the list |
| Enter or Space (on a row) | Mark the Item done |
| Esc | Dismiss the popup |

## Config

`%APPDATA%\Organizer\desktop.json` (not auto-created by the app; `install.ps1` creates it):

```json
{ "Address": "http://127.0.0.1:47821", "Secret": "<core secret>", "Hotkey": "Alt+Q" }
```

`Hotkey` is optional. `--config <path>` overrides the file. The file is read once at startup, with no hot reload: after changing it, choose **Quit** in the tray menu and start the app again. Startup errors show as the tray tooltip and one balloon.

Run and install instructions: `docs/install.md`.
