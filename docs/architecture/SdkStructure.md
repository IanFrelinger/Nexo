# SDK-style layout in the Nexo codebase

This repository uses a consistent **SDK composition** model so features stay **extensible** (interfaces + options + default implementations) and **discoverable** (grouped folders and entry types).

## Layers

| Layer | Responsibility | Typical contents |
| ----- | -------------- | ---------------- |
| **Ports** (`Nexo.Core.Application.*.Ports`, plus **`Nexo.Infrastructure.Sdk.Ports`** for host SDK surface) | Contracts (`interface`), commands/queries, DTOs | Stable integration surface |
| **Options** | Immutable-style configuration (`record` / `class` with init-only props) | Bound from DI / env / config sections |
| **Infrastructure** | Default adapters implementing ports | Swappable in tests or overrides |
| **Hosting SDK** (`Nexo.Hosting.Sdk`) | Kernel composition for bricks, agents, cards | `AddNexoSdk`, `NexoSdkOptions`, `HostNexoSdkBuilder` |
| **HTTP client SDK** (`Nexo.Sdk` NuGet, namespace `Nexo.Sdk.Client`) | Slim remote API client (`AddNexoClientSdk`, `NexoClientSdkBuilder`) | Unity / embedded callers |

## Folder conventions (physical)

These conventions usually **preserve** existing namespaces for bulk moves; **new** SDK folders may introduce **`Nexo.Infrastructure.Sdk.*`** namespaces where called out below.

- **`Sdk/Options/`** — option bags and enums tied to registration (`NexoHostingOptions`, deployment profile, host SDK options).
- **`Sdk/Builders/`** — fluent builders implementing port interfaces (`HostNexoSdkBuilder` implements `INexoSdkBuilder`).
- **`Sdk/Extensions/`** — `*ServiceCollectionExtensions`, OpenTelemetry hooks, etc.
- **`Observation/Sdk/Extensions/`** — DI extensions use namespace **`Nexo.Infrastructure.Observation.Sdk.Extensions`** (`AddObservationCore`, `AddObservationInfrastructure`).
- **Other feature areas** — same physical layout; namespaces follow **`Nexo.Infrastructure.Sdk.<Subsystem>`** unless a **name collision** with runtime types forces **`Nexo.Infrastructure.<Subsystem>.Sdk`** (see **`NodeCapabilityRuntime`**, **`Execution`**, **`Execution.Routing`**, **`Mesh`**).

### Mechanical sweep (`*ServiceCollectionExtensions`)

Every **`Nexo.Infrastructure`** DI extension file is under **`Feature/Sdk/Extensions/`** (filename pattern **`*ServiceCollectionExtensions*.cs`**). There are no parallel copies under legacy paths.

### Completed areas (DI extensions)

Extension entry points and namespaces (collision-safe variants where noted):

