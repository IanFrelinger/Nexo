# Self-Extend Safety Audit (SX-AUDIT)

**Sprint:** SX-AUDIT — characterize existing teeth; do **not** build new enforcement.  
**Branch:** `cursor/self-extend-audit-6118`  
**Scope:** Background-agent self-extend path only (not CLI `nexo self-extend`, not orchestration `LifecycleManager`).

## Purpose

Trace the **executing** control flow for background extender agents and record whether four safety invariants are enforced today. A documented **GAP** is a successful audit outcome.

## Control-flow trace

End-to-end path from host boot through one self-extend cycle:

```
BackgroundAgentService.ExecuteAsync                    [BackgroundAgentService.cs:59–91]
  └─ BackgroundAgentConfigLoader.LoadAsync             [BackgroundAgentService.cs:66]
  └─ foreach enabled config:
       CreateAndRegisterAgentAsync                     [BackgroundAgentService.cs:77]
         ├─ BackgroundAgentSpecBuilder.BuildSpec       [BackgroundAgentService.cs:123]
         ├─ AgentFactory.CreateAgent                   [BackgroundAgentService.cs:126]
         └─ BackgroundAgentRegistry.RegisterAsync       [BackgroundAgentService.cs:129]
              └─ stores instance; no cert/policy gates [BackgroundAgentRegistry.cs:191–209]

BackgroundAgentService → registry.StartAllAsync        [BackgroundAgentService.cs:87]
  └─ AgentScheduler schedules extender cadence         [BackgroundAgentRegistry.cs:345]

Scheduler tick → TrackedExecuteAgentAsync              [BackgroundAgentRegistry.cs:394–397]
  └─ ExecuteAgentAsync                                 [BackgroundAgentRegistry.cs:416+]
       └─ role == "extender" + RepoRoot/Path param    [BackgroundAgentRegistry.cs:513–516]
            ├─ IAggressivenessModeStore.GetMode        [BackgroundAgentRegistry.cs:518]
            │    default Active when store missing     [BackgroundAgentRegistry.cs:518]
            ├─ Passive → skip                          [BackgroundAgentRegistry.cs:519–525]
            ├─ SemiActive → IApprovalGate              [BackgroundAgentRegistry.cs:528–544]
            │    (not commercial ApprovalBridge)
            └─ ISelfExtendRunner.RunAsync              [BackgroundAgentRegistry.cs:555–557]
                 └─ SelfExtendRunnerAdapter.RunAsync  [SelfExtendRunnerAdapter.cs:74–196]
                      ├─ RepoFsToolboxFactory.CreateWithBuildTest
                      │    [SelfExtendRunnerAdapter.cs:116–121]
                      │    policies: PathAllowlist, MaxWriteSize, BuildTestBudget,
                      │    optional ForgeMediatedWritesPolicy
                      │    [RepoFsToolboxFactory.cs:117–131]
                      ├─ BuildSnapshot (AgentName, not agentId)
                      │    [SelfExtendRunnerAdapter.cs:208–304]
                      └─ ToolCallingAgent.RunCycleAsync
                           [SelfExtendRunnerAdapter.cs:149–150]
                           → PolicyEngine.Approve per tool call
                           → repo.fs.write / forge.propose_change / dotnet.*

Runtime agent activation (separate from extend cycle):
  EnableAgentTool → registry.StartAsync              [EnableAgentTool.cs:49]
  UpdateAgentConfigTool → RegisterAsync (re-register) [UpdateAgentConfigTool.cs:75]
```

**Not on this path:** `CertificationGate`, `CompositionCertificationGate`, `BackgroundAgentPolicyEngineFactory` / `DataExfiltrationPolicy` (factory is test-only wiring today), `LifecycleManager`, commercial `ApprovalBridge` (Discord playtest fixes).

## Invariant verdicts

