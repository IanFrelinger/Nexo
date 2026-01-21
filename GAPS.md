# Pattern Gaps Found (GeoTerrain as dogfood)

GeoTerrain stresses a few areas where Nexo’s current patterns are not perfectly consistent.

## Gap 1: “No-enum rule” is not consistently applied outside Domain

- **What GeoTerrain needs**: stable, portable “value objects” for units/config (file formats, coordinate reference, LOD strategy) to avoid brittle enums.
- **What Nexo currently has**:
  - Domain uses **value-object-over-enum** patterns (e.g., `Nexo.Core.Domain.Values.RiskLevel`, `TaskStatus`, `TaskPriority`).
  - Ports/adapters still use **enums** (e.g., `Nexo.Orchestration.Assets.Ports.ImageSize`).
- **Proposed solution**:
  - For GeoTerrain **domain**, keep the no-enum rule: use value objects (static instances + parsing).
  - For orchestration ports, decide explicitly:
    - either allow enums in port DTOs (current reality), or
    - upgrade ports to domain-style value objects and update adapters accordingly.
- **Risk**: Changing existing ports is cross-cutting; for GeoTerrain, we can stay consistent in domain and document the inconsistency.

## Gap 2: Two different “tool” abstractions exist (ITool vs domain ports)

- **What GeoTerrain needs**: both “atomic operations” (like read file, write mesh) and “domain services” (like mesh generation).
- **What Nexo currently has**:
  - `Nexo.Abstractions.ITool` + `ToolResult` + `IActionDelta` (used by `AgentHost`-style agents and tooling)
  - Orchestration “tools” are modeled as **ports** returning domain outputs (`IBuildTool`, `IImageGenerator`, etc.)
- **Proposed solution**:
  - GeoTerrain should adopt both:
    - **ports** for online providers and domain services used by orchestrated agents
    - **ITool** wrappers in `Nexo.Tools.GeoTerrain` for filesystem/interop actions (to keep raw I/O out of core layers)
- **Risk**: Without a unified convention, some parts will look “tool-ish” and others “port-ish”. Documented and aligned to existing patterns.

## Gap 3: Progress/events are standardized for bricks, not for port-based pipelines

- **What GeoTerrain needs**: mesh generation can be long-running; streaming progress is valuable.
- **What Nexo currently has**:
  - `BehaviorExecutor.ExecuteWithEventsAsync` emits rich `ExecutionEvent` streams for **brick steps**.
  - Port/adapters (e.g., `DalleImageGenerator`) do not emit standardized progress events.
- **Proposed solution**:
  - Prefer modeling long-running GeoTerrain steps as **bricks** (so progress/events and swap-on-failure come “for free”).
  - Keep ports for “provider calls” (download) but wrap them into bricks when used in orchestrated workflows.
- **Risk**: Some duplication (a port + a brick wrapper) but consistent with Nexo’s evented runtime.

## Gap 4: Retry/resilience style is inconsistent across adapters

- **What GeoTerrain needs**: robust download retry + backoff for providers (Mapbox/OpenElevation).
- **What Nexo currently has**:
  - Many adapters implement manual retry loops (e.g., `DalleImageGenerator`).
  - There’s no single shared retry abstraction used everywhere.
- **Proposed solution**:
  - For GeoTerrain adapters, follow the existing style (manual retries) or introduce a shared helper in the appropriate top layer (Infrastructure/Adapters) if it already exists.
- **Risk**: Introducing a new shared retry primitive would be a “framework change”; better to start with the current idiom and later unify.

## Gap 5: Test discovery is hard-coded to a small assembly allowlist and expects built outputs

- **What GeoTerrain needs**: new modules should “just show up” in `nexo test`.
- **What Nexo currently has**:
  - `TestRunnerAdapter` has a hard-coded `testAssemblyNames` list (previously omitted GeoTerrain).
  - It also loads from a fixed `bin/Debug/net8.0` path when assemblies aren’t already loaded.
- **Proposed solution**:
  - Keep the allowlist but ensure new test projects are appended as modules are added.
  - Longer term: discover `Nexo.Tests.*` assemblies dynamically from `AppContext.BaseDirectory` (or configuration).
- **Risk**: Dynamic discovery risks loading unintended assemblies; allowlist is safer but less ergonomic.

