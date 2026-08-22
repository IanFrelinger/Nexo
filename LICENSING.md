# Licensing and open-core boundary

Ashlar uses an open-core boundary:

- **Single-node + inspectable = OPEN (Apache-2.0).**
- **Fleet-scale + governance + vertical product packaging = COMMERCIAL.**
- **Trust primitives are always open.** Ashlar monetizes trust at the operational layer, not by hiding policy, audit, sanitization, or SDK primitives behind a paywall.

The repository root license is Apache-2.0. See [`LICENSE`](LICENSE).

Extraction **Phases A–E** are complete (see [`docs/CommercialExtractionPlan.md`](docs/CommercialExtractionPlan.md)). The open/commercial project graph is enforced in CI by [`scripts/dependency-boundary-gate.sh`](scripts/dependency-boundary-gate.sh). Optional follow-up extractions (for example `src/**/Networking/**`) are classified in [`docs/FleetGovernanceExtractionInventory.md`](docs/FleetGovernanceExtractionInventory.md).

## Tier 1 — OPEN (Apache-2.0)

Tier 1 is the adoption and trust surface: SDKs, contracts, single-node runtime/hosts, trust primitives, tests, samples, and inspectable extension points. These projects carry Apache-2.0 package metadata either directly in their `.csproj` or through the repository-wide `Directory.Build.props` / `Directory.Build.targets` defaults (`PackageLicenseExpression=Apache-2.0` for non-commercial projects).

Rationale: this tier protects two moats at once — an extensible SDK and a single runtime across surfaces. `Ashlar.Policies`, `Ashlar.Policies.Dev`, and `Ashlar.Bricks.Owasp` are open on purpose: trust-by-design fails if the trust primitives are a paywalled black box.

