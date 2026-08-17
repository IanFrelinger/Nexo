# Nexo project tiers

This is the canonical repository map for contributors and reviewers. Use it with [`README.md`](../README.md) for orientation and [`DistributionModels.md`](DistributionModels.md) for how each surface is consumed or shipped.

The monorepo tracks **109** `.csproj` files (`git ls-files "*.csproj"`): **58** under `src/`, **3** under `application/`, **10** under `applications/`, **17** under `commercial/`, **7** under `docs/` (demos + samples), **6** under `samples/`, **3** under `spikes/`, and **5** under `tools/`. Everything outside `commercial/` is **open (Apache-2.0)**; the 17 `commercial/` projects are commercial (see [`LICENSING.md`](../LICENSING.md) and `make dependency-boundary-gate`). The **runnable open product** is roughly **17 projects** (Tiers **0** and **0b**): kernel libraries plus the two deployable hosts (`Nexo.CLI`, `Nexo.API`).

Tiers depend **inward** only (satellites reference the spine, not the reverse). The **`layer-boundary`** CI gate enforces the `src/` vs `application/` split and **`dependency-boundary`** enforces open -> commercial and `src/` -> `applications/` reference direction.

Every tracked `.csproj` file name must appear in this document — the **Onboarding Docs Guard** workflow (`.github/workflows/onboarding-docs-guard.yml`, step "Every tracked csproj is listed in docs/ProjectTiers.md") fails when a project is added without a row here.

## Tier 0 — kernel spine (libraries)

| Project | Role |
|---------|------|
| `src/Nexo.Abstractions/Nexo.Abstractions.csproj` | Shared interfaces (`IAgent`, `IModel`, …) |
| `src/Nexo.Core.Domain/Nexo.Core.Domain.csproj` | Domain model, `NexoDefaults`, brick authoring base types |
| `src/Nexo.Core.Application/Nexo.Core.Application.csproj` | Use cases and ports |
| `src/Nexo.Contracts/Nexo.Contracts.csproj` | Cross-cutting HTTP request/response DTOs |
| `src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj` | Brick extension contracts and wire DTOs |
| `src/Nexo.Certification.Contracts/Nexo.Certification.Contracts.csproj` | Content-bound certification record verification (referenced by `Nexo.Core.Application`) |
| `src/Nexo.Policies/Nexo.Policies.csproj` | Policy primitives |
| `src/Nexo.Infrastructure/Nexo.Infrastructure.csproj` | Execution, persistence, adapters; open `MeshLab` worker executor (polls commercial fleet director) |
| `src/Nexo.Orchestration/Nexo.Orchestration.csproj` | Orchestrator, routing, coordination |
| `src/Nexo.BackgroundAgents/Nexo.BackgroundAgents.csproj` | Scheduler, RAG, tools |
| `src/Nexo.BackgroundAgents.HostRunners/Nexo.BackgroundAgents.HostRunners.csproj` | Host runner adapters |
| `src/Nexo.Adapters.Models/Nexo.Adapters.Models.csproj` | Model adapter wiring |
| `src/Nexo.AI.Pipeline/Nexo.AI.Pipeline.csproj` | Microsoft.Extensions.AI governed chat pipeline (Ollama, local ONNX/LLamaSharp targets); referenced by `Nexo.Hosting` |
| `src/Nexo.Analyzers/Nexo.Analyzers.csproj` | Roslyn rules for brick code (contract drift, constructor purity, determinism, analyzer-gate catalog); referenced as an analyzer by spine projects |
| `src/Nexo.Hosting/Nexo.Hosting.csproj` | `AddNexo()` DI entrypoint |

> **Known limitation:** `AddNexo(options => ...)` builds its own `IConfiguration` from **environment variables only** (`src/Nexo.Hosting/NexoServiceCollectionExtensions.cs`), so kernel options such as `Nexo:Meai` and `Nexo:Autonomy` bind from `Nexo__X__Y` env vars, not from `appsettings.json`, in the API/CLI hosts.

## Tier 0b — deployable hosts

| Project | Role |
|---------|------|
| `application/src/Nexo.CLI/Nexo.CLI.csproj` | **`nexo`** CLI entrypoint |
| `application/src/Nexo.API/Nexo.API.csproj` | ASP.NET Core HTTP host (also composes the MCP server/client and A2A server from Tier 2) |

