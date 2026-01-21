# Nexo Pattern Analysis (for `Nexo.GeoTerrain` dogfooding)

This document extracts **existing Nexo patterns** from the codebase so `Nexo.GeoTerrain` can be implemented in a way that is indistinguishable from a native Nexo module.

## Tool pattern(s)

Nexo has **two “tool-like” patterns** in active use:

### Pattern A: Generic agent tools (`Nexo.Abstractions.ITool`)

- **Contract**: `src/Nexo.Abstractions/Abstractions.cs`
  - `ITool` is identified by `Id`, exposes `ToolSchema`, and executes via `InvokeAsync(ToolCall, WorldSnapshot, CancellationToken)`.
  - Tools return a `ToolResult` containing:
    - a signed/mergeable **world delta** (`IActionDelta`)
    - an optional **payload** (tool-specific structured data)

```csharp
public interface ITool
{
    string Id { get; }
    ToolSchema Schema { get; }
    Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct);
}
```

**How tools report results/progress**

- Tools return:
  - `delta.Log` strings (coarse-grained progress)
  - `payload` anonymous object (typed-shape by convention)
- Example tools:
  - `src/Nexo.Tools.Dev/DotnetBuildTool.cs`
  - `src/Nexo.Tools.Dev/DotnetTestTool.cs`
  - `src/Nexo.Tools.Dev/RepoFsWriteTool.cs`

Example return shape pattern:

```csharp
return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
```

**Async execution**

- Yes: `InvokeAsync` is `Task<ToolResult>`.
- Implementations often shell out (e.g. `DotnetRunner`) or do I/O, and they are placed in top-layer projects (`Nexo.Tools.*`) where raw calls are allowed.

**Registration / discovery**

- `CapabilityRegistry` implements `IToolbox`:
  - `Register(ITool)`
  - `Schemas()` for discovery
  - `InvokeAsync` dispatch by `Id`
  - per-agent memory via `MemoryFor(IAgent)`
  - `src/Nexo.Runtime/CapabilityRegistry.cs`

```csharp
public sealed class CapabilityRegistry : IToolbox
{
    public void Register(ITool tool) => _tools[tool.Id] = tool;
    public IEnumerable<ToolSchema> Schemas() => _tools.Values.Select(t => t.Schema);
    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct) => _tools[call.Id].InvokeAsync(call, s, ct);
}
```

### Pattern B: Domain-specific ports as “tools” (Orchestration ports)

Some “tools” are expressed as **ports** (interfaces) in `Nexo.Orchestration.*.Ports`, implemented by adapters in other projects.

- Example: `src/Nexo.Orchestration/Build/Ports/IBuildTool.cs`
- Example implementation: `src/Nexo.Tools.Unity/UnityBuildTool.cs`

These are not `ITool` and do not return `ToolResult`/`IActionDelta`. They return **domain outputs** (e.g., `BuildOutput`) and use host DI for registration.

## Agent pattern(s)

Nexo has **two agent execution styles** sharing a single `Nexo.Abstractions.IAgent` contract:

### Pattern A: “AgentHost” simulation agents (`IAgent.ThinkAsync`)

- Contract: `src/Nexo.Abstractions/Abstractions.cs`

```csharp
public interface IAgent
{
    string Name { get; }
    Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct);
}
```

- Runtime: `src/Nexo.Runtime/AgentHost.cs`
  - pulls `ToolCall`s from agent
  - policy-checks each call (`PolicyEngine`)
  - executes approved calls via `IToolbox`
  - merges deltas (`ActionDelta.Merge`) and signs them (`PolicyEngine.Sign`)

### Pattern B: Orchestrated lifecycle agents (`Nexo.Orchestration.Agents.BaseAgent`)

- `BaseAgent` implements `IAgent`, but the primary “real” execution path is:
  - `InitializeAsync()`
  - `WaitForDependenciesAsync(...)`
  - `ExecuteAsync(...)`
  - `ShutdownAsync()`
- `ThinkAsync` is present for compatibility and delegates to `ExecuteAsync` when state is `Ready`.

Files:
- `src/Nexo.Orchestration/Agents/BaseAgent.cs`
- Example specializations:
  - `src/Nexo.Orchestration/Agents/CodeGeneration/CodeGenerationAgent.cs`
  - `src/Nexo.Orchestration/Agents/Security/SecurityAnalysisAgent.cs`

**How agents use tools**

