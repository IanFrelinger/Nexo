# Game Adapter Protocol (Unity Plugin / Mock)

The `GameAdapter` talks to a small TCP server running inside the game (typically a Unity plugin).

This repo also ships a **mock server** (`Nexo.Tools.GamePluginMock`) so the adapter can be exercised without Unity.

## Target formats

- Executable path: launches the game and connects to `localhost:9999`
- `tcp://host:port` or `game://host:port`: connects directly

Examples:

- `tcp://localhost:9999`
- `game://127.0.0.1:9999`

## Commands

All commands are **single-line**, newline-delimited.

- `HELLO` → `NEXO_PLUGIN 1.0` (optional; for diagnostics)
- `PING` → `PONG`
- `SCREENSHOT` → `DATA:<base64-png>`
- `GAMESTATE` → `<json>`
- `PLAYERSTATE` → `<json>`
- `INTERACTABLES` → `<json array>`
- `PERFORMANCE` → `<json>`
- Action inputs:
  - `MOVE <string>`
  - `LOOK <x> <y>`
  - `INPUT <name>`
  - `KEY <key>`
  - `CLICK <x> <y>`
  - `INTERACT <id>`

## Run the mock server

From repo root:

```bash
dotnet run --project src/Nexo.Tools.GamePluginMock -- 9999
```