The CLI project also references spine-adjacent packs: **`Nexo.Bricks.Owasp`**, **`Nexo.Policies.Dev`**, **`Nexo.Tools.Dev`** (policy/dev tooling, not part of the minimal Tier 0 graph). The singular `application/` folder holds only these hosts and their tests; the plural [`applications/`](../applications/README.md) folder is a different thing (Tier 3a below).

## Tier 1 — distribution & SDK

| Project | Role |
|---------|------|
| `src/Nexo.Sdk/Nexo.Sdk.csproj` | Client SDK registration (`AddNexoSdk`); slim client for Unity/Unreal/embedded use |
| `src/Nexo.Framework.Sdk/Nexo.Framework.Sdk.csproj` | Framework-facing SDK surface (HTTP client + kernel registration entry points) |
| `src/Nexo.Client/Nexo.Client.csproj` | HTTP client (`INexoClient`) |
| `src/Nexo.Lite/Nexo.Lite.csproj` | Reduced surface distribution for edge / air-gapped hosts |
| `src/Nexo.Compat/` | Source-only polyfills (no `.csproj`; linked by consuming projects) |
| `src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj` | Kernel + `Nexo.Hosting` metapackage |
| `src/Nexo.Runtime/Nexo.Runtime.csproj` | Runtime services, barriers, routing |
| `src/Nexo.Runtime.Bundle/Nexo.Runtime.Bundle.csproj` | Kernel libraries without `Nexo.Hosting` |
| `src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj` | Agent assembly tooling |
| `src/ValidationUtilities/ValidationUtilities.csproj` | Shared validation helpers (console tool) |

### Tier 1b — authoring, certification, brick and policy packs

| Project | Role |
|---------|------|
| `src/Nexo.Authoring/Nexo.Authoring.csproj` | Minimal package for code-authored bricks: `Brick` base types, execution contracts, host registration helpers (see [`AuthoringBricks.md`](AuthoringBricks.md)) |
| `src/Nexo.Certification.State/Nexo.Certification.State.csproj` | Attested state log binding: schema-bound mutable state with certified transition provenance |
| `src/Nexo.Bricks.Owasp/Nexo.Bricks.Owasp.csproj` | OWASP brick pack (referenced by the CLI) |
| `src/Nexo.Bricks.SqlProfile/Nexo.Bricks.SqlProfile.csproj` | SQL-profile brick pack built on `Nexo.Authoring` (exercised by `Nexo.Tests.Application`) |
| `src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj` | Development policy pack |
| `src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj` | Development tool pack |

## Tier 2 — transport, protocols, ingress (optional)

| Project | Role |
|---------|------|
| `src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj` | gRPC transport contracts |
| `src/Nexo.Transport.Grpc.Server/Nexo.Transport.Grpc.Server.csproj` | gRPC server implementation |
| `src/Nexo.Transport.Grpc.Server.Host/Nexo.Transport.Grpc.Server.Host.csproj` | Standalone gRPC host |
| `src/Nexo.Mcp.Server/Nexo.Mcp.Server.csproj` | Nexo as an **MCP server** (tool exposure over stdio/HTTP) — [`ProtocolIntegration-MCP-A2A.md`](architecture/ProtocolIntegration-MCP-A2A.md) |
| `src/Nexo.Mcp.Server.Host/Nexo.Mcp.Server.Host.csproj` | Standalone stdio MCP host executable |
| `src/Nexo.Mcp.Client/Nexo.Mcp.Client.csproj` | Nexo as an **MCP client** (allow-listed remote tools) — [`ProtocolIntegration-MCP-A2A.md`](architecture/ProtocolIntegration-MCP-A2A.md) |
| `src/Nexo.Transport.A2A/Nexo.Transport.A2A.csproj` | **A2A** client transport (`A2AAgentTransport : IAgentTransport`) — [`ProtocolIntegration-MCP-A2A.md`](architecture/ProtocolIntegration-MCP-A2A.md) |
| `src/Nexo.Transport.A2A.Server/Nexo.Transport.A2A.Server.csproj` | **A2A** server core mounted by `Nexo.API` |
| `src/Nexo.Ingress.AwsSns/Nexo.Ingress.AwsSns.csproj` | AWS SNS ingress adapter |
| `src/Nexo.Ingress.DynamoDb/Nexo.Ingress.DynamoDb.csproj` | DynamoDB ingress adapter |

CI: `mcp-a2a-gate.yml` (push, path-filtered) and `grpc-transport-gate.yml` (push, path-filtered).

## Tier 3 — product surfaces & satellites

