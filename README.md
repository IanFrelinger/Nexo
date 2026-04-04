# Nexo

Nexo is a .NET framework and CLI for building AI-enabled software with orchestration, trust controls, adaptive pipelines, and test automation.

Repository: <https://github.com/IanFrelinger/Nexo>

## Why Nexo

- Build AI-powered workflows as composable, testable runtime pipelines.
- Mix deterministic and agentic execution with fallback chains.
- Enforce trust boundaries (barriers, audit, routing policy).
- Run validation and multi-environment checks from one CLI.

## Quick Start (5 minutes)

### Choose your lane (recommended)

**Lane A: fastest path (container runtime)**

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

**Lane B: full local dev path (native SDK)**

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project src/Nexo.CLI -- --help
```

Prefer this one-shot installer if you want fewer manual steps:

```bash
bash scripts/install/install.sh --yes
```

### 1) Prerequisites

- .NET SDK 9.x (the repo is pinned by `global.json`)
- Git
- Optional: Docker (for containerized test workflows)
- Optional: Ollama/OpenAI/Azure credentials (for live model backends)

### Environment setup scripts (Windows/macOS/Linux)

Use the setup scripts to validate required tooling and restore baseline NuGet packages used by the core CI gates.
The CI setup gate also validates these scripts in an ephemeral Linux container (`mcr.microsoft.com/dotnet/sdk:9.0`) on every run:

```bash
# Linux/macOS: check required dependencies
bash scripts/setup/setup.sh check

# Linux/macOS: restore NuGet packages/solutions
bash scripts/setup/setup.sh restore

# Linux/macOS: run check + restore
bash scripts/setup/setup.sh all
```

```powershell
# Windows PowerShell: check required dependencies
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode check

# Windows PowerShell: restore NuGet packages/solutions
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode restore

# Windows PowerShell: run check + restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
```

**Windows + Docker (no local .NET SDK):** restoring the full `Nexo.sln` in a plain Linux SDK container fails on MAUI/Android workloads. Use the setup-gate restore (same projects as `setup.sh restore`) inside one container, with a persistent NuGet volume:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\docker-restore.ps1
# optional: also build Nexo.CLI in-container
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\docker-restore.ps1 -Build
```

### Remote development (Cursor / VS Code)

**Dev Container (recommended):** install the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension, open this repo, then **Dev Containers: Reopen in Container**. The container uses .NET 9, persists NuGet packages in a named volume, and runs the same setup-gate `dotnet restore` graph as `scripts/docker-restore.ps1` (not full `Nexo.sln`, which requires MAUI workloads).

**Remote SSH:** on the Linux/macOS host, install prerequisites (`bash scripts/setup/setup.sh all` or your usual bootstrap), clone the repo, then in Cursor use **Remote-SSH: Connect to Host…** and open the folder. Cursor installs its server component on first connect; you only need a normal user account, SSH, and the toolchain on the machine.

### One-click bootstrap installers (Option 1)

For single-command install/bootstrap wrappers, see:

- `docs/OneClickInstall.md`

These wrappers can auto-install missing required prerequisites (including `.NET SDK 9`) in guided mode, then run restore/build checks.

Quick examples:

```bash
bash scripts/install/install.sh --yes
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes
```

Container-first one-shot bootstrap (installs Docker if needed, pulls CLI + SDK images, smoke-runs both):

```bash
bash scripts/install/container-bootstrap.sh --yes
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\container-bootstrap.ps1 -Yes
```

### Container image usage (flexible deployability)

Build local CLI image:

```bash
docker build -f .docker/Dockerfile.cli -t nexo-cli:local .
docker run --rm nexo-cli:local --help
```

Pull published image (GHCR):

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

Build with explicit framework/runtime versions:

```bash
docker build \
  --build-arg DOTNET_SDK_VERSION=9.0 \
  --build-arg TARGETFRAMEWORK=net8.0 \
  --build-arg DOTNET_RUNTIME_VERSION=8.0 \
  -f .docker/Dockerfile.cli \
  -t nexo-cli:net8 .
```

Validate a pipeline template from your current workspace using the published image:

```bash
docker run --rm \
  -v "$PWD:/work" \
  -w /work \
  ghcr.io/ianfrelinger/nexo-cli:latest \
  pipeline validate --template /work/path/to/template.json
```

### 2) Clone and build

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

### 3) Confirm CLI is working

```bash
dotnet run --project src/Nexo.CLI -- --help
```

### 3b) Run onboarding doctor (single pass/fail report)

```bash
dotnet run --project src/Nexo.CLI -- doctor --json
```

### 4) Run a first high-signal command

`validate` can execute a broad architecture/test sweep and may be heavier on constrained hosts. For first-run confidence, start with CLI help and a pipeline validate command:

```bash
dotnet run --project src/Nexo.CLI -- --help
tmp_dir="$(mktemp -d)"
template_path="$tmp_dir/nexo_pipeline_quickstart.json"
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
dotnet run --project src/Nexo.CLI -- validate
```

## First Successful Pipeline Run

```bash
tmp_dir="$(mktemp -d)"
template_path="$tmp_dir/nexo_pipeline_demo.json"

cat > "$template_path" <<'JSON'
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

dotnet run --project src/Nexo.CLI -- pipeline validate --template "$template_path"
dotnet run --project src/Nexo.CLI -- pipeline run --template "$template_path" --run-id demo-run --format-json
dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json
```