| Allocation | Path |
|------------|------|
| OPEN | `src/Ashlar.Abstractions/Ashlar.Abstractions.csproj` |
| OPEN | `src/Ashlar.Contracts/Ashlar.Contracts.csproj` |
| OPEN | `src/Ashlar.Brick.Contracts/Ashlar.Brick.Contracts.csproj` |
| OPEN | `src/Ashlar.Analyzers/Ashlar.Analyzers.csproj` |
| OPEN | `src/Ashlar.Sdk/Ashlar.Sdk.csproj` |
| OPEN | `src/Ashlar.Framework.Sdk/Ashlar.Framework.Sdk.csproj` |
| OPEN | `src/Ashlar.Client/Ashlar.Client.csproj` |
| OPEN | `src/Ashlar.Core.Application/Ashlar.Core.Application.csproj` |
| OPEN | `src/Ashlar.Core.Domain/Ashlar.Core.Domain.csproj` |
| OPEN | `src/Ashlar.Runtime/Ashlar.Runtime.csproj` |
| OPEN | `src/Ashlar.Runtime.Bundle/Ashlar.Runtime.Bundle.csproj` |
| OPEN | `src/Ashlar.Lite/Ashlar.Lite.csproj` |
| OPEN | `application/src/Ashlar.CLI/Ashlar.CLI.csproj` |
| OPEN | `application/src/Ashlar.API/Ashlar.API.csproj` |
| OPEN | `src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj` (includes open `MeshLab` worker executor) |
| OPEN | `src/Ashlar.Orchestration/Ashlar.Orchestration.csproj` |
| OPEN | `src/Ashlar.Adapters.Models/Ashlar.Adapters.Models.csproj` |
| OPEN | `src/Ashlar.BackgroundAgents/Ashlar.BackgroundAgents.csproj` |
| OPEN | `src/Ashlar.BackgroundAgents.HostRunners/Ashlar.BackgroundAgents.HostRunners.csproj` |
| OPEN | `src/Ashlar.Hosting/Ashlar.Hosting.csproj` |
| OPEN | `src/Ashlar.Hosting.Bundle/Ashlar.Hosting.Bundle.csproj` |
| OPEN | `src/Ashlar.Policies/Ashlar.Policies.csproj` |
| OPEN | `src/Ashlar.Policies.Dev/Ashlar.Policies.Dev.csproj` |
| OPEN | `src/Ashlar.Bricks.Owasp/Ashlar.Bricks.Owasp.csproj` |
| OPEN | `src/Ashlar.Compat/` (source-only compatibility/polyfill surface; no `.csproj`) |
| OPEN | `src/ValidationUtilities/ValidationUtilities.csproj` |
| OPEN | `src/Ashlar.Tools.Assembly/Ashlar.Tools.Assembly.csproj` |
| OPEN | `src/Ashlar.Tools.Dev/Ashlar.Tools.Dev.csproj` |
| OPEN | `src/Ashlar.Ingress.AwsSns/Ashlar.Ingress.AwsSns.csproj` |
| OPEN | `src/Ashlar.Ingress.AwsSns.Tests/Ashlar.Ingress.AwsSns.Tests.csproj` |
| OPEN | `src/Ashlar.Ingress.DynamoDb/Ashlar.Ingress.DynamoDb.csproj` |
| OPEN | `src/Ashlar.Ingress.DynamoDb.Tests/Ashlar.Ingress.DynamoDb.Tests.csproj` |
| OPEN | `src/Ashlar.Transport.Grpc/Ashlar.Transport.Grpc.csproj` |
| OPEN | `src/Ashlar.Transport.Grpc.Server/Ashlar.Transport.Grpc.Server.csproj` |
| OPEN | `src/Ashlar.Transport.Grpc.Server.Host/Ashlar.Transport.Grpc.Server.Host.csproj` |
| OPEN | `src/Ashlar.Mcp.Server/Ashlar.Mcp.Server.csproj` |
| OPEN | `src/Ashlar.Mcp.Server.Host/Ashlar.Mcp.Server.Host.csproj` |
| OPEN | `src/Ashlar.Mcp.Server.Tests/Ashlar.Mcp.Server.Tests.csproj` |
| OPEN | `src/Ashlar.Mcp.Client/Ashlar.Mcp.Client.csproj` |
| OPEN | `src/Ashlar.Mcp.Client.Tests/Ashlar.Mcp.Client.Tests.csproj` |
| OPEN | `src/Ashlar.Transport.A2A/Ashlar.Transport.A2A.csproj` |
| OPEN | `src/Ashlar.Transport.A2A.Server/Ashlar.Transport.A2A.Server.csproj` |
| OPEN | `src/Ashlar.Transport.A2A.Tests/Ashlar.Transport.A2A.Tests.csproj` |
| OPEN | `src/Ashlar.Transport.A2A.Server.Tests/Ashlar.Transport.A2A.Server.Tests.csproj` |
| OPEN | `src/Ashlar.Tests.Application/Ashlar.Tests.Application.csproj` |
| OPEN | `src/Ashlar.Tests.BackgroundAgents/Ashlar.Tests.BackgroundAgents.csproj` |
| OPEN | `src/Ashlar.Tests.Contracts/Ashlar.Tests.Contracts.csproj` |
| OPEN | `src/Ashlar.Tests.Domain/Ashlar.Tests.Domain.csproj` |
| OPEN | `src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj` |
| OPEN | `src/Ashlar.Tests.Kernel/Ashlar.Tests.Kernel.csproj` |
| OPEN | `src/Ashlar.Tests.Orchestration/Ashlar.Tests.Orchestration.csproj` |
| OPEN | `src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj` |
| OPEN | `application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj` |
| OPEN | `docs/samples/NugetOrgRestoreHostingOnly/Ashlar.NugetOrgRestoreHostingOnly.csproj` |
| OPEN | `docs/samples/NugetOrgRestoreVerify/Ashlar.NugetOrgRestoreVerify.csproj` |
| OPEN | `docs/samples/StableSdkHostSample/StableSdkHostSample.csproj` |
| OPEN | `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj` |
| OPEN | `samples/**` |
| OPEN | `applications/**` — open products on the core (physical-atom certification, provenance graph, spatial); Apache-2.0 by the `Directory.Build.targets` rule, no `AshlarCommercialProject` flag; see [`applications/README.md`](applications/README.md) |
| OPEN | `tools/**`, `spikes/**` — repo tools and evidence spikes, same rule |

