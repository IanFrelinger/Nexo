# Duplicate and Redundant Code Analysis

This document reports duplicate or redundant code across the Nexo framework after the addition of background-agent self-testing, self-analyzing, and self-extending features. Findings are grouped by area with concrete locations and refactor options.

---

## 1. Background agent registry: parameter extraction (High)

**Location:** `src/Nexo.BackgroundAgents/Registry/BackgroundAgentRegistry.cs`

**Pattern:** Three nearly identical helpers:

- `TryGetAnalysisPath(config, out path)` — tries `Path`, then `AnalysisPath`
- `TryGetTestFilter(config, out filter)` — tries `Filter`
- `TryGetExtenderRepoRoot(config, out repoRoot)` — tries `RepoRoot`, then `Path`

Each does: `if (config.Parameters == null || config.Parameters.Count == 0) return false;` then tries a sequence of keys with `TryGetValue` and `string.IsNullOrWhiteSpace` checks.

**Refactor:** Replace with a single helper:

```csharp
private static bool TryGetParameter(BackgroundAgentConfig config, string[] keys, out string value)
{
    value = null!;
    if (config.Parameters == null || config.Parameters.Count == 0) return false;
    foreach (var key in keys)
    {
        if (config.Parameters.TryGetValue(key, out var obj) && obj is string s && !string.IsNullOrWhiteSpace(s))
        {
            value = s;
            return true;
        }
    }
    return false;
}
```

Then call with `TryGetParameter(config, new[] { "Path", "AnalysisPath" }, out var path)`, `TryGetParameter(config, new[] { "Filter" }, out var filter)`, and `TryGetParameter(config, new[] { "RepoRoot", "Path" }, out var repoRoot)`.

---

## 2. ThinkAsync + approve + invoke loop (High)

**Locations:**

- `src/Nexo.Runtime/AgentHost.cs` — `StepAsync`: `ThinkAsync` → for each tool call, `Approve` → `InvokeAsync` (and memory write on deny).
- `src/Nexo.CLI/Commands/BackgroundAgent/SelfExtendRunnerAdapter.cs` — same loop in `RunAsync`: `ThinkAsync` → for each call, `Approve` → `InvokeAsync`, with executed/denied counts.

**Redundancy:** The “think → policy check → invoke” loop is duplicated. `AgentHost` returns `IActionDelta?`; `SelfExtendRunnerAdapter` returns `SelfExtendRunResult` with executed/denied counts.

**Refactor options:**

- **A (preferred):** Use `AgentHost` inside `SelfExtendRunnerAdapter`: build `AgentHost(agents, tools, policies)`, call `StepAsync(snapshot, ct)`. Then either:
  - Build `SelfExtendRunResult` from the merged delta (e.g. infer executed from `delta.Log`), and accept no explicit “denied” count unless `AgentHost` is extended, or
  - Extend `AgentHost` to return `(IActionDelta? delta, int executed, int denied)` (or a small result type) so both call sites can reuse the same loop and counts.
- **B:** Extract a shared helper, e.g. `AgentHost.RunStepWithCountsAsync(agents, tools, policies, snapshot, ct)` that returns `(delta, executed, denied)`, and have both `AgentHost.StepAsync` and `SelfExtendRunnerAdapter` call it (with `StepAsync` ignoring counts if needed).

---

## 3. Repo-fs toolbox and policy setup (Medium)

**Locations:**

- `src/Nexo.CLI/Demo/SelfExtend/Pipeline/SelfExtendToolRuntime.cs` — builds `CapabilityRegistry` with `RepoFsWriteTool`, `RepoFsSearchReplaceTool`, plus `DotnetBuildTool`, `DotnetTestTool`, `DotnetRunTool`, `RoslynAnalyzeTool`; `PolicyEngine` with `OutputPathSandboxed`, `PathAllowlist`, `MaxWriteSize`, `PerfHeadroom`.
- `src/Nexo.CLI/Commands/BackgroundAgent/SelfExtendRunnerAdapter.cs` — builds `CapabilityRegistry` with only `RepoFsWriteTool` and `RepoFsSearchReplaceTool`; `PolicyEngine` with `PathAllowlist` and `MaxWriteSize`.

