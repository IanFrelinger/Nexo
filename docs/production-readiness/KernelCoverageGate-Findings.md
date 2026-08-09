# kernel-coverage gate: why it never completes

**Status:** the crash is FIXED. The gate COMPLETES (Domain + Core.Application floors). Infrastructure coverage remains excluded, but the reason has changed: fixing the crash exposed a **separate, previously invisible hang** in the full Infrastructure suite. It is not a required check. This document records the evidenced root cause, what was tried, and what remains.

> **THREE defects, stacked.** Everything below the "Symptom" heading was written when
> only the first was known, and parts of it are wrong — corrected inline.
>
> Each one was invisible until the one in front of it was fixed. That is the real
> lesson of this document: a failure that kills the process early hides everything
> behind it, and every "we ruled that out" conclusion drawn from a truncated run is
> worthless.
>
> 1. **The collectible-`AssemblyLoadContext` crash — FIXED.** The certification
>    mutation engine tore down overlapping collectible load contexts; finalizing them
>    killed the process. Fixed in `BrickMutationEngine` (single owner, serialised
>    teardown, unconditional unload). Proven crash-free across ~2 hours of CI, where
>    the process previously could not survive half a minute.
>
> 2. **A dependency cycle that hung API host startup — FIXED.** Not a deadlock and not
>    environmental:
>
>    ```
>    IBackgroundAgentRegistry factory  -> sp.GetService<ISelfExtendRunner>()
>    ISelfExtendRunner factory         -> sp.GetRequiredService<SelfExtendRunnerAdapter>()
>    SelfExtendRunnerAdapter ctor      -> IBackgroundAgentRegistry   (back to the top)
>    ```
>
>    A full dump caught ~298 repetitions of that pair across 8,340 stack lines, on one
>    thread, with `WebApplicationFactory.get_Services()` blocked on host startup and no
>    exception ever thrown. **Microsoft.Extensions.DependencyInjection cannot detect
>    this**: its circular-dependency check inspects constructor-injected graphs at
>    validation time, and the loop is laundered through two factory lambdas, so the
>    graph passes `ValidateOnBuild` and then recurses at *resolution* time. The
>    parameter being optional does not help — the registry is registered, so DI
>    re-enters the factory rather than passing null. Fixed by deferring the registry
>    behind `Lazy<T>`, registered beside `IBackgroundAgentRegistry` itself so the two
>    can never be wired apart. `ApiDevelopmentHostDiTests` went from unbounded to 1s.
>
>    *Follow-up (not done):* `SelfExtendRunnerAdapter` never calls a single method on
>    the registry — it passes it straight through to `RepoFsToolboxFactory`. That is
>    accidental coupling; segregating the slice the toolbox actually needs would remove
>    the cycle structurally rather than deferring it.
>
> 3. **A process-lifetime leak — OPEN, newly discovered, and the current blocker.**
>    `InfrastructureRoutingGapCoverageTests` passes **7/7 in 588ms** and then the test
>    host **never exits**. `coverlet.msbuild` writes its report only after the host
>    exits, so the coverage step waits forever — no crash, no failing test, no output.
>    Three capped runs died exactly this way, all with **zero** crash signatures:
>    30m20s, 60m20s, 45m21s. **Raising the cap cannot help**, because the process never
>    exits at all. A single trivial test from the same assembly exits cleanly, so this
>    is test-specific rather than assembly-wide. Suspect: the federated-mesh path
>    (`RemoteCatalogBaseUrls: https://peer.example`) starting something with a
>    non-background thread. This is also the cause of the orphaned `testhost` processes
>    that lock output DLLs and silently make `--no-build` runs execute stale binaries.
>
> Infrastructure coverage is therefore still unmeasured, and **no trustworthy
> coverage number for it exists**: every historical run was truncated by the crash at
> a different point, so the 83% floor in the table below was never measured against a
> complete run.

## Symptom

The `kernel-coverage` job runs until GitHub's 6-hour job cap and is auto-cancelled.
Observed on `master` (2026-07-13, 2026-07-14), on `dependabot/*` branches
(2026-08-01), and on every feature branch that reaches the second step. It has
never produced a verdict.

## Root cause

`scripts/ci/kernel-coverage-gate.sh` runs three coverage steps in order:

| Step | Target | Threshold | Behaviour |
|---|---|---|---|
| 1 | `Nexo.Tests.Domain` → `[Nexo.Core.Domain]` | 100% line | completes, ~1 min |
| 2 | `Nexo.Tests.Infrastructure -f net9.0` → `[Nexo.Infrastructure]` | 83% line | **never returns** |
| 3 | `Nexo.Tests.Application` → `[Nexo.Core.Application]` | 67% line | never reached |

Step 2 reproduced locally. The tests themselves are fine:

```
Passed!  - Failed: 0, Passed: 572, Skipped: 0, Total: 572, Duration: 50 s
The active test run was aborted. Reason: Test host process crashed
Test Run Aborted.
```

> **CORRECTION — the paragraph below is wrong on both counts.** It is not a teardown
> crash, and 572 was never the total.
>
> The crash fires **mid-run**, from the finalizer thread after a GC, so where it lands
> depends on GC timing rather than on reaching the end of the suite. Every "total" it
> reported was a truncation artifact: across runs on both frameworks the counts were
> **52, 77, 182, 199, 217, 243 and 461**. 572 was simply how far one run happened to
> get before dying, and it was read as a complete pass.
>
> This mattered. "All tests pass, then it crashes at teardown" framed the problem as
> cosmetic — a cleanup issue after the real work succeeded — when in fact most of the
> suite was never running at all, and a second defect was hiding behind it.

**All 572 tests pass in 50 seconds, then the test host crashes during teardown**
and the run is marked Aborted. The crash is the long-standing
`System.Reflection.LoaderAllocatorScout.Finalize` / `0x80131506` "Internal CLR
error" seen whenever this suite runs, with or without coverage.

The consequence specific to this gate: **coverlet never receives the results, so no
coverage report is written**. Verified — `CoverageReports/infra*` is absent after
the run. The gate therefore cannot produce a verdict no matter how long it waits.

Until now the job had no `timeout-minutes`, so it inherited the 6-hour cap.

## What was ruled out

- ~~**Not a slow or hanging test.** Execution finishes in 50s; the crash is in
  teardown, after the last test.~~
  **WRONG, and this is the entry that cost the most.** There *is* a hanging test. The
  50-second figure came from a run that died partway through, so "execution finishes"
  described a truncated run, and the conclusion ruled out the very thing that is now
  the remaining blocker. Ruling a cause out on evidence from a crashed run is what
  kept the second defect invisible.
