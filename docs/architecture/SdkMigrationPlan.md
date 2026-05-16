# Plan: close remaining SDK-style layout gaps

## Execution status (branch)

| Item | Status |
| ---- | ------ |
| **Track A — Hosting partials** | **`AddNexo`** → **`NexoKernelRegistrar.Register`**; **`NexoKernelRegistrationContext`** + **20 phase methods** in **`NexoKernelRegistrar.Phases.cs`** + **`NexoKernelRegistrar.Ephemeral.cs`** (`EphemeralModelsEnabled`). **`ModuleSelection`** in **`NexoKernelRegistrationModels.cs`**. **`NexoServiceCollectionExtensions.Deployment.cs`** — deployment profile helpers; **`NexoServiceCollectionExtensions.NodeCapabilityRuntime.cs`** — **`RegisterNodeCapabilityRuntime`**. |
| **Track B — Infrastructure `Sdk/`** | **Done:** `*ServiceCollectionExtensions` under **`Feature/Sdk/Extensions/`** with **`Nexo.Infrastructure.Sdk.<Area>`** namespaces. Collision-safe: **`Nexo.Infrastructure.NodeCapabilityRuntime.Sdk`**, **`Nexo.Infrastructure.Execution.Sdk`**, **`Nexo.Infrastructure.Execution.Routing.Sdk`**, **`Nexo.Infrastructure.Mesh.Sdk`**. |
| **Consumer projects** | **`GlobalUsings.Infrastructure.Sdk.cs`** in **`Nexo.Hosting`**; **`Nexo.CLI`** and **`Nexo.Tests.Infrastructure`** link it for Sdk extension resolution. |
| **Non-goal — ports** | **`INexoSdkBuilder`** in **`Nexo.Infrastructure.Sdk.Ports`**. |
| **Non-goal — megapackage** | **`Nexo.Framework.Sdk`** + **`AddNexoFramework`**. |

---

This document turns the “remaining gaps” from [`SdkStructure.md`](SdkStructure.md) into **ordered, low-risk work** with clear completion criteria. No calendar estimates — only **what must change**, **dependencies**, and **risk**.

---

## Goal

1. **`Nexo.Hosting` — kernel registration**  
   Keep **`AddNexo`** as the public entry point while making wiring **navigable**: deployment/profile resolution stays in **`NexoServiceCollectionExtensions`** partials (**`Deployment`**, **`NodeCapabilityRuntime`**); all subsystem registration runs through **`NexoKernelRegistrar`** (**`Register`** → **`NexoKernelRegistrationContext`** → **`RegisterPhase01`–`RegisterPhase20`** in **`NexoKernelRegistrar.Phases.cs`**). Behavior and order remain unchanged.

2. **`Nexo.Infrastructure`**  
   DI registration surface lives under **`Feature/Sdk/Extensions/`** with **`Nexo.Infrastructure.Sdk.*`** extension namespaces (collision-safe **`Nexo.Infrastructure.<Feature>.Sdk`** where needed). Implementation types stay under **`Nexo.Infrastructure.<Feature>`**. Optional physical **`Sdk/Options/`** groups option types without renaming namespaces (see **`Pipelines/Sdk/Options/`** pilot).

---

## Principle (non-negotiable)

- **Sdk extension namespaces** — `*ServiceCollectionExtensions` for DI use **`Nexo.Infrastructure.Sdk.*`** (see [`SdkStructure.md`](SdkStructure.md)). Application/runtime types keep existing **`Nexo.Infrastructure.<Feature>`** namespaces. Consumer apps may use **`GlobalUsings.Infrastructure.Sdk.cs`** (or explicit `using` lines) to bring extension methods into scope.
- **One mechanical theme per PR** — easier review, bisection, and rollback.
- **`dotnet build Nexo.sln` + relevant `dotnet test` filters** after each merge.

---

