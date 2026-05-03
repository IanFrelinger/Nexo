# Nexo

Private, traceable AI computing with self-extending capabilities.

Nexo is a .NET platform for composable AI workflows with structural trust enforcement. Capabilities are modular components with standardized contracts that compose into pipelines. The platform autonomously generates new components when it identifies capability gaps, validates them against regression, and promotes them under configurable policy constraints. All generated output follows enforced standards and remains readable, auditable, and extractable from the core.

Nexo operates entirely on infrastructure you control. Cloud providers are opt-in execution targets, not dependencies. No data leaves the host unless explicitly routed to a trusted peer. Air-gapped deployment is supported without modification.

**Primary development** uses the **Dev Container** (`.devcontainer/`): open the repo in Cursor or VS Code, **Reopen in Container**, then use **`dotnet`** in the integrated terminal—no host-installed .NET SDK.

**Deployment** defaults to **containers** only: published images on **GHCR**, `Dockerfile.*` under `.docker/`, and **`docker-compose*.yml`** stacks, plus the **Nexo CLI** inside those environments. Shell installers under `scripts/install/` and `scripts/setup/` are **escape hatches** when Docker is impossible.

Repository: <https://github.com/IanFrelinger/Nexo>

Architecture notes for contributors and reviewers: **`docs/architecture/`** (trust boundaries, testing model, .NET SDK vs. target frameworks).

**Production readiness (all audiences):** structured checklists and runbooks in **`docs/production-readiness/`** — use with **`docs/ProductionReadinessGate-v1.md`** and **`docs/DEPLOYMENT.md`**.

## Default workflow

