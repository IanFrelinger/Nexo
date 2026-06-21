# Atomic Brick Portability Spike Report

Version pin: `0.1.0` (from `VERSION`)
Probe brick: `ErrorSummaryExtractor` (deterministic log scanner)

## Step results

| Step | Description | Result |
|------|-------------|--------|
| 1 | Generate deterministic probe brick via `INewBrickGenerator` | PASS |
| 2 | Certify through S0–S2 gate (signed admission record) | FAIL |
| 3 | Pack Nexo.Brick.Contracts + Nexo.Authoring (+ Hosting.Bundle) @ 0.1.0 | PASS |
| 4 | Consume generated brick from external template (package pins only) | PASS |
| 5 | Cross-project HTTP execute assertion | PASS |

**Step 2 detail:** S0–S2 certification gate script not found in repository (see generated/certification-record.json)

## Contract-stability gaps (repo-internal context in generated brick)

- Generated source uses `Nexo.Core.Domain.Bricks` and `Nexo.Core.Domain.Execution` namespaces. These types ship through `Nexo.Authoring` / `Nexo.Brick.Contracts`, but the generated `using` lines do not reference the pinned package IDs directly — external consumers must know the namespace mapping.
- `NewBrickGenerator` only emits `ImplementationSource` for the hard-coded `ErrorSummaryExtractor` pattern; other pattern types still return `ImplementationSource: null` (manifest-only).
- No signed certification record or registry admission path exists yet (Step 2 blocker).

## Next steps to close gaps

1. Add S0–S2 brick certification gate with real execution (not rubber-stamp) and signed admission records.
2. Teach `NewBrickGenerator` to emit package-qualified usings or a stable authoring namespace alias aligned with `Nexo.Brick.Contracts`.
3. Extend `BrickRecompiler` to compile `ImplementationSource` without repo-internal types.

## Artifacts

- `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs`
- `spikes/portability/generated/manifest.json`
- `spikes/portability/generated/certification-record.json`
