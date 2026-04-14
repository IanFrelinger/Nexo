# Nexo

Private, traceable AI computing with self-extending capabilities.

Nexo is a .NET platform for composable AI workflows with structural trust enforcement. Capabilities are modular components with standardized contracts that compose into pipelines. The platform autonomously generates new components when it identifies capability gaps, validates them against regression, and promotes them under configurable policy constraints. All generated output follows enforced standards and remains readable, auditable, and extractable from the core.

Nexo operates entirely on infrastructure you control. Cloud providers are opt-in execution targets, not dependencies. No data leaves the host unless explicitly routed to a trusted peer. Air-gapped deployment is supported without modification.

Repository: <https://github.com/IanFrelinger/Nexo>

## Why Nexo

- **Data sovereignty.** The platform runs on local infrastructure with no external dependencies. Cloud LLM providers are available as opt-in execution targets with sanitization at the boundary. Air-gapped deployment is supported out of the box.
- **Autonomous capability extension.** The platform detects capability gaps through usage pattern analysis, generates new components, validates them against the existing test suite, and promotes them to the capability registry — all under policy constraints with a complete audit trail.
- **Enforceable output standards.** Generated components conform to configurable quality and structural standards. They are testable, decoupled from the core, and independently extractable.
- **Full data provenance.** Every execution decision, routing choice, and adaptation is logged. Barrier identity resolution determines trust level per request. Structured audit sinks provide a complete chain of custody.
- **Federated capability mesh.** Any .NET-capable host can join the mesh, advertise available capabilities and models, and serve execution requests from trusted peers. Trust tiers and routing policies control which peers may receive which workloads.

## Product Features

- **Setup wizard.** On first visit, the web portal guides you through provider configuration and a test prompt — no terminal required.
- **Copilot task flow.** Submit coding tasks via the portal or `POST /api/copilot/task` and receive output with an integrated audit trail. See `docs/CopilotMvpWalkthrough.md`.
- **Activity feed.** Background agent actions and system events surface in the portal as a live feed.
- **Changelog assistant.** Generate project change summaries from adaptation, pattern, and audit stores — in the portal or via `POST /api/changelog/generate`.
- **Strict mode.** Set `NEXO_STRICT_MODE=1` for fail-fast + verbose diagnostics during development. Flip to permissive for production. See `docs/Configuration.md`.
- **Centralized defaults.** All tunable constants live in `Nexo.Core.Domain.NexoDefaults` — no magic numbers scattered in the codebase.

## Quick Start

### One command (recommended)

From a cloned repo:

```bash
bash scripts/install/quickstart.sh
# Opens http://localhost:8080 — portal running with mock provider, no API keys needed.
```

The script detects Docker or .NET SDK, builds, and starts the portal. Works on Linux and macOS. Mock provider is enabled by default so the setup wizard and chat work immediately.

Stop with `docker stop nexo-quickstart` (Docker path) or `Ctrl+C` (native path).

### Other lanes

**Docker only (portal + API):**

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
docker build -f .docker/Dockerfile.quickstart -t nexo:quickstart .
docker run --rm -p 8080:8080 nexo:quickstart
# Open http://localhost:8080
```

**Docker CLI only (no portal):**

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

**Native SDK:**

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
bash scripts/install/install.sh --yes
NEXO_ALLOW_MOCK=1 dotnet run --project src/Nexo.API
# Open http://localhost:5000
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
| Analyze bricks | `dotnet run --project src/Nexo.CLI -- analyze bricks` |
| Run background-agent daemon mode | `dotnet run --project src/Nexo.CLI -- background-agent daemon --duration 10m` |
| Run pipeline template | `dotnet run --project src/Nexo.CLI -- pipeline run --template <file>` |
| Pipeline diagnostics | `dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json` |
| View trust boundary/audit | `dotnet run --project src/Nexo.CLI -- trust --help` |
| Trust dashboard | `dotnet run --project src/Nexo.CLI -- trust dashboard` |
| Apply a trust policy pack | `dotnet run --project src/Nexo.CLI -- trust pack apply --id strict-enterprise` |
| Run local test entrypoint | `dotnet run --project src/Nexo.CLI -- test local` |
| Portable tests | `dotnet run --project src/Nexo.CLI -- test portable --scope persistence` |
| Multi-environment tests | `dotnet run --project src/Nexo.CLI -- test multi-env --suite framework --all` |
| CI verification bundle | `dotnet run --project src/Nexo.CLI -- ci verify` |
| Onboarding doctor | `dotnet run --project src/Nexo.CLI -- doctor --json` |
| Orchestrate a request | `dotnet run --project src/Nexo.CLI -- orchestrate "<request>"` |
| Interactive chat | `dotnet run --project src/Nexo.CLI -- chat` |
| Runtime execute | `dotnet run --project src/Nexo.CLI -- runtime execute --runtime-manifest <file>` |
| Runtime release gate | `dotnet run --project src/Nexo.CLI -- runtime release-gate` |
| Workflow scaffold/stress | `dotnet run --project src/Nexo.CLI -- workflow scaffold` |
| Mesh sync/capabilities | `dotnet run --project src/Nexo.CLI -- mesh sync` |
| Escalation management | `dotnet run --project src/Nexo.CLI -- escalate list` |
| Metrics report | `dotnet run --project src/Nexo.CLI -- metrics report` |
| Self-extend preflight | `dotnet run --project src/Nexo.CLI -- self-extend preflight` |
| Observe/Adapt/Improve | `dotnet run --project src/Nexo.CLI -- observe` / `adapt` / `improve` |
| Config management | `dotnet run --project src/Nexo.CLI -- config show` |
| Docker management | `dotnet run --project src/Nexo.CLI -- docker build` / `run` / `clean` |
| Dogfood validation | `dotnet run --project src/Nexo.CLI -- dogfood all` |
| Compose pipelines | `dotnet run --project src/Nexo.CLI -- compose` |
| Changelog generation | `dotnet run --project src/Nexo.CLI -- changelog` |
| Maintenance cleanup | `dotnet run --project src/Nexo.CLI -- maintenance clean` |

## Demo Scripts (for rollout and live demos)

Use these when you need an end-to-end walkthrough without manually stitching commands together.

```bash
# high-signal demo flow (build, bootstrap, chat, orchestration, dogfood)
bash scripts/oh-shit-demo.sh --quick

