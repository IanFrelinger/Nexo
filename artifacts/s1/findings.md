# S1 Gate Escape Rate

## S1.3 → S1.4 closeout — before/after

| Metric | s1.3-v1 (before) | s1.4-v1 (after) |
| --- | ---: | ---: |
| Probe classes | 12 | **18** (corpus subsumes catalog) |
| Witness pinning | single-seed | **multi-seed** (8 seeds; vacuity on odd `SwappedOperands`) |
| Intent density | 100.0% (12/12) | **100.0%** (18/18) |
| Wrong-impl escape rate | 5.6% (8/144) | **0.0%** (0/144) |
| Wrong-impl false-reject rate | 0.0% | **0.0%** |
| Certification equivalence | over-claimed (density 1.0 with 8 escapes) | **faithful** (density 1.0 ⇔ escapes 0) |
| Negative control | n/a | density **88.9%**, escapes **8** (relation removed) |

Residual escapes closed in S1.4:

- `SemanticSamplingWindow` — acceptance `["1","2","hello"] => String` + metamorphic full-column test
- `SwappedOperands` odd seeds — behavioral vacuity (identical on frozen witnesses)

## Headline

- **Catalog version**: `s1.4-v1`
- **Adversary**: `offline` (offline taxonomy; not adaptive/LLM)
- **Seeds**: 8 (144 distinct wrong-impl trials; 144 total runs)
- **Wrong-impl escape rate** (PropertyGate): **0.0%** (0/144 adversarial candidates escaped)
- **Wrong-impl false-reject rate**: 0.0%
- **Weak-test dimension**: skipped:mutation-sample-zero (MutationGate escape rate: n/a)

## Tool availability

- dotnet: available
- dotnet-stryker: skipped — not installed (Possible reasons for this include:
  * You misspelled a built-in dotnet command.
  * You intended to execute a .NET program, but dotnet-stryker does not exist.
  * You intended to run a global tool, but a dotnet-prefixed executable with this name could not be found on the PATH.

Could not execute because the specified command or file was not found.)

## Wrong-impl per-transform breakdown

| Transform | Total | Escapes | Caught | Escape rate | Attribution |
| --- | ---: | ---: | ---: | ---: | --- |
| `OffByOne` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `BoundaryInclusive` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["2024-01-15"] => Date |
| `NegatedCondition` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["true","false"] => Boolean |
| `DroppedBranch` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["2024-01-15"] => Date |
| `ConstantReturn` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `SwappedOperands` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal; vacuous boolean/date swap on odd seeds |
| `SemanticTypePrecedenceDecimalFirst` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `SemanticTypePrecedenceZeroOneBool` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["0","1"] => Integer |
| `SemanticEmptyWhitespaceRetained` | 8 | 0 | 8 | 0.0% | caught: invariant: whitespace-only cells treated as empty |
| `SemanticFormatLeadingZeros` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["007"] => Integer |
| `SemanticFormatThousands` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1,000"] => Decimal |
| `SemanticFormatScientific` | 8 | 0 | 8 | 0.0% | caught: frozen acceptance criteria (spec-derived) |
| `SemanticFormatLocaleComma` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1,5"] => Date; ["1,."] => Decimal |
| `SemanticFormatSignedZero` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["+0","-0"] => Integer |
| `SemanticSamplingWindow` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","hello"] => String; metamorphic: full-column not prefix |
| `SemanticBooleanYesNo` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["yes","no"] => String |
| `SemanticBooleanYn` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["Y","N"] => String |
| `SemanticHeterogeneousFallback` | 8 | 0 | 8 | 0.0% | caught: acceptance: mixed numeric and text => String |
## Metric scope

Catalog `s1.4-v1` measures escape rate for a **fixed offline transform catalog** on the S0 CSV inferencer fixtures. Escapes are signal: each names a missing property relation. This is not a target of 0% — attributed escapes form the property-authoring backlog. Adaptive or LLM adversaries may find additional escapes beyond this taxonomy.

## Intent density (multi-witness)

- **Scope**: Scoped to the fixed offline transform catalog and configured seed range; a lower bound on oracle faithfulness, not universal correctness.
- **Probe corpus version**: `s1.4-v1`
- **Witness seeds per class**: 8
- **Intent density**: **100.0%** (18/18 probe classes pinned across all seeds)
- **Certification threshold**: 95%
- **Honest-impl certification**: **Certifiable** — all probe classes pinned across all witness seeds

| Probe class | Status | Witnesses | Escaping seeds | Deciding relation |
| --- | --- | ---: | --- | --- |
| `zero-one-literals` | Pinned | 8 | — | acceptance: ["0","1"] => Integer |
| `leading-zeros` | Pinned | 8 | — | acceptance: ["007"] => Integer |
| `thousands-separator` | Pinned | 8 | — | acceptance: ["1,000"] => Decimal |
| `locale-comma-decimal` | Pinned | 8 | — | acceptance: ["1,5"] => Date; ["1,."] => Decimal |
| `signed-zero` | Pinned | 8 | — | acceptance: ["+0","-0"] => Integer |
| `boolean-yes-no` | Pinned | 8 | — | acceptance: ["yes","no"] => String |
| `boolean-yn` | Pinned | 8 | — | acceptance: ["Y","N"] => String |
| `whitespace-only` | Pinned | 8 | — | invariant: whitespace-only cells treated as empty |
| `scientific-notation` | Pinned | 8 | — | none — gap: scientific notation literals not in frozen acceptance criteria |
| `decimal-first-precedence` | Pinned | 8 | — | acceptance: ["1","2","3"] => Integer |
| `sampling-window-widening` | Pinned | 8 | — | acceptance: ["1","2","hello"] => String; metamorphic: full-column not prefix |
| `heterogeneous-fallback` | Pinned | 8 | — | acceptance: mixed numeric and text => String |
| `integer-minimum-count` | Pinned | 8 | — | acceptance: ["1","2","3"] => Integer |
| `single-value-date-boundary` | Pinned | 8 | — | acceptance: ["2024-01-15"] => Date |
| `boolean-branch-polarity` | Pinned | 8 | — | acceptance: ["true","false"] => Boolean |
| `date-branch-required` | Pinned | 8 | — | acceptance: ["2024-01-15"] => Date |
| `non-constant-inference` | Pinned | 8 | — | acceptance: ["1","2","3"] => Integer |
| `integer-decimal-precedence` | Pinned | 8 | — | acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal; vacuous boolean/date swap on odd seeds |

### Certification equivalence (capstone)

| density==1.0 | escapes==0 | equivalence holds |
| --- | --- | --- |
| True | True | **True** |

### Negative control (one relation removed)

- **Removed**: `acceptance: ["1","2","hello"] => String; metamorphic: full-column not prefix`
- **Intent density**: 88.9%
- **Wrong-impl escapes**: 8
- **Equivalence broken (expected)**: True
