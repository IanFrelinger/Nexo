# Licensing and open-core boundary

Nexo uses an open-core boundary:

- **Single-node + inspectable = OPEN (Apache-2.0).**
- **Fleet-scale + governance + vertical product packaging = COMMERCIAL.**
- **Trust primitives are always open.** Nexo monetizes trust at the operational layer, not by hiding policy, audit, sanitization, or SDK primitives behind a paywall.

The repository root license is Apache-2.0. See [`LICENSE`](LICENSE).

For the follow-up project/module extraction sequence, see [`docs/CommercialExtractionPlan.md`](docs/CommercialExtractionPlan.md). For fleet/mesh governance classification, see [`docs/FleetGovernanceExtractionInventory.md`](docs/FleetGovernanceExtractionInventory.md).

## Tier 1 — OPEN (Apache-2.0)

Tier 1 is the adoption and trust surface: SDKs, contracts, single-node runtime/hosts, trust primitives, tests, samples, and inspectable extension points. These projects carry Apache-2.0 package metadata either directly in their `.csproj` or through the repository-wide `Directory.Build.props` / `Directory.Build.targets` defaults.

Rationale: this tier protects two moats at once — an extensible SDK and a single runtime across surfaces. `Nexo.Policies`, `Nexo.Policies.Dev`, and `Nexo.Bricks.Owasp` are open on purpose: trust-by-design fails if the trust primitives are a paywalled black box.

| Allocation | Path |
|------------|------|
| OPEN | `src/Nexo.Abstractions/Nexo.Abstractions.csproj` |
| OPEN | `src/Nexo.Contracts/Nexo.Contracts.csproj` |
| OPEN | `src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj` |
| OPEN | `src/Nexo.Sdk/Nexo.Sdk.csproj` |
| OPEN | `src/Nexo.Framework.Sdk/Nexo.Framework.Sdk.csproj` |
| OPEN | `src/Nexo.Client/Nexo.Client.csproj` |
| OPEN | `src/Nexo.Core/Nexo.Core.csproj` |
| OPEN | `src/Nexo.Core.Application/Nexo.Core.Application.csproj` |
| OPEN | `src/Nexo.Core.Domain/Nexo.Core.Domain.csproj` |
| OPEN | `src/Nexo.Runtime/Nexo.Runtime.csproj` |
| OPEN | `src/Nexo.Runtime.Bundle/Nexo.Runtime.Bundle.csproj` |
| OPEN | `src/Nexo.Lite/Nexo.Lite.csproj` |
| OPEN | `application/src/Nexo.CLI/Nexo.CLI.csproj` |
| OPEN | `application/src/Nexo.API/Nexo.API.csproj` |
| OPEN | `src/Nexo.Infrastructure/Nexo.Infrastructure.csproj` |
| OPEN | `src/Nexo.Orchestration/Nexo.Orchestration.csproj` |
| OPEN | `src/Nexo.Adapters.Models/Nexo.Adapters.Models.csproj` |
| OPEN | `src/Nexo.BackgroundAgents/Nexo.BackgroundAgents.csproj` |
| OPEN | `src/Nexo.BackgroundAgents.HostRunners/Nexo.BackgroundAgents.HostRunners.csproj` |
| OPEN | `src/Nexo.Hosting/Nexo.Hosting.csproj` |
| OPEN | `src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj` |
| OPEN | `src/Nexo.Policies/Nexo.Policies.csproj` |
| OPEN | `src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj` |
| OPEN | `src/Nexo.Bricks.Owasp/Nexo.Bricks.Owasp.csproj` |
| OPEN | `src/Nexo.Compat/` (source-only compatibility/polyfill surface; no `.csproj`) |
| OPEN | `src/ValidationUtilities/ValidationUtilities.csproj` |
| OPEN | `src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj` |
| OPEN | `src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj` |
| OPEN | `src/Nexo.Ingress.AwsSns/Nexo.Ingress.AwsSns.csproj` |
| OPEN | `src/Nexo.Ingress.AwsSns.Tests/Nexo.Ingress.AwsSns.Tests.csproj` |
| OPEN | `src/Nexo.Ingress.DynamoDb/Nexo.Ingress.DynamoDb.csproj` |
| OPEN | `src/Nexo.Ingress.DynamoDb.Tests/Nexo.Ingress.DynamoDb.Tests.csproj` |
| OPEN | `src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj` |
| OPEN | `src/Nexo.Transport.Grpc.Server/Nexo.Transport.Grpc.Server.csproj` |
| OPEN | `src/Nexo.Transport.Grpc.Server.Host/Nexo.Transport.Grpc.Server.Host.csproj` |
| OPEN | `src/Nexo.Tests.Application/Nexo.Tests.Application.csproj` |
| OPEN | `src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj` |
| OPEN | `src/Nexo.Tests.Contracts/Nexo.Tests.Contracts.csproj` |
| OPEN | `src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj` |
| OPEN | `src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj` |
| OPEN | `src/Nexo.Tests.Kernel/Nexo.Tests.Kernel.csproj` |
| OPEN | `src/Nexo.Tests.Orchestration/Nexo.Tests.Orchestration.csproj` |
| OPEN | `src/Nexo.Tests.Transport/Nexo.Tests.Transport.csproj` |
| OPEN | `application/src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj` |
| OPEN | `docs/samples/NugetOrgRestoreHostingOnly/Nexo.NugetOrgRestoreHostingOnly.csproj` |
| OPEN | `docs/samples/NugetOrgRestoreVerify/Nexo.NugetOrgRestoreVerify.csproj` |
| OPEN | `docs/samples/StableSdkHostSample/StableSdkHostSample.csproj` |
| OPEN | `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj` |
| OPEN | `samples/**` |