# skip build if CLI is already built
bash scripts/oh-shit-demo.sh --quick --no-build

# unity sidecar demo (generate + compile validation + dogfood block1)
bash scripts/unity-sidecar-demo.sh run-demo --prompt "add a dash ability"

# supervisor loop (gameplay/combat/economy/ai) with iterative validation
bash scripts/unity-sidecar-demo.sh supervise --game "co-op dungeon crawler" --iterations 2
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

# Ollama in Docker only (models in a named volume; use with host-run Nexo — see scripts/run-ollama-docker.ps1)
docker compose -f docker-compose.ollama.yml up -d

# One-shot dev: Docker Ollama → wait for health → run Nexo.API (Windows / PowerShell)
# powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-nexo-api-dev.ps1 -Pull   # first time or new model
# powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-nexo-api-dev.ps1
# Phone on same Wi‑Fi: add -ListenLan (Windows) or --listen-lan (bash); open http://<pc-ip>:8080 — allow firewall
# Linux/macOS/WSL: bash scripts/start-nexo-api-dev.sh --pull
# Stop Ollama: scripts/stop-nexo-api-dev.ps1 | scripts/stop-nexo-api-dev.sh

# start ephemeral Ollama container (and optional Postgres profile)
docker compose -f docker-compose.ephemeral.yml up ollama
docker compose -f docker-compose.ephemeral.yml --profile db up postgres

# start self-hosted director portal (API + dailies + Ollama)
docker compose -f docker-compose.portal.yml up -d --build

# full portal stack: mounted workspace + background agents (see apps/runtime-studio README)
docker compose -f docker-compose.agent-server.yml up -d --build
```

Notes:
- `docker-compose.test.yml` mounts `./test-results` and runs `BaseFrameworkSmokeTests` (`net9.0`) in container, writing log/summary artifacts there.
- `docker-compose.ephemeral.yml` is for disposable local orchestration dependencies.
- **Portal stacks:** `docker-compose.portal.yml` = Director UI + API + Ollama. `docker-compose.agent-server.yml` = same plus **mounted repo** + default background agents from `apps/runtime-studio/config/agent_set.local.json`. Relationship diagram: `apps/runtime-studio/README.md#how-runtime-studio-fits-with-nexo-api`. Docker tuning: `docs/SelfHostedAgentServer.md`, `docs/config/agent-server.env.example`.

## Providers

LLM/vision routing is provider-based:

| Provider | Notes |
|----------|-------|
| `offline`, `mock`, `mock-json`, `echo` | deterministic/local-friendly paths (require `NEXO_ALLOW_MOCK=1`) |
| `local` | in-process ONNX/LLamaSharp; requires `NEXO_LOCAL_MODEL_PATH` |
| `ollama` | local model runtime (`OLLAMA_BASE_URL`, `OLLAMA_MODEL`) |
| `openai` | requires `OPENAI_API_KEY` |
| `azure` | requires `AZURE_OPENAI_*` settings |
| `video` | SmolVLM2-Video in Docker; requires `VIDEO_SERVICE_URL` |

## Project Layout

```text
Nexo/
├── src/
│   ├── Nexo.CLI/                 # CLI surface (System.CommandLine)
│   ├── Nexo.API/                 # ASP.NET Core host with REST endpoints
│   ├── Nexo.Hosting/             # AddNexo() integration entrypoint
│   ├── Nexo.Sdk/                 # Client SDK registration (AddNexoSdk)
│   ├── Nexo.Client/              # HTTP client (INexoClient)
│   ├── Nexo.Infrastructure/      # execution, persistence, adapters, mesh
│   ├── Nexo.Orchestration/       # orchestrator, routing, coordination
│   ├── Nexo.Runtime/             # runtime services and barrier plumbing
│   ├── Nexo.BackgroundAgents/    # scheduler, RAG, web search, trust, tools
│   ├── Nexo.Core.Application/    # use cases and ports
│   ├── Nexo.Core.Domain/         # domain model
│   ├── Nexo.Abstractions/        # shared interfaces (IAgent, IModel, etc.)
│   ├── Nexo.Brick.Contracts/     # brick extension contracts
│   ├── Nexo.Transport.Grpc*/     # gRPC transport layer
│   └── Nexo.Tests.*/             # test suites
├── apps/                         # application configs (runtime-studio, release-manager)
├── config/                       # trust policy packs (air-gapped, internal-only, strict-enterprise)
├── docs/                         # docs, specs, guides
├── scripts/                      # setup, install, demos, onboarding
├── .docker/                      # docker test/runtime definitions
├── .github/                      # CI workflows and templates
├── global.json                   # SDK pin (.NET 9)
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
- `apps/runtime-studio/README.md` – Runtime Studio agent-set JSON; hub for CLI vs compose vs Director portal (see “How this fits” there).

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
