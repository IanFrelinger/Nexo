# SDK-style layout in the Nexo codebase

This repository uses a consistent **SDK composition** model so features stay **extensible** (interfaces + options + default implementations) and **discoverable** (grouped folders and entry types).

## Layers

| Layer | Responsibility | Typical contents |
| ----- | -------------- | ---------------- |
| **Ports** (`Nexo.Core.Application.*.Ports`) | Contracts (`interface`), commands/queries, DTOs | Stable integration surface |
| **Options** | Immutable-style configuration (`record` / `class` with init-only props) | Bound from DI / env / config sections |
| **Infrastructure** | Default adapters implementing ports | Swappable in tests or overrides |
| **Hosting SDK** (`Nexo.Hosting.Sdk`) | Kernel composition for bricks, agents, cards | `AddNexoSdk`, `NexoSdkOptions`, `HostNexoSdkBuilder` |
| **HTTP client SDK** (`Nexo.Sdk` NuGet, namespace `Nexo.Sdk.Client`) | Slim remote API client (`AddNexoClientSdk`, `NexoClientSdkBuilder`) | Unity / embedded callers |

## Folder conventions (physical)

These conventions apply **without renaming namespaces**, so existing callers stay valid:

- **`Sdk/Options/`** — option bags and enums tied to registration (`NexoHostingOptions`, deployment profile, host SDK options).
- **`Sdk/Builders/`** — fluent builders implementing port interfaces (`HostNexoSdkBuilder` implements `INexoSdkBuilder`).
- **`Sdk/Extensions/`** — `*ServiceCollectionExtensions`, OpenTelemetry hooks, etc.
- **`Locking/`** (tests) — cross-process primitives (`ICrossProcessLockProvider`) shared by integration tests.

## Naming

- **`HostNexoSdkBuilder`** — in-process Nexo kernel registration (before `AddNexo`).
- **`NexoClientSdkBuilder`** — NuGet client package configuration (`Nexo.Sdk`).
- Legacy aliases (`[Obsolete]`) remain until the next major bump.

## Composition order

1. Optional: `services.AddNexoSdk(...)` (host SDK — bricks/agents).
2. `services.AddNexo(...)` (kernel).

Remote-only apps use **`AddNexoClientSdk`** instead of `AddNexo`.
