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

## Open decisions for owner

- **Open-core boundary:** `LICENSING.md` records candidate open-core and future commercial-tier projects only as `ASSUMPTION:` entries. The owner must decide the definitive boundary before any project is relicensed, moved behind commercial terms, dual-licensed, or marketed as part of a paid tier.
- **Mesh/federation packaging:** Technical docs and product docs both reference mesh, but the owner must decide whether mesh remains Apache open core, becomes a paid add-on, is dual-licensed, or is separated into another module.
- **Ingress/app packaging:** AWS ingress adapters and the four `apps/` configurations need an owner decision: open integrations/samples, commercial connectors/SKU templates, or something else.

## Suggested follow-up issues

- Define and approve the legal open-core/commercial licensing boundary.

## Validation

_To be finalized as tasks are completed._
