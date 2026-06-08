# Licensing and open-core boundary

Nexo uses an open-core boundary:

- **Single-node + inspectable = OPEN (Apache-2.0).**
- **Fleet-scale + governance + vertical product packaging = COMMERCIAL.**
- **Trust primitives are always open.** Nexo monetizes trust at the operational layer, not by hiding policy, audit, sanitization, or SDK primitives behind a paywall.

The repository root license is Apache-2.0. See [`LICENSE`](LICENSE).

Extraction **Phases A–E** are complete (see [`docs/CommercialExtractionPlan.md`](docs/CommercialExtractionPlan.md)). The open/commercial project graph is enforced in CI by [`scripts/dependency-boundary-gate.sh`](scripts/dependency-boundary-gate.sh). Optional follow-up extractions (for example `src/**/Networking/**`) are classified in [`docs/FleetGovernanceExtractionInventory.md`](docs/FleetGovernanceExtractionInventory.md).

## Tier 1 — OPEN (Apache-2.0)

Tier 1 is the adoption and trust surface: SDKs, contracts, single-node runtime/hosts, trust primitives, tests, samples, and inspectable extension points. These projects carry Apache-2.0 package metadata either directly in their `.csproj` or through the repository-wide `Directory.Build.props` / `Directory.Build.targets` defaults (`PackageLicenseExpression=Apache-2.0` for non-commercial projects).

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
| OPEN | `src/Nexo.Infrastructure/Nexo.Infrastructure.csproj` (includes open `MeshLab` worker executor) |
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

Open mesh **primitives** (local discovery, capability advertisement, trust middleware) remain under `src/Nexo.Core.Application/Mesh/**` and `src/Nexo.Infrastructure/Mesh/**`. Mesh-lab **workers** poll the commercial fleet director via open `src/Nexo.Infrastructure/MeshLab/**`.

## Verify-then-place decisions

These projects were explicitly inspected and placed:

| Project | Decision | Reason |
|---------|----------|--------|
| `application/src/Nexo.API/Nexo.API.csproj` | OPEN | Single-node HTTP/API host over the open kernel. Fleet `/api/mesh/*` director endpoints live on `Nexo.Commercial.Fleet.Host`, not open `Nexo.API`. |
| `src/Nexo.Transport.Grpc.Server/Nexo.Transport.Grpc.Server.csproj` | OPEN | Server implementation exposes the open gRPC transport surface; it is not a fleet-scale director/control-plane project. |
| `src/Nexo.Transport.Grpc.Server.Host/Nexo.Transport.Grpc.Server.Host.csproj` | OPEN | Standalone gRPC host for the open transport server, not a governance tier. |
| `application/Nexo.Application.sln` contents | OPEN for current contents | The solution contains `Nexo.API`, `Nexo.CLI`, and `Nexo.Tests.CLI` as open surfaces. Game domain and Game Director live under `commercial/`. |

## Tier 2 — COMMERCIAL (fleet + governance)

Tier 2 is the commercial layer for fleet-scale and governance capabilities:

- mesh control plane / distributed execution,
- knowledge replication on the director,
- elastic scheduling,
- leases and checkpoints,
- data-plane federation,
- operator hardening,
- director persistence,
- centralized policy management (future `Nexo.Commercial.Governance`),
- aggregated tamper-evident audit,
- RBAC/SSO and organization-scale governance.

**Current modules** (each directory has `COMMERCIAL-LICENSE.md` and `NexoCommercialProject=true` in its `.csproj`):

| Module | Path |
|--------|------|
| Fleet contracts | `commercial/src/Nexo.Commercial.Fleet.Contracts/` |
| Fleet infrastructure | `commercial/src/Nexo.Commercial.Fleet.Infrastructure/` |
| Fleet API extensions | `commercial/src/Nexo.Commercial.Fleet.Api/` |
| Fleet operator host | `commercial/src/Nexo.Commercial.Fleet.Host/` |
| Mesh director CLI | `commercial/src/Nexo.Commercial.MeshDirector/` |
| Fleet tests | `commercial/tests/Nexo.Commercial.Tests.Fleet/`, `commercial/tests/Nexo.Commercial.Tests.Fleet.Host/` |
| Mesh director tests | `commercial/tests/Nexo.Commercial.Tests.MeshDirector/` |

Mesh-lab **peer-a** runs `Nexo.Commercial.Fleet.Host` (`.docker/Dockerfile.fleet-host`). Open duplicate fleet trees under `src/**/Fleet/**` have been removed.

**Optional follow-up** (not required for the open/commercial boundary): classify and optionally move `src/Nexo.Core.Application/Networking/**` and `src/Nexo.Infrastructure/Networking/**` per [`docs/FleetGovernanceExtractionInventory.md`](docs/FleetGovernanceExtractionInventory.md).

## Tier 3 — COMMERCIAL (verticals)

Tier 3 is the commercial product/vertical layer.

| Allocation | Path |
|------------|------|
| COMMERCIAL | `commercial/src/Nexo.Commercial.GameDomain/` |
| COMMERCIAL | `commercial/tests/Nexo.Commercial.Tests.GameDomain/` |
| COMMERCIAL | `commercial/samples/ForgeMapHostSample/` |
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

Stub text in each `COMMERCIAL-LICENSE.md`:

> Not licensed under Apache-2.0. Commercial terms TBD. See /LICENSING.md.

Commercial vertical projects may reference each other and the open core; open projects must not reference commercial projects.

Recommended future open-source candidate: `apps/release-manager` is the best single app to open later as a minimal SDK reference because it demonstrates generic release-readiness automation without making the defense-adjacent Game Director wedge open.

## Dependency-direction safety

Rule: no Tier 1 OPEN project may reference a Tier 2/3 COMMERCIAL project.

Enforced in CI by `scripts/dependency-boundary-gate.sh` (workflow: `.github/workflows/dependency-boundary.yml`). The scanner classifies projects by path and `NexoCommercialProject`, fails on open→commercial `ProjectReference` edges, requires `COMMERCIAL-LICENSE.md` beside commercial `.csproj` files, and verifies open packable projects resolve `PackageLicenseExpression=Apache-2.0`.

Local verification:

```bash
make dependency-boundary-gate
```

## Open questions (post-extraction)

- Should `src/**/Networking/**` move to commercial fleet/governance modules, or remain a smaller open substrate?
- Should open `MeshHubCommand` split further so only local mesh inspection stays in `Nexo.CLI`?
- Should `apps/release-manager` become the future minimal open SDK reference app?

See [`docs/CommercialExtractionPlan.md`](docs/CommercialExtractionPlan.md) for the completed extraction sequence and validation gates.