**Redundancy:** The “repo.fs.write + repo.fs.search_replace + PathAllowlist + MaxWriteSize” subset is duplicated. The Demo adds more tools and policies (OutputPathSandboxed, PerfHeadroom, build/test/run/analyze tools).

**Refactor:** Introduce a shared factory (e.g. in `Nexo.Tools.Dev` or a small shared CLI helper) that returns the minimal repo-fs toolbox and policy set:

- `CreateRepoFsToolbox()` → `(IToolbox tools, PolicyEngine policies)` with write + search_replace and PathAllowlist + MaxWriteSize.

Then:

- `SelfExtendRunnerAdapter` uses this factory and does not duplicate tool/policy construction.
- `SelfExtendToolRuntime` (Demo) uses the same factory and then registers additional tools and policies (DotnetBuildTool, etc., OutputPathSandboxed, PerfHeadroom).

This keeps one place defining “minimal safe repo-fs toolbox” and avoids drift between adapter and Demo.

---

## 4. Path validation in background-agent adapters (Medium)

**Locations:**

- `src/Nexo.CLI/Commands/BackgroundAgent/CodeAnalysisRunnerAdapter.cs` — `if (string.IsNullOrWhiteSpace(path)) return ...; var dir = new DirectoryInfo(path); if (!dir.Exists) return ...;`
- `src/Nexo.CLI/Commands/BackgroundAgent/SelfExtendRunnerAdapter.cs` — same for `repoRoot`: `string.IsNullOrWhiteSpace(repoRoot)` then `DirectoryInfo(repoRoot).Exists`.

**Redundancy:** Same “non-empty path + directory exists” check and early return with a failure result.

**Refactor:** Add a small shared helper (e.g. in CLI Commands/BackgroundAgent or a shared util) such as:

- `TryResolveDirectory(string path, out DirectoryInfo? dir, out string? errorMessage)`  
  or  
- `ValidateDirectoryPath(string path, string paramName)` throwing or returning a result.

Then both adapters call it and avoid duplicated validation and error messages.

---

## 5. Run-result record shape (Low)

**Locations:**

- `CodeAnalysisRunResult(bool Success, int ViolationCount, string Summary)` — `Nexo.BackgroundAgents/Optimization/ICodeAnalysisRunner.cs`
- `TestRunResult(bool Success, int TotalTests, int PassedTests, int FailedTests, string Summary)` — `Nexo.BackgroundAgents/Testing/TestRunResult.cs`
- `SelfExtendRunResult(bool Success, int ToolCallsExecuted, int ToolCallsDenied, string Summary)` — `Nexo.BackgroundAgents/Extending/SelfExtendRunResult.cs`

**Observation:** All have `Success` and `Summary`; each adds role-specific counts. Not strictly duplicate, but a shared base could reduce repetition:

- e.g. `BackgroundAgentRunResult(bool Success, string Summary)` and the three records extend or contain it with their own counts.

**Refactor (optional):** Introduce `BackgroundAgentRunResult` or a small base type only if you want a single “run result” abstraction (e.g. for logging or UI). Otherwise keeping three separate records is acceptable.

---

## 6. Test helpers: FindRepoRoot (Medium)

**Locations:** Identical `FindRepoRoot()` in:

- `src/Nexo.Tests.Infrastructure/Tests/CLI/WorldCliE2ETests.cs`
- `src/Nexo.Tests.Infrastructure/Tests/CLI/DemoCliE2ETests.cs`
- `src/Nexo.Tests.Infrastructure/Tests/CLI/GeoVectorCliE2ETests.cs`
- `src/Nexo.Tests.Infrastructure/Tests/CLI/GeoTerrainCliE2ETests.cs`

Implementation: walk up from `AppContext.BaseDirectory` until `Nexo.sln` exists; throw if not found.

**Refactor:** Move to a shared test helper (e.g. `Nexo.Tests.Infrastructure/TestPaths.cs` or a shared base class for CLI E2E tests) and have all four test classes use it. Reduces copy-paste and keeps “find repo root” logic in one place.

---

## 7. WorldSnapshot construction for repo/output (Low)

