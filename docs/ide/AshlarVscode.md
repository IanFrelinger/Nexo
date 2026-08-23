# Ashlar IDE bridge (VS Code / Cursor)

The **Ashlar** extension (`extensions/ashlar-vscode`) is a thin client over the agent-server IDE API.

## Server endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/ide/health` | Connectivity + Ollama/local readiness |
| `GET` | `/api/ide/models` | Model picker (NCR + Ollama tags) |
| `GET` | `/api/ide/agents` | Agent picker (registry + agent-set JSON) |
| `POST` | `/api/ide/session` | Bind workspace root → session id |
| `POST` | `/api/ide/chat` | Ask with optional file contexts (`mode`: ask/plan) |
| `POST` | `/api/ide/chat/stream` | SSE chat tokens (`event: run\|delta\|done\|error`) |
| `POST` | `/api/ide/plan` | Implementation plan only (no patches) |
| `POST` | `/api/ide/edit` | Propose `{ proposalId, patches: [{ path, originalContent, newContent, summary }] }` |
| `POST` | `/api/ide/director` | Multi-agent director-style goal (tracked) |
| `GET` | `/api/ide/runs` | Recent run timeline |
| `GET` | `/api/ide/runs/{runId}` | One run |
| `POST` | `/api/ide/runs/{runId}/cancel` | Cancel a running operation |
| `GET` | `/api/workloads/provider` | Active scaler provider |
| `GET` | `/api/workloads` | Configured workloads |
| `GET` | `/api/workloads/{id}/replicas` | Desired / current / ready |
| `PUT` | `/api/workloads/{id}/replicas` | Set desired replicas |

Path sandbox: patches must be **relative**, no `..`, no drive letters / absolute paths.

Implementation: `application/src/Ashlar.API/Endpoints/IdeEndpoints.cs`, run tracker in `src/Ashlar.Infrastructure/Ide/IdeRunTracker.cs`.

Streaming uses MEAI `IChatClient.GetStreamingResponseAsync` when registered; otherwise falls back to orchestrator and emits one `delta`.

## Extension (v0.5.2)

- **Ask / Plan / Edit / Director** in the sidebar
- **Streaming chat** (`ashlar.streamingChat`, default on)
- Agent roster + **run timeline** with per-run **Cancel**
- Pending patch list with **Diff / Apply / Reject** per file
- **Apply all / Reject all / Undo last apply**
- Side-by-side diffs via `ashlar-patch:` virtual documents
- **Workload panel**: provider status, replica counts, **Scale**
- **Configurable host/port** (`ashlar.apiHost` / `ashlar.apiPort`, or `ashlar.baseUrl` override)
- Workspace path checks on apply
- Optional `ashlar.includeOpenEditors` context packing
- Status bar shows pending patch count / active run

## Connection config

| Setting / env | Purpose | Default (fullstack lane) |
|---------------|---------|--------------------------|
| `ashlar.apiHost` | API host | `127.0.0.1` |
| `ashlar.apiPort` | API port | `8088` |
| `ashlar.baseUrl` | Full URL override (wins over host/port) | _(empty)_ |
| `ashlar.ollamaPort` | Host Ollama port for Start Local Stack | `11434` |
| `ashlar.defaultModel` | Default model + start-stack model | _(server default)_ |
| `ashlar.useBundledOllama` | Compose `ollama` service instead of host | `false` |
| `ASHLAR_API_HOST` | Written to `.env` by start script | `127.0.0.1` |
| `ASHLAR_AGENT_SERVER_HTTP_PORT` | Compose published API port | `8088` |
| `ASHLAR_OLLAMA_HOST_PORT` | Compose / host Ollama publish port | `11434` |
| `OLLAMA_BASE_URL` | Where API reaches Ollama | `http://host.docker.internal:11434` |
| `OLLAMA_MODEL` | Model tag | `codellama:7b` |

Change the port in any of these places:

1. **Chat sidebar** — host + port fields, then **Apply** (or Enter)
2. Status bar **Ashlar :<port>** — click for the full configure wizard
3. Command **Ashlar: Configure Connection (Host / Port)**
4. Settings `ashlar.apiHost` / `ashlar.apiPort`

**Ashlar: Start Local Stack** passes those values into `Start-FullstackAgentServer.ps1`, which upserts `.env`.

## Local stack

```powershell
# defaults from .env (or 8088 / 11434)
.\scripts\Start-FullstackAgentServer.ps1

# explicit ports
.\scripts\Start-FullstackAgentServer.ps1 -ApiPort 8090 -OllamaHostPort 11434 -OllamaModel llama3.1:latest

.\scripts\Smoke-IdeApi.ps1                  # reads .env
.\scripts\Smoke-IdeApi.ps1 -ApiPort 8090    # or -BaseUrl http://127.0.0.1:8090
```

Workload scaling defaults to the `null` provider (read-only empty list). Set `Ashlar:WorkloadScaling:Provider` to `kubernetes` or `compose` on the agent-server to enable scale actions (see `docs/WorkloadScaling.md`).

## Extension install

**Dev:** **Extensions: Install from Location…** → `extensions/ashlar-vscode`  
(Reload window after upgrade so commands/UI refresh.)

**VSIX:**

```powershell
.\scripts\Package-AshlarVscode.ps1
# then: Extensions: Install from VSIX… → extensions/ashlar-vscode/ashlar-0.5.0.vsix
```
