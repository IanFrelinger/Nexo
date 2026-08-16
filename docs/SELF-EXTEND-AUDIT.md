# Self-Extend Safety Audit (SX-AUDIT / SX-ENFORCE)

**Sprint:** SX-AUDIT (characterize) + SX-ENFORCE (A/B/C teeth) + SX-ENFORCE-D (D ceiling, 2026-08-16)  
**Branch:** `cursor/self-extend-enforce-6118` (off `cursor/self-extend-audit-6118`); D on `feat/sx-invariant-d-extension-ceiling`  
**Scope:** Background-agent self-extend path only (not CLI `nexo self-extend`, not orchestration `LifecycleManager`).

## Purpose

Trace the **executing** control flow for background extender agents and record whether four safety invariants are enforced. All four are now enforced on the live path (D closed by SX-ENFORCE-D, 2026-08-16).

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
              ├─ AgentPolicyNarrowingValidator (B)     [BackgroundAgentRegistry.cs:203–206]
              │    skip when ParentId absent (trust root)
              └─ stores instance                       [BackgroundAgentRegistry.cs:209–217]

BackgroundAgentService → registry.StartAllAsync        [BackgroundAgentService.cs:87]
  └─ AgentScheduler schedules extender cadence         [BackgroundAgentRegistry.cs:345]

Scheduler tick → TrackedExecuteAgentAsync              [BackgroundAgentRegistry.cs:394–397]
  └─ ExecuteAgentAsync                                 [BackgroundAgentRegistry.cs:416+]
       └─ role == "extender" + RepoRoot/Path param    [BackgroundAgentRegistry.cs:513–516]
            ├─ IAggressivenessModeStore.GetMode        [BackgroundAgentRegistry.cs:518]
            │    default Passive when store missing    [BackgroundAgentRegistry.cs:518]
            ├─ Passive → skip (C)                      [BackgroundAgentRegistry.cs:519–525]
            ├─ SemiActive → IApprovalGate (deny if null) [BackgroundAgentRegistry.cs:528–544]
            ├─ ExtensionCeiling.NarrowedBy(config) +
            │    ExtensionLedger.Refusal(depth/rate/unattended) (D)
            │    → refuse: log Warning + observation + telemetry, no runner call
            │    → else ledger.RecordCycle()          [BackgroundAgentRegistry.cs extender branch]
            └─ ISelfExtendRunner.RunAsync(+ agentId)   [BackgroundAgentRegistry.cs]
                 └─ SelfExtendRunnerAdapter.RunAsync  [SelfExtendRunnerAdapter.cs:74–196]
                      ├─ RepoFsToolboxFactory.CreateWithBuildTest
                      │    [SelfExtendRunnerAdapter.cs:116–131]
                      │    policies: PathAllowlist, MaxWriteSize, BuildTestBudget,
                      │    SelfProducedBrickCertificationPolicy (A),
                      │    DataExfiltrationPolicy (B),
                      │    optional ForgeMediatedWritesPolicy
                      │    [RepoFsToolboxFactory.cs:117–145]
                      ├─ BuildSnapshot (agentId + selfExtendAdmission)
                      │    [SelfExtendRunnerAdapter.cs:224–232]
                      └─ ToolCallingAgent.RunCycleAsync
                           [SelfExtendRunnerAdapter.cs:149–150]
                           → PolicyEngine.Approve per tool call
                           → repo.fs.write / forge.propose_change / dotnet.*

Runtime agent activation (separate from extend cycle):
  EnableAgentTool → registry.StartAsync              [EnableAgentTool.cs:49]
  UpdateAgentConfigTool → RegisterAsync (re-register) [UpdateAgentConfigTool.cs:75]
