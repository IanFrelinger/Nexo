# Commercial extraction plan

This plan defines the next commercial-boundary work after the open-core licensing sprint. It is a planning document only: do not move code, rename projects, or change CI from this document alone.

## Goal

Create a clean project/module boundary where:

- Tier 1 **OPEN** projects remain Apache-2.0 and inspectable.
- Tier 2/3 **COMMERCIAL** projects can be licensed separately.
- No open project has a `ProjectReference` to a commercial project.
- Trust primitives stay open; operational governance and vertical packaging are monetized.

## Current blockers

### Vertical / Game Director graph

The vertical code is being separated from the open application surface. The API and CLI seams are now resolved; GameDomain has moved to `commercial/`; Game Director projects are commercially marked in place and can be physically moved in a later cleanup.

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

### Fleet / mesh / governance graph

Fleet-scale mesh and governance behavior is currently woven through open projects:

- `src/Nexo.Core.Application/Fleet/**`
- `src/Nexo.Infrastructure/Fleet/**`
- `src/Nexo.Core.Application/Networking/**`
- `src/Nexo.Infrastructure/Networking/**`
- fleet-scale portions of `src/Nexo.Core.Application/Mesh/**`
- fleet-scale portions of `src/Nexo.Infrastructure/Mesh/**`
- `application/src/Nexo.CLI/Commands/MeshDirectorCommand.cs`
- `application/src/Nexo.CLI/Commands/MeshHubCommand.cs`
- mesh/fleet governance endpoints and middleware under `application/src/Nexo.API/**`

Those files cannot be marked commercial while they remain inside open projects such as `Nexo.Core.Application`, `Nexo.Infrastructure`, `Nexo.Runtime`, `Nexo.Orchestration`, `Nexo.CLI`, or `Nexo.API`.

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
| `Nexo.Commercial.Fleet.Contracts` | selected fleet DTOs/ports now in `Nexo.Core.Application/Fleet` | Commercial contracts needed by commercial fleet runtime; keep only generic/open primitives in open core. |
| `Nexo.Commercial.Fleet.Core` | fleet placement/task/checkpoint abstractions and policies | Fleet scheduling and governance logic. |
| `Nexo.Commercial.Fleet.Infrastructure` | `Nexo.Infrastructure/Fleet` persistence/worker/director services | Commercial implementations for persistence, director state, workers, leases, checkpoints. |
| `Nexo.Commercial.MeshDirector` | CLI/API director/hub surfaces and director persistence | Commercial control plane host/API/CLI module. |
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

### Phase D — licensing and package metadata

Purpose: make the legal boundary match the project graph.

1. Add Apache-2.0 package metadata to open projects.
2. Add commercial stubs to commercial project directories.
3. Remove any ambiguous “pending extraction” notes once extraction is complete.
4. Update `LICENSING.md`, `docs/ProjectTiers.md`, `docs/DistributionModels.md`, and package/solution docs.

Exit criteria:

- `LICENSING.md` has no “pending extraction” entries for extracted modules.
- Open package graph and commercial module graph are clear.

### Phase E — automate dependency safety

Purpose: prevent boundary regressions.

Recommended check:

1. Maintain an allowlist/map of open project paths and commercial project paths.
2. Scan every `.csproj` for `<ProjectReference>`.
3. Fail if an open project references a commercial project.
4. Warn if a commercial project lacks a `COMMERCIAL-LICENSE.md` stub.
5. Warn if an open packable project lacks effective `PackageLicenseExpression=Apache-2.0`.

This can start as a script and become a CI gate after the first extraction PR lands.

## Validation gates by phase

| Phase | Minimum validation |
|-------|--------------------|
| A | `dotnet build application/src/Nexo.API/Nexo.API.csproj`, `dotnet test application/src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj`, open/commercial dependency scan |
| B | Build commercial Game Director solution/filter, run Game Director tests, open/commercial dependency scan |
| C | Build open core and commercial fleet solution/filter, run open mesh primitive tests and commercial fleet tests, open/commercial dependency scan |
| D | Package metadata check, docs link check, NuGet pack graph alignment |
| E | New dependency-boundary script passing locally and in CI |

## Recommended PR sequence

1. **PR 1 — API vertical seam:** remove `Nexo.API` and `Nexo.Tests.CLI` direct references to `Nexo.GameDomain`.
2. **PR 2 — CLI/GameDomain seam:** move Unity pipeline helpers into the open CLI surface and remove `Nexo.CLI -> Nexo.GameDomain`.
3. **PR 3 — GameDomain/GameDirector commercial move:** move `Nexo.GameDomain`, GameDirector code, and tests into commercial module layout.
4. **PR 4 — fleet inventory split:** classify open mesh primitives vs commercial fleet/governance files in [`FleetGovernanceExtractionInventory.md`](FleetGovernanceExtractionInventory.md).
5. **PR 5 — fleet extraction:** move fleet task/direction/governance implementations into commercial modules.
6. **PR 6 — dependency-boundary gate:** add scanner script and optional CI enforcement.

Do not combine these into one large refactor. The dependency graph and licensing boundary should be reviewable at every step.