## Track A — Hosting composition (supersedes old “slice AddNexo into many partials” plan)

### A.1 Files (current)

| File | Role |
| ---- | ---- |
| `NexoServiceCollectionExtensions.cs` | **`AddNexo`**, **`AddNexoProfile`** → builds **`NexoKernelRegistrationContext`** and calls **`NexoKernelRegistrar.Register`**. |
| `NexoServiceCollectionExtensions.Deployment.cs` | **`ResolveDeploymentProfile`**, **`GetModuleSelection`**, **`ResolveStrictMode`**, **`ParseBooleanEnvironmentVariable`** (internal). |
| `NexoServiceCollectionExtensions.NodeCapabilityRuntime.cs` | **`RegisterNodeCapabilityRuntime`** (internal) — NCR + model artifact catalog wiring when NCR is enabled. |
| `NexoKernelRegistrationModels.cs` | **`ModuleSelection`**, **`NexoKernelRegistrationContext`**. |
| `NexoKernelRegistrar.cs` | **`Register`** dispatches phases **01–20**. |
| `NexoKernelRegistrar.Phases.cs` | One private method per **`// ──`** section (kernel subsystems). |
| `NexoKernelRegistrar.Ephemeral.cs` | **`EphemeralModelsEnabled()`** shared by ephemeral lifecycle + trust phases. |

### A.2 Completion criteria

- **Zero** behavior change vs monolithic registration: same order, env vars, and deployment-profile gates.
- Reviewers can open **`NexoKernelRegistrar.Phases.cs`** and jump by section comment.

### A.3 Risks

- **Merge conflicts** if many branches touch **`NexoKernelRegistrar.Phases.cs`** or **`NexoServiceCollectionExtensions`** entry points — coordinate kernel wiring changes in focused PRs.

---

## Track B — Infrastructure SDK folders (incremental)

### B.1 Convention (repeat per area)

Under each **top-level feature folder** (e.g. `Observation/`, `NodeCapabilityRuntime/`, `Pipelines/`):

```
Feature/
  Sdk/
    Options/          # optional — registration-related *Options.cs (namespaces often unchanged)
    Extensions/       # *ServiceCollectionExtensions.cs — DI registration surface
  ... existing impl files ...
```

- **DI extension classes** (`*ServiceCollectionExtensions`) use **`Nexo.Infrastructure.Sdk.<Subsystem>`** (or **`Nexo.Infrastructure.<Subsystem>.Sdk`** when the simple name collides with runtime types).
- **Implementation types** (stores, adapters, domain-ish infrastructure services) keep **`Nexo.Infrastructure.<Feature>`** (or deeper sub-namespaces). Physical folder may differ from namespace by design.
- **Do not** force every type into `Sdk/` — only **registration entry points** and, optionally, **options bags** under **`Sdk/Options/`**.

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

- Types compile with **stable public namespaces** (extensions moved to **`Nexo.Infrastructure.Sdk.*`** as agreed; options moved physically may keep **`Nexo.Infrastructure.<Feature>`** namespace).
- No new public API unless explicitly intended (prefer moves only).
- **`dotnet build Nexo.sln`** + targeted tests when touching DI.

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

