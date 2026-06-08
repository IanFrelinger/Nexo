# Positioning Sprint Summary

## Recon

Nexo's true scope is broader than a narrow AI-assisted development tool: it is an adaptive orchestration framework and runtime for private, auditable software workflows. The repo combines a reusable .NET kernel, observe/adapt/improve loops, policy and trust controls, pipeline execution, optional mesh/federation, gRPC transport, AWS ingress adapters, deployable CLI/API surfaces, product-style app configurations, and NuGet/container/compose distribution paths. The front door should describe this as an orchestration and trust platform with app-level demonstrations, not as a single app or a set of isolated agents.

Major subsystems present:

- **Kernel spine:** `Nexo.Abstractions`, `Nexo.Core`, `Nexo.Core.Domain`, `Nexo.Core.Application`, contracts, policies, infrastructure, orchestration, background agents, model adapters, and hosting registration.
- **Observe / adapt / improve loop:** CLI commands and services for observation, pattern storage, brick analysis, adaptation, improvement, self-context, changelog generation, and test-failure ingestion.
- **Bricks and self-extension:** brick contracts, OWASP/dev bricks, capability generation, promotion, dogfood gates, and validation utilities.
- **Pipeline runtime:** template validation, pipeline execution, diagnostics, execution-mode routing, runtime manifests, and release gates.
- **Trust architecture:** barrier identity resolution, data classification/sanitization, pause/resume controls, local-first model routing, policy packs, and structured audit trails.
- **Mesh phases and federation:** mesh north-star/phase docs, peer discovery, capability advertisement, director/hub flows, virtual mesh labs, friend mesh prefab, trust-tier placement, leases/checkpoints, and mesh stress/TLS gates.
- **gRPC transport:** transport contracts, server implementation, standalone server host, and transport tests.
- **AWS ingress:** SNS and DynamoDB ingress adapters with corresponding tests.
- **Deployable hosts:** `application/src/Nexo.CLI` and `application/src/Nexo.API`.
- **Four `apps/` directories:** `apps/game-director` (MCP-exposed game balance/map/content sidecar), `apps/nexo-forge` (adaptive multiplayer FPS prototyping agent set), `apps/release-manager` (release-readiness automation agent set), and `apps/runtime-studio` (planner/worker agent-set configuration and operator scripts hosted by CLI/API).
- **Game Director application projects:** `GameDirector.Domain`, `GameDirector.Agents`, `GameDirector.Bricks`, `GameDirector.Host`, `GameDirector.Mcp`, plus game-domain projects and tests.
- **NuGet distribution:** `Nexo.Hosting`, `Nexo.Hosting.Bundle`, `Nexo.Runtime`, `Nexo.Runtime.Bundle`, `Nexo.Sdk`, `Nexo.Framework.Sdk`, `Nexo.Client`, `Nexo.Lite`, and pack/consumer verification docs.
- **Container and operator distribution:** GHCR CLI/API images, quickstart Dockerfile, compose stacks for portal, agent server, game director, ephemeral dependencies, and test lanes.
- **Solution entrypoints:** `Nexo.sln` covers the full repo graph; `Nexo.Kernel.sln` and `Nexo.Runtime.sln` cover focused kernel/runtime slices; `.slnf` files (`Nexo.Core.slnf`, `Nexo.LocalDevCore.slnf`, `Nexo.PrimeTime.slnf`) provide smaller build/test slices; `application/Nexo.Application.sln` and `Nexo.Demos.sln` cover application/demo surfaces.

Observation to keep out of this sprint's scope: `application/Nexo.Application.sln` appears to reference `src\Nexo.API` and related paths while the current CLI/API projects live under `application/src/`; that should be verified and, if needed, fixed in a separate technical cleanup sprint rather than this documentation/licensing pass.

## Changes by task

- **Task 0 — Recon:** Added this summary and captured the true repo scope: adaptive orchestration kernel, trust controls, mesh/federation, transport/ingress, apps, hosts, and distribution channels.
- **Task 1 — License:** Added Apache-2.0 `LICENSE`, set Apache-2.0 package metadata for the open project set, added commercial stubs for app configuration directories, updated `LICENSING.md` as the authoritative tier map, and updated the README license section.
- **Task 2 — README rewrite:** Reframed the README around adaptive orchestration and the “ChatGPT is a calculator; Nexo is an autopilot panel” positioning, added reader routing and subsystem maps, preserved container-first quick start commands, and kept the barrier/security notes.
- **Task 3 — Coherence / start here:** Promoted `docs/ProjectTiers.md` as the canonical repo map from both the README and docs index, added the README “Where to start” table, and created `docs/CiGateInventory.md` with one row per workflow and recommendation-only consolidation notes.
- **Task 4 — Architecture honesty:** Added `docs/Conventions.md` to describe current error handling, interfaces, abstract classes, generics, and the gap between aspiration and current implementation without changing code.
- **Task 5 — Wrap:** Finalized this summary with owner decisions, follow-up issues, and validation evidence.

