# Getting Started with Nexo

This guide covers initial setup, trust configuration, and first pipeline validation. Nexo operates on local infrastructure with no external service dependencies. Trust controls are available but disabled by default — enable with `NEXO_TRUST_ENABLED=1`.

The **default** path is **containers + CLI**: develop inside the **Dev Container** (or run published **GHCR** images / **compose** stacks). Use **native** installers and `scripts/setup/*` only when Docker is not an option. See `README.md` for the full map.

## Quickest path (recommended)

**Cursor / VS Code:** install [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers), open the repo, **Dev Containers: Reopen in Container**. The first open restores the setup-gate NuGet graph via `.devcontainer/post-create.sh`.

**Portal in Docker only** (no IDE):

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
docker build -f .docker/Dockerfile.quickstart -t nexo:quickstart .
docker run --rm -p 8080:8080 nexo:quickstart
# Open http://localhost:8080 — mock provider; no API keys needed.
```

**One-command script** (uses Docker when present, otherwise tries a local SDK): `bash scripts/install/quickstart.sh` — see `scripts/install/quickstart.sh`.

## What you will do (manual path)

In ~10-15 minutes, you will:

1. Pick one startup lane (**Dev Container**, **GHCR CLI**, **quickstart image**, or **native** escape hatch).
2. Verify CLI is working.
3. Validate and run a minimal pipeline.
4. Move to deeper validation/testing only after first success.

## Prerequisites

- **Default:** Docker (Desktop or Engine) and Git. You do **not** need a host .NET SDK for Dev Container, quickstart image, or `docker run … ghcr.io/ianfrelinger/nexo-cli`.
- **Native lane:** .NET SDK **9.x** (repo is pinned in `global.json`).
- Optional: Ollama/OpenAI/Azure credentials (model-backed commands).

## 1) Choose your startup lane

### Lane A (fastest): container-first — Dev Container + `dotnet` CLI

Use **Dev Containers: Reopen in Container** (see `README.md`). Then:

```bash
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project src/Nexo.CLI -- --help
```

### Lane B: published CLI image (minimal host)

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

With workspace mount:

```bash
docker run --rm -v "$PWD:/work" -w /work ghcr.io/ianfrelinger/nexo-cli:latest --help
```

### Lane C (escape hatch): native setup scripts + CLI build

After **`bash scripts/setup/setup.sh all`** (same as **`bash scripts/setup/setup-unix.sh all`** on macOS/Linux; those dispatch to `setup-linux.sh` / `setup-macos.sh`), or Windows **`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all`**, Nexo runs a **bounded** Runtime Studio **`workflow optimize`** and writes the winning **Ollama `ModelName` values** into `apps/runtime-studio/config/agent_set.local.json`. Skip with **`NEXO_SKIP_RUNTIME_STUDIO_TUNE=1`** (Unix), **`-SkipRuntimeStudioTune`** (Windows `setup.ps1`), or rely on automatic skip in **CI** (`CI` / `GITHUB_ACTIONS`).

These scripts validate prerequisites and restore the setup-gate baseline NuGet graph for this repository:

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/install/install.sh --yes
```

```powershell
git clone https://github.com/IanFrelinger/Nexo.git
Set-Location Nexo
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes
```

One-click zero-to-hero launchers:

```bash
# Linux
chmod +x scripts/install/nexo-zero-to-hero-linux.sh
bash scripts/install/nexo-zero-to-hero-linux.sh

# macOS (double-click from Finder also works)
chmod +x scripts/install/nexo-zero-to-hero-macos.command
open scripts/install/nexo-zero-to-hero-macos.command
```

```powershell
# Windows PowerShell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\nexo-zero-to-hero-windows.ps1
```

If you prefer the lower-level setup script flow instead of one-shot installer wrappers:

```bash
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

## 2) Verify CLI is available

```bash
dotnet run --project src/Nexo.CLI -- --help
dotnet run --project src/Nexo.CLI -- doctor --json
```

You should see commands including `analyze`, `validate`, `pipeline`, `trust`, `test`, `orchestrate`, `agent`, `chat`, `runtime`, `workflow`, `mesh`, `config`, `doctor`, `dogfood`, `escalate`, `metrics`, and more.

## 2b) Try the copilot task flow (optional)

Submit a coding task and receive output with an audit trail:

```bash
# Via API (mock provider for testing):
NEXO_ALLOW_MOCK=1 dotnet run --project src/Nexo.API &
curl -s http://localhost:5000/api/copilot/task \
  -H "Content-Type: application/json" \
  -d '{"task": "Analyze the security posture of this project"}' | jq .
