# Nexo project tiers

This is the canonical repository map for contributors and reviewers. Use it with [`README.md`](../README.md) for orientation and [`DistributionModels.md`](DistributionModels.md) for how each surface is consumed or shipped.

The monorepo has **~62** `.csproj` projects. The **runnable product** is roughly **15 projects** (Tiers **0** and **1**): kernel libraries, distribution/SDK packs, and the two deployable hosts. Everything else is packaging, optional transport/mesh, product experiments, demos/samples, and tests.

Tiers depend **inward** only (satellites reference the spine, not the reverse). The **`layer-boundary`** CI gate enforces this.

## Tier 0 — kernel spine (libraries)

| Project | Role |
|---------|------|
| `Nexo.Abstractions` | Shared interfaces (`IAgent`, `IModel`, …) |
| `Nexo.Core` | Shared primitives |
| `Nexo.Core.Domain` | Domain model, `NexoDefaults` |
| `Nexo.Core.Application` | Use cases and ports |
| `Nexo.Contracts` | Cross-cutting contracts |
| `Nexo.Brick.Contracts` | Brick extension contracts |
| `Nexo.Policies` | Policy primitives |
| `Nexo.Infrastructure` | Execution, persistence, adapters |
| `Nexo.Orchestration` | Orchestrator, routing, coordination |
| `Nexo.BackgroundAgents` | Scheduler, RAG, tools |
| `Nexo.BackgroundAgents.HostRunners` | Host runner adapters |
| `Nexo.Adapters.Models` | Model adapter wiring |
| `Nexo.Hosting` | `AddNexo()` DI entrypoint |

## Tier 0b — deployable hosts

| Project | Role |
|---------|------|
| `application/src/Nexo.CLI` | **`nexo`** CLI entrypoint |
| `application/src/Nexo.API` | ASP.NET Core HTTP host |

The CLI project also references spine-adjacent packs: **`Nexo.Bricks.Owasp`**, **`Nexo.Policies.Dev`**, **`Nexo.Tools.Dev`** (policy/dev tooling, not part of the minimal Tier 0 graph).

## Tier 1 — distribution & SDK

| Project | Role |
|---------|------|
| `Nexo.Sdk` | Client SDK registration (`AddNexoSdk`) |
| `Nexo.Framework.Sdk` | Framework-facing SDK surface |
| `Nexo.Client` | HTTP client (`INexoClient`) |
| `Nexo.Lite` | Reduced surface distribution |
| `Nexo.Compat/` | Source-only polyfills (no `.csproj`; linked by consuming projects) |
| `Nexo.Hosting.Bundle` | Kernel + `Nexo.Hosting` metapackage |
| `Nexo.Runtime` | Runtime services, barriers, routing |
| `Nexo.Runtime.Bundle` | Kernel libraries without `Nexo.Hosting` |
| `Nexo.Tools.Assembly` | Agent assembly tooling |
| `ValidationUtilities` | Shared validation helpers |

## Tier 2 — transport & mesh (optional)

| Project | Role |
|---------|------|
| `Nexo.Transport.Grpc` | gRPC transport contracts |
| `Nexo.Transport.Grpc.Server` | gRPC server implementation |
| `Nexo.Transport.Grpc.Server.Host` | Standalone gRPC host |
| `Nexo.Ingress.AwsSns` | AWS SNS ingress adapter |
| `Nexo.Ingress.DynamoDb` | DynamoDB ingress adapter |

## Tier 3 — product surfaces & satellites

| Area | Projects / paths |
|------|------------------|
| Game Director | `commercial/src/Nexo.Commercial.GameDirector.Domain`, `Nexo.Commercial.GameDirector.Agents`, `Nexo.Commercial.GameDirector.Bricks`, `Nexo.Commercial.GameDirector.Host`, `Nexo.Commercial.GameDirector.Mcp` |
| Game domain | `commercial/src/Nexo.Commercial.GameDomain` |
| Fleet | `commercial/src/Nexo.Commercial.Fleet.Contracts`, `commercial/src/Nexo.Commercial.Fleet.Infrastructure` |
| App configs | `apps/runtime-studio`, `apps/nexo-forge`, `apps/game-director`, `apps/release-manager` |
| Tools | `tools/Nexo.UnitySidecarDemo`, `tools/ApplyFeedbackChanges` |
| Demos | `docs/demos/Nexo.Demos.Avalonia`, `Nexo.Demos.BlazorWeb`, `Nexo.Demos.ConsoleClient` |
| Samples | `docs/samples/*` (e.g. `StableSdkHostSample`, NuGet restore verify samples); commercial samples such as `commercial/samples/ForgeMapHostSample` live under `commercial/samples/` |

## Tier 4 — tests

| Project | Role |
|---------|------|
| `Nexo.Tests.Domain` | Domain unit tests |
| `Nexo.Tests.Application` | Application-layer tests |
| `Nexo.Tests.Infrastructure` | Infrastructure / pipeline tests |
| `Nexo.Tests.Orchestration` | Orchestration tests |
| `Nexo.Tests.BackgroundAgents` | Background agent tests |
| `Nexo.Tests.Contracts` | Contract tests |
| `Nexo.Tests.Kernel` | Kernel integration tests |
| `Nexo.Tests.Transport` | Transport tests |
| `Nexo.Ingress.AwsSns.Tests` | SNS ingress tests |
| `Nexo.Ingress.DynamoDb.Tests` | DynamoDB ingress tests |
| `application/src/Nexo.Tests.CLI` | CLI tests |
| `commercial/tests/Nexo.Commercial.Tests.GameDirector` | commercial Game Director tests |
| `commercial/tests/Nexo.Commercial.Tests.GameDomain` | commercial game domain tests |

## Minimal clone-to-run core

For a fast first build after clone, use the existing filter solution:

```bash
dotnet build Nexo.LocalDevCore.slnf
```

That graph includes `Nexo.CLI`, core domain/infrastructure tests, and related dependencies — enough for local dev smoke without restoring all of `Nexo.sln`.

## `Nexo.Core.slnf`

**`Nexo.Core.slnf`** at the repo root lists **Tier 0 + Tier 0b** only so a first compile builds the spine and hosts without distribution bundles, transport, demos, or test projects:

```bash
dotnet build Nexo.Core.slnf
```

Pack references pulled transitively by the CLI (`Nexo.Bricks.Owasp`, `Nexo.Policies.Dev`, `Nexo.Tools.Dev`) still restore when building the CLI; the filter omits them if you only need `dotnet build` on libraries first.

## See also

- **`README.md`** — Project Layout tree
- **`docs/architecture/runtime-vs-application.md`** — runtime vs application boundary
- **`docs/DistributionModels.md`** — consumption and CI gates per distribution path
