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
- **`Observation/Sdk/Extensions/`** — DI extensions use namespace **`Nexo.Infrastructure.Sdk.Observation`** (`AddObservationCore`, `AddObservationInfrastructure`).
- **Other feature areas** — same physical layout; namespaces follow **`Nexo.Infrastructure.Sdk.<Subsystem>`** unless a **name collision** with runtime types forces **`Nexo.Infrastructure.<Subsystem>.Sdk`** (see **`NodeCapabilityRuntime`**, **`Execution`**, **`Execution.Routing`**, **`Mesh`**).

### Bringing Sdk extensions into scope

Extension methods require their namespace in scope. **`src/Nexo.Hosting/GlobalUsings.Infrastructure.Sdk.cs`** centralizes **`global using`** lines for Sdk namespaces; **`Nexo.CLI`** and **`Nexo.Tests.Infrastructure`** **link** that file in their `.csproj` to avoid duplicating imports across commands and tests.

## Naming

- **`HostNexoSdkBuilder`** — in-process Nexo kernel registration (before `AddNexo`).
- **`NexoClientSdkBuilder`** — NuGet client package configuration (`Nexo.Sdk`).
- Legacy aliases (`[Obsolete]`) remain until the next major bump.

## Composition order

1. Optional: `services.AddNexoSdk(...)` (host SDK — bricks/agents).
2. `services.AddNexo(...)` (kernel).

Remote-only apps use **`AddNexoClientSdk`** instead of `AddNexo`.
