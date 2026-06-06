# Licensing and open-core boundary

Nexo is licensed under the Apache License, Version 2.0. See [`LICENSE`](LICENSE).

This document records the intended open-core split as it can be inferred from the current repository documentation, especially [`docs/DistributionModels.md`](docs/DistributionModels.md). It is **not** a relicensing action and does **not** finalize a commercial boundary. Ambiguous projects remain ambiguous until the owner makes an explicit product/licensing decision.

## Current license

The repository contents are covered by Apache-2.0 unless a file or directory later receives an explicit, owner-approved notice stating otherwise. No such alternate notice is introduced in this sprint.

## Candidate open-core projects

The projects below look like candidates for the open core because they are documented as reusable runtime, SDK, client, or package surfaces in the current distribution model.

| Candidate | Rationale |
|-----------|-----------|
| `ASSUMPTION:` `src/Nexo.Abstractions` | Shared agent/model abstractions used by the reusable kernel. |
| `ASSUMPTION:` `src/Nexo.Core` | Shared primitives that support the kernel spine. |
| `ASSUMPTION:` `src/Nexo.Core.Domain` | Domain model and defaults for the core runtime. |
| `ASSUMPTION:` `src/Nexo.Core.Application` | Application use cases and ports used by host and package consumers. |
| `ASSUMPTION:` `src/Nexo.Contracts` | Cross-cutting contracts for package and host boundaries. |
| `ASSUMPTION:` `src/Nexo.Brick.Contracts` | Extension contracts for bricks/components. |
| `ASSUMPTION:` `src/Nexo.Policies` | Policy primitives used by the kernel. |
| `ASSUMPTION:` `src/Nexo.Infrastructure` | Runtime infrastructure used by the host/embed graph. |
| `ASSUMPTION:` `src/Nexo.Orchestration` | Orchestration services that make the kernel useful as a workflow runtime. |
| `ASSUMPTION:` `src/Nexo.BackgroundAgents` | Background-agent services used by local and hosted orchestration workflows. |
| `ASSUMPTION:` `src/Nexo.BackgroundAgents.HostRunners` | Host runner adapters used by CLI/API and app-level configurations. |
| `ASSUMPTION:` `src/Nexo.Adapters.Models` | Model adapter wiring used by the documented provider model. |
| `ASSUMPTION:` `src/Nexo.Hosting` | `AddNexo()` host integration surface documented as a NuGet host-embed path. |
| `ASSUMPTION:` `src/Nexo.Hosting.Bundle` | Bundle/metapackage for the documented NuGet host-embed path. |
| `ASSUMPTION:` `src/Nexo.Runtime` | Runtime services, barriers, and routing used by package and host consumers. |
| `ASSUMPTION:` `src/Nexo.Runtime.Bundle` | Bundle/metapackage for runtime-only package consumers. |
| `ASSUMPTION:` `src/Nexo.Sdk` | SDK registration surface documented as a NuGet consumer path. |
| `ASSUMPTION:` `src/Nexo.Framework.Sdk` | Framework-facing SDK surface. |
| `ASSUMPTION:` `src/Nexo.Client` | Typed HTTP client used by external consumers. |
| `ASSUMPTION:` `src/Nexo.Lite` | Reduced-surface distribution package. |
| `ASSUMPTION:` `src/Nexo.Tools.Assembly` | Assembly tooling referenced by validation and package workflows. |
| `ASSUMPTION:` `src/ValidationUtilities` | Shared validation helper project. |

## Candidate future commercial-tier or product-boundary projects

The projects and paths below may belong in open core, may remain samples/apps, or may become part of a future commercial tier. The current repository documentation does not define the legal boundary, so each entry is intentionally labeled as an assumption and must be resolved by the owner before any commercial packaging or relicensing.

| Candidate | Why it needs an owner decision |
|-----------|--------------------------------|
| `ASSUMPTION:` `src/Nexo.Transport.Grpc`, `src/Nexo.Transport.Grpc.Server`, `src/Nexo.Transport.Grpc.Server.Host` | Transport is documented as an optional distribution/mesh surface. The owner must decide whether it is open infrastructure or part of a paid federation tier. |
| `ASSUMPTION:` mesh and federation features built on the kernel/runtime | Product docs describe mesh as a possible premium add-on, while technical docs present mesh labs and phases in-repo. The exact open-core boundary is undecided. |
| `ASSUMPTION:` `src/Nexo.Ingress.AwsSns` and `src/Nexo.Ingress.DynamoDb` | Cloud ingress adapters may be open integration examples or commercial/operator connectors. The owner must decide. |
| `ASSUMPTION:` `src/Nexo.Policies.Dev`, `src/Nexo.Tools.Dev`, and `src/Nexo.Bricks.Owasp` | These are dev/tooling/policy projects used by the CLI graph today. Their packaging boundary is not specified in `docs/DistributionModels.md`. |
| `ASSUMPTION:` `application/src/Nexo.CLI` and `application/src/Nexo.API` | The CLI/API are deployable product surfaces and also the main repo operating surface. The owner must decide whether all host code is open core or whether a future control plane is separate. |
| `ASSUMPTION:` `application/src/GameDirector.*` and `application/src/Nexo.GameDomain` | Game Director is a product-style vertical application. Current docs do not state whether it is a sample, open app, or commercial SKU template. |
| `ASSUMPTION:` `apps/game-director` | App configuration for the Game Director sidecar; packaging/licensing status is undecided. |
| `ASSUMPTION:` `apps/nexo-forge` | App configuration for adaptive multiplayer FPS prototyping; packaging/licensing status is undecided. |
| `ASSUMPTION:` `apps/release-manager` | App configuration for release-readiness automation; packaging/licensing status is undecided. |
| `ASSUMPTION:` `apps/runtime-studio` | Runtime Studio is an application-level planner/worker agent-set configuration; packaging/licensing status is undecided. |

## Not decided in this sprint

- Whether mesh/federation is Apache-licensed open core, a paid add-on, dual-licensed, or a separate commercial module.
- Whether AWS and future cloud ingress adapters are open integrations or paid connectors.
- Whether the four `apps/` directories are samples, open products, commercial SKU templates, or internal deployment presets.
- Whether a future SaaS/control-plane layer will live in this repository or in a separate private repository.
- Whether any project should receive a non-Apache license or additional commercial terms.

Until those decisions are made, contributors should treat this document as an inventory and decision log, not a final legal architecture.