| ID | Invariant | Verdict | Executing enforcement (or bypass path) | Test |
|----|-----------|---------|----------------------------------------|------|
| A | Cert-gate inheritance | **GAP** | **Bypass:** `SelfExtendRunnerAdapter.RunAsync` → `RepoFsToolboxFactory.CreateWithBuildTest` builds `PolicyEngine` with `PathAllowlist`, `MaxWriteSize`, `BuildTestBudget`, optional `ForgeMediatedWritesPolicy` only — no `ICertificationGate` / `CertificationGate` call on write or register (`RepoFsToolboxFactory.cs:117–131`, `SelfExtendRunnerAdapter.cs:116–150`). `BackgroundAgentRegistry.RegisterAsync` stores config with no cert check (`BackgroundAgentRegistry.cs:191–209`). Zero `CertificationGate` references under `Nexo.BackgroundAgents*` / `HostRunners`. | `SelfExtendInvariantACertGateTests` — 2 characterization PASS, 1 rejection **SKIP (GAP)** |
| B | Monotonic policy narrowing | **GAP** | **Bypass:** `RegisterAsync` accepts any `ExfiltrationPolicy` on config with no parent subset check (`BackgroundAgentRegistry.cs:191–209`). `ParentId` is copied into spawn spec dependencies only (`BackgroundAgentSpecBuilder.cs:52–57`). Self-extend cycle uses `RepoFsToolboxFactory` policies without `DataExfiltrationPolicy` (`RepoFsToolboxFactory.cs:117–131`). Snapshot sets `AgentName` not `agentId`, so even if `DataExfiltrationPolicy` were present it fail-opens (`SelfExtendRunnerAdapter.cs:220`, `DataExfiltrationPolicy.cs:73–77`). `BackgroundAgentPolicyEngineFactory` is not wired on this path. | `SelfExtendInvariantBPolicyNarrowingTests` — 2 characterization PASS, 1 rejection **SKIP (GAP)** |
| C | Human admission seam (ApprovalBridge) | **GAP** | **Bypass:** Default aggressiveness is **Active** when mode file missing (`BackgroundAgentRegistry.cs:518`, `FileBasedAggressivenessModeStore.cs:33–34`). Extender runs without approval in Active/Ambient. `EnableAgentTool` → `StartAsync` with no admission token (`EnableAgentTool.cs:49`). Commercial `ApprovalBridge` (Discord emoji → playtest fixes) is not referenced by background agent registration/activation. `IApprovalGate` gates **SemiActive extender cycles** only (`BackgroundAgentRegistry.cs:528–544`); default DI is `NoApprovalGate` (`ServiceCollectionExtensions.cs:56`). | `SelfExtendInvariantCHumanAdmissionTests` — 3 characterization PASS, 1 rejection **SKIP (GAP)** |
| D | Recursion / runaway ceiling | **GAP** (partial per-cycle caps) | **Partial:** `ToolCallingAgent.DefaultMaxIterations` (=5) and `DefaultPerCycleDeadline` (5 min) bound a single ReAct cycle (`ToolCallingAgent.cs:33–43`). `BuildTestBudget` caps build/test tool calls per cycle (`BuildTestBudget.cs:50–76`, wired in `RepoFsToolboxFactory.cs:121`). **Bypass:** No extender recursion depth counter or cross-cycle rate limit — `ExecuteOnceAsync` / scheduler can invoke extender repeatedly with no refusal (`BackgroundAgentRegistry.cs:555–601`; characterization runs 12 cycles unblocked). | `SelfExtendInvariantDRecursionCeilingTests` — 3 characterization PASS, 1 rejection **SKIP (GAP)** |

### Invariant A — cert-gate inheritance

**Verdict: GAP**

Self-proposed bricks written via `repo.fs.write` on the extender path are approved by dev filesystem policies only. Nothing on the path calls `CertificationGate.CertifyAsync` or `CertifiedBrickAdmission` before registration or tool execution.

### Invariant B — monotonic policy narrowing

**Verdict: GAP**

A child agent config with `BlockExternalLLMs = false`, `RequireLocalOnly = false`, and `MaxAllowedLevel = "Secret"` registers successfully even when the nominal parent carries a stricter envelope. No code compares child `ExfiltrationPolicy` to creator policy at spawn/register time.

### Invariant C — human admission seam

**Verdict: GAP**

“Activation” into the live mesh (`StartAsync` / `enable_agent`) does not require commercial `ApprovalBridge` or any human approval token. Default **Active** mode runs extender cycles immediately. SemiActive + `IApprovalGate` is a separate, optional per-cycle gate — not mesh admission — and defaults to deny when unwired.

### Invariant D — recursion / runaway ceiling

**Verdict: GAP** (with **partial** per-cycle enforcement)

Within one cycle, iteration and build/test budgets exist. Across cycles, nothing refuses an extender after N cumulative self-extend invocations or enforces a spawn-depth ceiling for agents created by extenders.

## REORDER note

**Invariants A and B are GAP — roadmap-changing.**

Before treating self-extend output as production-safe mesh expansion:

1. **Cert gate (A) must precede runnable bricks** — wire `CertificationGate` / composition admission on the propose → write → register edge so uncertified self-proposed bricks are refused, not merely path-allowlisted.
2. **Policy narrowing (B) must precede spawn** — compare child `ExfiltrationPolicy` and `MaxDataSensitivity` to creator envelope at `RegisterAsync` / spawn-spec validation; wire `DataExfiltrationPolicy` on the self-extend snapshot (`agentId`) if exfiltration teeth are required at tool time.

Human admission (C) and extender recursion ceiling (D) are also GAP but are operational guardrails that can follow A/B in priority — uncertified, over-privileged agents are the higher-severity expansion risk.

## Test index

| File | Role |
|------|------|
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantACertGateTests.cs` | Invariant A |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantBPolicyNarrowingTests.cs` | Invariant B |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantCHumanAdmissionTests.cs` | Invariant C |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantDRecursionCeilingTests.cs` | Invariant D |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendAuditTestSupport.cs` | Shared helpers |

Rejection tests use `[Fact(Skip = "GAP: … see docs/SELF-EXTEND-AUDIT.md#…")]` so CI stays green while the gap remains visible in test discovery output.