| Feature folder | Extension namespace |
| -------------- | ------------------- |
| **Adaptation** (`Adaptation/Sdk/Extensions/`) | `Nexo.Infrastructure.Adaptation.Sdk.Extensions` |
| **Analysis** (`Analysis/BrickAnalyzer/Sdk/Extensions/`) | `Nexo.Infrastructure.Analysis.BrickAnalyzer.Sdk.Extensions` |
| **Composition** (`Composition/Sdk/Extensions/`) | `Nexo.Infrastructure.Composition.Sdk.Extensions` |
| **Execution** (`Execution/Sdk/Extensions/`) | `Nexo.Infrastructure.Execution.Sdk.Extensions.Extensions` |
| **Execution/Routing** (`Execution/Routing/Sdk/Extensions/`) | `Nexo.Infrastructure.Execution.Routing.Sdk.Extensions.Extensions` |
| **Maintenance** (`Maintenance/Sdk/Extensions/`) | `Nexo.Infrastructure.Maintenance.Sdk.Extensions` |
| **Mesh** (`Mesh/Sdk/Extensions/`) | `Nexo.Infrastructure.Mesh.Sdk.Extensions.Extensions` |
| **ModelArtifacts** (`ModelArtifacts/Sdk/Extensions/`) | `Nexo.Infrastructure.ModelArtifacts.Sdk.Extensions` |
| **NodeCapabilityRuntime** (`NodeCapabilityRuntime/Sdk/Extensions/`) | `Nexo.Infrastructure.NodeCapabilityRuntime.Sdk.Extensions.Extensions` |
| **Observation** (`Observation/Sdk/Extensions/`) | `Nexo.Infrastructure.Observation.Sdk.Extensions` |
| **ParallelTesting** (`ParallelTesting/Sdk/Extensions/`) | `Nexo.Infrastructure.ParallelTesting.Sdk.Extensions` |
| **Persistence** (`Persistence/Sdk/Extensions/`) | `Nexo.Infrastructure.Persistence.Sdk.Extensions` |
| **Pipelines** (`Pipelines/Sdk/Extensions/`) | `Nexo.Infrastructure.Pipelines.Sdk.Extensions` |
| **Rollback** (`Rollback/Sdk/Extensions/`) | `Nexo.Infrastructure.Rollback.Sdk.Extensions` |
| **SelfContext** (`SelfContext/Sdk/Extensions/`) | `Nexo.Infrastructure.SelfContext.Sdk.Extensions` |
| **SelfImprovement** (`SelfImprovement/Sdk/Extensions/`) | `Nexo.Infrastructure.SelfImprovement.Sdk.Extensions` |
| **Trust** (`Trust/Sdk/Extensions/`) | `Nexo.Infrastructure.Trust.Sdk.Extensions` |

### Optional `Sdk/Options` pilot

**Pipelines:** **`PipelineExecutionOptions`**, **`PipelinePersistenceOptions`**, **`PipelineExecutionAdapterOptions`** live under **`Pipelines/Sdk/Options/`** with namespaces unchanged (**`Nexo.Infrastructure.Pipelines`**).

### Bringing Sdk extensions into scope

Extension methods require their namespace in scope.

| Approach | When to use |
| -------- | ----------- |
| **Link `src/Nexo.Hosting/GlobalUsings.Infrastructure.Sdk.cs`** from your `.csproj` (`<Compile Include="..\Nexo.Hosting\GlobalUsings.Infrastructure.Sdk.cs" Link="GlobalUsings.Infrastructure.Sdk.cs" />`) | Projects that **wire `IServiceCollection` manually** and call Infrastructure Sdk extensions (**same pattern as `Nexo.CLI`** and **`Nexo.Tests.Infrastructure`**). Keeps one source of truth when Hosting adds Sdk namespaces. |
| **Explicit `using Nexo.Infrastructure.Sdk.*`** (or feature-specific Sdk namespaces) | Libraries that **cannot** reference Hosting paths; small surface area. |
| **Neither** | Apps that only call **`services.AddNexo(...)`** (**e.g. `Nexo.API`**) — kernel registration pulls in dependencies; no Infrastructure Sdk `using` needed for typical **`Program.cs`**. |
| **Types only** | Projects like **`Nexo.Bricks.Owasp`** that reference Infrastructure for **adapters / types** but not registration extensions — **no** global Sdk usings file. |

**`src/Nexo.Hosting/GlobalUsings.Infrastructure.Sdk.cs`** lists **`global using`** lines for Sdk namespaces used by the host.

## Naming

- **`HostNexoSdkBuilder`** — in-process Nexo kernel registration (before `AddNexo`).
- **`NexoClientSdkBuilder`** — NuGet client package configuration (`Nexo.Sdk`).
- Legacy aliases (`[Obsolete]`) remain until the next major bump.

## Composition order

1. Optional: `services.AddNexoSdk(...)` (host SDK — bricks/agents).
2. `services.AddNexo(...)` (kernel).

Remote-only apps use **`AddNexoClientSdk`** instead of `AddNexo`.