### Tier 3a — `applications/` (open products on the core, Apache-2.0)

Plural **`applications/`** holds open products built **on top of** the kernel: they reference `src/` and are never referenced by it (dependency-boundary check 4). Layout note: [`applications/README.md`](../applications/README.md); boundary rationale: [`architecture/runtime-vs-application.md`](architecture/runtime-vs-application.md).

| Project | Role |
|---------|------|
| `applications/Nexo.Certification.Physical/Nexo.Certification.Physical.csproj` | Physical-atom certificate schema, Ed25519 signing, standalone verification for digital-twin asset binding |
| `applications/Nexo.Provenance.Graph/Nexo.Provenance.Graph.csproj` | Neo4j-backed read-only certification provenance projection (CI: `provenance-graph-gate.yml`) |
| `applications/Nexo.Spatial.Contracts/Nexo.Spatial.Contracts.csproj` | Platform-agnostic spatial anchor contracts and headless fakes |
| `applications/Nexo.Spatial.Runtime/Nexo.Spatial.Runtime.csproj` | Certified atom pose binding (identity/pose seam) |
| `applications/Nexo.Spatial.Multiplayer/Nexo.Spatial.Multiplayer.csproj` | Host-authoritative scoped pose relay for LAN-local play |
| `applications/Nexo.Spatial.Platform.ARKit/Nexo.Spatial.Platform.ARKit.csproj` | ARKit `ISpatialAnchorProvider` (fails closed on headless hosts) |
| `applications/Nexo.Spatial.Platform.VisionPro/Nexo.Spatial.Platform.VisionPro.csproj` | visionOS WorldTracking `ISpatialAnchorProvider` (fails closed on headless hosts) |
| `applications/Nexo.Spatial.Platform.XREAL/Nexo.Spatial.Platform.XREAL.csproj` | XREAL NRSDK `ISpatialAnchorProvider` (fails closed on headless hosts) |
| `applications/Nexo.Applications.Tests/Nexo.Applications.Tests.csproj` | Tests for the applications above (physical atom, spatial) |
| `applications/Nexo.Provenance.Graph.Tests/Nexo.Provenance.Graph.Tests.csproj` | Provenance graph tests (in-memory; `Category=Integration` needs Neo4j) |

### Tier 3b — `commercial/` (not Apache-2.0)

| Area | Projects / paths |
|------|------------------|
| Game Director | `commercial/src/Nexo.Commercial.GameDirector.Domain/GameDirector.Domain.csproj`, `commercial/src/Nexo.Commercial.GameDirector.Agents/GameDirector.Agents.csproj`, `commercial/src/Nexo.Commercial.GameDirector.Bricks/GameDirector.Bricks.csproj`, `commercial/src/Nexo.Commercial.GameDirector.Host/GameDirector.Host.csproj`, `commercial/src/Nexo.Commercial.GameDirector.Mcp/GameDirector.Mcp.csproj` |
| Game domain | `commercial/src/Nexo.Commercial.GameDomain/Nexo.Commercial.GameDomain.csproj` |
| Fleet | `commercial/src/Nexo.Commercial.Fleet.Contracts/Nexo.Commercial.Fleet.Contracts.csproj`, `commercial/src/Nexo.Commercial.Fleet.Infrastructure/Nexo.Commercial.Fleet.Infrastructure.csproj`, `commercial/src/Nexo.Commercial.Fleet.Api/Nexo.Commercial.Fleet.Api.csproj`, `commercial/src/Nexo.Commercial.Fleet.Host/Nexo.Commercial.Fleet.Host.csproj`, `commercial/src/Nexo.Commercial.MeshDirector/Nexo.Commercial.MeshDirector.csproj` |
| Commercial samples | `commercial/samples/ForgeMapHostSample/ForgeMapHostSample.csproj` |
| App configs (no `.csproj`) | `apps/runtime-studio`, `apps/nexo-forge`, `apps/game-director`, `apps/release-manager` — agent-set / host configuration surfaces, listed as commercial in `LICENSING.md` |

### Tier 3c — demos, samples, tools, spikes (open)

