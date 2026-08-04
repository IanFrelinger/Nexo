# kernel-coverage gate: why it never completes

**Status:** partially resolved. The gate now COMPLETES (Domain + Core.Application floors); Infrastructure coverage is excluded pending a fix for the teardown crash. It is not a required check. This document records the evidenced root cause, what was tried, and what remains.

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

## Still open

Infrastructure coverage is **not measured**. Restoring it requires fixing the
teardown crash itself — option 1 (bisect the suite) remains the only untried
route. Related tracked item: `TestRunnerAdapter.ExecuteTestAsync` abandons its
`runTask` on the per-test timeout path (latent, never executed today, separate
bug).

If nobody intends to fix the crash, option 4 — retiring the Infrastructure floor
permanently rather than leaving it commented out — is the honest end state.
