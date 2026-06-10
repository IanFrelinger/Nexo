# Commercial extraction plan

This plan records the commercial-boundary work after the open-core licensing sprint.

**Status (2026-06):** Phases **A–F** are **complete** (F = networking extraction to commercial fleet). GameDomain, Game Director, fleet/mesh governance, licensing metadata (Phase D), and the dependency-boundary CI gate (Phase E) are in place on `master`. Optional follow-ups (Networking extraction, CLI mesh-hub split, `Nexo.Commercial.Governance`) are tracked under [Open questions](#open-questions-post-extraction) in [`LICENSING.md`](../LICENSING.md).

## Goal

Create a clean project/module boundary where:

- Tier 1 **OPEN** projects remain Apache-2.0 and inspectable.
- Tier 2/3 **COMMERCIAL** projects can be licensed separately.
- No open project has a `ProjectReference` to a commercial project.
- Trust primitives stay open; operational governance and vertical packaging are monetized.

## Current blockers

None for the core open/commercial boundary. Phases A–E exit criteria are met.

### Vertical / Game Director graph (resolved)

| Current project | Current references that matter for extraction |
|-----------------|-----------------------------------------------|
| `application/src/Nexo.API/Nexo.API.csproj` | **Resolved:** the Forge/GameDomain HTTP surface moved to the Game Director/MCP application layer, so `Nexo.API` no longer references `Nexo.GameDomain`. |
| `application/src/Nexo.CLI/Nexo.CLI.csproj` | **Resolved:** Unity pipeline helper types moved into `Nexo.CLI`, so the CLI no longer references `Nexo.GameDomain`. |
| `commercial/src/Nexo.Commercial.GameDomain/Nexo.Commercial.GameDomain.csproj` | **Resolved:** moved from `application/src/Nexo.GameDomain`; references open core and is now commercially marked. |
| `commercial/src/Nexo.Commercial.GameDirector.Domain/GameDirector.Domain.csproj` | Moved to commercial layout; references commercial GameDomain and open core. |
| `commercial/src/Nexo.Commercial.GameDirector.Agents/GameDirector.Agents.csproj` | Moved to commercial layout; references GameDirector bricks/domain, `Nexo.Client`, and open abstractions/application/domain. |
| `commercial/src/Nexo.Commercial.GameDirector.Bricks/GameDirector.Bricks.csproj` | Moved to commercial layout; references GameDirector domain plus open abstractions/domain/application/infrastructure. |
| `commercial/src/Nexo.Commercial.GameDirector.Mcp/GameDirector.Mcp.csproj` | Moved to commercial layout; references GameDirector domain, `Nexo.Client`, open core, infrastructure, and brick contracts. |
| `commercial/src/Nexo.Commercial.GameDirector.Host/GameDirector.Host.csproj` | Moved to commercial layout; references all Game Director projects plus `Nexo.API`, `Nexo.Client`, and `Nexo.Hosting`. |
| `application/src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj` | **Resolved:** game-domain descriptor serialization assertions moved to `Nexo.Tests.GameDomain`, so the CLI test assembly no longer directly references `Nexo.GameDomain`. |
| `commercial/tests/Nexo.Commercial.Tests.GameDomain/Nexo.Commercial.Tests.GameDomain.csproj` | **Resolved:** moved with commercial GameDomain tests. |
| `commercial/tests/Nexo.Commercial.Tests.GameDirector/Nexo.Tests.GameDirector.csproj` | Moved to commercial layout; references GameDirector projects and commercial GameDomain. |

### Fleet / mesh / governance graph (resolved)

Fleet-scale code has moved to `commercial/src/Nexo.Commercial.Fleet.*`, `Nexo.Commercial.MeshDirector`, and `Nexo.Commercial.Fleet.Host`. Open `src/**/Fleet/**` trees are removed. Mesh-lab workers use open `src/Nexo.Infrastructure/MeshLab/**` to call the commercial director HTTP API.

**Optional follow-up:** networking extraction (Phase F, see PR #155) and CLI mesh hub split (done: `mesh peers`/`health` vs `director list-nodes`/`health`) — see [`FleetGovernanceExtractionInventory.md`](FleetGovernanceExtractionInventory.md).

## Target boundary

### Open core remains

Open projects should continue to expose:

- contracts, abstractions, SDKs, clients, hosting, and bundles;
- single-node `Nexo.CLI` and `Nexo.API` shells;
- trust primitives: classification, policy packs, barrier identity, sanitization, audit primitives, and local-first controls;
- single-node mesh primitives: local capability advertisement, local transport, and inspectable peer primitives;
- gRPC transport and server host;
- AWS ingress adapters;
- tests and samples that validate the open surface.

### Commercial verticals

Target commercial projects/modules:

| Proposed commercial module | Source today | Target shape |
|----------------------------|--------------|--------------|
| `Nexo.Commercial.GameDomain` | `commercial/src/Nexo.Commercial.GameDomain` | Commercial domain package used only by commercial vertical hosts/tests. |
| `Nexo.Commercial.GameDirector.Domain` | `commercial/src/Nexo.Commercial.GameDirector.Domain` | Vertical-specific domain layer. |
| `Nexo.Commercial.GameDirector.Bricks` | `commercial/src/Nexo.Commercial.GameDirector.Bricks` | Vertical bricks over open `Nexo.Brick.Contracts` and open runtime ports. |
| `Nexo.Commercial.GameDirector.Agents` | `commercial/src/Nexo.Commercial.GameDirector.Agents` | Vertical agents over open abstractions/client/application ports. |
| `Nexo.Commercial.GameDirector.Mcp` | `commercial/src/Nexo.Commercial.GameDirector.Mcp` | Commercial MCP surface for the vertical. |
| `Nexo.Commercial.GameDirector.Host` | `commercial/src/Nexo.Commercial.GameDirector.Host` | Commercial host that composes open API/hosting with vertical modules. |
| `Nexo.Commercial.Tests.GameDomain` | `commercial/tests/Nexo.Commercial.Tests.GameDomain` | Commercial test assembly for GameDomain. |
| `Nexo.Commercial.Tests.GameDirector` | `commercial/tests/Nexo.Commercial.Tests.GameDirector` | Commercial vertical test assembly. |

`Nexo.API` should lose its direct `ProjectReference` to `Nexo.GameDomain`. Use one of these patterns:

1. **Plugin registration:** commercial Game Director host registers game-domain services into open API extension points.
2. **Separate host:** `Nexo.Commercial.GameDirector.Host` owns all vertical endpoints and references open `Nexo.API` only as a shell/shared web host.
3. **Out-of-process integration:** open API remains generic; Game Director runs as a separate service/MCP server.

### Commercial fleet / governance

Target commercial modules:

| Proposed commercial module | Source today | Target shape |
|----------------------------|--------------|--------------|
| `Nexo.Commercial.Fleet.Contracts` | seeded from fleet DTOs/ports now in `Nexo.Core.Application/Fleet` | Commercial contracts needed by commercial fleet runtime; originals remain temporarily until consumers move. |
| `Nexo.Commercial.Fleet.Core` | fleet placement/task/checkpoint abstractions and policies | Fleet scheduling and governance logic. |
| `Nexo.Commercial.Fleet.Infrastructure` | seeded from `Nexo.Infrastructure/Fleet` persistence/worker/director services | Commercial implementations for persistence, director state, workers, leases, checkpoints; originals remain temporarily until consumers move. |
| `Nexo.Commercial.Fleet.Api` | seeded from `Nexo.API` `/api/mesh` fleet/task/knowledge endpoints | Commercial endpoint extension for fleet director HTTP APIs; open endpoints remain temporarily until mesh-lab migrates. |
| `Nexo.Commercial.Fleet.Host` | new commercial operator host | Wires commercial fleet DI and `MapCommercialFleetEndpoints()`; open `Nexo.API` fleet routes remain until mesh-lab migration. |
| `Nexo.Commercial.MeshDirector` | CLI director HTTP client surface | Commercial control plane CLI/API module. |
| `Nexo.Commercial.Governance` | centralized policy, RBAC/SSO, aggregated audit surfaces | Commercial organization-level governance module. |
| `Nexo.Commercial.Tests.Fleet` | fleet/mesh governance tests currently under open test projects | Commercial test assembly for extracted fleet behavior. |

Open core should retain low-level trust primitives and local/single-node mesh primitives. Commercial fleet modules may depend on open core; open core must not depend on commercial modules.

## Extraction sequence

### Phase A — make plugin seams explicit

Purpose: break direct open-host references to vertical code before moving code.

1. **Done:** identify every `Nexo.API` use of `Nexo.GameDomain` types.
2. **Done:** move the Forge HTTP surface to the Game Director/MCP application layer.
3. **Done:** ensure `Nexo.API` can build without `Nexo.GameDomain`.
4. **Done:** move CLI tests that require game-domain asset descriptors into `Nexo.Tests.GameDomain`.
5. **Done:** move Unity pipeline helpers from `Nexo.GameDomain` into `Nexo.CLI`.

Exit criteria:

- `Nexo.API` no longer references `Nexo.GameDomain`.
- `Nexo.Tests.CLI` no longer references `Nexo.GameDomain`.
- `Nexo.CLI` no longer references `Nexo.GameDomain`.
- Game Director host owns the Forge HTTP endpoint/service registration.

### Phase B — extract GameDomain and mark Game Director

Purpose: move vertical code to commercial projects/modules.

1. **Done:** create commercial GameDomain project/test layout under `commercial/`.
2. **Done:** move `Nexo.GameDomain` and `Nexo.Tests.GameDomain`.
3. **Done:** add `COMMERCIAL-LICENSE.md` stubs for commercial GameDomain, GameDirector, and Forge sample paths.
4. **Done:** keep references pointing inward: commercial projects may reference open projects; open projects may not reference commercial projects.
5. **Done:** update solution/filter membership after project moves.

Exit criteria:

- GameDomain code and tests are commercial.
- Game Director code projects are commercially marked in place.
- Open API/CLI/test projects do not reference commercial projects.
- Game Director host/tests validate in the commercial project graph.

### Phase C — extract fleet / mesh governance

Purpose: split fleet-scale control plane from open mesh/trust primitives.

Classification source: [`FleetGovernanceExtractionInventory.md`](FleetGovernanceExtractionInventory.md).

1. Separate single-node/open mesh primitives from fleet-scale coordination.
2. Move fleet task registry, placement, leases/checkpoints, director persistence, worker executor, registration keys, knowledge replication, and trust-tier fleet policy into commercial modules.
3. Move `mesh director` and commercial `mesh hub` CLI/API surfaces into commercial modules, or keep only local/open mesh commands in the open CLI.
4. Keep trust primitives open: barrier identity, audit event contracts, policy pack primitives, sensitivity rules, sanitization primitives.

Exit criteria:

- Open `Nexo.Core.Application`, `Nexo.Infrastructure`, `Nexo.Runtime`, `Nexo.Orchestration`, `Nexo.CLI`, and `Nexo.API` no longer contain fleet-scale commercial code.
- Commercial fleet modules compile against open contracts.
- Mesh/fleet tests are split into open primitive tests and commercial fleet tests.

### Phase D — licensing and package metadata (done)

Purpose: make the legal boundary match the project graph.

1. **Done:** Apache-2.0 package metadata on open projects (`Directory.Build.props` / `Directory.Build.targets`; verified by dependency-boundary gate).
2. **Done:** `COMMERCIAL-LICENSE.md` stubs beside every commercial `.csproj`.
3. **Done:** removed ambiguous “pending extraction” notes for extracted modules.
4. **Done:** updated `LICENSING.md`, `docs/ProjectTiers.md`, `docs/DistributionModels.md`.

Exit criteria:

- `LICENSING.md` has no “pending extraction” entries for extracted modules.
- Open package graph and commercial module graph are clear.

Validation: `make dependency-boundary-gate`

### Phase E — automate dependency safety (done)

Purpose: prevent boundary regressions.

1. **Done:** `scripts/verify-open-commercial-dependency-boundary.py` classifies open vs commercial projects.
2. **Done:** scan every `.csproj` for `<ProjectReference>`; fail on open→commercial edges.
3. **Done:** require `COMMERCIAL-LICENSE.md` beside commercial projects.
4. **Done:** verify open packable projects resolve `PackageLicenseExpression=Apache-2.0`.
5. **Done:** CI workflow `.github/workflows/dependency-boundary.yml` and `make dependency-boundary-gate`.

### Phase F — networking extraction (done)

Purpose: move cross-node knowledge-sync, network bus, plasticity, and agent-bus bridge out of open `src/`.

1. **Done:** move `Networking` models/ports to `commercial/src/Nexo.Commercial.Fleet.Contracts/Networking/**`.
2. **Done:** move HTTP networking implementations, `BrickUsageTracker`, `AdaptiveBrickCache`, and `AgentBusNetworkBridge` to `commercial/src/Nexo.Commercial.Fleet.Infrastructure/**`.
3. **Done:** move networking tests to `commercial/tests/Nexo.Commercial.Tests.Fleet/**`.
4. **Done:** expose `AddNexoCommercialFleetNetworking()` / `AddAgentBusNetworkBridge()` on commercial fleet DI.

Validation: `make dependency-boundary-gate`, `dotnet test commercial/tests/Nexo.Commercial.Tests.Fleet`.

## Validation gates by phase

| Phase | Minimum validation |
|-------|--------------------|
| A | `dotnet build application/src/Nexo.API/Nexo.API.csproj`, `dotnet test application/src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj`, open/commercial dependency scan |
| B | Build commercial Game Director solution/filter, run Game Director tests, open/commercial dependency scan |
| C | Build open core and commercial fleet solution/filter, run open mesh primitive tests and commercial fleet tests, open/commercial dependency scan |
| D | Package metadata check, docs link check, NuGet pack graph alignment |
| E | New dependency-boundary script passing locally and in CI |
| F | Commercial fleet tests green; no open `src/**/Networking/**` trees remain |

## Recommended PR sequence

1. **PR 1 — API vertical seam:** remove `Nexo.API` and `Nexo.Tests.CLI` direct references to `Nexo.GameDomain`.
2. **PR 2 — CLI/GameDomain seam:** move Unity pipeline helpers into the open CLI surface and remove `Nexo.CLI -> Nexo.GameDomain`.
3. **PR 3 — GameDomain/GameDirector commercial move:** move `Nexo.GameDomain`, GameDirector code, and tests into commercial module layout.
4. **PR 4 — fleet inventory split:** classify open mesh primitives vs commercial fleet/governance files in [`FleetGovernanceExtractionInventory.md`](FleetGovernanceExtractionInventory.md).
5. **PR 5 — fleet contracts baseline:** seed `Nexo.Commercial.Fleet.Contracts` from fleet task/node DTOs and ports.
6. **PR 6 — fleet infrastructure baseline:** seed `Nexo.Commercial.Fleet.Infrastructure` from current fleet implementations.
7. **PR 7 — mesh director migration:** move mesh-lab/operator packaging to `Nexo.Commercial.MeshDirector` and remove the open CLI duplicate.
8. **PR 8 — fleet API baseline:** seed `Nexo.Commercial.Fleet.Api` from open `/api/mesh` fleet/task/knowledge endpoints.
9. **PR 9 — fleet host wiring:** add `Nexo.Commercial.Fleet.Host` and wire commercial fleet DI plus `MapCommercialFleetEndpoints()`.
10. **PR 10 — open fleet cleanup:** migrate mesh-lab peer-a to the commercial fleet host and remove open `/api/mesh` fleet/task/knowledge handlers from `Nexo.API`.
11. **PR 11 — open fleet infrastructure cleanup (done):** removed open fleet trees; mesh-lab worker executor lives in `src/Nexo.Infrastructure/MeshLab/**`; fleet tests moved to `commercial/tests/Nexo.Commercial.Tests.Fleet`.
12. **PR 12 — dependency-boundary gate (done):** `scripts/dependency-boundary-gate.sh`, `.github/workflows/dependency-boundary.yml`, and `make dependency-boundary-gate`.
13. **PR 13 — Phase D licensing (done):** align `Directory.Build.props` license default with Apache-2.0; refresh `LICENSING.md`, `ProjectTiers.md`, and `DistributionModels.md` to match the post-extraction graph.
14. **PR 14 — Phase F networking (done):** move open `Networking` trees and related execution/bridge code into `Nexo.Commercial.Fleet.*`.

Do not combine large refactors retroactively. The dependency graph and licensing boundary should stay reviewable at every step.

## Open questions (post-extraction)

See [`LICENSING.md`](../LICENSING.md) — CLI mesh-hub split and `apps/release-manager` open-source candidacy.
