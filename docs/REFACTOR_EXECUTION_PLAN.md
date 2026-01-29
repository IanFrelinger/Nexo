# Refactor Execution Plan: Duplicate and Redundancy Fixes

This plan executes the refactors identified in [DUPLICATE_AND_REDUNDANCY_ANALYSIS.md](DUPLICATE_AND_REDUNDANCY_ANALYSIS.md) in dependency order with clear steps and verification.

---

## Principles

- **One phase at a time**: Complete and verify each phase before starting the next.
- **Tests first**: Run relevant tests after each refactor; run full BackgroundAgents + CLI tests after Phase 1 and Phase 2.
- **No behavior change**: Refactors are structural only; observable behavior (logs, results, config keys) stays the same.
- **Rollback**: Each phase is independently revertible via git.

---

## Phase 1: Quick Wins (Low Risk)

**Goal:** Remove obvious duplication with minimal code surface. No new public APIs.

**Verification:** `dotnet test src/Nexo.Tests.BackgroundAgents` and `dotnet build Nexo.sln` after Phase 1.

---

### 1.1 Registry parameter extraction

**Refactor #1 from analysis.**

| Item | Detail |
|------|--------|
| **Objective** | Replace `TryGetAnalysisPath`, `TryGetTestFilter`, `TryGetExtenderRepoRoot` with a single `TryGetParameter(config, keys, out value)`. |
| **Files** | `src/Nexo.BackgroundAgents/Registry/BackgroundAgentRegistry.cs` |
| **Dependencies** | None |
| **Risk** | Low (private static helpers only) |

**Steps:**

1. Add `private static bool TryGetParameter(BackgroundAgentConfig config, string[] keys, out string value)` with the implementation from the analysis doc.
2. Replace the optimizer branch: `TryGetAnalysisPath(instance.Config, out var analysisPath)` → `TryGetParameter(instance.Config, new[] { "Path", "AnalysisPath" }, out var analysisPath)`.
3. Replace the tester branch: `TryGetTestFilter(instance.Config, out var f)` → `TryGetParameter(instance.Config, new[] { "Filter" }, out var f)`; keep `filter = f` (or use `out var filter` directly).
4. Replace the extender branch: `TryGetExtenderRepoRoot(instance.Config, out var repoRoot)` → `TryGetParameter(instance.Config, new[] { "RepoRoot", "Path" }, out var repoRoot)`.
5. Remove the three old methods: `TryGetAnalysisPath`, `TryGetTestFilter`, `TryGetExtenderRepoRoot`.
6. Build and run `Nexo.Tests.BackgroundAgents` (all 115 tests). Optionally run a quick manual check: load `dogfood-optimizer.json`, `dogfood-tester.json`, `dogfood-extender.json` and ensure agents still resolve path/filter/repo root from config.

**Done criteria:** Same tests pass; no new public API; registry behavior unchanged.

---

### 1.2 Shared FindRepoRoot for CLI E2E tests

**Refactor #6 from analysis.**

| Item | Detail |
|------|--------|
| **Objective** | Single `FindRepoRoot()` used by all four CLI E2E test classes. |
| **Files** | New: `src/Nexo.Tests.Infrastructure/TestPaths.cs` (or similar). Edit: `WorldCliE2ETests.cs`, `DemoCliE2ETests.cs`, `GeoVectorCliE2ETests.cs`, `GeoTerrainCliE2ETests.cs`. |
| **Dependencies** | None |
| **Risk** | Low (test-only) |

**Steps:**

1. Create `src/Nexo.Tests.Infrastructure/TestPaths.cs` with `public static class TestPaths` and `public static string FindRepoRoot()` (implementation: walk from `AppContext.BaseDirectory` until `Nexo.sln` exists; throw if not found).
2. In `WorldCliE2ETests.cs`: remove local `FindRepoRoot()`, add `using` if needed, replace calls with `TestPaths.FindRepoRoot()`.
3. Same for `DemoCliE2ETests.cs`, `GeoVectorCliE2ETests.cs`, `GeoTerrainCliE2ETests.cs`.
4. Run `Nexo.Tests.Infrastructure` tests (or at least the four CLI E2E tests) to confirm they still find the repo.

**Done criteria:** All four test classes use `TestPaths.FindRepoRoot()`; E2E tests still pass.

---

**Phase 1 checkpoint:** Run `dotnet test src/Nexo.Tests.BackgroundAgents` and `dotnet test src/Nexo.Tests.Infrastructure` (or full solution test). Commit as "Phase 1: registry TryGetParameter + shared FindRepoRoot".

---

## Phase 2: Core Refactors (Medium Risk)

**Goal:** Remove duplicated think/approve/invoke loop and shared repo-fs toolbox/policy. One place for policy and execution.

