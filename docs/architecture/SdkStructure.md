# SDK-style layout in the Ashlar codebase

This repository uses a consistent **SDK composition** model so features stay **extensible** (interfaces + options + default implementations) and **discoverable** (grouped folders and entry types).

## Layers

| Layer | Responsibility | Typical contents |
| ----- | -------------- | ---------------- |
| **Ports** (`Ashlar.Core.Application.*.Ports`, plus **`Ashlar.Infrastructure.Sdk.Ports`** for host SDK surface) | Contracts (`interface`), commands/queries, DTOs | Stable integration surface |
| **Options** | Immutable-style configuration (`record` / `class` with init-only props) | Bound from DI / env / config sections |
| **Infrastructure** | Default adapters implementing ports | Swappable in tests or overrides |
| **Hosting SDK** (`Ashlar.Hosting.Sdk`) | Kernel composition for bricks, agents, cards | `AddAshlarSdk`, `AshlarSdkOptions`, `HostAshlarSdkBuilder` |
| **HTTP client SDK** (`Ashlar.Sdk` NuGet, namespace `Ashlar.Sdk.Client`) | Slim remote API client (`AddAshlarClientSdk`, `AshlarClientSdkBuilder`) | Unity / embedded callers |

## Folder conventions (physical)

These conventions usually **preserve** existing namespaces for bulk moves; **new** SDK folders may introduce **`Ashlar.Infrastructure.Sdk.*`** namespaces where called out below.

- **`Sdk/Options/`** — option bags and enums tied to registration (`AshlarHostingOptions`, deployment profile, host SDK options).
- **`Sdk/Builders/`** — fluent builders implementing port interfaces (`HostAshlarSdkBuilder` implements `IAshlarSdkBuilder`).
- **`Sdk/Extensions/`** — `*ServiceCollectionExtensions`, OpenTelemetry hooks, etc.
- **`Observation/Sdk/Extensions/`** — DI extensions use namespace **`Ashlar.Infrastructure.Observation.Sdk.Extensions`** (`AddObservationCore`, `AddObservationInfrastructure`).
- **Other feature areas** — same physical layout; namespaces follow **`Ashlar.Infrastructure.Sdk.<Subsystem>`** unless a **name collision** with runtime types forces **`Ashlar.Infrastructure.<Subsystem>.Sdk`** (see **`NodeCapabilityRuntime`**, **`Execution`**, **`Execution.Routing`**, **`Mesh`**).

### Mechanical sweep (`*ServiceCollectionExtensions`)

Every **`Ashlar.Infrastructure`** DI extension file is under **`Feature/Sdk/Extensions/`** (filename pattern **`*ServiceCollectionExtensions*.cs`**). There are no parallel copies under legacy paths.

### Completed areas (DI extensions)

Extension entry points and namespaces (collision-safe variants where noted):