| Area | Projects / paths |
|------|------------------|
| Demos (`Nexo.Demos.sln`) | `docs/demos/Nexo.Demos.Avalonia/Nexo.Demos.Avalonia.csproj`, `docs/demos/Nexo.Demos.BlazorWeb/Nexo.Demos.BlazorWeb.csproj`, `docs/demos/Nexo.Demos.ConsoleClient/Nexo.Demos.ConsoleClient.csproj` |
| Docs samples (distribution proofs) | `docs/samples/StableSdkHostSample/StableSdkHostSample.csproj`, `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj`, `docs/samples/NugetOrgRestoreVerify/Nexo.NugetOrgRestoreVerify.csproj`, `docs/samples/NugetOrgRestoreHostingOnly/Nexo.NugetOrgRestoreHostingOnly.csproj` |
| Samples (`samples/`, see [`samples/README.md`](../samples/README.md)) | `samples/hello-brick/HelloBrick/HelloBrick.csproj`, `samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj`, `samples/templates/brick/__BrickName__Brick/__BrickName__Brick.csproj`, `samples/templates/brick/__BrickName__Brick.Tests/__BrickName__Brick.Tests.csproj` (token template copied by `nexo new brick`), `samples/certified-brick-reuse/Nexo.Certified.DamageResolver/Nexo.Certified.DamageResolver.csproj`, `samples/certified-brick-reuse/ProjectB/ProjectB.csproj` |
| Tools (`tools/`) | `tools/Nexo.CertifyBrick/Nexo.CertifyBrick.csproj` (certify a brick from the CLI), `tools/Nexo.ExportCertifiedBrick/ExportCertifiedBrick.csproj` (export a certified brick + record), `tools/Nexo.Provenance.Demo/Nexo.Provenance.Demo.csproj` (`nexo-provenance-demo`, Neo4j walkthrough), `tools/Nexo.UnitySidecarDemo/Nexo.UnitySidecarDemo.csproj`, `tools/ApplyFeedbackChanges/ApplyFeedbackChanges.csproj` |
| Spikes (`spikes/`; evidence, not product) | `spikes/autonomy-first-flight/FirstFlight/FirstFlight.csproj` (trust-loop first flight runner), `spikes/portability/tools/GenerateProbeBrick/GenerateProbeBrick.csproj` and `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.csproj` (portability probe; see `spikes/portability/REPORT.md`) |

## Tier 4 — tests and test support

| Project | Role |
|---------|------|
| `src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj` | Domain unit tests |
| `src/Nexo.Tests.Application/Nexo.Tests.Application.csproj` | Application-layer tests |
| `src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj` | Infrastructure / pipeline tests; `WebApplicationFactory` tests live under `Tests/VirtualProduction/**` (net9-only) |
| `src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj` | Helper console project used by the infrastructure test scripts (not a test project) |
| `src/Nexo.Tests.Orchestration/Nexo.Tests.Orchestration.csproj` | Orchestration tests |
| `src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj` | Background agent tests |
| `src/Nexo.Tests.Contracts/Nexo.Tests.Contracts.csproj` | Contract tests |
| `src/Nexo.Tests.Kernel/Nexo.Tests.Kernel.csproj` | Kernel integration tests |
| `src/Nexo.Tests.Transport/Nexo.Tests.Transport.csproj` | gRPC transport tests |
| `src/Nexo.Tests.AI.Pipeline/Nexo.Tests.AI.Pipeline.csproj` | MEAI pipeline tests |
| `src/Nexo.Analyzers.Tests/Nexo.Analyzers.Tests.csproj` | Roslyn analyzer tests |
| `src/Nexo.Mcp.Server.Tests/Nexo.Mcp.Server.Tests.csproj` | MCP server tests |
| `src/Nexo.Mcp.Client.Tests/Nexo.Mcp.Client.Tests.csproj` | MCP client tests |
| `src/Nexo.Transport.A2A.Tests/Nexo.Transport.A2A.Tests.csproj` | A2A client transport tests |
| `src/Nexo.Transport.A2A.Server.Tests/Nexo.Transport.A2A.Server.Tests.csproj` | A2A server tests |
| `src/Nexo.Ingress.AwsSns.Tests/Nexo.Ingress.AwsSns.Tests.csproj` | SNS ingress tests |
| `src/Nexo.Ingress.DynamoDb.Tests/Nexo.Ingress.DynamoDb.Tests.csproj` | DynamoDB ingress tests |
| `src/Nexo.Agents.TestKit/Nexo.Agents.TestKit.csproj` | Library of agent fakes shared by the test projects (`IsTestProject=false`) |
| `application/src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj` | CLI tests |
| `commercial/tests/Nexo.Commercial.Tests.GameDirector/Nexo.Tests.GameDirector.csproj` | commercial Game Director tests |
| `commercial/tests/Nexo.Commercial.Tests.GameDomain/Nexo.Commercial.Tests.GameDomain.csproj` | commercial game domain tests |
| `commercial/tests/Nexo.Commercial.Tests.Fleet/Nexo.Commercial.Tests.Fleet.csproj` | commercial fleet/director tests |
| `commercial/tests/Nexo.Commercial.Tests.Fleet.Host/Nexo.Commercial.Tests.Fleet.Host.csproj` | commercial fleet host smoke tests |
| `commercial/tests/Nexo.Commercial.Tests.MeshDirector/Nexo.Commercial.Tests.MeshDirector.csproj` | commercial mesh director CLI tests |

