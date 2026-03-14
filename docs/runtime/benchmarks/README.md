# Runtime Benchmark Sets

This folder separates production promotion benchmarks from stress/chaos scenarios, with split release lanes.

## Files

- `release_core_goals.txt`  
  Stable non-visual objectives used for the required `release-core` lane.

- `release_visual_goals.txt`  
  Visual objectives used for the strict `release-visual` lane (advisory until streak promotion).

- `chaos_goals.txt`  
  High-stress objectives used for resilience testing and failure-mode observation.

## Usage

Release matrix + gate:

- `dotnet run --project src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/release_core_goals.txt --policies release --benchmark-set release-core --run-tests --json`
- `dotnet run --project src/Nexo.CLI -- runtime gate --policy release --benchmark-set release-core --min-pass-rate 0.85 --min-total 10 --min-consecutive-passes 2 --json`

Visual release lane:

- `dotnet run --project src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/release_visual_goals.txt --policies release --benchmark-set release-visual --run-tests --json`
- `dotnet run --project src/Nexo.CLI -- runtime gate --policy release --benchmark-set release-visual --min-pass-rate 0.8 --min-total 8 --min-consecutive-passes 3 --json`

When visual lane is still advisory, pass `--allow-visual-capability-degrade` so strict visual fallback can degrade if docker/ollama capabilities are unavailable.

Chaos matrix (non-gating by default):

- `dotnet run --project src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/chaos_goals.txt --policies prod --benchmark-set chaos --run-tests --json`

Unified C# lane runner (replacement for shell gate scripts):

- `dotnet run --project src/Nexo.CLI -- runtime release-gate --repo-root . --mode full`
- `dotnet run --project src/Nexo.CLI -- runtime release-gate --repo-root . --mode core`
- `dotnet run --project src/Nexo.CLI -- runtime release-gate --repo-root . --mode visual`
- `dotnet run --project src/Nexo.CLI -- runtime release-gate --repo-root . --mode chaos`