| Feature folder | Extension namespace |
| -------------- | ------------------- |
| **Adaptation** (`Adaptation/Sdk/Extensions/`) | `Ashlar.Infrastructure.Adaptation.Sdk.Extensions` |
| **Analysis** (`Analysis/BrickAnalyzer/Sdk/Extensions/`) | `Ashlar.Infrastructure.Analysis.BrickAnalyzer.Sdk.Extensions` |
| **Composition** (`Composition/Sdk/Extensions/`) | `Ashlar.Infrastructure.Composition.Sdk.Extensions` |
| **Execution** (`Execution/Sdk/Extensions/`) | `Ashlar.Infrastructure.Execution.Sdk.Extensions.Extensions` |
| **Execution/Routing** (`Execution/Routing/Sdk/Extensions/`) | `Ashlar.Infrastructure.Execution.Routing.Sdk.Extensions.Extensions` |
| **Maintenance** (`Maintenance/Sdk/Extensions/`) | `Ashlar.Infrastructure.Maintenance.Sdk.Extensions` |
| **Mesh** (`Mesh/Sdk/Extensions/`) | `Ashlar.Infrastructure.Mesh.Sdk.Extensions.Extensions` |
| **ModelArtifacts** (`ModelArtifacts/Sdk/Extensions/`) | `Ashlar.Infrastructure.ModelArtifacts.Sdk.Extensions` |
| **NodeCapabilityRuntime** (`NodeCapabilityRuntime/Sdk/Extensions/`) | `Ashlar.Infrastructure.NodeCapabilityRuntime.Sdk.Extensions.Extensions` |
| **Observation** (`Observation/Sdk/Extensions/`) | `Ashlar.Infrastructure.Observation.Sdk.Extensions` |
| **ParallelTesting** (`ParallelTesting/Sdk/Extensions/`) | `Ashlar.Infrastructure.ParallelTesting.Sdk.Extensions` |
| **Persistence** (`Persistence/Sdk/Extensions/`) | `Ashlar.Infrastructure.Persistence.Sdk.Extensions` |
| **Pipelines** (`Pipelines/Sdk/Extensions/`) | `Ashlar.Infrastructure.Pipelines.Sdk.Extensions` |
| **Rollback** (`Rollback/Sdk/Extensions/`) | `Ashlar.Infrastructure.Rollback.Sdk.Extensions` |
| **SelfContext** (`SelfContext/Sdk/Extensions/`) | `Ashlar.Infrastructure.SelfContext.Sdk.Extensions` |
| **SelfImprovement** (`SelfImprovement/Sdk/Extensions/`) | `Ashlar.Infrastructure.SelfImprovement.Sdk.Extensions` |
| **Trust** (`Trust/Sdk/Extensions/`) | `Ashlar.Infrastructure.Trust.Sdk.Extensions` |

### Optional `Sdk/Options` pilot

**Pipelines:** **`PipelineExecutionOptions`**, **`PipelinePersistenceOptions`**, **`PipelineExecutionAdapterOptions`** live under **`Pipelines/Sdk/Options/`** with namespaces unchanged (**`Ashlar.Infrastructure.Pipelines`**).

### Bringing Sdk extensions into scope

Extension methods require their namespace in scope.

| Approach | When to use |
| -------- | ----------- |
| **Link `src/Ashlar.Hosting/GlobalUsings.Infrastructure.Sdk.cs`** from your `.csproj` (`<Compile Include="..\Ashlar.Hosting\GlobalUsings.Infrastructure.Sdk.cs" Link="GlobalUsings.Infrastructure.Sdk.cs" />`) | Projects that **wire `IServiceCollection` manually** and call Infrastructure Sdk extensions (**same pattern as `Ashlar.CLI`** and **`Ashlar.Tests.Infrastructure`**). Keeps one source of truth when Hosting adds Sdk namespaces. |
| **Explicit `using Ashlar.Infrastructure.Sdk.*`** (or feature-specific Sdk namespaces) | Libraries that **cannot** reference Hosting paths; small surface area. |
| **Neither** | Apps that only call **`services.AddAshlar(...)`** (**e.g. `Ashlar.API`**) — kernel registration pulls in dependencies; no Infrastructure Sdk `using` needed for typical **`Program.cs`**. |
| **Types only** | Projects like **`Ashlar.Bricks.Owasp`** that reference Infrastructure for **adapters / types** but not registration extensions — **no** global Sdk usings file. |

**`src/Ashlar.Hosting/GlobalUsings.Infrastructure.Sdk.cs`** lists **`global using`** lines for Sdk namespaces used by the host.

## Naming

- **`HostAshlarSdkBuilder`** — in-process Ashlar kernel registration (before `AddAshlar`).
- **`AshlarClientSdkBuilder`** — NuGet client package configuration (`Ashlar.Sdk`).
- Legacy aliases (`[Obsolete]`) remain until the next major bump.

## Composition order

1. Optional: `services.AddAshlarSdk(...)` (host SDK — bricks/agents).
2. `services.AddAshlar(...)` (kernel).

Remote-only apps use **`AddAshlarClientSdk`** instead of `AddAshlar`.
