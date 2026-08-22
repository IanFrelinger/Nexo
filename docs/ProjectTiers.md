# Ashlar project tiers

This is the canonical repository map for contributors and reviewers. Use it with [`README.md`](../README.md) for orientation and [`DistributionModels.md`](DistributionModels.md) for how each surface is consumed or shipped.

The monorepo tracks **109** `.csproj` files (`git ls-files "*.csproj"`): **58** under `src/`, **3** under `application/`, **10** under `applications/`, **17** under `commercial/`, **7** under `docs/` (demos + samples), **6** under `samples/`, **3** under `spikes/`, and **5** under `tools/`. Everything outside `commercial/` is **open (Apache-2.0)**; the 17 `commercial/` projects are commercial (see [`LICENSING.md`](../LICENSING.md) and `make dependency-boundary-gate`). The **runnable open product** is roughly **17 projects** (Tiers **0** and **0b**): kernel libraries plus the two deployable hosts (`Ashlar.CLI`, `Ashlar.API`).

Tiers depend **inward** only (satellites reference the spine, not the reverse). The **`layer-boundary`** CI gate enforces the `src/` vs `application/` split and **`dependency-boundary`** enforces open -> commercial and `src/` -> `applications/` reference direction.

Every tracked `.csproj` file name must appear in this document — the **Onboarding Docs Guard** workflow (`.github/workflows/onboarding-docs-guard.yml`, step "Every tracked csproj is listed in docs/ProjectTiers.md") fails when a project is added without a row here.

## Tier 0 — kernel spine (libraries)

| Project | Role |
|---------|------|
| `src/Ashlar.Abstractions/Ashlar.Abstractions.csproj` | Shared interfaces (`IAgent`, `IModel`, …) |
| `src/Ashlar.Core.Domain/Ashlar.Core.Domain.csproj` | Domain model, `AshlarDefaults`, brick authoring base types |
| `src/Ashlar.Core.Application/Ashlar.Core.Application.csproj` | Use cases and ports |
| `src/Ashlar.Contracts/Ashlar.Contracts.csproj` | Cross-cutting HTTP request/response DTOs |
| `src/Ashlar.Brick.Contracts/Ashlar.Brick.Contracts.csproj` | Brick extension contracts and wire DTOs |
| `src/Ashlar.Certification.Contracts/Ashlar.Certification.Contracts.csproj` | Content-bound certification record verification (referenced by `Ashlar.Core.Application`) |
| `src/Ashlar.Policies/Ashlar.Policies.csproj` | Policy primitives |
| `src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj` | Execution, persistence, adapters; open `MeshLab` worker executor (polls commercial fleet director) |
| `src/Ashlar.Orchestration/Ashlar.Orchestration.csproj` | Orchestrator, routing, coordination |
| `src/Ashlar.BackgroundAgents/Ashlar.BackgroundAgents.csproj` | Scheduler, RAG, tools |
| `src/Ashlar.BackgroundAgents.HostRunners/Ashlar.BackgroundAgents.HostRunners.csproj` | Host runner adapters |
| `src/Ashlar.Adapters.Models/Ashlar.Adapters.Models.csproj` | Model adapter wiring |
| `src/Ashlar.AI.Pipeline/Ashlar.AI.Pipeline.csproj` | Microsoft.Extensions.AI governed chat pipeline (Ollama, local ONNX/LLamaSharp targets); referenced by `Ashlar.Hosting` |
| `src/Ashlar.Analyzers/Ashlar.Analyzers.csproj` | Roslyn rules for brick code (contract drift, constructor purity, determinism, analyzer-gate catalog); referenced as an analyzer by spine projects |
| `src/Ashlar.Hosting/Ashlar.Hosting.csproj` | `AddAshlar()` DI entrypoint |