**Verification:** Full `dotnet test Nexo.sln` (or at least BackgroundAgents, Infrastructure, CLI-related tests) after Phase 2.

---

### 2.1 Use AgentHost in SelfExtendRunnerAdapter

**Refactor #2 from analysis.**

| Item | Detail |
|------|--------|
| **Objective** | `SelfExtendRunnerAdapter` uses `AgentHost.StepAsync` instead of reimplementing the think → approve → invoke loop. |
| **Files** | `src/Nexo.CLI/Commands/BackgroundAgent/SelfExtendRunnerAdapter.cs`, optionally `src/Nexo.Runtime/AgentHost.cs` if extending for counts. |
| **Dependencies** | None (can do before or after 2.2) |
| **Risk** | Medium (behavior of self-extend runner; we lose explicit executed/denied unless we extend AgentHost) |

**Steps (option A — use AgentHost, infer summary from delta):**

1. In `SelfExtendRunnerAdapter.RunAsync`, after building `tools`, `policies`, `agent`, and `snapshot`:
   - Construct `var host = new AgentHost(new[] { agent }, tools, policies)`.
   - Call `var delta = await host.StepAsync(snapshot, cancellationToken).ConfigureAwait(false)`.
2. Build `SelfExtendRunResult` from `delta`:
   - If `delta == null`: `executed = 0`, `denied` unknown → use summary like "No tool calls executed." and e.g. `Success = true` (no failure).
   - If `delta != null`: infer executed from `delta.Log` (e.g. count lines containing `"write:"` or `"s&r:"`) or set a simple "Tool calls executed." summary; set `denied = 0` (AgentHost doesn’t expose denied count).
3. Remove the local loop (ThinkAsync + foreach tool call + Approve + InvokeAsync).
4. Keep the same early returns for empty repoRoot and missing directory; keep the same catch block returning `SelfExtendRunResult(false, 0, 0, ...)`.
5. Run BackgroundAgents tests and any SelfExtend/CLI tests that hit the adapter.

**Steps (option B — extend AgentHost to return counts):**

1. In `Nexo.Runtime`, add a result type, e.g. `StepResult(IActionDelta? Delta, int Executed, int Denied)`, and a new method `Task<StepResult> StepWithCountsAsync(...)` in `AgentHost` that does the same loop but increments `executed` and `denied` and returns them (and keep `StepAsync` as-is, or have it call `StepWithCountsAsync` and ignore counts).
2. In `SelfExtendRunnerAdapter`, use `StepWithCountsAsync`, then build `SelfExtendRunResult` from `result.Executed`, `result.Denied`, and `result.Delta`.
3. Remove the local loop in `SelfExtendRunnerAdapter`.

**Recommendation:** Start with option A (simpler; no Runtime API change). If you need exact denied count in logs later, add option B.

**Done criteria:** Self-extend runner still runs one ThinkAsync cycle and executes only approved tool calls; result summary is still meaningful; tests pass.

---

### 2.2 Shared repo-fs toolbox and policy factory

**Refactor #3 from analysis.**

| Item | Detail |
|------|--------|
| **Objective** | One factory that creates the minimal repo-fs toolbox (write + search_replace) and policy (PathAllowlist + MaxWriteSize). Adapter and Demo both use it. |
| **Files** | New: shared factory (see below). Edit: `SelfExtendRunnerAdapter.cs`, `SelfExtendToolRuntime.cs`. |
| **Dependencies** | None (can do before or after 2.1) |
| **Risk** | Medium (policy/tool set is shared; must not break Demo or adapter) |

**Steps:**

1. **Choose location** (pick one):
   - **A:** `src/Nexo.Tools.Dev/RepoFsToolboxFactory.cs` — keeps tools and policy in one place; CLI and Demo both reference Tools.Dev.
   - **B:** `src/Nexo.CLI/Commands/BackgroundAgent/RepoFsToolboxFactory.cs` — CLI-only; Demo would need to reference CLI or we duplicate a thin wrapper.

2. **Implement factory** (if A, in Nexo.Tools.Dev):
   - Add `RepoFsToolboxFactory` with static method `CreateMinimal(out IToolbox tools, out PolicyEngine policies)` (or return a record holding both).
   - Inside: create `CapabilityRegistry`, register `RepoFsWriteTool` and `RepoFsSearchReplaceTool`; create `PolicyEngine` with `PathAllowlist` and `MaxWriteSize()`; assign to out params or return.
   - Nexo.Tools.Dev already references Nexo.Runtime and Nexo.Policies.Dev; add Nexo.Policies if needed for PolicyEngine.

3. **Use in SelfExtendRunnerAdapter:** Replace inline construction of `CapabilityRegistry` and `PolicyEngine` with a call to the factory (e.g. `RepoFsToolboxFactory.CreateMinimal(out var tools, out var policies)` or similar). Keep building `agent` and `snapshot` as today.