Pipeline runtime option precedence:
1) defaults, 2) config under `Nexo:Pipelines:*`, 3) environment variables (`NEXO_PIPELINE_*`).

## Common CLI Workflows

| Goal | Command |
|------|---------|
| Show all commands | `dotnet run --project src/Nexo.CLI -- --help` |
| Validate architecture/contracts | `dotnet run --project src/Nexo.CLI -- validate` |
| Analyze source/assemblies | `dotnet run --project src/Nexo.CLI -- analyze --path .` |
| Run background-agent daemon mode | `dotnet run --project src/Nexo.CLI -- background-agent daemon --duration 10m` |
| Run pipeline template | `dotnet run --project src/Nexo.CLI -- pipeline run --template <file>` |
| View trust boundary/audit | `dotnet run --project src/Nexo.CLI -- trust --help` |
| Run local test entrypoint | `dotnet run --project src/Nexo.CLI -- test local` |
| CI verification bundle | `dotnet run --project src/Nexo.CLI -- ci` |

## Demo Scripts (for rollout and live demos)

Use these when you need an end-to-end walkthrough without manually stitching commands together.

```bash
# high-signal demo flow (build, bootstrap, chat, orchestration, dogfood)
bash scripts/oh-shit-demo.sh --quick

# skip build if CLI is already built
bash scripts/oh-shit-demo.sh --quick --no-build

# unity sidecar demo (generate + compile validation + dogfood block1)
bash scripts/unity-sidecar-demo.sh run-demo --prompt "add a dash ability"
```

References:
- `scripts/oh-shit-demo.sh`
- `scripts/unity-sidecar-demo.sh`
- `docs/UnitySidecarDemo.md`

## Docker Compose Workflows

The repository includes optional compose definitions for test and ephemeral runtime scenarios.

CI validation:
- `.github/workflows/compose-gate.yml` runs compose-based checks on every relevant PR/push.
- You can trigger manually with:
  - `gh workflow run "Compose Gate" --ref master`

```bash
# run Ubuntu test service and write JSON/log output into ./test-results
docker compose -f docker-compose.test.yml up --build test-ubuntu

# start ephemeral Ollama container (and optional Postgres profile)
docker compose -f docker-compose.ephemeral.yml up ollama
docker compose -f docker-compose.ephemeral.yml --profile db up postgres

# start self-hosted director portal (API + dailies + Ollama)
docker compose -f docker-compose.portal.yml up -d --build

# self-hosted agent server: portal + API + Ollama + Runtime Studio background agent cluster (mounts repo at /work)
docker compose -f docker-compose.agent-server.yml up -d --build
```

Notes:
- `docker-compose.test.yml` mounts `./test-results` and runs `BaseFrameworkSmokeTests` (`net9.0`) in container, writing log/summary artifacts there.
- `docker-compose.ephemeral.yml` is for disposable local orchestration dependencies.
- `docker-compose.portal.yml` serves a browser portal on `http://localhost:8080` for iterative directorial runs and daily review.
- `docker-compose.agent-server.yml` adds a mounted workspace and loads the Runtime Studio agent set; see `docs/SelfHostedAgentServer.md`.

## Providers

LLM/vision routing is provider-based:

| Provider | Notes |
|----------|-------|
| `offline`, `mock`, `mock-json` | deterministic/local-friendly paths |
| `ollama` | local model runtime (`OLLAMA_BASE_URL`, `OLLAMA_MODEL`) |
| `openai` | requires `OPENAI_API_KEY` |
| `azure` | requires `AZURE_OPENAI_*` settings |

## Project Layout

```text
Nexo/
├── src/
│   ├── Nexo.CLI/                 # CLI surface
│   ├── Nexo.Hosting/             # AddNexo() integration entrypoint
│   ├── Nexo.Infrastructure/      # execution, persistence, adapters
│   ├── Nexo.Orchestration/       # orchestrator, routing, coordination
│   ├── Nexo.Runtime/             # runtime services and barrier plumbing
│   ├── Nexo.Core.Application/    # use cases and ports
│   ├── Nexo.Core.Domain/         # domain model
│   └── Nexo.Tests.*/             # test suites
├── docs/                         # docs, specs, guides
├── .docker/                      # docker test/runtime definitions
├── global.json                   # SDK pin
└── Nexo.sln
```

## Testing

```bash
# full solution tests
dotnet test Nexo.sln

# focused pipeline tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~Pipelines"
```

## Documentation Map

Start here:
- `docs/GettingStarted.md` – guided first-hour setup and usage
- `docs/DocsIndex.md` – where to find docs by task
- `apps/runtime-studio/README.md` – application-layer planner+worker agent set integration.

Core references:
- `docs/Configuration.md` – environment/config options
- `docs/Architecture.md` – architecture and subsystem overview
- `docs/Testing.md` – test strategy, guard rails, and commands
- `docs/OneClickInstall.md` – one-shot install wrappers for Linux/macOS/Windows
- `docs/OnboardingAutomation.md` – what setup is automated vs. still manual
- `docs/TrustAndInformationArchitecture.md` – trust model, barriers, audit
- `docs/ProductionReadinessGate-v1.md` – production gate procedure
- `docs/EnvironmentSetupGate-v1.md` – cross-platform dependency/bootstrap gate
- `docs/ReleaseCandidateChecklist-v1.md` – RC readiness checklist

## Trust Notes

- JWT barrier resolution reads pre-validated claims from host auth middleware.
- API keys are stored as SHA-256 hashes, not plaintext.
- Audit details never include full API key values.

## License

See repository for license information.