**Locations:**

- `SelfExtendRunnerAdapter.RunAsync`: `new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = repoRoot, ["OutputRoot"] = outputRoot })`
- `SelfExtendToolRuntime.InvokeAsync`: same shape with `ctx.RepoRoot`, `ctx.OutputRoot`, and `ctx.Iteration` as tick.
- `AgentExecutorAdapter.ExecuteAsync`: `["OutputRoot"] = ..., ["RepoRoot"] = ...`

**Observation:** The same “RepoRoot + OutputRoot” (and optionally tick) pattern appears in multiple places. Not heavy duplication, but a small helper (e.g. `WorldSnapshot.ForRepo(string repoRoot, string? outputRoot = null, int tick = 0)`) would centralize the keys and structure.

**Refactor (optional):** Add a static helper on `WorldSnapshot` or in a small util used by CLI/BackgroundAgents only, if you want a single place for “repo world state” construction.

---

## 8. Background agent runner interfaces (Informational)

**Locations:**

- `ICodeAnalysisRunner.RunAsync(string path, ct) → CodeAnalysisRunResult`
- `ITestRunRunner.RunAsync(string? filter, ct) → TestRunResult`
- `ISelfExtendRunner.RunAsync(string repoRoot, ct) → SelfExtendRunResult`

**Observation:** Three separate interfaces with similar “RunAsync with one main input and cancellation, return a result” pattern. Unifying them (e.g. a generic `IBackgroundAgentRunner<TResult>` or a single runner that takes `BackgroundAgentConfig` and dispatches by role) would be a larger design change and could obscure role-specific semantics. Keeping three interfaces is reasonable; the main duplication to address is in the **registry** (parameter extraction and execution branches), not the interfaces themselves.

---

## 9. Adapter error handling and result construction (Low)

**Locations:**

- `CodeAnalysisRunnerAdapter`: try/catch, on failure return `new CodeAnalysisRunResult(false, 0, $"Analysis failed: {ex.Message}")`, log warning.
- `TestRunRunnerAdapter`: try/catch, on failure return `new TestRunResult(false, 0, 0, 0, $"Test run failed: {ex.Message}")`, log warning.
- `SelfExtendRunnerAdapter`: try/catch, return `new SelfExtendRunResult(false, 0, 0, $"Run failed: {ex.Message}")`, log warning.

**Observation:** Same “catch, log, return failure result with message” pattern. A tiny shared helper (e.g. `static TResult Failure<TResult>(string message, ILogger? log, Exception ex, ...)`) could standardize this, but the benefit is small; optional cleanup only.

---

## Summary and priority

| # | Area                         | Severity | Effort  | Action |
|---|------------------------------|----------|---------|--------|
| 1 | Registry parameter extraction | High     | Low     | Add `TryGetParameter(config, keys, out value)` and use it for analysis path, test filter, extender repo root. |
| 2 | Think + approve + invoke loop | High     | Medium  | Use `AgentHost` in `SelfExtendRunnerAdapter` (and optionally extend `AgentHost` to return executed/denied counts). |
| 3 | Repo-fs toolbox + policy      | Medium   | Medium  | Extract shared factory for minimal repo-fs toolbox and policy; reuse in adapter and Demo. |
| 4 | Path validation in adapters   | Medium   | Low     | Shared “resolve/validate directory path” helper for CodeAnalysis and SelfExtend adapters. |
| 5 | Run-result record shape       | Low      | Low     | Optional: base type or interface for Success + Summary if desired for logging/UI. |
| 6 | FindRepoRoot in tests         | Medium   | Low     | Single `FindRepoRoot()` in shared test helper; use from all four CLI E2E test classes. |
| 7 | WorldSnapshot repo/output     | Low      | Low     | Optional: static helper for “repo world state” snapshot. |
| 8 | Runner interfaces             | Info     | —       | Keep three interfaces; no change recommended. |
| 9 | Adapter catch/result          | Low      | Low     | Optional: small shared failure-result helper. |

Recommended order: **1** (quick, reduces registry duplication), then **6** (tests), then **2** (removes duplicated agent loop), then **3** and **4** as you touch those areas.