> **Known limitation:** `AddAshlar(options => ...)` builds its own `IConfiguration` from **environment variables only** (`src/Ashlar.Hosting/AshlarServiceCollectionExtensions.cs`), so kernel options such as `Ashlar:Meai` and `Ashlar:Autonomy` bind from `Ashlar__X__Y` env vars, not from `appsettings.json`, in the API/CLI hosts.

## Tier 0b — deployable hosts

| Project | Role |
|---------|------|
| `application/src/Ashlar.CLI/Ashlar.CLI.csproj` | **`ashlar`** CLI entrypoint |
| `application/src/Ashlar.API/Ashlar.API.csproj` | ASP.NET Core HTTP host (also composes the MCP server/client and A2A server from Tier 2) |

The CLI project also references spine-adjacent packs: **`Ashlar.Bricks.Owasp`**, **`Ashlar.Policies.Dev`**, **`Ashlar.Tools.Dev`** (policy/dev tooling, not part of the minimal Tier 0 graph). The singular `application/` folder holds only these hosts and their tests; the plural [`applications/`](../applications/README.md) folder is a different thing (Tier 3a below).

## Tier 1 — distribution & SDK

| Project | Role |
|---------|------|
| `src/Ashlar.Sdk/Ashlar.Sdk.csproj` | Client SDK registration (`AddAshlarSdk`); slim client for Unity/Unreal/embedded use |
| `src/Ashlar.Framework.Sdk/Ashlar.Framework.Sdk.csproj` | Framework-facing SDK surface (HTTP client + kernel registration entry points) |
| `src/Ashlar.Client/Ashlar.Client.csproj` | HTTP client (`IAshlarClient`) |
| `src/Ashlar.Lite/Ashlar.Lite.csproj` | Reduced surface distribution for edge / air-gapped hosts |
| `src/Ashlar.Compat/` | Source-only polyfills and the shared `DomainBrick` global-using alias (no `.csproj`; linked by consuming projects) |
| `src/Ashlar.Hosting.Bundle/Ashlar.Hosting.Bundle.csproj` | Kernel + `Ashlar.Hosting` metapackage |
| `src/Ashlar.Runtime/Ashlar.Runtime.csproj` | Runtime services, barriers, routing |
| `src/Ashlar.Runtime.Bundle/Ashlar.Runtime.Bundle.csproj` | Kernel libraries without `Ashlar.Hosting` |
| `src/Ashlar.Tools.Assembly/Ashlar.Tools.Assembly.csproj` | Agent assembly tooling |
| `src/ValidationUtilities/ValidationUtilities.csproj` | Shared validation helpers (console tool) |

### Tier 1b — authoring, certification, brick and policy packs

| Project | Role |
|---------|------|
| `src/Ashlar.Authoring/Ashlar.Authoring.csproj` | Minimal package for code-authored bricks: `Brick` base types, execution contracts, host registration helpers (see [`AuthoringBricks.md`](AuthoringBricks.md)) |
| `src/Ashlar.Certification.State/Ashlar.Certification.State.csproj` | Attested state log binding: schema-bound mutable state with certified transition provenance |
| `src/Ashlar.Bricks.Owasp/Ashlar.Bricks.Owasp.csproj` | OWASP brick pack (referenced by the CLI) |
| `src/Ashlar.Bricks.SqlProfile/Ashlar.Bricks.SqlProfile.csproj` | SQL-profile brick pack built on `Ashlar.Authoring` (exercised by `Ashlar.Tests.Application`) |
| `src/Ashlar.Policies.Dev/Ashlar.Policies.Dev.csproj` | Development policy pack |
| `src/Ashlar.Tools.Dev/Ashlar.Tools.Dev.csproj` | Development tool pack |

## Tier 2 — transport, protocols, ingress (optional)

