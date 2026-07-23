# Nexo VS Code / Cursor extension

Offline multi-agent coding UX on top of the Nexo agent-server.

## Install (dev)

1. Start Nexo agent-server (from repo root):

```powershell
.\scripts\Start-FullstackAgentServer.ps1
```

2. In VS Code or Cursor: **Extensions: Install from Location…** → select `extensions/nexo-vscode`.

3. Configure connection: **Nexo: Configure Connection (Host / Port)** or settings
   `nexo.apiHost` / `nexo.apiPort` (default `127.0.0.1:8088`). Optional full override: `nexo.baseUrl`.

4. Reload the window after updating the extension.

### Port / URL knobs

| Knob | Where |
|------|--------|
| API host/port | `nexo.apiHost`, `nexo.apiPort` or `nexo.baseUrl` |
| Ollama host port | `nexo.ollamaPort` → start script → `.env` `NEXO_OLLAMA_HOST_PORT` |
| Model | `nexo.defaultModel` / `.env` `OLLAMA_MODEL` |
| Start stack CLI | `.\scripts\Start-FullstackAgentServer.ps1 -ApiPort 8090 -OllamaHostPort 11434` |

**Nexo: Start Local Stack** passes the extension’s host/port/model into the start script and refreshes `.env` so Compose and smoke stay aligned.

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
- Side-by-side diffs (`nexo-patch:` scheme)
- Workspace path sandboxing
- Optional open-editors context (`nexo.includeOpenEditors`)

### v0.3 (Runs + director)
- **Director** multi-agent goals
- Run timeline (sidebar + **Nexo: Show Run Timeline**)
- **Cancel active run** / per-run cancel

### v0.4 (Workloads)
- Workload provider + replica status in sidebar
- **Nexo: Show Workloads** / **Nexo: Scale Workload**
- Poll interval `nexo.pollWorkloadsMs` (default 10s)

### v0.5 (Stream + VSIX)
- Streaming chat via `/api/ide/chat/stream` (`nexo.streamingChat`)
- Package with `.\scripts\Package-NexoVscode.ps1`

### v0.5.2 (Ports)
- `nexo.apiHost` / `nexo.apiPort` (+ optional `nexo.baseUrl`)
- **Nexo: Configure Connection**
- Start Local Stack / `.env` / smoke share the same port knobs

## Commands

| Command | Action |
|---------|--------|
| Nexo: Check Connection | Probe agent-server |
| Nexo: Configure Connection | Set host / API port / Ollama port / model |
| Nexo: Plan With Prompt | Plan only (no writes) |
| Nexo: Edit With Prompt | Propose patches |
| Nexo: Director Goal | Multi-agent director run |
| Nexo: Cancel Active Run | Cancel in-flight request |
| Nexo: Show Run Timeline | Inspect / cancel runs |
| Nexo: Show Workloads | List scaler provider + replicas |
| Nexo: Scale Workload | Set desired replicas |
| Nexo: Apply All Pending Patches | Apply queue |
| Nexo: Reject All Pending Patches | Clear queue |
| Nexo: Undo Last Apply | Restore previous contents |
| Nexo: Show Pending Patches | Quick-pick diffs |
| Nexo: Start Local Stack | Run compose helper with configured ports |

## API

See `docs/ide/NexoVscode.md`.