Open mesh **primitives** (local discovery, capability advertisement, trust middleware) remain under `src/Ashlar.Core.Application/Mesh/**` and `src/Ashlar.Infrastructure/Mesh/**`. Mesh-lab **workers** poll the commercial fleet director via open `src/Ashlar.Infrastructure/MeshLab/**`.

## Verify-then-place decisions

These projects were explicitly inspected and placed:

| Project | Decision | Reason |
|---------|----------|--------|
| `application/src/Ashlar.API/Ashlar.API.csproj` | OPEN | Single-node HTTP/API host over the open kernel. Fleet `/api/mesh/*` director endpoints live on `Ashlar.Commercial.Fleet.Host`, not open `Ashlar.API`. |
| `src/Ashlar.Transport.Grpc.Server/Ashlar.Transport.Grpc.Server.csproj` | OPEN | Server implementation exposes the open gRPC transport surface; it is not a fleet-scale director/control-plane project. |
| `src/Ashlar.Transport.Grpc.Server.Host/Ashlar.Transport.Grpc.Server.Host.csproj` | OPEN | Standalone gRPC host for the open transport server, not a governance tier. |
| `application/Ashlar.Application.sln` contents | OPEN | The solution contains `Ashlar.API`, `Ashlar.CLI`, and `Ashlar.Tests.CLI` only. It previously also listed `Ashlar.Commercial.GameDomain` and its tests "for local dev"; those were removed so the tester quickstart never compiles commercial code. `Ashlar.LocalDevCore.slnf` likewise pulls no `commercial/` project. Filters that deliberately span both tiers (`Ashlar.PrimeTime.slnf`, `Ashlar.sln`) say so in `docs/ProjectTiers.md`. |

## Tier 2 — COMMERCIAL (fleet + governance)

Tier 2 is the commercial layer for fleet-scale and governance capabilities:

- mesh control plane / distributed execution,
- knowledge replication on the director,
- elastic scheduling,
- leases and checkpoints,
- data-plane federation,
- operator hardening,
- director persistence,
- centralized policy management (future `Ashlar.Commercial.Governance`),
- aggregated tamper-evident audit,
- RBAC/SSO and organization-scale governance.

**Current modules** (each directory has `COMMERCIAL-LICENSE.md` and `AshlarCommercialProject=true` in its `.csproj`):

| Module | Path |
|--------|------|
| Fleet contracts | `commercial/src/Ashlar.Commercial.Fleet.Contracts/` |
| Fleet infrastructure | `commercial/src/Ashlar.Commercial.Fleet.Infrastructure/` |
| Fleet API extensions | `commercial/src/Ashlar.Commercial.Fleet.Api/` |
| Fleet operator host | `commercial/src/Ashlar.Commercial.Fleet.Host/` |
| Mesh director CLI | `commercial/src/Ashlar.Commercial.MeshDirector/` |
| Fleet tests | `commercial/tests/Ashlar.Commercial.Tests.Fleet/`, `commercial/tests/Ashlar.Commercial.Tests.Fleet.Host/` |
| Mesh director tests | `commercial/tests/Ashlar.Commercial.Tests.MeshDirector/` |

Mesh-lab **peer-a** runs `Ashlar.Commercial.Fleet.Host` (`.docker/Dockerfile.fleet-host`). Open duplicate fleet trees under `src/**/Fleet/**` have been removed.

**Networking (Phase F, done):** knowledge-sync / network-bus / adaptive-cache surfaces live under `commercial/src/Ashlar.Commercial.Fleet.Contracts/Networking/**` and `commercial/src/Ashlar.Commercial.Fleet.Infrastructure/Networking/**` (namespaces retain `Ashlar.Core.Application.Networking` / `Ashlar.Commercial.Fleet.Infrastructure.Networking` for compatibility). Register via `AddAshlarCommercialFleetNetworking()` on the commercial fleet host.

## Tier 3 — COMMERCIAL (verticals)

Tier 3 is the commercial product/vertical layer.

