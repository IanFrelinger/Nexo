# Production-readiness pass

Applied on branch `claude/production-grade`, after the Nexo→Ashlar rename, the game-layer
extraction, and the sandbox-escape fix had merged. Every item below was verified against the
current tree by a 37-agent adversarial audit (findings reproduced live, not taken on trust),
then fixed and re-verified in the dev container.

## What "production grade" was defined as, and where it stands

| Spec | Before | After |
|---|---|---|
| One command builds everything shippable (`dotnet build Ashlar.sln`) | fails `NU1201` | builds |
| Every gate tells the truth | `cert-gate-config.sh` claimed 99, ran 178 | runtime-derived, no static count |
| Shipped metadata is accurate | `Ashlar.Lite`/`Ashlar.Sdk` described software that isn't there | descriptions match the code |
| No shipped path fails silently | 5 confirmed silent failures | each signals or is documented |
| Extracted game layer is a buildable package | loose directory | (tracked separately — see game-layer README) |

## Build integrity

- **`Ashlar.sln` now restores and builds.** Root cause: `Ashlar.API` is net10.0-only, and three
  commercial hosts referenced it while targeting net8.0 → `NU1201`. `GameDirector.Host` (the only
  such project *in* `Ashlar.sln`) plus `Fleet.Api`/`Fleet.Host`/`Tests.Fleet.Host` were moved to
  net10.0. net10 references net8 downlevel, so `Fleet.Contracts`/`Fleet.Infrastructure` stayed
  net8.0 — no multi-targeting needed. `Dockerfile.fleet-host` runtime bumped `aspnet:8.0`→`10.0`
  to match the net10 publish. The misleading multi-target comment on `Ashlar.API.csproj` that
  hid this was corrected.
- **A net10-leg compile error that had never surfaced** because `Ashlar.sln` never built:
  `VisionProSpatialAvailability` called `OperatingSystem.IsVisionOS()` behind `#if NET9_0_OR_GREATER`,
  but that method needs a visionOS-platform TFM, not just the .NET version. Replaced with the
  portable heuristic the net8 leg already used.
- **`MeshDirector` + its test project** were in no solution, so CI never gated them. Added to
  `Ashlar.sln`.

## Silent failures (each a real behaviour change, tested)

- **Pipeline default adapters fabricated success.** `Default{Deterministic,Agentic}StageExecutionAdapter`
  returned `Succeeded=true` doing no work, on the shipped `ashlar pipeline run` path. Now fail
  honestly with an `Error`; the run finalizes Failed. Test assertions flipped.
- **`GenericAgent` reported "completed task" for a domain it did no work on.** The `AgentFactory`
  fallback now logs a warning and flags the result `Placeholder=true` with honest output. Not
  thrown — it is a legitimate, widely-relied-on fallback.
- **`BehaviorExecutor` reported success even when steps errored.** `EvaluateSuccessCriteria` was
  hardcoded `true`; now a threaded step-failure count feeds `BehaviorCompletedEvent.Success`.
- **`BehaviorExecutor` silently skipped steps with unsupported condition syntax.** Now surfaced
  as a step error honouring `OnStepFailure`, instead of a silent skip.
- **`ImplementationSelector` recognised only 2 of the condition forms bricks author.** The shipped
  OWASP scanner's language/depth routing silently no-opped. Implemented the real grammar (bare
  boolean, `==`, `in [...]`); routing now takes effect. 8 grammar tests added.
- `ParallelLoopKernel.EnableParallel` on sync overloads is documented as ignored (docs-only; the
  behaviour was contract-compliant).

## Truthfulness (behaviour-preserving)

- `Ashlar.Sdk` / `Ashlar.Lite` descriptions rewritten to match what the packages deliver.
- `cert-gate-config.sh` stale 99-count enumeration removed; the count is runtime-derived.
- `Directory.Build.props` gained `<Authors>`/`<Company>`/`<Copyright>` so `dotnet pack` stops
  writing the assembly id into the package author field. **MAINTAINER: confirm the author string.**

## Deliberately NOT done — maintainer decisions

- **Container image rename** `nexo-cli`/`nexo-api` → `ashlar-*`: a publish action, deferred with
  the GitHub repo rename (handoff §6.0). Publisher and consumers agree on `nexo-*` today.
- **Ship-vs-delete** for `ValidationUtilities` (dead), `ApplyFeedbackChanges` (orphaned tool): kept
  and flagged rather than deleted — deletion is a product call.
- No fresh security review, performance work, or coverage expansion beyond the tests noted above.

## Mutation testing (gold plan step 5) — run 2026-08-23

Stryker.NET 4.16 over the admission decision core. Numbers and the finding that matters:

- Default (coverage-based) mode reported 71.33% with 29 survivors — **materially wrong**.
  The scariest "survivor" (the sealed-check equality flip) was hand-applied and killed by
  15 existing tests.
- Accurate mode (`coverage-analysis: off`, the checked-in `stryker-config.json` in
  Tests.Kernel) reported 94.29% on AdmissionGate with 2 survivors — **both also false**:
  the emptied required-gates foreach was hand-applied and killed by 7 tests.
- By hand-verification, AdmissionGate is **35/35 mutation-killed**. The run still paid for
  itself: four survivor clusters matched real test-inventory gaps (policy apiVersion/kind,
  parsed-null documents, manifest wrong-kind, null-arg guards) — six killer tests added
  in #376.

**Protocol:** in this environment (net10 + xunit in the dev container) Stryker's per-mutant
test execution produces false survivors even in accurate mode. Treat its survivor list as
PROPOSALS: every claimed survivor is hand-applied and the suite run before it is believed.
Do not wire mutation testing into CI as a gate until the false-survivor cause is found;
run it manually per release with this protocol.