```

Or open `http://localhost:5000` in a browser — the portal includes a setup wizard, Quick chat, activity feed, and changelog assistant.

See `docs/CopilotMvpWalkthrough.md` for the full walkthrough.

## 2c) Run background-agent daemon mode (optional)

Use this when you want Nexo to run as a long-lived local process with hosted background agents.
If your config does not define `Nexo:Barriers:Levels`, the daemon defaults to `["public","internal"]` for local bootstrap.

```bash
# run for 30 seconds (smoke test)
dotnet run --project src/Nexo.CLI -- background-agent daemon --duration 30s

# run until Ctrl+C
dotnet run --project src/Nexo.CLI -- background-agent daemon

# use an explicit background-agent config file
dotnet run --project src/Nexo.CLI -- background-agent daemon --config docs/background-agents/examples/minimal-agent.json
```

## 3) First success command (high confidence, low friction)

```bash
tmp_dir="$(mktemp -d)"
template_path="$tmp_dir/nexo_quickstart.json"
cat > "$template_path" <<'JSON'
{
  "templateId": "quickstart",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON
dotnet run --project src/Nexo.CLI -- pipeline validate --template "$template_path"
```

## 4) Run first high-signal commands

Run these from the repository root:

```bash
# code and assembly analysis
dotnet run --project src/Nexo.CLI -- analyze --path .

# optional (heavier) architecture/test validation
dotnet run --project src/Nexo.CLI -- validate
```

## 5) Run your first pipeline

Create a minimal template:

```bash
cat > /tmp/nexo_pipeline_demo.json <<'JSON'
{
  "templateId": "demo",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON
```

Validate + run:

```bash
dotnet run --project src/Nexo.CLI -- pipeline validate --template /tmp/nexo_pipeline_demo.json
dotnet run --project src/Nexo.CLI -- pipeline run --template /tmp/nexo_pipeline_demo.json --run-id demo-run --format-json
dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json
```

## 6) Optional provider setup

If you plan to run model-backed workflows:

### OpenAI

```bash
export OPENAI_API_KEY="sk-..."
export OPENAI_MODEL="gpt-4o-mini"
```

### Ollama

```bash
export OLLAMA_BASE_URL="http://localhost:11434"
export OLLAMA_MODEL="llama3.1:latest"
```

Provider behavior and full configuration are documented in `docs/Configuration.md`.

## 7) Testing workflows

```bash
# targeted infrastructure tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~Pipelines"

# execution routing smoke + stress (NCR local, peer network fallback, cloud routing behavior)
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "FullyQualifiedName~PeerToPeerRoutingSmokeTests|FullyQualifiedName~CapabilityRoutingBrickTests"

# broader local test command
dotnet run --project src/Nexo.CLI -- test local
```

For timeout policy and anti-hang guidance, see `docs/Testing.md`.

## 8) Embed in your host application

At minimum:

```csharp
using Nexo.Hosting;

services.AddNexo();
```

Then resolve application ports from DI (analysis, validation, orchestration, etc.).

## Common pitfalls

- If commands fail due to SDK mismatch, ensure your local SDK honors `global.json` (`9.x`).
- `dotnet build Nexo.sln` can require mobile workloads depending on host; use setup scripts + CLI project build first.
- Prefer running heavy validations sequentially (not in parallel terminals) to avoid resource pressure.
- For CI parity, use the documented gate workflows under `.github/workflows/`.
- If native setup is blocking, switch to container lane first and continue there.

## Next documents to read

- `README.md` — high-level orientation and full CLI command map.
- `docs/Architecture.md` — subsystem layout and flow.
- `docs/Configuration.md` — environment variables and config reference.
- `docs/api/index.md` — REST API endpoints and hosting options.
- `docs/sdk.md` — SDK integration (host embedding and HTTP client).
- `docs/TrustAndInformationArchitecture.md` — barrier/trust model.
- `docs/Persistence.md` — persistence defaults and LiteDB options.
- `docs/ProductionReadinessGate-v1.md` — release gate and operator checks.
- `docs/DocsIndex.md` — full documentation index.
