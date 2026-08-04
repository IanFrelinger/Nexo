# kernel-coverage gate: why it never completes

**Status:** unresolved. The gate has never passed in recent history and is **not** a
required check. This document records the evidenced root cause and the options, so
the next person does not have to re-derive it.

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

**All 572 tests pass in 50 seconds, then the test host crashes during teardown**
and the run is marked Aborted. The crash is the long-standing
`System.Reflection.LoaderAllocatorScout.Finalize` / `0x80131506` "Internal CLR
error" seen whenever this suite runs, with or without coverage.

The consequence specific to this gate: **coverlet never receives the results, so no
coverage report is written**. Verified — `CoverageReports/infra*` is absent after
the run. The gate therefore cannot produce a verdict no matter how long it waits.

Until now the job had no `timeout-minutes`, so it inherited the 6-hour cap.

## What was ruled out

- **Not a slow or hanging test.** Execution finishes in 50s; the crash is in
  teardown, after the last test.
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

## Options for a real resolution

1. **Bisect the teardown crash.** Halve the suite repeatedly under
   `-f net9.0` + coverlet until the crashing subset is isolated, then fix or quarantine
   it. Bounded in principle, potentially many iterations; the crash is at process
   teardown so it does not point at a specific test.
2. **Switch the collector.** Try `--collect:"XPlat Code Coverage"` (data collector)
   instead of `coverlet.msbuild`. May or may not survive a host crash — worth one
   experiment before committing to option 1.
3. **Drop Infrastructure from the gate.** Keep the Domain 100% and Application 67%
   floors (both complete fine) and stop gating Infrastructure until the crash is
   fixed. Preserves most of the gate's value immediately.
4. **Retire the gate.** It has never passed, is not required, and currently
   consumes runner time to report nothing. If nobody is prepared to own options 1-3,
   deleting it is more honest than leaving permanently-red CI that everyone has
   learned to ignore.

**Recommendation:** option 3 as the immediate step (restores a working gate for two
of three assemblies), with option 2 as a cheap experiment. Option 4 is the correct
fallback if Infrastructure coverage is not actually wanted — a gate nobody can pass
is worse than no gate, because it trains people to ignore red.
