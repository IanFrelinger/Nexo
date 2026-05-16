# Runtime Benchmark Sets

This folder separates production promotion benchmarks from stress/chaos scenarios, with split release lanes.

## Files

- `release_core_goals.txt`  
  Stable non-visual objectives used for the required `release-core` lane.

- `release_visual_goals.txt`  
  Visual objectives used for both strict and degraded visual release lanes.

- `chaos_goals.txt`  
  High-stress objectives used for resilience testing and failure-mode observation.

## Usage

Release matrix + gate:

- `dotnet run --project application/src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/release_core_goals.txt --policies release --benchmark-set release-core --run-tests --json`
- `dotnet run --project application/src/Nexo.CLI -- runtime gate --policy release --benchmark-set release-core --min-pass-rate 0.85 --min-total 10 --min-consecutive-passes 2 --json`

Visual release lane:

- strict: `dotnet run --project application/src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/release_visual_goals.txt --policies release --benchmark-set release-visual-strict --run-tests --json`
- strict gate: `dotnet run --project application/src/Nexo.CLI -- runtime gate --policy release --benchmark-set release-visual-strict --min-pass-rate 0.8 --min-total 8 --min-consecutive-passes 3 --json`

Use benchmark attribution split:

- strict visual lane history: `release-visual-strict`
- degraded visual lane history: `release-visual-degraded`

When visual lane is still advisory, pass `--allow-visual-capability-degrade` so strict visual fallback can degrade if docker/ollama capabilities are unavailable (or use `runtime release-gate --visual-required-mode false`, which records degraded runs in `release-visual-degraded`).

Chaos matrix (non-gating by default):

- `dotnet run --project application/src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/chaos_goals.txt --policies prod --benchmark-set chaos --run-tests --json`

Unified C# lane runner (replacement for shell gate scripts):

- `dotnet run --project application/src/Nexo.CLI -- runtime release-gate --repo-root . --mode full`
- `dotnet run --project application/src/Nexo.CLI -- runtime release-gate --repo-root . --mode core`
- `dotnet run --project application/src/Nexo.CLI -- runtime release-gate --repo-root . --mode visual`
- `dotnet run --project application/src/Nexo.CLI -- runtime release-gate --repo-root . --mode chaos`

For promotion-style confidence in one run, increase repetitions and strictness, for example:

- `dotnet run --project application/src/Nexo.CLI -- runtime release-gate --repo-root . --mode full --lane-repetitions 3 --core-min-total 9 --core-history-window 9 --visual-required-mode true --visual-min-total 9 --visual-history-window 9`

CI shortcuts:

- smoke gate: `dotnet run --project application/src/Nexo.CLI -- ci runtime-gate`
- strict promotion profile: `dotnet run --project application/src/Nexo.CLI -- ci runtime-promotion`

For ephemeral CI runs, use per-run windows (for example `--core-min-total 3 --core-history-window 3`) so gating evaluates the just-executed lane matrix instead of requiring long-lived local history.