## Verify-then-place decisions

These projects were explicitly inspected and placed:

| Project | Decision | Reason |
|---------|----------|--------|
| `application/src/Nexo.API/Nexo.API.csproj` | OPEN | Current project is a single-node HTTP/API host over the open kernel. The Forge/GameDomain HTTP surface has been moved to the Game Director application layer, so this host no longer references `Nexo.GameDomain`. |
| `src/Nexo.Transport.Grpc.Server/Nexo.Transport.Grpc.Server.csproj` | OPEN | Server implementation exposes the open gRPC transport surface; it is not a fleet-scale director/control-plane project. |
| `src/Nexo.Transport.Grpc.Server.Host/Nexo.Transport.Grpc.Server.Host.csproj` | OPEN | Standalone gRPC host for the open transport server, not a governance tier. |
| `application/Nexo.Application.sln` contents | OPEN for current contents | The solution contains `Nexo.API`, `Nexo.CLI`, and `Nexo.Tests.CLI` as open surfaces. `Nexo.GameDomain` and its tests have moved to `commercial/`. |

## Tier 2 — COMMERCIAL (fleet + governance)

Tier 2 is the future commercial layer for fleet-scale and governance capabilities:

- mesh control plane / distributed execution,
- knowledge sync,
- elastic scheduling,
- leases and checkpoints,
- data-plane federation,
- operator hardening,
- director persistence,
- centralized policy management,
- aggregated tamper-evident audit,
- RBAC/SSO and organization-scale governance.

Current status: fleet contracts, fleet infrastructure, mesh director, fleet API endpoint baselines, and a commercial fleet host now exist under `commercial/`. Some fleet/governance implementation code remains in open projects while consumers are migrated incrementally; open `Nexo.API` still owns fleet/task/knowledge endpoint handlers until mesh-lab scripts and tests move to the commercial host.

The current classification inventory is [`docs/FleetGovernanceExtractionInventory.md`](docs/FleetGovernanceExtractionInventory.md). Likely extraction candidates include:

- `src/Nexo.Core.Application/Fleet/**`
- `src/Nexo.Infrastructure/Fleet/**`
- `src/Nexo.Core.Application/Networking/**`
- `src/Nexo.Infrastructure/Networking/**`
- fleet-scale portions of `src/Nexo.Core.Application/Mesh/**` and `src/Nexo.Infrastructure/Mesh/**`
- fleet/director CLI surfaces such as `MeshDirectorCommand` and `MeshHubCommand`
- API mesh/fleet governance middleware and endpoints under `application/src/Nexo.API/**`

## Tier 3 — COMMERCIAL (verticals)

Tier 3 is the commercial product/vertical layer.

These app configuration directories are marked with a `COMMERCIAL-LICENSE.md` stub:

| Allocation | Path |
|------------|------|
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDomain/` |
| COMMERCIAL | `commercial/tests/Nexo.Commercial.Tests.GameDomain/` |
| COMMERCIAL | `commercial/samples/ForgeMapHostSample/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.Fleet.Contracts/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.Fleet.Infrastructure/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.Fleet.Api/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.Fleet.Host/` |
| COMMERCIAL | `commercial/tests/Nexo.Commercial.Tests.Fleet.Host/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.MeshDirector/` |
| COMMERCIAL | `commercial/tests/Nexo.Commercial.Tests.MeshDirector/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDirector.Domain/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDirector.Agents/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDirector.Bricks/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDirector.Mcp/` |
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDirector.Host/` |
| COMMERCIAL | `commercial/tests/Nexo.Commercial.Tests.GameDirector/` |
| COMMERCIAL | `apps/game-director/` |
| COMMERCIAL | `apps/nexo-forge/` |
| COMMERCIAL | `apps/release-manager/` |
| COMMERCIAL | `apps/runtime-studio/` |

Stub text:

> Not licensed under Apache-2.0. Commercial terms TBD. See /LICENSING.md.

The GameDomain module and Game Director code/test projects have been moved into `commercial/`, so they may reference each other and the open core without creating open-to-commercial project references.

Recommended future open-source candidate: `apps/release-manager` is the best single app to open later as a minimal SDK reference because it demonstrates generic release-readiness automation without making the defense-adjacent Game Director wedge open.

## Dependency-direction safety

Rule: no Tier 1 OPEN project may reference a Tier 2/3 COMMERCIAL project.

Current result: **passes for committed placement** because no Tier 1 `.csproj` references a commercial project.

Preflight result for the vertical split: **unblocked for GameDomain/GameDirector**. `Nexo.API`, `Nexo.CLI`, and `Nexo.Tests.CLI` no longer reference `Nexo.GameDomain`; `Nexo.GameDomain`, Game Director code, and their tests have moved to commercial paths. Remaining extraction work is fleet/governance extraction.

## Open questions

- Which fleet/governance files should be extracted first into commercial projects? Start from [`docs/FleetGovernanceExtractionInventory.md`](docs/FleetGovernanceExtractionInventory.md).
- Should `Nexo.API` remain a purely open single-node host, or should commercial fleet/governance endpoints move to a separate host?
- Should `apps/release-manager` become the future minimal open SDK reference app?

See [`docs/CommercialExtractionPlan.md`](docs/CommercialExtractionPlan.md) for the proposed extraction order and validation gates.