- **Not the `xunit.runner.json` copy-directive trap** that made `Nexo.Tests.CLI`
  silently parallel. This project has the same missing directive, but its stale
  `bin\` copy and its source are behaviourally identical (the only delta is an
  explicit `parallelizeTestCollections: true`, which is also the default). The
  directive was added anyway as hygiene; it changes nothing here.
- **Not solely the collectible-`AssemblyLoadContext` certification path.**
  `MutantAssemblyLoadContext` is the obvious `LoaderAllocator` suspect, and it does
  call `Unload()` correctly. Excluding `AstMutationEngineTests` entirely still
  crashes (432/432 pass, host still dies, still no report). So either another test
  loads assemblies collectibly — `TestRunnerAdapter` uses `Assembly.LoadFrom`, and
  the `UnitTestBridge` path loads assemblies at runtime — or the crash is not
  ALC-specific at all.

## What was changed here

Bounded and low-risk only:

1. `timeout-minutes: 30` on the job. Converts a 6-hour burn into a 30-minute
   failure. Does **not** fix the crash.
2. `<Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />`
   in `Nexo.Tests.Infrastructure.csproj` — hygiene against config drift.

## Resolution applied

**Option 2 (swap the collector) was tried first and rejected.** Added
`coverlet.collector` plus a runsettings with `Format=cobertura` and
`Include=[Nexo.Infrastructure]*`, mirroring the old `-p:Include` exactly:

```
Passed! - Failed: 0, Passed: 452, Skipped: 1, Total: 453, Duration: 58 s
The active test run was aborted. Reason: Test host process crashed
Test Run Aborted.
-> no coverage.cobertura.xml produced; command still had to be killed at 560s
```

Collecting out-of-process does not help, because the run is **aborted** — VSTest
discards the collector's attachments exactly as it discarded coverlet's. Both
routes fail identically:

| Route | Tests | Outcome |
|---|---|---|
| `coverlet.msbuild` instrumentation | 572/572 pass | host crashes, **no report** |
| `XPlat Code Coverage` collector | 452/453 pass | host crashes, **no report** |

The crash — not the instrumentation method — is the blocker.

**Option 3 (drop Infrastructure) is what shipped.** The gate now runs Domain
(100%) and Core.Application (67%) only, both of which complete, so it returns a
real pass/fail instead of hanging. `timeout-minutes: 30` is kept as a backstop.

## Resolution: the crash is fixed

Root cause was the certification mutation engine, and it was **not** the leaked,
never-unloaded context everyone assumed. `Unload()` was always being called on the
happy path. The defect was **concurrent teardown of several collectible contexts**:
unload was requested while the caller still held the mutant instance and assembly,
the loop immediately loaded the next mutant into a new collectible context, and
callers ran in parallel because xunit executes collections concurrently. Finalizing
those overlapping `LoaderAllocator`s is what killed the process.

Evidence: dumping `AssemblyLoadContext.All` showed the only collectible contexts in
the process were the unnamed ones holding `MutantBrick_*` assemblies, churning
2 → 3 → 2 as they piled up mid-unload. Making the context non-collectible made
crashing runs clean, which established causality.

Fixed in `BrickMutationEngine`: per-mutant work confined to a `NoInlining` frame that
lets nothing ALC-typed escape, the reflectively-invoked witness fully awaited inside
that frame, `Unload()` moved to an unconditional `finally`, collection driven until
the context is actually released, and a **process-wide** semaphore held across the
whole load/execute/unload/collect sequence so at most one context exists anywhere.
Serialising within a single loop was tried first and was **not** sufficient —
concurrent callers still overlapped. The context stays collectible; making it
non-collectible would swap the crash for an unbounded assembly leak.

Two genuine leaks were fixed alongside: `Unload()` ran only on the success path, so a
throwing witness leaked the context outright; and the temp directory was deleted
while the DLL was still memory-mapped, so the delete silently failed and those
directories accumulated for the life of the process.

## Still open: a process-lifetime leak

Infrastructure coverage is **still not measured**, now for a third reason.

The hang that followed the crash was diagnosed and fixed (the DI cycle above). What
remains is different in kind: the suite *passes*, and the process simply never exits.

| cap | outcome | crash signatures |
|---|---|---|
| 30 min | cancelled at 30m20s | 0 |
| 60 min | cancelled at 60m20s | 0 |
| 45 min | cancelled at 45m21s | 0 |

Local reproduction, in under a second:

```
dotnet test --filter FullyQualifiedName~InfrastructureRoutingGapCoverageTests
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 588 ms
-> process still alive; killed externally after 180s
```

A single trivial test from the same assembly exits cleanly, so this is **test-specific,
not assembly-wide**. Something that test starts holds a foreground thread. The likely
area is the federated-mesh registry resolution it performs with
`RemoteCatalogBaseUrls: https://peer.example`.

Two consequences worth stating plainly:

- **No cap can fix it.** `coverlet.msbuild` writes its report after the host exits, and
  the host never exits. Raising the limit only changes how long the runner burns.
- **It has been corrupting local runs all along.** The orphaned `testhost` processes
  that hold output DLLs — making builds fail and `--no-build` silently execute *stale*
  binaries — are this leak. Results produced that way looked authoritative and were
  meaningless.

Whether this is a test-side leak (a fixture not disposing) or a product bug (a mesh
service spawning a non-background thread it never disposes, which would leak in
production too, exactly as the DI cycle did) is not yet established. Next step is a
`dotnet-dump collect` against the live non-exiting process to name the thread.

## Method note

Three separate conclusions in this document were drawn from runs that had already been
truncated by an earlier defect, and all three were wrong: "all 572 tests pass", "not a
slow or hanging test", and the 83% floor. Evidence gathered after a process dies early
describes the prefix, not the system.

**There is no trustworthy Infrastructure coverage number, and the 83% floor was never
measured.** Every historical run was truncated by the crash at a different point, so
restoring 83 would be restoring a figure nobody ever verified. The real floor should
be set from the first complete run, not inherited.

Related tracked item: `TestRunnerAdapter.ExecuteTestAsync` abandons its `runTask` on
the per-test timeout path (latent, never executed today, separate bug).