| Project | Role |
|---------|------|
| `src/Ashlar.Transport.Grpc/Ashlar.Transport.Grpc.csproj` | gRPC transport contracts |
| `src/Ashlar.Transport.Grpc.Server/Ashlar.Transport.Grpc.Server.csproj` | gRPC server implementation |
| `src/Ashlar.Transport.Grpc.Server.Host/Ashlar.Transport.Grpc.Server.Host.csproj` | Standalone gRPC host |
| `src/Ashlar.Mcp.Server/Ashlar.Mcp.Server.csproj` | Ashlar as an **MCP server** (tool exposure over stdio/HTTP) — [`ProtocolIntegration-MCP-A2A.md`](architecture/ProtocolIntegration-MCP-A2A.md) |
| `src/Ashlar.Mcp.Server.Host/Ashlar.Mcp.Server.Host.csproj` | Standalone stdio MCP host executable |
| `src/Ashlar.Mcp.Client/Ashlar.Mcp.Client.csproj` | Ashlar as an **MCP client** (allow-listed remote tools) — [`ProtocolIntegration-MCP-A2A.md`](architecture/ProtocolIntegration-MCP-A2A.md) |
| `src/Ashlar.Transport.A2A/Ashlar.Transport.A2A.csproj` | **A2A** client transport (`A2AAgentTransport : IAgentTransport`) — [`ProtocolIntegration-MCP-A2A.md`](architecture/ProtocolIntegration-MCP-A2A.md) |
| `src/Ashlar.Transport.A2A.Server/Ashlar.Transport.A2A.Server.csproj` | **A2A** server core mounted by `Ashlar.API` |
| `src/Ashlar.Ingress.AwsSns/Ashlar.Ingress.AwsSns.csproj` | AWS SNS ingress adapter |
| `src/Ashlar.Ingress.DynamoDb/Ashlar.Ingress.DynamoDb.csproj` | DynamoDB ingress adapter |

CI: `mcp-a2a-gate.yml` (push, path-filtered) and `grpc-transport-gate.yml` (push, path-filtered).

## Tier 3 — product surfaces & satellites

### Tier 3a — `applications/` (open products on the core, Apache-2.0)

Plural **`applications/`** holds open products built **on top of** the kernel: they reference `src/` and are never referenced by it (dependency-boundary check 4). Layout note: [`applications/README.md`](../applications/README.md); boundary rationale: [`architecture/runtime-vs-application.md`](architecture/runtime-vs-application.md).

| Project | Role |
|---------|------|
| `applications/Ashlar.Certification.Physical/Ashlar.Certification.Physical.csproj` | Physical-atom certificate schema, Ed25519 signing, standalone verification for digital-twin asset binding |
| `applications/Ashlar.Provenance.Graph/Ashlar.Provenance.Graph.csproj` | Neo4j-backed read-only certification provenance projection (CI: `provenance-graph-gate.yml`) |
| `applications/Ashlar.Spatial.Contracts/Ashlar.Spatial.Contracts.csproj` | Platform-agnostic spatial anchor contracts and headless fakes |
| `applications/Ashlar.Spatial.Runtime/Ashlar.Spatial.Runtime.csproj` | Certified atom pose binding (identity/pose seam) |
| `applications/Ashlar.Spatial.Multiplayer/Ashlar.Spatial.Multiplayer.csproj` | Host-authoritative scoped pose relay for LAN-local play |
| `applications/Ashlar.Spatial.Platform.ARKit/Ashlar.Spatial.Platform.ARKit.csproj` | ARKit `ISpatialAnchorProvider` (fails closed on headless hosts) |
| `applications/Ashlar.Spatial.Platform.VisionPro/Ashlar.Spatial.Platform.VisionPro.csproj` | visionOS WorldTracking `ISpatialAnchorProvider` (fails closed on headless hosts) |
| `applications/Ashlar.Spatial.Platform.XREAL/Ashlar.Spatial.Platform.XREAL.csproj` | XREAL NRSDK `ISpatialAnchorProvider` (fails closed on headless hosts) |
| `applications/Ashlar.Applications.Tests/Ashlar.Applications.Tests.csproj` | Tests for the applications above (physical atom, spatial) |
| `applications/Ashlar.Provenance.Graph.Tests/Ashlar.Provenance.Graph.Tests.csproj` | Provenance graph tests (in-memory; `Category=Integration` needs Neo4j) |