`applications/` test projects are listed under Tier 3a.

## Which solution do I open?

The root holds several entry points; a bare `dotnet build` fails with MSB1011, so name one. None of the filters below pull `commercial/` except `Nexo.PrimeTime.slnf` and `Nexo.sln`.

| File | Open it when | Contains |
|------|--------------|----------|
| `Nexo.Kernel.sln` | Kernel/library development without the hosts | Tier 0 spine, `Nexo.Runtime`, gRPC transport, brick/policy packs, kernel test projects (23 projects; **no** `Nexo.CLI` / `Nexo.API`) |
| `Nexo.Core.slnf` | First compile of spine + hosts | The 12 original spine libraries + the two hosts (14 projects; `Nexo.Certification.Contracts`, `Nexo.AI.Pipeline`, `Nexo.Analyzers` restore transitively) |
| `Nexo.LocalDevCore.slnf` | The CLI dev loop with core tests (`make restore-core` / `make build-core` / `make test-framework-prod-first`) | `Nexo.CLI`, `Nexo.Tests.Domain`, `Nexo.Tests.Infrastructure` |
| `Nexo.PrimeTime.slnf` | The ProdStyle test gate (`make test-prime-time`) | Eight test assemblies: Application, BackgroundAgents, CLI, Domain, Infrastructure, Orchestration, Transport **and** `commercial/tests/Nexo.Commercial.Tests.GameDomain` — this filter deliberately spans open + commercial |
| `Nexo.Runtime.sln` | Publishing the embeddable kernel graph (no `application/`) | Runtime libraries + `Nexo.Tests.AI.Pipeline` (18 projects) |
| `application/Nexo.Application.sln` | Application-gate style builds of the open hosts | `Nexo.API`, `Nexo.CLI`, `Nexo.Tests.CLI` (open only) |
| `Nexo.Demos.sln` | The three demo clients | `docs/demos/*` |
| `Nexo.sln` | Everything the CI matrix builds on Linux | 78 projects: `src/` (except `Nexo.Hosting.Bundle`, `ValidationUtilities` and the `copy-assemblies` helper), `application/`, `applications/`, 9 of the 17 `commercial/` projects (Game Director, GameDomain and three commercial test projects), `tools/Nexo.Provenance.Demo`. Samples, spikes, the other tools, and the commercial Fleet/MeshDirector projects are built from their own paths |

## Minimal clone-to-run core

For a fast first build after clone, use the existing filter solution:

```bash
dotnet build Nexo.LocalDevCore.slnf
```

That graph includes `Nexo.CLI`, core domain/infrastructure tests, and related dependencies — enough for local dev smoke without restoring all of `Nexo.sln`, and without compiling anything under `commercial/`.

## `Nexo.Core.slnf`

**`Nexo.Core.slnf`** at the repo root lists the original **Tier 0** spine libraries plus the **Tier 0b** hosts so a first compile builds the spine and hosts without distribution bundles, transport, demos, or test projects (spine-adjacent projects such as `Nexo.Certification.Contracts`, `Nexo.AI.Pipeline` and `Nexo.Analyzers` restore transitively):

```bash
dotnet build Nexo.Core.slnf
```

Pack references pulled transitively by the CLI (`Nexo.Bricks.Owasp`, `Nexo.Policies.Dev`, `Nexo.Tools.Dev`) still restore when building the CLI; the filter omits them if you only need `dotnet build` on libraries first.

## See also

- **`README.md`** — Project Layout tree
- **`docs/architecture/runtime-vs-application.md`** — runtime vs application boundary
- **`docs/architecture/ProtocolIntegration-MCP-A2A.md`** — MCP + A2A adapter projects
- **`docs/DistributionModels.md`** — consumption and CI gates per distribution path
- **`applications/README.md`** — `application/` vs `applications/` vs `apps/`