| Allocation | Path |
|------------|------|
| COMMERCIAL | `commercial/src/Ashlar.Commercial.GameDomain/` |
| COMMERCIAL | `commercial/tests/Ashlar.Commercial.Tests.GameDomain/` |
| COMMERCIAL | `commercial/samples/ForgeMapHostSample/` |
| COMMERCIAL | `commercial/src/Ashlar.Commercial.GameDirector.Domain/` |
| COMMERCIAL | `commercial/src/Ashlar.Commercial.GameDirector.Agents/` |
| COMMERCIAL | `commercial/src/Ashlar.Commercial.GameDirector.Bricks/` |
| COMMERCIAL | `commercial/src/Ashlar.Commercial.GameDirector.Mcp/` |
| COMMERCIAL | `commercial/src/Ashlar.Commercial.GameDirector.Host/` |
| COMMERCIAL | `commercial/tests/Ashlar.Commercial.Tests.GameDirector/` |
| COMMERCIAL | `apps/game-director/` |
| COMMERCIAL | `apps/ashlar-forge/` |
| COMMERCIAL | `apps/release-manager/` |
| COMMERCIAL | `apps/runtime-studio/` |

Stub text in each `COMMERCIAL-LICENSE.md`:

> Not licensed under Apache-2.0. Commercial terms TBD. See /LICENSING.md.

Commercial vertical projects may reference each other and the open core; open projects must not reference commercial projects.

### Evaluation use of `commercial/` sources — PROPOSED TEXT, needs owner sign-off

> **Status: draft, not in force.** The 21 `COMMERCIAL-LICENSE.md` stubs say "terms TBD", so today the `commercial/` sources in this public repository carry **no grant at all** beyond what copyright law and GitHub's terms allow (viewing and forking the repository). The paragraph below is a proposal for the repository owner to accept, edit, or reject; nothing here is a license until the stubs are replaced.
>
> *Proposed:* "Source code under `commercial/` is made visible for evaluation. You may build and run it locally, in CI on your own fork, and in non-production test environments, solely to evaluate Ashlar. You may not deploy it in production, offer it as a service, or redistribute it (in source or binary form) without a separate written agreement. The open core under `src/`, `application/`, `applications/`, `samples/`, `tools/`, `docs/`, and `spikes/` remains Apache-2.0 and is unaffected by this paragraph."
>
> Until the owner signs off and updates each `COMMERCIAL-LICENSE.md`, contributors and evaluators should treat `commercial/` as **all rights reserved** and keep the tester quickstart (`Ashlar.LocalDevCore.slnf`, `application/Ashlar.Application.sln`, `Ashlar.Core.slnf`) free of it, which it now is.

Recommended future open-source candidate: `apps/release-manager` is the best single app to open later as a minimal SDK reference because it demonstrates generic release-readiness automation without making the defense-adjacent Game Director wedge open.

## Dependency-direction safety

Rule: no Tier 1 OPEN project may reference a Tier 2/3 COMMERCIAL project.

Enforced in CI by `scripts/dependency-boundary-gate.sh` (workflow: `.github/workflows/dependency-boundary.yml`). The scanner classifies projects by path and `AshlarCommercialProject`, fails on open→commercial `ProjectReference` edges, requires `COMMERCIAL-LICENSE.md` beside commercial `.csproj` files, and verifies open packable projects resolve `PackageLicenseExpression=Apache-2.0`.

Local verification:

```bash
make dependency-boundary-gate
```

## Open questions (post-extraction)

- ~~Should `src/**/Networking/**` move to commercial?~~ **Done (Phase F):** under `Ashlar.Commercial.Fleet.*`.
- ~~Should open `MeshHubCommand` split further?~~ **Done:** open `ashlar mesh peers` / `mesh health` (local probe); fleet `list-nodes` / director `health` on `Ashlar.Commercial.MeshDirector`.
- Should `apps/release-manager` become the future minimal open SDK reference app?

See [`docs/CommercialExtractionPlan.md`](docs/CommercialExtractionPlan.md) for the completed extraction sequence and validation gates.