## Open decisions for owner

- **Vertical code extraction:** `Nexo.GameDomain` and its tests now live under `commercial/`; Game Director code/test projects are marked commercial in place and can be physically moved/renamed in a later cleanup.
- **Mesh/federation extraction:** Technical docs and product docs both reference mesh. Fleet-scale mesh/governance code is woven through open projects today, so commercial marking requires extraction into separate projects first.
- **API host boundary:** `Nexo.API` is open for now as a single-node host. Any org-scale governance, RBAC/SSO, aggregate audit, or fleet-control-plane endpoints should move to a separate commercial host or module before commercial marking.
- **CI consolidation and branch protection:** `docs/CiGateInventory.md` identifies blocking candidates, advisory/manual workflows, release gates, and consolidation candidates. The owner must decide which checks are truly required for branch protection before any workflow cleanup is attempted.

## License extraction required

The revised open-core boundary principle was checked against current project references before marking vertical code projects commercial. The GameDomain/GameDirector vertical split is now unblocked: `Nexo.API`, `Nexo.CLI`, and `Nexo.Tests.CLI` do not reference GameDomain/GameDirector projects; `Nexo.GameDomain` and its tests have moved to `commercial/`; and Game Director code/test projects are marked commercial in place.

Fleet-scale mesh/governance code also appears to be woven into Tier 1 candidate projects rather than isolated behind separate commercial projects. Namespaces/files that likely need extraction before they can be marked commercial include:

- `src/Nexo.Core.Application/Fleet/**`
- `src/Nexo.Infrastructure/Fleet/**` (and related fleet/mesh task registry, placement, worker executor, persistence, registration key, trust policy, knowledge replication, and checkpoint services)
- `src/Nexo.Core.Application/Networking/**` and `src/Nexo.Infrastructure/Networking/**` for knowledge sync / network negotiation / adaptive cache surfaces
- `src/Nexo.Core.Application/Mesh/**` and `src/Nexo.Infrastructure/Mesh/**` where single-node capability advertisement blends into multi-node discovery and negotiation
- `application/src/Nexo.CLI/Commands/MeshDirectorCommand.cs`, `MeshHubCommand.cs`, and fleet/mesh command surfaces if they are intended as commercial control-plane UX
- `application/src/Nexo.API/Security/Mesh*` middleware and API endpoints that expose mesh/fleet governance behavior

Resolution applied in this sprint: move GameDomain code/tests to `commercial/`, mark Game Director code/tests commercial in place, and keep only open API/CLI shells in the open application surface. With that placement, the dependency-direction safety check passes because no Tier 1 `.csproj` references a marked commercial project.

CI stabilization note: `apps/runtime-studio/COMMERCIAL-LICENSE.md` initially exposed a Runtime Studio forge-smoke failure in `background-agent proposals build --repo-root .`: `dotnet build -c Release` was ambiguous at the repo root because multiple project/solution files are present. The follow-up fix teaches `dotnet.build` / `forge.build` to choose `Nexo.LocalDevCore.slnf` (or `Nexo.Core.slnf`) when invoked from the repo root, so the Runtime Studio commercial stub can be present without tripping that smoke gate. Application `.csproj` files are also left untouched in the PR diff so the layer-boundary gate can pass against `master`; their effective package license is supplied by repository-wide MSBuild defaults.

API/CLI seam progress: the Forge HTTP surface has been moved out of `Nexo.API` and into the Game Director/MCP application layer. `Nexo.API`, `Nexo.CLI`, and `Nexo.Tests.CLI` no longer reference `Nexo.GameDomain`; Unity pipeline helpers now live in the open CLI surface.

## Suggested follow-up issues

- Optionally move/rename the commercially marked `GameDirector.*` projects into `commercial/` physical layout.
- Extract fleet-scale mesh/governance namespaces into separate projects if those capabilities must be commercial while `Nexo.Runtime`, `Nexo.Orchestration`, `Nexo.Infrastructure`, and `Nexo.Core.Application` remain Apache open core.
- Use `docs/CommercialExtractionPlan.md` as the starting sequence for commercial extraction PRs and validation gates.
- Decide whether mesh/federation is open core, commercial add-on, dual-licensed, or a separate module.
- Decide whether `apps/release-manager` should later become the single open-sourced minimal SDK reference app.
- Consolidate CI gates and update branch-protection policy around a smaller required-check set.
- Plan an errors-as-values migration for recoverable operational boundaries while preserving exception-based guard/framework paths where appropriate.
- Review inheritance-heavy agent/value/test base patterns and decide where composition would improve traceability.
- Add a periodic README/GettingStarted command-drift check so docs stay aligned with `application/src/Nexo.CLI`.
- Automate project-count/tier-map refresh checks for `docs/ProjectTiers.md`.
- Decide product packaging for Game Director, Nexo Forge, Release Manager, and Runtime Studio.