### Tier 3b — `commercial/` (not Apache-2.0)

| Area | Projects / paths |
|------|------------------|
| Game Director | `commercial/src/Ashlar.Commercial.GameDirector.Domain/GameDirector.Domain.csproj`, `commercial/src/Ashlar.Commercial.GameDirector.Agents/GameDirector.Agents.csproj`, `commercial/src/Ashlar.Commercial.GameDirector.Bricks/GameDirector.Bricks.csproj`, `commercial/src/Ashlar.Commercial.GameDirector.Host/GameDirector.Host.csproj`, `commercial/src/Ashlar.Commercial.GameDirector.Mcp/GameDirector.Mcp.csproj` |
| Game domain | `commercial/src/Ashlar.Commercial.GameDomain/Ashlar.Commercial.GameDomain.csproj` |
| Fleet | `commercial/src/Ashlar.Commercial.Fleet.Contracts/Ashlar.Commercial.Fleet.Contracts.csproj`, `commercial/src/Ashlar.Commercial.Fleet.Infrastructure/Ashlar.Commercial.Fleet.Infrastructure.csproj`, `commercial/src/Ashlar.Commercial.Fleet.Api/Ashlar.Commercial.Fleet.Api.csproj`, `commercial/src/Ashlar.Commercial.Fleet.Host/Ashlar.Commercial.Fleet.Host.csproj`, `commercial/src/Ashlar.Commercial.MeshDirector/Ashlar.Commercial.MeshDirector.csproj` |
| Commercial samples | `commercial/samples/ForgeMapHostSample/ForgeMapHostSample.csproj` |
| App configs (no `.csproj`) | `apps/runtime-studio`, `apps/ashlar-forge`, `apps/game-director`, `apps/release-manager` — agent-set / host configuration surfaces, listed as commercial in `LICENSING.md` |

### Tier 3c — demos, samples, tools, spikes (open)

| Area | Projects / paths |
|------|------------------|
| Demos (`Ashlar.Demos.sln`) | `docs/demos/Ashlar.Demos.Avalonia/Ashlar.Demos.Avalonia.csproj`, `docs/demos/Ashlar.Demos.BlazorWeb/Ashlar.Demos.BlazorWeb.csproj`, `docs/demos/Ashlar.Demos.ConsoleClient/Ashlar.Demos.ConsoleClient.csproj` |
| Docs samples (distribution proofs) | `docs/samples/StableSdkHostSample/StableSdkHostSample.csproj`, `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj`, `docs/samples/NugetOrgRestoreVerify/Ashlar.NugetOrgRestoreVerify.csproj`, `docs/samples/NugetOrgRestoreHostingOnly/Ashlar.NugetOrgRestoreHostingOnly.csproj` |
| Samples (`samples/`, see [`samples/README.md`](../samples/README.md)) | `samples/hello-brick/HelloBrick/HelloBrick.csproj`, `samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj`, `samples/templates/brick/__BrickName__Brick/__BrickName__Brick.csproj`, `samples/templates/brick/__BrickName__Brick.Tests/__BrickName__Brick.Tests.csproj` (token template copied by `ashlar new brick`), `samples/certified-brick-reuse/Ashlar.Certified.DamageResolver/Ashlar.Certified.DamageResolver.csproj`, `samples/certified-brick-reuse/ProjectB/ProjectB.csproj` |
| Tools (`tools/`) | `tools/Ashlar.CertifyBrick/Ashlar.CertifyBrick.csproj` (certify a brick from the CLI), `tools/Ashlar.ExportCertifiedBrick/ExportCertifiedBrick.csproj` (export a certified brick + record), `tools/Ashlar.Provenance.Demo/Ashlar.Provenance.Demo.csproj` (`ashlar-provenance-demo`, Neo4j walkthrough), `tools/Ashlar.UnitySidecarDemo/Ashlar.UnitySidecarDemo.csproj`, `tools/ApplyFeedbackChanges/ApplyFeedbackChanges.csproj` |
| Spikes (`spikes/`; evidence, not product) | `spikes/autonomy-first-flight/FirstFlight/FirstFlight.csproj` (trust-loop first flight runner), `spikes/portability/tools/GenerateProbeBrick/GenerateProbeBrick.csproj` and `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.csproj` (portability probe; see `spikes/portability/REPORT.md`) |