```

**Not on this path:** commercial `ApprovalBridge` (Discord playtest fixes), `LifecycleManager`.

## Invariant verdicts

| ID | Invariant | Verdict | Executing enforcement | Test |
|----|-----------|---------|----------------------|------|
| A | Cert-gate inheritance | **ENFORCED** | `SelfProducedBrickCertificationPolicy.Approve` on self-extend writes when `selfExtendAdmission=true`; verifies admitted record via `CertificationTrustVerifier` (content-bound) [`SelfProducedBrickCertificationPolicy.cs:24–78`, wired `RepoFsToolboxFactory.cs:137–140`]. Missing store → `FailClosedCertificationRecordStore` denies all brick admissions. Human boot roots unaffected (no self-extend snapshot). | `SelfExtendInvariantACertGateTests` |
| B | Monotonic policy narrowing | **ENFORCED** | `AgentPolicyNarrowingValidator.ValidateOrThrow` at `RegisterAsync` for `ParentId` children [`BackgroundAgentRegistry.cs:203–206`, `AgentPolicyNarrowingValidator.cs`]. Self-extend snapshot sets `agentId` + wires `DataExfiltrationPolicy` [`SelfExtendRunnerAdapter.cs:224–232`, `RepoFsToolboxFactory.cs:142–145`]. Roots without `ParentId` skip narrowing (trust root). | `SelfExtendInvariantBPolicyNarrowingTests` |
| C | Fail-closed default | **ENFORCED** | `FileBasedAggressivenessModeStore` missing/corrupt file → **Passive** [`FileBasedAggressivenessModeStore.cs:33–34`, `41–44`]. Registry fallback when mode store absent → **Passive** [`BackgroundAgentRegistry.cs:518`]. SemiActive without `IApprovalGate` → denied [`BackgroundAgentRegistry.cs:528–535`]. Explicit Active or approved SemiActive → runs. | `SelfExtendInvariantCHumanAdmissionTests` |
| D | Recursion / runaway ceiling | **ENFORCED** | Per-cycle: `ToolCallingAgent.DefaultMaxIterations`, `BuildTestBudget`. Cross-cycle: `ExtensionCeiling` (lineage depth, unattended cycles since human arm, cycles per trailing hour) enforced by `BackgroundAgentRegistry` after the mode gate and before `ISelfExtendRunner.RunAsync` [`ExtensionCeiling.cs`, `ExtensionLedger.cs`, `BackgroundAgentRegistry.cs` extender branch]. Overrides (env, agent `Parameters`) may only LOWER; ledgers live outside the agent instance so re-registration cannot reset them; re-arm is `RearmExtension` (operator surface) or restart. Refusals are logged (Warning), observed (`stopped_reason=extension_ceiling`), and carried in cycle telemetry. | `SelfExtendInvariantDRecursionCeilingTests` — rejection **PASSES** |

### Invariant A — cert-gate inheritance

**Verdict: ENFORCED**

Self-produced brick writes under `src/Nexo.Bricks*/` on the self-extend admission edge require an admitted, content-bound certification record. Uncertified, missing-record, and tampered content are refused by `SelfProducedBrickCertificationPolicy`.

### Invariant B — monotonic policy narrowing

**Verdict: ENFORCED**

Machine-spawned agents (`ParentId` set) must have an envelope ⊆ creator at registration. Human-authored trust roots (`ParentId` absent) register unchanged. Self-extend cycles key `DataExfiltrationPolicy` on `agentId` in the snapshot.

### Invariant C — fail-closed default

**Verdict: ENFORCED**

Unconfigured aggressiveness defaults to **Passive** — extender cycles do not run until an operator sets Active (or approves SemiActive). This is distinct from mesh `enable_agent` activation; monitors/testers in the boot agent set still start normally.

### Invariant D — recursion / runaway ceiling

**Verdict: ENFORCED**

Within-cycle ReAct and build/test budgets exist. Across cycles, `ExtensionCeiling` refuses an extend cycle when any of three ceilings is reached — `MaxLineageDepth` (default 1: roots and their direct children may extend, machine-spawned grandchildren may not; depth = `ParentId` hops, an unresolvable parent still counts), `MaxUnattendedCycles` (default 8 since a human last armed the agent, then hold), `MaxCyclesPerHour` (default 4, trailing hour). The environment (`NEXO_EXTENSION_MAX_LINEAGE_DEPTH`, `NEXO_EXTENSION_MAX_UNATTENDED_CYCLES`, `NEXO_EXTENSION_MAX_CYCLES_PER_HOUR`) and an agent's own `Parameters` (`MaxLineageDepth`, `MaxUnattendedCycles`, `MaxCyclesPerHour`) may only lower these — the same posture as the certified loop's `RecursionDiscipline`; raising a default is a code change. Only cycles that actually reach the runner consume budget (a Passive skip or approval denial does not). Re-arm is an operator act: `BackgroundAgentRegistry.RearmExtension(agentId)` or a process restart; re-registration deliberately does not re-arm because agents can re-register themselves (`UpdateAgentConfigTool`), and re-arm clears the unattended count but not the trailing-hour rate. A CLI verb for re-arm is a follow-up on an `application/*` branch.

## REORDER note (post SX-ENFORCE-D)

Invariants **A, B, C and D** are now enforced on the live path. Unattended multi-cycle self-extension is bounded: an Active extender runs at most `MaxUnattendedCycles` cycles before a human must re-arm it, at most `MaxCyclesPerHour` in any hour, and only within `MaxLineageDepth` of a human-authored root. What remains open is not a ceiling but convergence: the legacy extender path and the certified autonomy loop are two self-extension paths, and the long-term intent is one.

## Test index

| File | Role |
|------|------|
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantACertGateTests.cs` | Invariant A |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantBPolicyNarrowingTests.cs` | Invariant B |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantCHumanAdmissionTests.cs` | Invariant C |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendInvariantDRecursionCeilingTests.cs` | Invariant D |
| `src/Nexo.Tests.BackgroundAgents/SelfExtend/SelfExtendAuditTestSupport.cs` | Shared helpers |

The invariant D rejection test is live (no `Skip`); it was the last `GAP` marker in the suite.
