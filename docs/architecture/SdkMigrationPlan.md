# Plan: close remaining SDK-style layout gaps

## Execution status (branch)

| Item | Status |
| ---- | ------ |
| **Track A — Hosting partials** | **`AddNexo`** → **`NexoKernelRegistrar.Register`**; **`NexoKernelRegistrationContext`** + **20 private phase methods** in **`NexoKernelRegistrar.Phases.cs`** (same **`// ──`** sections). **`ModuleSelection`** in **`NexoKernelRegistrationModels.cs`**. **`NexoServiceCollectionExtensions.Deployment.cs`** holds deployment helpers + **`RegisterNodeCapabilityRuntime`**. |
| **Track B — Infrastructure `Sdk/`** | **Done:** `*ServiceCollectionExtensions` under **`Feature/Sdk/Extensions/`** with **`Nexo.Infrastructure.Sdk.<Area>`** namespaces. Collision-safe: **`Nexo.Infrastructure.NodeCapabilityRuntime.Sdk`**, **`Nexo.Infrastructure.Execution.Sdk`**, **`Nexo.Infrastructure.Execution.Routing.Sdk`**, **`Nexo.Infrastructure.Mesh.Sdk`**. |
| **Consumer projects** | **`GlobalUsings.Infrastructure.Sdk.cs`** in **`Nexo.Hosting`**; **`Nexo.CLI`** and **`Nexo.Tests.Infrastructure`** link it for Sdk extension resolution. |
| **Non-goal — ports** | **`INexoSdkBuilder`** in **`Nexo.Infrastructure.Sdk.Ports`**. |
| **Non-goal — megapackage** | **`Nexo.Framework.Sdk`** + **`AddNexoFramework`**. |

---

This document turns the “remaining gaps” from [`SdkStructure.md`](SdkStructure.md) into **ordered, low-risk work** with clear completion criteria. No calendar estimates — only **what must change**, **dependencies**, and **risk**.

---

## Goal

1. **`Nexo.Hosting` — `NexoServiceCollectionExtensions`**  
   Split the monolithic `AddNexo` registration into **partial static classes** so each subsystem is navigable without changing behavior or public API.

2. **`Nexo.Infrastructure`**  
   Adopt **`Sdk/Extensions/`** (and optional **`Sdk/Options`**, **`Sdk/Builders`**) **per feature area**. DI extension namespaces use **`Nexo.Infrastructure.Sdk.<Subsystem>`** (with collision-safe variants noted in [`SdkMigrationPlan.md`](SdkMigrationPlan.md) execution status).

---

## Principle (non-negotiable)

- **Sdk extension namespaces** — `*ServiceCollectionExtensions` for DI use **`Nexo.Infrastructure.Sdk.*`** (see [`SdkStructure.md`](SdkStructure.md)). Application/runtime types keep existing **`Nexo.Infrastructure.<Feature>`** namespaces. Consumer apps may use **`GlobalUsings.Infrastructure.Sdk.cs`** (or explicit `using` lines) to bring extension methods into scope.
- **One mechanical theme per PR** — easier review, bisection, and rollback.
- **`dotnet build Nexo.sln` + relevant `dotnet test` filters** after each merge.

---

## Track A — Split `NexoServiceCollectionExtensions` (Hosting)

### A.1 Inventory

Target file: `src/Nexo.Hosting/NexoServiceCollectionExtensions.cs` (~700+ lines).

Identify natural seams (already commented in source):

| Partial file (proposed) | Contents |
| ----------------------- | -------- |
| `NexoServiceCollectionExtensions.cs` | Public `AddNexo`, `AddNexoProfile`; delegate to internals |
| `NexoServiceCollectionExtensions.Deployment.cs` | `ModuleSelection`, `GetModuleSelection`, `ResolveDeploymentProfile`, `TryParseDeploymentProfile`, `ResolveStrictMode`, `ParseBooleanEnvironmentVariable` |
| `NexoServiceCollectionExtensions.NodeCapabilityRuntime.cs` | `RegisterNodeCapabilityRuntime` |
| `NexoServiceCollectionExtensions.AddNexo.Core.cs` | Strict mode, configuration, MediatR, config adapter, loop kernel start |
| `NexoServiceCollectionExtensions.AddNexo.Orchestration.cs` | Orchestration + optional transport |
| `NexoServiceCollectionExtensions.AddNexo.PersistenceAdaptation.cs` | Persistence, adaptation, federated mesh, copilot store |
| `NexoServiceCollectionExtensions.AddNexo.KnowledgePipeline.cs` | Knowledge query, pipeline composition |
| `NexoServiceCollectionExtensions.AddNexo.AgentsObservation.cs` | Background agents, RAG, observation pipeline |
| `NexoServiceCollectionExtensions.AddNexo.ModelExecution.cs` | Model decorator chain, provider factory branches |
| `NexoServiceCollectionExtensions.AddNexo.WorkflowTesting.cs` | Workflow executor, analysis/validation, testing adapters |

