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

## Composition certification

Composition: **error-summary-pipeline** — certified `mutation-probe-brick` → `error-summary-formatter` with independent composition-level witness (not derived from constituent witnesses).

| Case | Setup | Witness | Result | Gate detail |
|------|-------|---------|--------|-------------|
| 4a | Correct wiring | Strong (`errorCount` + `summary`) | **ADMIT** | `composition_escape_rate=0`, signed composition record |
| 4b | Broken seam (`firstErrorMessage` → `errorCount`) | Strong | **REJECT** | `seam` — producer type `string` does not satisfy consumer `int` |
| 4c | Correct wiring | Weak (`errorCount` only) | **REJECT** | `mutation` — `composition_escape_rate > 0`; survivors include `drop-node-format`, `reorder-adjacent-0` |
| 4d | Uncertified `uncertified-brick` constituent | Strong | **REJECT** | `constituents` — no valid atom certification record |
| 4e | Volatile `nonce` output (unwitnessed) | Strong | **REJECT** | `determinism` — composition outputs differ under AuditMode |

Graph mutations only (reorder/drop/redirect/swap brick assignments); constituent brick source is sealed.

## Dogfood run

**honest=ADMIT, buggy=REJECT, tests_executed=19** — **CI-confirmed** on PR #191 @ `802e6d18`

Intent: **damage-resolver** — Cursor-authored `CursorGeneratorModel` (`cursor:honest` / `cursor:buggy`); human-authored witness in `DamageResolverDogfoodWitness.Spec` (generation blind).

### CI verification (authoritative)

| Fact | Value |
|------|-------|
| Workflow | [Cert gate run 27918244198](https://github.com/IanFrelinger/Nexo/actions/runs/27918244198) — `conclusion: success` |
| Commit | `802e6d180bcae8cb7538d0497a644f67a5153893` |
| Tests executed | **19** (TRX `Counters total="19" executed="19" passed="19"`) |
| `HonestCursorGeneration_Admits_WithZeroEscapeRate` | **PASS** (CI TRX `outcome="Passed"`, 941 ms on `runnervm7b5n9`) |
| `BuggyCursorGeneration_Rejects` | **PASS** (CI TRX `outcome="Passed"`, 63 ms) |
| Honest brick `escape_rate` | **0** (test assertion on ADMIT path; no mutant survivors) |
| Honest brick `signed` | **true** (test assertion on ADMIT path) |
| TRX artifact | `cert-gate-trx` uploaded by workflow (not committed to repo) |

CI console: `Test Run Successful. Total tests: 19, Passed: 19`; `cert-gate executed 19 tests (expected>=19).`

### Local reproduction

`bash scripts/run-cert-gate.sh` also passes on branch `cursor/dogfood-cursor-generator-921c` (2026-06-21).

## Contract-stability gaps (repo-internal context in generated brick)

- Generated brick uses Nexo.Core.Domain.* namespaces (shipped via Nexo.Authoring/Nexo.Brick.Contracts but not the pinned package IDs)

## Artifacts

- `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs`
- `spikes/portability/generated/manifest.json`
- `spikes/portability/generated/certification-record.json`