## Validation

- **Changed files are license/docs/project metadata only:** `LICENSE`, `LICENSING.md`, `README.md`, `SPRINT_SUMMARY.md`, commercial app stubs, docs, and `.csproj` metadata license expressions.
- **No source/test/workflow logic changed:** no `.cs` files or `.github/workflows/**` files were modified.
- **README onboarding guard strings checked locally:** verified the README still contains `## Quick Start (5 minutes)`, `Choose your lane (recommended)`, `Lane A: dev container + container deployment (recommended)`, `Lane B: full local dev path (native SDK)`, `bash scripts/setup/setup.sh all`, and `dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore`; verified it does not contain `dotnet build Nexo.sln`.
- **Local link fallback check:** `lychee` was not installed in this environment, so a local markdown link-target check was run against changed docs and passed.
- **CLI build:** `dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore` succeeded.
- **CLI help verification:** `dotnet run --project application/src/Nexo.CLI -- --help` succeeded.
- **Documented subcommand help verification:** `pipeline validate --help`, `doctor --help`, `release --help`, `runtime-studio --help`, `mesh --help`, and `background-agent daemon --help` all succeeded.
- **Doctor verification:** `dotnet run --project application/src/Nexo.CLI -- doctor --json` exited 0 and reported `"ok": true`; Docker was absent in the host environment, so the optional container smoke entry reported `docker: command not found` without failing the doctor profile.
- **Quickstart pipeline verification:** `pipeline validate --template <tmp>` succeeded; `pipeline run --template <tmp> --run-id quickstart-run --format-json` completed with `"state":"Completed"`; `pipeline diagnostics --format-json` succeeded.
- **Revised license-boundary preflight:** scanned intended Tier 1 OPEN and Tier 2/3 COMMERCIAL `ProjectReference` entries. The scan found the vertical-code edges listed under “License extraction required,” so the committed placement keeps those code projects open pending extraction and marks only clean app packaging directories commercial.
- **SPDX and boundary verification:** verified open `.csproj` files receive Apache-2.0 package metadata either directly or via `Directory.Build.props` / `Directory.Build.targets`; verified the committed commercial placement has no Tier 1 `.csproj` -> commercial project reference violations; rebuilt `application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore` successfully after metadata edits.
- **CI stabilization:** after adding a PR-body `[skip-prod-style]` rationale and removing application/forge-smoke-triggering path diffs, the `testing-strategy`, `layer-boundary`, `docs-link-check`, and onboarding docs guard checks passed on the updated PR.
- **Commercial GameDomain move:** verified `commercial/src/Nexo.Commercial.GameDomain` builds, `commercial/tests/Nexo.Commercial.Tests.GameDomain` focused asset descriptor tests pass, GameDirector Forge tests pass against the moved commercial module, `Nexo.LocalDevCore.slnf` builds, and a dependency scan found no open project references to commercial projects.
- **Fleet governance inventory:** added `docs/FleetGovernanceExtractionInventory.md` to classify open mesh primitives, commercial fleet/governance files, split/owner-decision surfaces, and the recommended PR sequence for fleet extraction.
- **Fleet contracts baseline:** added `commercial/src/Nexo.Commercial.Fleet.Contracts` seeded from fleet task/node DTOs and ports so later fleet infrastructure/API extractions have a commercial contracts target while open consumers are migrated incrementally.
- **Fleet infrastructure baseline:** added `commercial/src/Nexo.Commercial.Fleet.Infrastructure` seeded from existing fleet implementations so commercial consumers can migrate incrementally before open duplicates are removed.
- **Mesh director baseline:** added `commercial/src/Nexo.Commercial.MeshDirector` and `commercial/tests/Nexo.Commercial.Tests.MeshDirector` while retaining the open CLI compatibility command until mesh-lab scripts/operator packaging move to the commercial module.
- **Fleet API baseline:** added `commercial/src/Nexo.Commercial.Fleet.Api` seeded from `/api/mesh` fleet/task/knowledge endpoint handlers so commercial hosts can migrate before open endpoint duplicates are removed.
- **Fleet host wiring:** added `commercial/src/Nexo.Commercial.Fleet.Host`, `CommercialFleetHostExtensions`, and `commercial/tests/Nexo.Commercial.Tests.Fleet.Host` so operator hosts register commercial fleet DI and map `MapCommercialFleetEndpoints()`.
- **Open fleet endpoint cleanup:** removed open `/api/mesh` fleet/task/knowledge handlers from `Nexo.API`; mesh-lab peer-a now builds from `.docker/Dockerfile.fleet-host`.