Exact slice boundaries can follow the **`// ── Section ──`** comments already in the file; adjust partial names if a slice is still too large.

### A.2 Completion criteria

- All partials use **`partial static class NexoServiceCollectionExtensions`** in namespace **`Nexo.Hosting`**.
- **No** duplicate static helper names across partials (private methods stay private per partial).
- **Zero** behavior change: same registration order and conditional branches.

### A.3 Risks

- **Merge conflicts** if many branches touch `AddNexo` — mitigate by completing Track A in one focused PR after a short freeze or rebasing feature branches.

---

## Track B — Infrastructure SDK folders (incremental)

### B.1 Convention (repeat per area)

Under each **top-level feature folder** (e.g. `Observation/`, `NodeCapabilityRuntime/`, `Pipelines/`):

```
Feature/
  Sdk/
    Options/          # *Options.cs already co-located — move here if not
    Extensions/       # *ServiceCollectionExtensions.cs for this feature
  ... existing impl files ...
```

- Keep **`namespace Nexo.Infrastructure.<Feature>`** on moved files.
- **Do not** force every subfolder into `Sdk/` — only types that are clearly **registration surface** or **options bags**.

### B.2 Suggested order (dependency / churn)

| Phase | Area | Rationale |
| ----- | ---- | ----------- |
| **B.2.1** | **NodeCapabilityRuntime** | Already has `NodeCapabilityRuntimeOptions.cs` + `*ServiceCollectionExtensions.cs`; small, validates pattern. |
| **B.2.2** | **Observation** | `Observation`-related extensions + options grouped; touches Forge-adjacent tests. |
| **B.2.3** | **Pipelines** | Many `Pipeline*` options + `PipelineServiceCollectionExtensions`; high readability win. |
| **B.2.4** | **Persistence / Database** | `PersistenceServiceCollectionExtensions`, `DatabaseServiceCollectionExtensions`, Ephemeral options. |
| **B.2.5** | **Adaptation** | `AdaptationServiceCollectionExtensions`, `AdaptationBrickOptions`. |
| **B.2.6** | **Trust** | `TrustServiceCollectionExtensions`, boundary/gate options. |
| **B.2.7** | **Execution** (routing, mesh bricks) | Larger; split only extension entry files first, leave adapters in place. |
| **B.2.8** | **Remaining** `*ServiceCollectionExtensions.cs` at Infrastructure root | Sweep or fold into nearest feature `Sdk/Extensions`. |

Lower phases can proceed in parallel **only** if they touch disjoint paths (separate PRs).

### B.3 Completion criteria (per phase)

- Files compile with **unchanged namespaces**.
- No new public API unless explicitly intended (prefer moves only).
- **`dotnet build`** + **`dotnet test Nexo.LocalDevCore.slnf`** (or narrower filter if area-specific tests exist).

### B.4 Risks

- **Glob imports / IDE** — developers rely on path; communicate in [`SdkStructure.md`](SdkStructure.md) when a phase completes.
- **Copy-assemblies / test harness** — `Nexo.Tests.Infrastructure` copies assemblies; confirm **no hard-coded paths** to old locations (usually unaffected).

---

## Track C — Documentation sync

After each Track A/B milestone:

- One-line note under **Folder conventions** in [`SdkStructure.md`](SdkStructure.md) listing completed areas (optional table).
- No duplicate prose — link to this plan for “what’s left.”

---

## Definition of “done” for the overall initiative

- **Track A** merged: `AddNexo` is split into partials; reviewers can navigate by subsystem file.
- **Track B** merged through **B.2.8** (or explicitly descoped areas listed with reason).
- CI green on **`Nexo.sln`** and **`Nexo.LocalDevCore.slnf`** (and **`Nexo.PrimeTime.slnf`** if present on branch).

---

## Explicit non-goals (unless product asks)

- Renaming **`Nexo.Infrastructure`** namespaces to `Nexo.Infrastructure.Sdk.*` (breaking).
- Introducing a **single mega-package** that re-exports all extensions (maintenance burden).
- Moving **port interfaces** out of `Nexo.Core.Application` into Infrastructure.