## Tier 4 — tests and test support

| Project | Role |
|---------|------|
| `src/Ashlar.Tests.Domain/Ashlar.Tests.Domain.csproj` | Domain unit tests |
| `src/Ashlar.Tests.Application/Ashlar.Tests.Application.csproj` | Application-layer tests |
| `src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj` | Infrastructure / pipeline tests; `WebApplicationFactory` tests live under `Tests/VirtualProduction/**` (net9-only) |
| `src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj` | Helper console project used by the infrastructure test scripts (not a test project) |
| `src/Ashlar.Tests.Orchestration/Ashlar.Tests.Orchestration.csproj` | Orchestration tests |
| `src/Ashlar.Tests.BackgroundAgents/Ashlar.Tests.BackgroundAgents.csproj` | Background agent tests |
| `src/Ashlar.Tests.Contracts/Ashlar.Tests.Contracts.csproj` | Contract tests |
| `src/Ashlar.Tests.Kernel/Ashlar.Tests.Kernel.csproj` | Kernel integration tests |
| `src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj` | gRPC transport tests |
| `src/Ashlar.Tests.AI.Pipeline/Ashlar.Tests.AI.Pipeline.csproj` | MEAI pipeline tests |
| `src/Ashlar.Analyzers.Tests/Ashlar.Analyzers.Tests.csproj` | Roslyn analyzer tests |
| `src/Ashlar.Mcp.Server.Tests/Ashlar.Mcp.Server.Tests.csproj` | MCP server tests |
| `src/Ashlar.Mcp.Client.Tests/Ashlar.Mcp.Client.Tests.csproj` | MCP client tests |
| `src/Ashlar.Transport.A2A.Tests/Ashlar.Transport.A2A.Tests.csproj` | A2A client transport tests |
| `src/Ashlar.Transport.A2A.Server.Tests/Ashlar.Transport.A2A.Server.Tests.csproj` | A2A server tests |
| `src/Ashlar.Ingress.AwsSns.Tests/Ashlar.Ingress.AwsSns.Tests.csproj` | SNS ingress tests |
| `src/Ashlar.Ingress.DynamoDb.Tests/Ashlar.Ingress.DynamoDb.Tests.csproj` | DynamoDB ingress tests |
| `src/Ashlar.Agents.TestKit/Ashlar.Agents.TestKit.csproj` | Library of agent fakes shared by the test projects (`IsTestProject=false`) |
| `application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj` | CLI tests |
| `commercial/tests/Ashlar.Commercial.Tests.GameDirector/Ashlar.Tests.GameDirector.csproj` | commercial Game Director tests |
| `commercial/tests/Ashlar.Commercial.Tests.GameDomain/Ashlar.Commercial.Tests.GameDomain.csproj` | commercial game domain tests |
| `commercial/tests/Ashlar.Commercial.Tests.Fleet/Ashlar.Commercial.Tests.Fleet.csproj` | commercial fleet/director tests |
| `commercial/tests/Ashlar.Commercial.Tests.Fleet.Host/Ashlar.Commercial.Tests.Fleet.Host.csproj` | commercial fleet host smoke tests |
| `commercial/tests/Ashlar.Commercial.Tests.MeshDirector/Ashlar.Commercial.Tests.MeshDirector.csproj` | commercial mesh director CLI tests |

