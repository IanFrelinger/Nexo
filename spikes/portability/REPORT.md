# Atomic Brick Portability Spike Report

Version pin: `0.1.0` (from `VERSION`)
Probe brick: `ErrorSummaryExtractor` (deterministic log scanner)

## Step results

| Step | Description | Result |
|------|-------------|--------|
| 1 | Generate deterministic probe brick via `INewBrickGenerator` | PASS |
| 2 | Certify through S0–S2 gate (signed admission record) | PASS |
| 3 | Pack Nexo.Brick.Contracts + Nexo.Authoring (+ Hosting.Bundle) @ 0.1.0 | PASS |
| 4 | Consume generated brick from external template (package pins only) | PASS |
| 5 | Cross-project HTTP execute assertion | PASS |

**Step 2 detail:** Signed admission record at generated/certification-record.json (escape_rate=0)

## Gate teeth

Strong witness on the probe brick: ADMIT with `escape_rate=0`, mutants killed: ["flip-gt-error-count","flip-lt-index","off-by-one-plus","negate-contains","drop-error-branch","mutate-first-message-only"].
Weak witness (unit test `MutationProbeBrick`, errorCount-only): REJECT with `escape_rate > 0`; survivors include `flip-gt-error-count` and `mutate-first-message-only`.

## Contract-stability gaps (repo-internal context in generated brick)

- Generated brick uses Nexo.Core.Domain.* namespaces (shipped via Nexo.Authoring/Nexo.Brick.Contracts but not the pinned package IDs)

## Artifacts

- `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs`
- `spikes/portability/generated/manifest.json`
- `spikes/portability/generated/certification-record.json`
