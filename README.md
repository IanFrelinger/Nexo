# Nexo

Nexo is an **adaptive orchestration framework** for private, auditable software workflows. It watches how teams build, test, release, and operate software; learns repeatable patterns; and improves automations over time with built-in privacy controls such as pause/resume, local-first model routing, policy gates, and audit trails.

**ChatGPT is a calculator; Nexo is an autopilot panel.** A calculator answers the prompt in front of it. An autopilot panel observes the whole flight, keeps the route visible, applies policy, hands control back to the operator, and records what happened.

In this repo, that panel is a .NET runtime plus deployable hosts and app-level configurations: kernel libraries, observe/adapt/improve loops, mesh/federation, gRPC transport, AWS ingress, `Nexo.CLI`, `Nexo.API`, four `apps/` configurations, and NuGet/GHCR/compose distribution paths.

Repository: <https://github.com/IanFrelinger/Nexo>

## Where to start

| Reader | Start here | First command or artifact |
|--------|------------|---------------------------|
| **Evaluator** | [Quick Start](#quick-start-5-minutes), then [`docs/GettingStarted.md`](docs/GettingStarted.md) | `dotnet run --project application/src/Nexo.CLI -- doctor --json` or the quickstart Docker image |
| **Contributor** | [`docs/ProjectTiers.md`](docs/ProjectTiers.md) — canonical repo map, then [`CONTRIBUTING.md`](CONTRIBUTING.md) | `dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore` |
| **Integrator** | [`docs/DistributionModels.md`](docs/DistributionModels.md), [`docs/sdk.md`](docs/sdk.md), [`docs/SdkIntegrationGuide.md`](docs/SdkIntegrationGuide.md) | NuGet host embed, `Nexo.Client`, HTTP API, CLI image, compose, or source integration |

## Scope in 30 seconds

- **Kernel:** observe → adapt → improve loops, component/brick contracts, policy, persistence, orchestration, runtime routing, background agents.
- **Trust:** data classification, sanitization before cloud calls, barrier identity resolution, local-first defaults, pause/resume controls, structured audit sinks.
- **Mesh:** peer discovery, capability advertisement, director/hub flows, trust-tier placement, virtual labs, and phase docs for federation.
- **Transport and ingress:** optional gRPC transport plus AWS SNS and DynamoDB ingress adapters.
- **Hosts:** `application/src/Nexo.CLI` (`nexo`) and `application/src/Nexo.API` (ASP.NET Core HTTP/portal host).
- **Apps:** `apps/game-director`, `apps/nexo-forge`, `apps/release-manager`, and `apps/runtime-studio` are application-level agent-set/configuration surfaces.
- **Distribution:** NuGet packages (`Nexo.Hosting`, bundles, SDK/client/lite/runtime packages), GHCR images, Dockerfiles, compose stacks, and source/monorepo integration.

For the canonical tier-by-tier project map, see [`docs/ProjectTiers.md`](docs/ProjectTiers.md). For distribution channels and their validation gates, see [`docs/DistributionModels.md`](docs/DistributionModels.md).

## Subsystem map

| Area | What it contains | Where to look |
|------|------------------|---------------|
| Kernel spine | Abstractions, core/domain/application, contracts, policies, infrastructure, orchestration, background agents, hosting | [`src/`](src/), [`docs/ProjectTiers.md`](docs/ProjectTiers.md) |
| Observe/adapt/improve | Pattern observation, analysis, adaptation, self-improvement, changelog, dogfood gates | [`docs/GapAnalysis.md`](docs/GapAnalysis.md), [`docs/DogfoodValidation.md`](docs/DogfoodValidation.md) |
| Mesh/federation | Mesh phases, virtual lab, friend mesh prefab, trust-tier placement, leases/checkpoints | [`docs/MeshPhase0NorthStar.md`](docs/MeshPhase0NorthStar.md), [`docs/MeshVirtualLab.md`](docs/MeshVirtualLab.md) |
| gRPC transport | Transport contracts, server, standalone host | `src/Nexo.Transport.Grpc*` |
| AWS ingress | SNS and DynamoDB adapters | `src/Nexo.Ingress.AwsSns`, `src/Nexo.Ingress.DynamoDb`, [`docs/MiddlewareIngress.md`](docs/MiddlewareIngress.md) |
| App surfaces | Game Director, Nexo Forge, Release Manager, Runtime Studio agent sets and operator scripts | [`apps/`](apps/), [`docs/GameDirectorStudio.md`](docs/GameDirectorStudio.md), [`apps/runtime-studio/README.md`](apps/runtime-studio/README.md) |
| Trust architecture | Barrier identity, data sensitivity, audit, policy packs, local-first controls | [`docs/TrustAndInformationArchitecture.md`](docs/TrustAndInformationArchitecture.md), [`docs/Architecture.md`](docs/Architecture.md) |
| Distribution | NuGet, HTTP/API, CLI image, compose, source, mesh/federation | [`docs/DistributionModels.md`](docs/DistributionModels.md), [`docs/RELEASE.md`](docs/RELEASE.md) |

## Default workflow

1. **Evaluate / develop** — [Quick Start](#quick-start-5-minutes) → **Lane A** → Dev Container or container-first commands.
2. **Deploy / operate** — [Deploy (operators)](#deploy-operators) for GHCR images and compose stacks.
3. **Integrate** — [`docs/DistributionModels.md`](docs/DistributionModels.md) for NuGet, HTTP, CLI, compose, source, and mesh.
4. **Understand repo shape** — [`docs/ProjectTiers.md`](docs/ProjectTiers.md) for the canonical tier map.

## Why Nexo

- **Adaptive orchestration.** Nexo observes workflow signals, composes repeatable automations, and improves them under policy rather than treating every prompt as an isolated one-off.
- **Operator control.** Pause observation, keep execution local-first, route work by trust tier, and make every automation inspectable.
- **Data sovereignty.** Cloud providers are opt-in execution targets, not dependencies. Air-gapped and self-hosted deployments are first-class.
- **Traceability.** Decisions, routing, sanitization, adaptation, and promoted outputs are recorded for review.
- **Composable distribution.** Use the kernel via NuGet, run the CLI/API directly, deploy containers/compose, or federate trusted peers through mesh.

## Quick Start (5 minutes)

Choose your lane (recommended):

### Lane A: dev container + container deployment (recommended)

**Local development** should use the Dev Container. **Running Nexo as a service** uses the same container discipline: quickstart image, GHCR CLI image, compose, and/or published API images.

#### 1) Prerequisites

- Docker (Desktop or Engine) and Git.
- Optional: Ollama/OpenAI/Azure credentials for live model backends.

You do **not** need a host-installed .NET SDK for the Dev Container or published container paths. Install .NET SDK 9.x only for the native escape hatch in Lane B.

#### 2) Dev Container (Cursor / VS Code)

1. Install the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension.
2. Open this repository.
3. Run **Dev Containers: Reopen in Container**.

From the integrated terminal:

```bash
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project application/src/Nexo.CLI -- --help
dotnet run --project application/src/Nexo.CLI -- doctor --json
```

Headless dev-container check:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Verify-DevContainer.ps1
```

#### 3) Run the portal quickstart image

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
docker build -f .docker/Dockerfile.quickstart -t nexo:quickstart .
docker run --rm -p 8080:8080 nexo:quickstart
# Open http://localhost:8080 — mock provider; no API keys required.
```

#### 4) Published CLI image

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

Build a local CLI image:

```bash
docker build -f .docker/Dockerfile.cli -t nexo-cli:local .
docker run --rm nexo-cli:local --help
```

#### 5) Compose stacks

For multi-service deployment on a host you control, start from the root compose files and operator guides:

| File | Purpose |
|------|---------|
| `docker-compose.portal.yml` | Director portal + `nexo-api` + Ollama. |
| `docker-compose.agent-server.yml` | Portal + API + Ollama + mounted workspace + default Runtime Studio agent set. |
| `docker-compose.game-director.yml` | Game Director sidecar and MCP-facing workflow. |
| `docker-compose.ephemeral.yml` | Disposable local dependencies for tests and labs. |

```bash
docker compose -f docker-compose.portal.yml up -d --build
docker compose -f docker-compose.agent-server.yml up -d --build
```

### Lane B: full local dev path (native SDK)

Use this only when containers are not an option.

#### 6) Native setup

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project application/src/Nexo.CLI -- --help
dotnet run --project application/src/Nexo.CLI -- doctor --json
```

Windows PowerShell:

```powershell
git clone https://github.com/IanFrelinger/Nexo.git
Set-Location Nexo
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project application/src/Nexo.CLI -- --help
dotnet run --project application/src/Nexo.CLI -- doctor --json
```

No-Docker install/bootstrap escape hatches:

- `bash scripts/install/quickstart.sh`
- `bash scripts/setup/setup.sh all`
- `bash scripts/setup/setup-unix.sh`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\docker-restore.ps1`

#### 7) Run a first high-signal command

```bash
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

dotnet run --project application/src/Nexo.CLI -- pipeline validate --template "$template_path"
dotnet run --project application/src/Nexo.CLI -- pipeline run --template "$template_path" --run-id quickstart-run --format-json
dotnet run --project application/src/Nexo.CLI -- pipeline diagnostics --format-json
```

`validate` can execute a broader architecture/test sweep and may be heavier on constrained hosts:

```bash
dotnet run --project application/src/Nexo.CLI -- validate
```

## Common CLI workflows

| Goal | Command |
|------|---------|
| Show all commands | `dotnet run --project application/src/Nexo.CLI -- --help` |
| Onboarding doctor | `dotnet run --project application/src/Nexo.CLI -- doctor --json` |
| Validate architecture/contracts | `dotnet run --project application/src/Nexo.CLI -- validate` |
| Analyze source/assemblies | `dotnet run --project application/src/Nexo.CLI -- analyze --path .` |
| Validate a pipeline | `dotnet run --project application/src/Nexo.CLI -- pipeline validate --template <file>` |
| Run a pipeline | `dotnet run --project application/src/Nexo.CLI -- pipeline run --template <file>` |
| Pipeline diagnostics | `dotnet run --project application/src/Nexo.CLI -- pipeline diagnostics --format-json` |
| Orchestrate a request | `dotnet run --project application/src/Nexo.CLI -- orchestrate "<request>"` |
| Interactive chat | `dotnet run --project application/src/Nexo.CLI -- chat` |
| Observe / adapt / improve | `dotnet run --project application/src/Nexo.CLI -- observe` / `adapt` / `improve` |
| Dogfood validation | `dotnet run --project application/src/Nexo.CLI -- dogfood all` |
| Trust dashboard | `dotnet run --project application/src/Nexo.CLI -- trust dashboard` |
| Apply a trust policy pack | `dotnet run --project application/src/Nexo.CLI -- trust pack apply --id strict-enterprise` |
| Background-agent daemon | `dotnet run --project application/src/Nexo.CLI -- background-agent daemon --duration 10m` |
| Runtime Studio status | `dotnet run --project application/src/Nexo.CLI -- runtime-studio status` |
| Mesh sync/capabilities | `dotnet run --project application/src/Nexo.CLI -- mesh sync` |
| gRPC/runtime execution | `dotnet run --project application/src/Nexo.CLI -- runtime execute --runtime-manifest <file>` |
| CI verification bundle | `dotnet run --project application/src/Nexo.CLI -- ci verify` |
| Release preflight | `dotnet run --project application/src/Nexo.CLI -- release preflight <semver>` |
| Trigger release workflow | `dotnet run --project application/src/Nexo.CLI -- release dispatch <semver> [--ref master]` |
| Metrics report | `dotnet run --project application/src/Nexo.CLI -- metrics report` |
| Config management | `dotnet run --project application/src/Nexo.CLI -- config show` |
| Docker management | `dotnet run --project application/src/Nexo.CLI -- docker build` / `run` / `clean` |
| Changelog generation | `dotnet run --project application/src/Nexo.CLI -- changelog` |
| Maintenance cleanup | `dotnet run --project application/src/Nexo.CLI -- maintenance clean` |

## Application surfaces

| App | What it is | First doc |
|-----|------------|-----------|
| `apps/game-director` | Self-hosted, MCP-exposed AI sidecar for game balance, map validation, and content generation. | [`apps/game-director/README.md`](apps/game-director/README.md) |
| `apps/nexo-forge` | Vertical agent-set configuration for adaptive multiplayer FPS prototyping. | [`apps/nexo-forge/README.md`](apps/nexo-forge/README.md) |
| `apps/release-manager` | Release-readiness automation agent set for repo monitoring, tests, SLO evidence, and reports. | [`apps/release-manager/README.md`](apps/release-manager/README.md) |
| `apps/runtime-studio` | Planner/worker Runtime Studio agent set and operator scripts hosted by CLI or API. | [`apps/runtime-studio/README.md`](apps/runtime-studio/README.md) |

## Deploy (operators)

Ship Nexo from published container images and compose files. Host-native scripts are escape hatches for development or constrained environments, not the default production path.

**Images**

| Image | Use |
|-------|-----|
| `ghcr.io/ianfrelinger/nexo-cli:latest` | Automation, agents, validation, release preflight, and mounted-workspace commands. |
| Build from `.docker/Dockerfile.quickstart` | Single-container API + portal smoke path with mock-friendly defaults. |
| Build from `.docker/Dockerfile.api` | API image used by compose stacks. |

**Compose**

```bash
# Director portal + API + Ollama
docker compose -f docker-compose.portal.yml up -d --build

# Full agent-server stack with mounted workspace and Runtime Studio config
docker compose -f docker-compose.agent-server.yml up -d --build
```

Operator runbooks and deployment references:

- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)
- [`docs/SelfHostedAgentServer.md`](docs/SelfHostedAgentServer.md)
- [`apps/runtime-studio/README.md`](apps/runtime-studio/README.md)
- [`docs/ProductionReadinessGate-v1.md`](docs/ProductionReadinessGate-v1.md)
- [`docs/CiFirstHardwareSecond.md`](docs/CiFirstHardwareSecond.md)
- [`docs/RELEASE.md`](docs/RELEASE.md)

## Providers

Model routing is provider-based and local-first by default.

| Provider | Notes |
|----------|-------|
| `offline`, `mock`, `mock-json`, `echo` | Deterministic/local-friendly paths; mock paths require explicit opt-in where applicable. |
| `local` | In-process local model path. |
| `ollama` | Local Ollama runtime (`OLLAMA_BASE_URL`, `OLLAMA_MODEL`). |
| `openai` | Requires `OPENAI_API_KEY`; use trust/sanitization controls for sensitive workloads. |
| `azure` | Requires `AZURE_OPENAI_*` settings. |
| `video` | Video model service path where configured. |

See [`docs/Configuration.md`](docs/Configuration.md).

## Project layout

The canonical repo map is [`docs/ProjectTiers.md`](docs/ProjectTiers.md). Use it to understand which projects are kernel spine, deployable hosts, distribution packages, optional transport/mesh, product satellites, and tests.

```text
Nexo/
├── src/                          # kernel spine, runtime, distribution, optional transport/ingress, tests
├── application/src/              # CLI/API hosts, Game Director projects, app tests
├── apps/                         # runtime-studio, nexo-forge, game-director, release-manager configs
├── docs/                         # architecture, operations, mesh, release, SDK, samples, runbooks
├── config/                       # trust policy packs
├── scripts/                      # setup, install, CI, release helpers
├── tools/                        # sidecars and repo tools
├── .devcontainer/
├── .docker/
├── .github/
├── Nexo.sln                      # full repository solution
├── Nexo.Kernel.sln               # kernel-focused solution
├── Nexo.Runtime.sln              # runtime-focused solution
├── Nexo.Core.slnf                # Tier 0 + CLI/API hosts
├── Nexo.LocalDevCore.slnf        # fast local CLI + core test slice
└── Nexo.PrimeTime.slnf           # selected high-signal test projects
```

## Testing

For this repo, prefer focused validation first, then broaden only when the changed area requires it.

```bash
# CLI smoke
dotnet run --project application/src/Nexo.CLI -- --help

# focused pipeline tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~Pipelines"

# broader local CLI test runner path
dotnet run --project application/src/Nexo.CLI -- test local
```

Testing strategy and guard rails:

- [`docs/Testing.md`](docs/Testing.md)
- [`docs/architecture/TestingModel.md`](docs/architecture/TestingModel.md)
- [`docs/architecture/TestingStrategyPivot-v1.md`](docs/architecture/TestingStrategyPivot-v1.md)

## Documentation map

Start here:

- [`docs/DocsIndex.md`](docs/DocsIndex.md) — documentation hub.
- [`docs/ProjectTiers.md`](docs/ProjectTiers.md) — canonical repo/project tier map.
- [`docs/GettingStarted.md`](docs/GettingStarted.md) — guided first-hour setup and first pipeline.
- [`docs/DistributionModels.md`](docs/DistributionModels.md) — NuGet, HTTP, CLI, compose, source, mesh distribution channels.
- [`docs/Architecture.md`](docs/Architecture.md) — architecture and subsystem overview.
- [`docs/Conventions.md`](docs/Conventions.md) — current code conventions and migration honesty.
- [`docs/CiGateInventory.md`](docs/CiGateInventory.md) — CI workflow inventory and consolidation recommendations.
- [`docs/TrustAndInformationArchitecture.md`](docs/TrustAndInformationArchitecture.md) — trust model, barriers, audit, sensitivity.
- [`docs/Configuration.md`](docs/Configuration.md) — environment/config options.
- [`docs/ProductionReadinessGate-v1.md`](docs/ProductionReadinessGate-v1.md) — production gate procedure.
- [`docs/RELEASE.md`](docs/RELEASE.md) — NuGet + GHCR release hub.

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

For HTTPS, configure `ASPNETCORE_URLS=https://+:8443` with a certificate, or place Nexo behind a reverse proxy such as nginx, Caddy, or Traefik.

See [`docs/Configuration.md`](docs/Configuration.md) for security options and [`docs/TailscaleAndNexo.md`](docs/TailscaleAndNexo.md) for Tailnet deployment.

## Barrier Identity Resolution Notes

- JWT barrier resolution reads pre-validated claims from host auth middleware.
- API keys are stored as SHA-256 hashes, not plaintext.
- Audit details never include full API key values.
- Trust policy packs (`strict-enterprise`, `internal-only`, `air-gapped`) can be listed, described, and applied through `nexo trust pack ...`.
- Observation can be paused and resumed through `nexo trust pause` / `nexo trust resume`.

## License

Nexo uses an open-core model: single-node, inspectable runtime/SDK/trust surfaces are Apache-2.0, while fleet-scale governance and vertical app packaging are commercial. See [LICENSE](LICENSE) for Apache-2.0 terms and [LICENSING.md](LICENSING.md) for the authoritative tier map and CI-enforced project boundary (`make dependency-boundary-gate`).
