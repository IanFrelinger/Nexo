# Nexo

Nexo is an **adaptive orchestration framework** for private, auditable software workflows. It watches how teams build, test, release, and operate software; learns repeatable patterns; and improves automations over time with built-in privacy controls such as pause/resume, local-first model routing, policy gates, and audit trails.

**ChatGPT is a calculator; Nexo is an autopilot panel.** A calculator answers the prompt in front of it. An autopilot panel observes the whole flight, keeps the route visible, applies policy, hands control back to the operator, and records what happened.

In this repo, that panel is a .NET 8 runtime plus deployable hosts and app-level configurations: kernel libraries, observe/adapt/improve loops, mesh/federation, gRPC transport, AWS ingress, `Nexo.CLI`, `Nexo.API`, four `apps/` configurations, source-buildable Dockerfiles, compose stacks, and source/monorepo integration.

## Quick Start (5 minutes)

Copy and paste this block from a machine with Git and Docker:

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
docker build -f .docker/Dockerfile.quickstart -t nexo:quickstart .
container_id="$(docker run -d --rm -p 8080:8080 nexo:quickstart)"
until curl -fsS http://localhost:8080/health >/dev/null; do sleep 2; done
curl -fsS http://localhost:8080/api/status
printf '\nSUCCESS: open http://localhost:8080 — you should see the Nexo portal.\n'
```

The quickstart image is built from this repository and runs `Nexo.API` with the mock provider enabled, so no model API keys are required for first success. Stop it when finished:

```bash
docker rm -f "$container_id"
```

## What is in this repo?

| Area | Where to look |
| --- | --- |
| Kernel spine | [`src/`](src/), [`docs/ProjectTiers.md`](docs/ProjectTiers.md) |
| CLI host | [`application/src/Nexo.CLI`](application/src/Nexo.CLI) |
| API + portal host | [`application/src/Nexo.API`](application/src/Nexo.API) |
| App configurations | [`apps/`](apps/) |
| Dockerfiles and compose | [`.docker/`](.docker/), `docker-compose.*.yml` |
| Documentation hub | [`docs/DocsIndex.md`](docs/DocsIndex.md) |

The current repo baseline is .NET SDK 8.x / `net8.0`; see [`docs/architecture/DotnetVersions.md`](docs/architecture/DotnetVersions.md).

## Choose a workflow

| Reader | Start here | First local command |
| --- | --- | --- |
| Evaluator | This README, then [`docs/GettingStarted.md`](docs/GettingStarted.md) | Quickstart block above |
| Contributor | [`CONTRIBUTING.md`](CONTRIBUTING.md), [`docs/ProjectTiers.md`](docs/ProjectTiers.md) | `dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore` |
| Integrator | [`docs/DistributionModels.md`](docs/DistributionModels.md), [`docs/sdk.md`](docs/sdk.md), [`docs/SdkIntegrationGuide.md`](docs/SdkIntegrationGuide.md) | `dotnet run --project application/src/Nexo.CLI -- --help` |
| Operator | [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md), [`docs/ProductionReadinessGate-v1.md`](docs/ProductionReadinessGate-v1.md) | Build from `.docker/Dockerfile.api` or a compose file |

## Local development

The recommended development path is the Dev Container:

1. Install the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).
2. Open this repository in Cursor or VS Code.
3. Run **Dev Containers: Reopen in Container**.

From the integrated terminal:

```bash
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project application/src/Nexo.CLI -- --help
dotnet run --project application/src/Nexo.CLI -- doctor --json
```

If you cannot use Docker, install the .NET SDK version from [`global.json`](global.json) and run:

```bash
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project application/src/Nexo.CLI -- --help
```

Native setup depth lives in [`docs/GettingStarted.md`](docs/GettingStarted.md).

## Build local container images

Published `latest` GHCR images are available for the CLI/API, and source-built images remain the reproducible path from this checkout. CLI/API image commands and compose entry points are documented in [`docs/GettingStarted.md`](docs/GettingStarted.md) and [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md). Version-pinned public release artifacts are tracked in [`docs/Roadmap.md`](docs/Roadmap.md).

After the `v0.1.0` tag release workflow completes, pin install commands to the versioned artifacts:

```bash
dotnet add package Nexo.Sdk --version 0.1.0
dotnet add package Nexo.Hosting.Bundle --version 0.1.0
docker pull ghcr.io/ianfrelinger/nexo-cli:0.1.0
docker pull ghcr.io/ianfrelinger/nexo-api:0.1.0
```

## Test and gate commands

```bash
dotnet build Nexo.sln -v minimal
dotnet test Nexo.LocalDevCore.slnf --blame-hang-timeout 120s --blame-hang-dump-type none
make kernel-gate
make dependency-boundary-gate
```

Testing strategy and CI workflow details live in [`docs/Testing.md`](docs/Testing.md), [`docs/architecture/TestingModel.md`](docs/architecture/TestingModel.md), and [`docs/CiGateInventory.md`](docs/CiGateInventory.md).

## Architecture and trust

- [`docs/Architecture.md`](docs/Architecture.md) — layered architecture.
- [`docs/TrustAndInformationArchitecture.md`](docs/TrustAndInformationArchitecture.md) — data sensitivity, sanitization, audit, and barriers.
- [`docs/Configuration.md`](docs/Configuration.md) — environment variables and configuration.
- [`docs/MiddlewareIngress.md`](docs/MiddlewareIngress.md) — SNS/DynamoDB ingress and SMS approval seams.
- [`docs/MeshPhase0NorthStar.md`](docs/MeshPhase0NorthStar.md) and [`docs/MeshVirtualLab.md`](docs/MeshVirtualLab.md) — mesh/federation direction and lab validation.

## Distribution status

Nexo currently supports source/monorepo use, local Docker builds, published `latest` CLI/API GHCR images, compose stacks, and local NuGet/package verification paths. Version-pinned NuGet and GHCR release artifacts are tracked for the release sprint in [`docs/Roadmap.md`](docs/Roadmap.md).

Nexo uses an open-core model: single-node, inspectable runtime/SDK/trust surfaces are Apache-2.0, while fleet-scale governance and vertical app packaging are commercial. See [LICENSE](LICENSE) and [LICENSING.md](LICENSING.md).
