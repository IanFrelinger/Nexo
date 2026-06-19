# Probe catalog mapping (S1.4)

**Catalog version:** `s1.4-v1`  
**Probe corpus version:** `s1.4-v1`

## Invariant

Every wrong-impl transform tag in `TransformCatalog.WrongImplTags` maps to **at least one** probe class in `ProbeCorpus.All`. The mapping is enforced by `ProbeCatalogMapping` and unit-tested in `ProbeCatalogMappingTests.Every_wrong_impl_transform_has_at_least_one_probe_class`.

If a new transform is added to the catalog without a corresponding probe class, CI fails.

## Mapping table (18 transforms ↔ 18 probe classes)

| Probe class | Transform tag | Witness inputs | Deciding relation |
| --- | --- | --- | --- |
| `zero-one-literals` | `SemanticTypePrecedenceZeroOneBool` | `0`, `1` | acceptance: `["0","1"] => Integer` |
| `leading-zeros` | `SemanticFormatLeadingZeros` | `007` | acceptance: `["007"] => Integer` |
| `thousands-separator` | `SemanticFormatThousands` | `1,000` | acceptance: `["1,000"] => Decimal` |
| `locale-comma-decimal` | `SemanticFormatLocaleComma` | `1,5` | acceptance: `["1,5"] => Date; ["1,."] => Decimal` |
| `signed-zero` | `SemanticFormatSignedZero` | `+0`, `-0` | acceptance: `["+0","-0"] => Integer` |
| `boolean-yes-no` | `SemanticBooleanYesNo` | `yes`, `no` | acceptance: `["yes","no"] => String` |
| `boolean-yn` | `SemanticBooleanYn` | `Y`, `N` | acceptance: `["Y","N"] => String` |
| `whitespace-only` | `SemanticEmptyWhitespaceRetained` | `   ` | invariant: whitespace-only cells treated as empty |
| `scientific-notation` | `SemanticFormatScientific` | `1e3` | none — gap: scientific notation literals not in frozen acceptance criteria |
| `decimal-first-precedence` | `SemanticTypePrecedenceDecimalFirst` | `1`, `2`, `3` | acceptance: `["1","2","3"] => Integer` |
| `sampling-window-widening` | `SemanticSamplingWindow` | `1`, `2`, `hello` | acceptance: `["1","2","hello"] => String`; metamorphic: full-column not prefix |
| `heterogeneous-fallback` | `SemanticHeterogeneousFallback` | `1`, `hello` | acceptance: mixed numeric and text => String |
| `integer-minimum-count` | `OffByOne` | `1`, `2`, `3` | acceptance: `["1","2","3"] => Integer` |
| `single-value-date-boundary` | `BoundaryInclusive` | `2024-01-15` | acceptance: `["2024-01-15"] => Date` |
| `boolean-branch-polarity` | `NegatedCondition` | `true`, `false` | acceptance: `["true","false"] => Boolean` |
| `date-branch-required` | `DroppedBranch` | `2024-01-15` | acceptance: `["2024-01-15"] => Date` |
| `non-constant-inference` | `ConstantReturn` | `1`, `2`, `3` | acceptance: `["1","2","3"] => Integer` |
| `integer-decimal-precedence` | `SwappedOperands` | `1`, `2`, `3` | acceptance: `["1","2","3"] => Integer`; vacuous boolean/date swap on odd seeds |

## Multi-witness pinning (S1.4)

A probe class is **pinned** iff:

1. The honest implementation passes the property gate on **every** configured seed (0 … N−1), and
2. For every seed, the divergent transform either **fails** the property gate **or** is **behaviorally vacuous** (identical outputs on frozen acceptance witnesses).

Escaping seeds and vacuous seeds are reported per probe class in `intent-density-report.json`.

## Vacuous transforms

`SwappedOperands` on odd seeds swaps boolean/date branch order. On the frozen witness set this produces identical outputs to the honest implementation. S1.4 reclassifies such passes as **Caught** (escape harness) or **vacuous** (density analyzer), not escapes.

## Code references

- `src/Nexo.Spike.S1/IntentDensity/ProbeCorpus.cs` — probe class definitions
- `src/Nexo.Spike.S1/IntentDensity/ProbeCatalogMapping.cs` — transform ↔ probe enforcement
- `src/Nexo.Spike.S1/Transforms/TransformAttribution.cs` — deciding relations per transform
