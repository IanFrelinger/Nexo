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
- **Task 1 — License:** Added Apache-2.0 `LICENSE`, added `LICENSING.md`, and updated the README license section to point at both the license and the open-core boundary inventory.
- **Task 2 — README rewrite:** Reframed the README around adaptive orchestration and the “ChatGPT is a calculator; Nexo is an autopilot panel” positioning, added reader routing and subsystem maps, preserved container-first quick start commands, and kept the barrier/security notes.
- **Task 3 — Coherence / start here:** Promoted `docs/ProjectTiers.md` as the canonical repo map from both the README and docs index, added the README “Where to start” table, and created `docs/CiGateInventory.md` with one row per workflow and recommendation-only consolidation notes.
- **Task 4 — Architecture honesty:** Added `docs/Conventions.md` to describe current error handling, interfaces, abstract classes, generics, and the gap between aspiration and current implementation without changing code.
- **Task 5 — Wrap:** Finalized this summary with owner decisions, follow-up issues, and validation evidence.

## Open decisions for owner

- **Open-core boundary:** `LICENSING.md` records candidate open-core and future commercial-tier projects only as `ASSUMPTION:` entries. The owner must decide the definitive boundary before any project is relicensed, moved behind commercial terms, dual-licensed, or marketed as part of a paid tier.
- **Mesh/federation packaging:** Technical docs and product docs both reference mesh, but the owner must decide whether mesh remains Apache open core, becomes a paid add-on, is dual-licensed, or is separated into another module.
- **Ingress/app packaging:** AWS ingress adapters and the four `apps/` configurations need an owner decision: open integrations/samples, commercial connectors/SKU templates, or something else.
- **CI consolidation and branch protection:** `docs/CiGateInventory.md` identifies blocking candidates, advisory/manual workflows, release gates, and consolidation candidates. The owner must decide which checks are truly required for branch protection before any workflow cleanup is attempted.

## Suggested follow-up issues

- Define and approve the legal open-core/commercial licensing boundary.
- Decide whether mesh/federation is open core, commercial add-on, dual-licensed, or a separate module.
- Decide whether AWS ingress adapters and app configurations are open examples, commercial connectors/SKU templates, or internal presets.
- Consolidate CI gates and update branch-protection policy around a smaller required-check set.
- Plan an errors-as-values migration for recoverable operational boundaries while preserving exception-based guard/framework paths where appropriate.
- Review inheritance-heavy agent/value/test base patterns and decide where composition would improve traceability.
- Add a periodic README/GettingStarted command-drift check so docs stay aligned with `application/src/Nexo.CLI`.
- Automate project-count/tier-map refresh checks for `docs/ProjectTiers.md`.
- Decide product packaging for Game Director, Nexo Forge, Release Manager, and Runtime Studio.

## Validation

- **Changed files are docs/license only:** `LICENSE`, `LICENSING.md`, `README.md`, `SPRINT_SUMMARY.md`, `docs/CiGateInventory.md`, `docs/Conventions.md`, `docs/DocsIndex.md`, and `docs/ProjectTiers.md`.
- **No source/test/workflow logic changed:** no files under `src/**`, `application/src/**`, or `.github/workflows/**` were modified.
- **README onboarding guard strings checked locally:** verified the README still contains `## Quick Start (5 minutes)`, `Choose your lane (recommended)`, `Lane A: dev container + container deployment (recommended)`, `Lane B: full local dev path (native SDK)`, `bash scripts/setup/setup.sh all`, and `dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore`; verified it does not contain `dotnet build Nexo.sln`.
- **Local link fallback check:** `lychee` was not installed in this environment, so a local markdown link-target check was run against changed docs and passed.
- **CLI build:** `dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore` succeeded.
- **CLI help verification:** `dotnet run --project application/src/Nexo.CLI -- --help` succeeded.
- **Documented subcommand help verification:** `pipeline validate --help`, `doctor --help`, `release --help`, `runtime-studio --help`, `mesh --help`, and `background-agent daemon --help` all succeeded.
- **Doctor verification:** `dotnet run --project application/src/Nexo.CLI -- doctor --json` exited 0 and reported `"ok": true`; Docker was absent in the host environment, so the optional container smoke entry reported `docker: command not found` without failing the doctor profile.
- **Quickstart pipeline verification:** `pipeline validate --template <tmp>` succeeded; `pipeline run --template <tmp> --run-id quickstart-run --format-json` completed with `"state":"Completed"`; `pipeline diagnostics --format-json` succeeded.
