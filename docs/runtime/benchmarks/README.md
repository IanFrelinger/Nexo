# Runtime Benchmark Sets

This folder separates production promotion benchmarks from stress/chaos scenarios.

## Files

- `release_goals.txt`  
  Stable objectives used for release promotion and SLO gating.

- `chaos_goals.txt`  
  High-stress objectives used for resilience testing and failure-mode observation.

## Usage

Release matrix + gate:

- `dotnet run --project src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/release_goals.txt --policies release --run-tests --json`
- `dotnet run --project src/Nexo.CLI -- runtime gate --policy release --min-pass-rate 0.8 --min-total 5 --json`

Chaos matrix (non-gating by default):

- `dotnet run --project src/Nexo.CLI -- runtime evaluate --goals-file docs/runtime/benchmarks/chaos_goals.txt --policies prod --run-tests --json`
