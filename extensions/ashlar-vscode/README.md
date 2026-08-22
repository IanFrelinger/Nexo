# Ashlar VS Code / Cursor extension

Offline multi-agent coding UX on top of the Ashlar agent-server.

## Install (dev)

1. Start Ashlar agent-server (from repo root):

```powershell
.\scripts\Start-FullstackAgentServer.ps1
```

2. In VS Code or Cursor: **Extensions: Install from Location…** → select `extensions/ashlar-vscode`.

3. Configure connection: **Ashlar: Configure Connection (Host / Port)** or settings
   `ashlar.apiHost` / `ashlar.apiPort` (default `127.0.0.1:8088`). Optional full override: `ashlar.baseUrl`.

4. Reload the window after updating the extension.

### Port / URL knobs

| Knob | Where |
|------|--------|
| API host/port | `ashlar.apiHost`, `ashlar.apiPort` or `ashlar.baseUrl` |
| Ollama host port | `ashlar.ollamaPort` → start script → `.env` `ASHLAR_OLLAMA_HOST_PORT` |
| Model | `ashlar.defaultModel` / `.env` `OLLAMA_MODEL` |
| Start stack CLI | `.\scripts\Start-FullstackAgentServer.ps1 -ApiPort 8090 -OllamaHostPort 11434` |

**Ashlar: Start Local Stack** passes the extension’s host/port/model into the start script and refreshes `.env` so Compose and smoke stay aligned.

## Features

### v0.1
- Status bar connection to `/api/ide/health`
- Chat sidebar + active file / selection context
- Model / agent pickers
- Start Local Stack

### v0.2 (Composer-lite)
- **Ask / Plan / Edit** modes
- Pending patches with per-file **Diff / Apply / Reject**
- Apply all / Reject all / **Undo last apply**
- Side-by-side diffs (`ashlar-patch:` scheme)
- Workspace path sandboxing
- Optional open-editors context (`ashlar.includeOpenEditors`)

### v0.3 (Runs + director)
- **Director** multi-agent goals
- Run timeline (sidebar + **Ashlar: Show Run Timeline**)
- **Cancel active run** / per-run cancel

### v0.4 (Workloads)
- Workload provider + replica status in sidebar
- **Ashlar: Show Workloads** / **Ashlar: Scale Workload**
- Poll interval `ashlar.pollWorkloadsMs` (default 10s)

### v0.5 (Stream + VSIX)
- Streaming chat via `/api/ide/chat/stream` (`ashlar.streamingChat`)
- Package with `.\scripts\Package-AshlarVscode.ps1`

### v0.5.2 (Ports)
- `ashlar.apiHost` / `ashlar.apiPort` (+ optional `ashlar.baseUrl`)
- **Ashlar: Configure Connection**
- Start Local Stack / `.env` / smoke share the same port knobs

### v0.5.3
- Chat sidebar: editable **host + port** with Apply (Enter works)
- Status bar keeps showing `Ashlar :<port>`; click it for full configure wizard

## Commands

| Command | Action |
|---------|--------|
| Ashlar: Check Connection | Probe agent-server |
| Ashlar: Configure Connection | Set host / API port / Ollama port / model |
| Ashlar: Plan With Prompt | Plan only (no writes) |
| Ashlar: Edit With Prompt | Propose patches |
| Ashlar: Director Goal | Multi-agent director run |
| Ashlar: Cancel Active Run | Cancel in-flight request |
| Ashlar: Show Run Timeline | Inspect / cancel runs |
| Ashlar: Show Workloads | List scaler provider + replicas |
| Ashlar: Scale Workload | Set desired replicas |
| Ashlar: Apply All Pending Patches | Apply queue |
| Ashlar: Reject All Pending Patches | Clear queue |
| Ashlar: Undo Last Apply | Restore previous contents |
| Ashlar: Show Pending Patches | Quick-pick diffs |
| Ashlar: Start Local Stack | Run compose helper with configured ports |

## API

See `docs/ide/AshlarVscode.md`.