1. **Develop** — [Quick Start (5 minutes)](#quick-start-5-minutes) → **Lane A** → **Dev Container** (first subsection).
2. **Deploy / operate** — [Deploy (operators)](#deploy-operators) (GHCR + compose).
3. **No Docker** — **Lane B** (native SDK + `scripts/setup/*` / `scripts/install/*`).

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

## Quick Start (5 minutes)

Choose your lane (recommended):

### Lane A: dev container + container deployment (recommended)

**Local development** should be the **Dev Container** below. **Running Nexo as a service** (portal, agent server, CI) uses the same container discipline: **compose** and/or **GHCR** images—see [Deploy (operators)](#deploy-operators). Use the **CLI** (`dotnet run --project src/Nexo.CLI`) for builds, validation, and operators inside the dev container or a mounted workspace in `docker run`.

#### 1) Prerequisites (container-first path)

- **Docker** (Desktop or Engine) and **Git**
- Optional: Ollama/OpenAI/Azure credentials for live model backends

You do **not** need a host-installed .NET SDK for the paths below. Install **.NET SDK 9.x** only if you choose the native escape hatch (see end of this section).

#### 2) Recommended: Dev Container (Cursor / VS Code)

1. Install the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension.
2. Open this repository, then run **Dev Containers: Reopen in Container**.

The dev container uses **.NET 9**, mounts a named volume at `nexo-nuget-packages` for the NuGet cache, and runs **`.devcontainer/post-create.sh`** after the container is created. That script restores the **same setup-gate project graph** as `scripts/docker-restore.ps1` (not full `Nexo.sln`, which requires MAUI/Android workloads inside a plain SDK image).

**Headless check (no IDE):** from the repo root, with Docker running:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Verify-DevContainer.ps1
```

That is the same restore + `Nexo.CLI` build + `--help` smoke CI runs in `devcontainer-gate.yml`—one script, no extra shell toolchain on Windows.

From the integrated terminal inside the container:

```bash
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project src/Nexo.CLI -- --help
```

**Remote SSH:** connect to a host that already has Docker (or your chosen toolchain), open the repo there, then use **Reopen in Container** on the remote so the environment still comes from the image.

#### 3) Run the portal (Docker quickstart image)

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
docker build -f .docker/Dockerfile.quickstart -t nexo:quickstart .
docker run --rm -p 8080:8080 nexo:quickstart
# Open http://localhost:8080 — mock provider; no API keys required.
```

Stop with `Ctrl+C` or `docker stop` on the container ID.

#### 4) Published CLI image (CI, agents, minimal host)

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

Validate a pipeline template from your workspace:

```bash
docker run --rm \
  -v "$PWD:/work" \
  -w /work \
  ghcr.io/ianfrelinger/nexo-cli:latest \
  pipeline validate --template /work/path/to/template.json
```

Build a local CLI image (optional):

```bash
docker build -f .docker/Dockerfile.cli -t nexo-cli:local .
docker run --rm nexo-cli:local --help
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

#### 5) Compose stacks (director portal, agent server, dependencies)

For multi-service deployment on a host you control, start from the files in the repository root (`docker-compose.portal.yml`, `docker-compose.agent-server.yml`, `docker-compose.ephemeral.yml`, etc.) and the operator guides in `docs/SelfHostedAgentServer.md` and `apps/runtime-studio/README.md`.

### Lane B: full local dev path (native SDK)

#### 6) Escape hatches (no Docker)

Use these only when containers are not an option:

- **One command portal (script):** `bash scripts/install/quickstart.sh` — detects Docker or a local SDK, builds, starts the portal.
- **Native SDK + setup (no repo clone installer):** `bash scripts/setup/setup.sh all` then `dotnet build` / `dotnet run` — or use **`scripts/docker-restore.ps1`** on Windows without a local SDK.
- **Cross-platform setup / restore helpers:** on Linux/macOS use **`scripts/setup/setup-unix.sh`** (POSIX args or PowerShell-style flags such as `-Mode check`); **`scripts/setup/setup.sh`** forwards there. On Windows use **`scripts/setup/setup.ps1`**. Same graph CI validates in `mcr.microsoft.com/dotnet/sdk:9.0`.
- **Windows + Docker without host SDK:** `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\docker-restore.ps1` (optional `-Build`).

Container bootstrap (install Docker if needed, pull images, smoke-run):

```bash
bash scripts/install/container-bootstrap.sh --yes
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\container-bootstrap.ps1 -Yes
```

#### 7) Clone and build (native SDK only)

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

#### 8) Confirm CLI is working

```bash
dotnet run --project src/Nexo.CLI -- --help
```

#### 8b) Run onboarding doctor (single pass/fail report)

```bash
dotnet run --project src/Nexo.CLI -- doctor --json
```

#### 9) Run a first high-signal command

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

## Security Defaults

Out of the box, Nexo runs on **HTTP only** with **no authentication** on API endpoints. This is intentional for local development — the default `ExposureProfile` is `Localhost`.

For any network-exposed deployment:

```bash
# Set API key auth for mutating endpoints:
export Nexo__Security__AuthorizationMode=ApiKey
export Nexo__Security__ApiKey=your-secret-key
export Nexo__Security__AuthorizationScope=AllApi

# Or use bearer token:
export Nexo__Security__AuthorizationMode=BearerToken
export Nexo__Security__BearerToken=your-token
```

For HTTPS, configure `ASPNETCORE_URLS=https://+:8443` with a certificate, or place Nexo behind a reverse proxy (nginx, Caddy, Traefik).

See `docs/Configuration.md` for all security options and `docs/TailscaleAndNexo.md` for Tailnet deployment.

## Deploy (operators)

Ship Nexo from **published container images** and **compose** files. Do not rely on host-native installers for production paths.

**Images**

| Image | Use |
|-------|-----|
| `ghcr.io/ianfrelinger/nexo-cli:latest` | Automation, agents, `docker run` with a workspace mount for `pipeline validate`, `ci verify`, etc. |
| Build from `.docker/Dockerfile.quickstart` | Single-container API + portal (mock-friendly smoke). |
| Build from `.docker/Dockerfile.api` | API image used by compose stacks (see `docker-compose.portal.yml`). |

**Compose (recommended stacks)**

| File | Purpose |
|------|---------|
| `docker-compose.portal.yml` | Director portal + `nexo-api` + Ollama on a host you control. |
| `docker-compose.agent-server.yml` | Same lineage plus **mounted workspace** and default background agents (`apps/runtime-studio/config/agent_set.local.json`). |

```bash
# Director + API + Ollama (localhost bindings; tune for your exposure profile)
docker compose -f docker-compose.portal.yml up -d --build

# Full Runtime Studio agent-server stack
docker compose -f docker-compose.agent-server.yml up -d --build
```

Copy and adjust env from `docs/config/agent-server.env.example`. Operator runbooks: `docs/SelfHostedAgentServer.md`, `apps/runtime-studio/README.md`. Readiness: `docs/ProductionReadinessGate-v1.md` and workflow `.github/workflows/production-readiness-gate-v1.yml`.

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

## Docker Compose (CI and local extras)

Production-oriented compose is under **[Deploy (operators)](#deploy-operators)** above. Additional files are for **tests** and **local dependencies**:

CI validation:

- `.github/workflows/compose-gate.yml` — compose-based checks (run manually: `gh workflow run "Compose Gate" --ref <branch>`). See `.github/workflows/README.md`.

```bash
# Ubuntu test service → ./test-results
docker compose -f docker-compose.test.yml up --build test-ubuntu

docker compose -f docker-compose.ollama.yml up -d
docker compose -f docker-compose.ephemeral.yml up ollama
docker compose -f docker-compose.ephemeral.yml --profile db up postgres
```

Notes:

- `docker-compose.test.yml` runs `BaseFrameworkSmokeTests` in container.
- `docker-compose.ephemeral.yml` is for disposable local orchestration.
- Relationship diagram for portal vs agent-server: `apps/runtime-studio/README.md#how-runtime-studio-fits-with-nexo-api`.

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
├── scripts/                      # setup, install, demos, onboarding (escape hatches + CI)
├── .devcontainer/                # default Dev Container (Cursor / VS Code)
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
- `README.md` → **[Deploy (operators)](#deploy-operators)** — GHCR images and compose stacks for production-style hosts.
- `docs/CiFirstHardwareSecond.md` — run **Setup Smoke Suite** in GitHub Actions before iterating on slow target hardware.
- `docs/GettingStarted.md` – guided first-hour setup and usage
- `docs/DocsIndex.md` – where to find docs by task
- `apps/runtime-studio/README.md` – Runtime Studio agent-set JSON; hub for CLI vs compose vs Director portal (see “How this fits” there).

Core references:
- `docs/CiFirstHardwareSecond.md` – CI smoke before target-hardware setup loops
- `docs/Configuration.md` – environment/config options
- `docs/Architecture.md` – architecture and subsystem overview
- `docs/Testing.md` – test strategy, guard rails, and commands
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