4. **Use in SelfExtendToolRuntime:** In the constructor, call the factory to get the minimal tools and policies, then add the extra tools (DotnetBuildTool, DotnetTestTool, DotnetRunTool, RoslynAnalyzeTool) to the same registry and add OutputPathSandboxed and PerfHeadroom to the policy list (or create a new PolicyEngine with minimal policies plus the extras). Ensure the Demo still has the same effective toolbox and policies as before.

5. Build and run:
   - `Nexo.Tests.BackgroundAgents`
   - Any tests that use SelfExtendToolRuntime or the self-extend demo
   - Manual smoke: run one self-extend cycle (adapter) and one demo pipeline step if available

**Done criteria:** Adapter and Demo use the same minimal repo-fs toolbox/policy definition; behavior unchanged; tests pass.

---

### 2.3 Path validation helper for background-agent adapters

**Refactor #4 from analysis.**

| Item | Detail |
|------|--------|
| **Objective** | Shared “validate directory path” helper used by CodeAnalysisRunnerAdapter and SelfExtendRunnerAdapter. |
| **Files** | New: small helper (e.g. in CLI Commands/BackgroundAgent or in Tools.Dev). Edit: `CodeAnalysisRunnerAdapter.cs`, `SelfExtendRunnerAdapter.cs`. |
| **Dependencies** | None; can do before or after 2.1/2.2 |
| **Risk** | Low |

**Steps:**

1. Add a small static helper, e.g. `BackgroundAgentAdapterValidation.TryResolveDirectory(string path, string paramName, out DirectoryInfo? dir, out string? errorMessage)` returning bool: if path is null/whitespace set error and return false; else create `DirectoryInfo(path)` and if `!dir.Exists` set error and return false; otherwise set dir and return true.
2. In `CodeAnalysisRunnerAdapter.RunAsync`: call the helper with `path` and `"Path"`; on false return `new CodeAnalysisRunResult(false, 0, errorMessage)`.
3. In `SelfExtendRunnerAdapter.RunAsync`: call the helper with `repoRoot` and `"RepoRoot"`; on false return `new SelfExtendRunResult(false, 0, 0, errorMessage)`.
4. Run BackgroundAgents and CLI tests that hit these adapters.

**Done criteria:** Both adapters use the helper; validation behavior and error messages equivalent to before.

---

**Phase 2 checkpoint:** Full test run (e.g. `dotnet test Nexo.sln` or all BackgroundAgents + Infrastructure + CLI-related). Commit as "Phase 2: AgentHost in SelfExtendRunner, shared repo-fs toolbox, path validation helper".

---

## Phase 3: Optional Cleanups (Low Priority)

Only if time and value justify. Can be done in any order or skipped.

---

### 3.1 Run-result base type (Refactor #5)

- Add `BackgroundAgentRunResult(bool Success, string Summary)` (or interface) in BackgroundAgents; have `CodeAnalysisRunResult`, `TestRunResult`, `SelfExtendRunResult` extend or contain it.
- Update call sites only if they need a common type (e.g. logging); otherwise leave as-is.

### 3.2 WorldSnapshot helper for repo (Refactor #7)

- Add `WorldSnapshot.ForRepo(string repoRoot, string? outputRoot = null, int tick = 0)` (in Abstractions or a small util) and use it in SelfExtendRunnerAdapter, SelfExtendToolRuntime, and AgentExecutorAdapter if desired.

### 3.3 Adapter failure-result helper (Refactor #9)

- Add a small static helper that builds a failure result and logs (e.g. `FailureResult<T>(message, logger, ex)`) and use it in the three runner adapters’ catch blocks.

---

## Execution Order Summary

| Order | Item | Phase | Risk |
|-------|------|--------|------|
| 1 | Registry TryGetParameter | 1 | Low |
| 2 | Shared FindRepoRoot (tests) | 1 | Low |
| 3 | Path validation helper | 2 | Low |
| 4 | Use AgentHost in SelfExtendRunnerAdapter | 2 | Medium |
| 5 | Shared repo-fs toolbox factory | 2 | Medium |
| 6+ | Optional: run-result base, WorldSnapshot helper, adapter failure helper | 3 | Low |

**Suggested sequence:** 1 → 2 → 3 → 4 → 5, then Phase 3 items only if desired.

---

## Rollback

- **Phase 1:** Revert the single commit (registry + FindRepoRoot).
- **Phase 2:** Revert the Phase 2 commit; if you need to roll back only one refactor (e.g. AgentHost), do so by reverting just those file changes within the commit or in a follow-up revert.

---

## Sign-off

After each phase:

- [ ] Build succeeds.
- [ ] Relevant tests pass (list: …).
- [ ] No intentional behavior change; config and CLI usage unchanged.
- [ ] Commit (and optional tag) created.