`applications/` test projects are listed under Tier 3a.

## Which solution do I open?

The root holds several entry points; a bare `dotnet build` fails with MSB1011, so name one. None of the filters below pull `commercial/` except `Ashlar.PrimeTime.slnf` and `Ashlar.sln`.

| File | Open it when | Contains |
|------|--------------|----------|
| `Ashlar.Kernel.sln` | Kernel/library development without the hosts | Tier 0 spine, `Ashlar.Runtime`, gRPC transport, brick/policy packs, kernel test projects (23 projects; **no** `Ashlar.CLI` / `Ashlar.API`) |
| `Ashlar.Core.slnf` | First compile of spine + hosts | The 12 original spine libraries + the two hosts (14 projects; `Ashlar.Certification.Contracts`, `Ashlar.AI.Pipeline`, `Ashlar.Analyzers` restore transitively) |
| `Ashlar.LocalDevCore.slnf` | The CLI dev loop with core tests (`make restore-core` / `make build-core` / `make test-framework-prod-first`) | `Ashlar.CLI`, `Ashlar.Tests.Domain`, `Ashlar.Tests.Infrastructure` |
| `Ashlar.PrimeTime.slnf` | The ProdStyle test gate (`make test-prime-time`) | Eight test assemblies: Application, BackgroundAgents, CLI, Domain, Infrastructure, Orchestration, Transport **and** `commercial/tests/Ashlar.Commercial.Tests.GameDomain` — this filter deliberately spans open + commercial |
| `Ashlar.Runtime.sln` | Publishing the embeddable kernel graph (no `application/`) | Runtime libraries + `Ashlar.Tests.AI.Pipeline` (18 projects) |
| `application/Ashlar.Application.sln` | Application-gate style builds of the open hosts | `Ashlar.API`, `Ashlar.CLI`, `Ashlar.Tests.CLI` (open only) |
| `Ashlar.Demos.sln` | The three demo clients | `docs/demos/*` |
| `Ashlar.sln` | Everything the CI matrix builds on Linux | 78 projects: `src/` (except `Ashlar.Hosting.Bundle`, `ValidationUtilities` and the `copy-assemblies` helper), `application/`, `applications/`, 9 of the 17 `commercial/` projects (Game Director, GameDomain and three commercial test projects), `tools/Ashlar.Provenance.Demo`. Samples, spikes, the other tools, and the commercial Fleet/MeshDirector projects are built from their own paths |

## Minimal clone-to-run core

For a fast first build after clone, use the existing filter solution:

```bash
dotnet build Ashlar.LocalDevCore.slnf
```

That graph includes `Ashlar.CLI`, core domain/infrastructure tests, and related dependencies — enough for local dev smoke without restoring all of `Ashlar.sln`, and without compiling anything under `commercial/`.

## `Ashlar.Core.slnf`

**`Ashlar.Core.slnf`** at the repo root lists the original **Tier 0** spine libraries plus the **Tier 0b** hosts so a first compile builds the spine and hosts without distribution bundles, transport, demos, or test projects (spine-adjacent projects such as `Ashlar.Certification.Contracts`, `Ashlar.AI.Pipeline` and `Ashlar.Analyzers` restore transitively):

```bash
dotnet build Ashlar.Core.slnf
```

Pack references pulled transitively by the CLI (`Ashlar.Bricks.Owasp`, `Ashlar.Policies.Dev`, `Ashlar.Tools.Dev`) still restore when building the CLI; the filter omits them if you only need `dotnet build` on libraries first.

## See also

- **`README.md`** — Project Layout tree
- **`docs/architecture/runtime-vs-application.md`** — runtime vs application boundary
- **`docs/architecture/ProtocolIntegration-MCP-A2A.md`** — MCP + A2A adapter projects
- **`docs/DistributionModels.md`** — consumption and CI gates per distribution path
- **`applications/README.md`** — `application/` vs `applications/` vs `apps/`