This initiative is **functionally complete** for kernel DI and Infrastructure Sdk extensions (see **Execution status** above). Remaining work is **documentation alignment**, **optional layout polish**, **consumer ergonomics**, and **CI clarity** — tracked in **[Plan: close remaining gaps](#plan-close-remaining-gaps-post-migration)** below.

Historical bullets (superseded where noted):

- **Track A — achieved differently:** `AddNexo` delegates to **`NexoKernelRegistrar`** with phase partials (`NexoKernelRegistrar.Phases.cs`), not multiple `NexoServiceCollectionExtensions.*` partial files. Navigation goal is met via registrar phases + `Deployment` partial.
- **Track B — DI extensions:** `*ServiceCollectionExtensions` live under **`Feature/Sdk/Extensions/`** with **`Nexo.Infrastructure.Sdk.*`** (and collision-safe `*.Sdk` namespaces). Optional **`Sdk/Options`** physical grouping remains incremental.
- CI green on **`Nexo.sln`**; **`Nexo.LocalDevCore.slnf`** / **`Nexo.PrimeTime.slnf`** as documented in repo CI / contributor docs (see closing plan).

---

## Plan: close remaining gaps (post-migration)

Ordered for **low risk** first; each phase can be its own PR.

### Phase D1 — Documentation alignment (required)

| Step | Action | Done when |
| ---- | ------ | --------- |
| D1.1 | Rewrite **Goal**, **Track A §A.1–A.2**, and **Definition of done** in this file so they describe **`NexoKernelRegistrar`** + **`NexoKernelRegistrationContext`** + **`NexoKernelRegistrar.Phases.cs`**, not hypothetical `NexoServiceCollectionExtensions.AddNexo.*` partials. | Text matches repo; no contradictory inventory tables. |
| D1.2 | Fix **Track B §B.1**: state that **DI extension types** use **`Nexo.Infrastructure.Sdk.*`** (and **`Nexo.Infrastructure.<Feature>.Sdk`** where collision-safe), while **implementation types** remain **`Nexo.Infrastructure.<Feature>`**. Remove “keep namespace on moved files” if it implies zero namespace change for extensions. | Single coherent rule for extensions vs runtime types. |
| D1.3 | Add a short **“Completed areas”** table to [`SdkStructure.md`](SdkStructure.md) (folders + extension namespace pattern), or a bullet list linking to feature folders under **`Sdk/Extensions/`**. | Readers see what’s migrated without reading git history. |

### Phase D2 — Mechanical repo sweep (required)

| Step | Action | Done when |
| ---- | ------ | --------- |
| D2.1 | Search for **`*ServiceCollectionExtensions.cs`** outside **`**/Sdk/Extensions/`** under `src/Nexo.Infrastructure`. Either move stragglers into **`Sdk/Extensions/`** or document why they stay (e.g. generated, exceptional). | No unexplained duplicates at old paths. |
| D2.2 | Confirm **Observation** and other pilots still compile and tests touching DI registration pass (narrow filters acceptable). | `dotnet build Nexo.sln` green. |

### Phase D3 — Consumer ergonomics (recommended)

| Step | Action | Done when |
| ---- | ------ | --------- |
| D3.1 | Audit **`*.csproj`** files that **reference `Nexo.Infrastructure`** and call Sdk extension methods **without** going through **`AddNexo`**. For each: add **`<Compile Link="...GlobalUsings.Infrastructure.Sdk.cs">`** (same pattern as CLI / Tests.Infrastructure) **or** explicit **`using Nexo.Infrastructure.Sdk.*`** in a single `Usings.cs`. | No CS1061 surprises when adding new Sdk namespaces to Hosting’s global-usings file. |
| D3.2 | Document the **recommended pattern** in [`SdkStructure.md`](SdkStructure.md) (“link Hosting `GlobalUsings.Infrastructure.Sdk.cs` vs explicit usings”). | Contributors have a default choice. |

### Phase D4 — Optional `Sdk/Options` layout (incremental, descoping allowed)

| Step | Action | Done when |
| ---- | ------ | --------- |
| D4.1 | Pick **one** feature (e.g. **Pipelines** or **NodeCapabilityRuntime**) and move **registration-related option types** into **`Feature/Sdk/Options/`** without changing **public type names** or namespaces unless deliberate. | Pattern validated; tests/build green. |
| D4.2 | Repeat per feature **only** where readability wins; otherwise list **explicitly descoped** areas in this plan. | No forced churn for marginal benefit. |

**D4.1 pilot (done):** `PipelineExecutionOptions`, `PipelinePersistenceOptions`, and `PipelineExecutionAdapterOptions` live under **`src/Nexo.Infrastructure/Pipelines/Sdk/Options/`**; namespaces remain **`Nexo.Infrastructure.Pipelines`**.

**D4.2 descoped (for now):** bulk moves for NodeCapabilityRuntime options, Trust, Adaptation, Persistence DB options — defer until a feature owner requests clearer separation.

### Phase D5 — Hosting polish (optional)

| Step | Action | Done when |
| ---- | ------ | --------- |
| D5.1 | Extract **`RegisterNodeCapabilityRuntime`** into a dedicated **`NexoServiceCollectionExtensions.NodeCapabilityRuntime.cs`** partial **or** leave as-is with a one-line comment pointing to **`NexoKernelRegistrar`** phase 01. | Clear ownership of NCR registration story. |
| D5.2 | Optionally deduplicate **`ephemeralModels`** computation between **`RegisterPhase14_EphemeralLifecycle`** and **`RegisterPhase15_TrustProviderFactory3wayBranching`** via a private static helper or a small value on **`NexoKernelRegistrationContext`** (only if behavior stays identical). | One env-read path or documented equivalence. |

### Phase D6 — CI / “definition of done” clarity (recommended)

| Step | Action | Done when |
| ---- | ------ | --------- |
| D6.1 | Align **contributor / CI docs** (e.g. `.github` workflows, `CONTRIBUTING.md` if present) with which solution filters run on PRs: **`Nexo.sln`**, **`Nexo.LocalDevCore.slnf`**, **`Nexo.PrimeTime.slnf`**. | Expectations match automation. |
| D6.2 | If **`PrimeTime`** is PR-gated, note **minimum test command** for SDK-touching PRs in one place. | Authors know what to run locally. |

### Risks and mitigations

- **D3 wide linking** — Linking global usings into many projects can hide missing imports; mitigation: keep Hosting file as **single source of truth** and review link list when adding Sdk namespaces.
- **D4 options moves** — Namespace or folder churn can break analyzers; mitigation: **one feature per PR**, namespace-stable moves only.

---

### Phase D — execution checklist

| Phase | Summary |
| ----- | ------- |
| **D1** | Done — Goal / Track A / Track B §B.1 aligned with **`NexoKernelRegistrar`** and Sdk extension namespaces; **`SdkStructure.md`** lists completed areas and consumer guidance. |
| **D2** | Done — All **`src/Nexo.Infrastructure/**/*ServiceCollectionExtensions*.cs`** files live under **`Sdk/Extensions/`**; **`dotnet build Nexo.sln`** verified. |
| **D3** | Done — Audit documented in **`SdkStructure.md`**: **`GlobalUsings.Infrastructure.Sdk.cs`** linked from **CLI** and **Tests.Infrastructure**; **`AddNexo`** hosts (**e.g. Nexo.API**) need no separate Sdk usings; projects that only reference Infrastructure **types** skip the link. |
| **D4** | Done — **`Pipelines/Sdk/Options/`** pilot; further option-folder moves descoped in **D4.2** above. |
| **D5** | Done — **`NexoServiceCollectionExtensions.NodeCapabilityRuntime.cs`**; shared **`EphemeralModelsEnabled()`** in **`NexoKernelRegistrar.Ephemeral.cs`**. |
| **D6** | Done — **`CONTRIBUTING.md`** — solution filters, **PrimeTime** / **LocalDevCore**, **Makefile** targets, workflow pointers. |

---

## Explicit non-goals (unless product asks)

- **Mass-renaming** of **implementation** types from **`Nexo.Infrastructure.<Feature>`** to **`Nexo.Infrastructure.Sdk.*`** (breaking; extension **classes** already use Sdk-style namespaces by design).
- Introducing a **single mega-package** that re-exports all extensions (maintenance burden).
- Moving **port interfaces** out of `Nexo.Core.Application` into Infrastructure.
