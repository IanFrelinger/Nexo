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

Strong witness on the probe brick: ADMIT with `escape_rate=0`, mutants killed by AST operators (`flip-binary-op`, `negate-condition`, `mutate-int-literal`, `remove-statement`, etc.).
Weak witness (unit test `MutationProbeBrick`, errorCount-only): REJECT with `escape_rate > 0`; survivors include AST ids such as `flip-binary-op-*` and `mutate-int-literal-*`.

## General generation

Intent: **line-substring-counter** — extract count of lines containing a given substring (plus `firstMatchingLine` output).

Independent strong witness (human-provided, not authored by generator):

| Input | Expected output |
|-------|-----------------|
| `text="FOO line one\nplain\nFOO line two\n"`, `substring="FOO"` | `matchCount=2`, `firstMatchingLine="FOO line one"` |

Model seam: `IGeneratorModel` with hermetic `FixtureGeneratorModel` in tests; production `ProviderGeneratorModel` (`model:ollama:isolation-enforced`) behind the sealed seam.

### Generate→certify→admit results (AST-derived mutations, fixture model)

| Case | Variant | Witness | Result | Gate detail |
|------|---------|---------|--------|-------------|
| 4a | `fixture:correct` | Strong | **ADMIT** | `escape_rate=0`, signed admission record |
| 4b | `fixture:buggy` | Strong | **REJECT** | `correctness` — `firstMatchingLine` reports last match (`FOO line two`) instead of first |
| 4c | `fixture:correct` | Weak (`matchCount` only) | **REJECT** | `mutation` — `escape_rate > 0` (AST mutants survive weak witness) |
| 4d | `fixture:dependency-leak` | Strong | **REJECT** | `dependency` — source contains forbidden token `Nexo.Infrastructure` |

Generated manifest carries `GenerationProvenance` (e.g. `fixture:correct`) marking model/fixture origin on the artifact.

## Contract-stability gaps (repo-internal context in generated brick)

- Generated brick uses Nexo.Core.Domain.* namespaces (shipped via Nexo.Authoring/Nexo.Brick.Contracts but not the pinned package IDs)

## Artifacts

- `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs`
- `spikes/portability/generated/manifest.json`
- `spikes/portability/generated/certification-record.json`