- Orchestration agents typically depend on:
  - `IModel` for LLM calls
  - domain services (analyzers/optimizers/scanners)
  - ports (e.g., assets/build/playtest)
- The “IToolbox-style” tools are primarily used by **tool-running agents** and by demos/self-extend pipelines (via tool runtimes).

## Adapter / port pattern

Nexo’s port/adapter split shows up clearly in the **asset generation** feature:

- Ports live in orchestration:
  - `src/Nexo.Orchestration/Assets/Ports/IImageGenerator.cs`
  - `src/Nexo.Orchestration/Assets/Ports/IAudioGenerator.cs`
  - `src/Nexo.Orchestration/Assets/Ports/IModel3DGenerator.cs`
- Adapters/implementations live in:
  - `src/Nexo.Adapters.Assets/*`

**Multiple providers**

Provider choice is done via DI registration in an extensions method:
- `src/Nexo.Adapters.Assets/ServiceCollectionExtensions.cs`

This uses:
- `AddHttpClient<IPort, Adapter>()` for network-backed implementations
- `AddSingleton<IPort, EchoAdapter>()` as an offline placeholder/fallback

## Command / orchestration pattern (Application layer)

Nexo’s application layer uses a “command” abstraction:
- `src/Nexo.Core.Application/Interfaces/ICommand.cs`

```csharp
public interface ICommand<in TIn, TOut>
{
    ValueTask<TOut> ExecuteAsync(TIn input, CancellationToken ct);
}
```

Commands are run through `IOrchestrator` (implementation: `GenericCommandOrchestrator`) which supports:
- pre-validators (`IPreValidator`)
- post-validators (`IPostValidator<TOut>`)
- loop abstraction for hot paths (`ILoopKernel`)

File:
- `src/Nexo.Core.Application/Orchestration/GenericCommandOrchestrator.cs`

## Dual-mode (Deterministic ↔ Agentic) pattern

Nexo’s **primary dual-mode mechanism** is the **Brick** system:

- Domain types:
  - `src/Nexo.Core.Domain/Bricks/Brick.cs`
  - `src/Nexo.Core.Domain/Bricks/BrickImplementations.cs`
- Runtime selection + fallback:
  - `src/Nexo.Infrastructure/Execution/BehaviorExecutor.cs`

Key behaviors:
- **Air-gapped forces deterministic** (`ExecutionOptions.IsAirGapped`)
- Otherwise:
  - choose based on:
    - explicit step selection
    - per-brick overrides
    - runtime spec (`BrickRuntimeSpec.Prefer` + fallback chain)
    - or default/selector
  - filter for availability (agentic requires provider available and not air-gapped)
  - if `SwapOnFailure` is enabled, try fallbacks on exception/invalid output

## Test patterns

Tests use a **custom test harness**:
- Base: `src/Nexo.Core.Application/Testing/Abstractions/TestBase.cs`
- Assertions: `src/Nexo.Core.Application/Testing/Abstractions/UnitTestBase.cs`
- Results: `src/Nexo.Core.Application/Testing/Models/TestResult.cs`

Examples:
- Tool tests: `src/Nexo.Tests.Infrastructure/Tests/Tools/RoslynAnalyzeToolTests.cs`
- Agent smoke tests: `src/Nexo.Tests.Infrastructure/Tests/Agents/UniversalTesterAgentSmokeTests.cs`
- Domain value object tests: `src/Nexo.Tests.Domain/Tests/DomainValueObjectsTests.cs`

## DI wiring pattern

The CLI host composes the system via DI in:
- `src/Nexo.CLI/Program.cs`

Notable conventions:
- register orchestration layer via `services.AddNexoOrchestration();`
- register hot-path loop kernel with env var toggles (`NEXO_LOOP_PARALLEL`, `NEXO_LOOP_INSTRUMENT`)
- register infra adapters behind ports (often wrapped with caching decorators)

## Notes relevant to GeoTerrain

- The “dogfooded” dual-mode story should likely be:
  - **Bricks** for mesh generation steps (deterministic implementation + agentic planner/tuner)
  - **Ports/adapters** for online elevation providers (SRTM/Mapbox/OpenElevation) + **local offline** adapters
- Domain “no-enum” rule is real in `Nexo.Core.Domain.Values`, but **ports/adapters currently use enums** (e.g., `ImageSize`). This is a likely gap/consistency issue to document (see `GAPS.md`).

