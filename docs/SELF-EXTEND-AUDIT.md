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

> Findings table and REORDER note: added in `docs(sx): findings` after invariant tests land.
