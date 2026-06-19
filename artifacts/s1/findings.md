# S1 Gate Escape Rate

## S1.3 densification — before/after

| Metric | s1.2-v1 (before) | s1.3-v1 (after) |
| --- | ---: | ---: |
| Intent density | 33.3% (4/12 pinned) | **100.0%** (12/12 pinned) |
| Certification verdict | NotCertifiable | **Certifiable** |
| Wrong-impl escape rate | 50.7% (73/144) | **5.6%** (8/144) |
| Wrong-impl false-reject rate | 0.0% | **0.0%** |

### Eight backlog classes: escape → caught (deciding relation)

| Probe class | s1.2 escape rate | s1.3 escape rate | Deciding relation |
| --- | ---: | ---: | --- |
| `zero-one-literals` | 100% (8/8) | **0%** (0/8) | acceptance: `["0","1"] => Integer` |
| `leading-zeros` | 100% (8/8) | **0%** (0/8) | acceptance: `["007"] => Integer` |
| `thousands-separator` | 100% (8/8) | **0%** (0/8) | acceptance: `["1,000"] => Decimal` |
| `locale-comma-decimal` | 100% (8/8) | **0%** (0/8) | acceptance: `["1,5"] => Date; ["1,."] => Decimal` |
| `signed-zero` | 100% (8/8) | **0%** (0/8) | acceptance: `["+0","-0"] => Integer` |
| `boolean-yes-no` | 100% (8/8) | **0%** (0/8) | acceptance: `["yes","no"] => String` |
| `boolean-yn` | 100% (8/8) | **0%** (0/8) | acceptance: `["Y","N"] => String` |
| `whitespace-only` | 100% (8/8) | **0%** (0/8) | invariant: whitespace-only cells treated as empty |

No probe classes or transforms were removed vs s1.2 — only acceptance criteria, invariants, and metamorphic relations were added.

### Residual escape backlog (s1.3-v1)

Eight escapes remain across two transforms (seed sub-variants not fully pinned by current oracle):

- `SwappedOperands` — 50% (4/8); seeds 1,3,5,7 still escape; deciding relation gap at those seeds
- `SemanticSamplingWindow` — 50% (4/8); seeds 1,3,5,7 still escape; needs sampling-window / column-length invariance relation

## Headline

- **Catalog version**: `s1.3-v1`
- **Adversary**: `offline` (offline taxonomy; not adaptive/LLM)
- **Seeds**: 8 (144 distinct wrong-impl trials; 144 total runs)
- **Wrong-impl escape rate** (PropertyGate): **5.6%** (8/144 adversarial candidates escaped)
- **Wrong-impl false-reject rate**: 0.0%
- **Weak-test dimension**: completed-budget-truncated-before-sensitivity (MutationGate escape rate: 0.0%)

## Tool availability

- dotnet: available
- dotnet-stryker: available (dotnet stryker --help)

## Wrong-impl per-transform breakdown

| Transform | Total | Escapes | Caught | Escape rate | Attribution |
| --- | ---: | ---: | ---: | ---: | --- |
| `OffByOne` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `BoundaryInclusive` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["2024-01-15"] => Date |
| `NegatedCondition` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["true","false"] => Boolean |
| `DroppedBranch` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["2024-01-15"] => Date |
| `ConstantReturn` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `SwappedOperands` | 8 | 4 | 4 | 50.0% | caught: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal |
| `SemanticTypePrecedenceDecimalFirst` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `SemanticTypePrecedenceZeroOneBool` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["0","1"] => Integer |
| `SemanticEmptyWhitespaceRetained` | 8 | 0 | 8 | 0.0% | caught: invariant: whitespace-only cells treated as empty |
| `SemanticFormatLeadingZeros` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["007"] => Integer |
| `SemanticFormatThousands` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1,000"] => Decimal |
| `SemanticFormatScientific` | 8 | 0 | 8 | 0.0% | caught: frozen acceptance criteria (spec-derived) |
| `SemanticFormatLocaleComma` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1,5"] => Date; ["1,."] => Decimal |
| `SemanticFormatSignedZero` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["+0","-0"] => Integer |
| `SemanticSamplingWindow` | 8 | 4 | 4 | 50.0% | caught: metamorphic: value-order invariance |
| `SemanticBooleanYesNo` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["yes","no"] => String |
| `SemanticBooleanYn` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["Y","N"] => String |
| `SemanticHeterogeneousFallback` | 8 | 0 | 8 | 0.0% | caught: acceptance: mixed numeric and text => String |

## Weak-test per-transform breakdown

| Transform | Total | Escapes | Caught | Escape rate | Attribution |
| --- | ---: | ---: | ---: | ---: | --- |
| `AssertionRemoved` | 1 | 0 | 1 | 0.0% | caught: mutation score >= threshold (example assertions killed) |
| `TautologyReplacement` | 1 | 0 | 1 | 0.0% | caught: mutation score >= threshold (tautology detection / RED discipline) |
| `OverNarrowDomain` | 1 | 0 | 1 | 0.0% | caught: mutation score >= threshold (breadth of example coverage) |
| `TypeOnlyAssert` | 1 | 0 | 1 | 0.0% | caught: mutation score >= threshold (value assertions killed) |

## Threshold-sensitivity curve (weak-test)

Thresholds swept: 60%, 75%, 90%. Shows the mutation-score threshold at which each weakened test set begins to escape.

| Transform | First escape @ | 60% | 75% | 90% |
| --- | ---: | ---: | ---: | ---: |
| `AssertionRemoved` | never | caught | caught | caught |

## Missing property relations

Each escape names a property-oracle gap to close before self-generated bricks approach the stable surface:

### `SwappedOperands` (seed 1, WrongImpl)

- **Hypothesis**: Integer and decimal precedence swapped.
- **Missing relation**: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal

```diff
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Boolean;
+             return ColumnType.Date;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
- 
+ // seed-perturb-1
+
```

### `SemanticSamplingWindow` (seed 1, WrongImpl)

- **Hypothesis**: Inference uses only first two non-empty cells, missing later type-widening values.
- **Missing relation**: none — gap: sampling-window / column-length invariance not in frozen criteria

```diff
-         if (nonEmpty.Count == 0)
+                 nonEmpty = nonEmpty.Take(3).ToList();
-             return ColumnType.String;
+         if (nonEmpty.Count == 0)
- 
+             return ColumnType.String;
-         if (nonEmpty.All(IsBoolean))
+ 
-             return ColumnType.Boolean;
+         if (nonEmpty.All(IsBoolean))
- 
+             return ColumnType.Boolean;
-         if (nonEmpty.All(IsDate))
+ 
-             return ColumnType.Date;
+         if (nonEmpty.All(IsDate))
- 
+             return ColumnType.Date;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+ 
-             return ColumnType.Integer;
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
- 
+             return ColumnType.Integer;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+ 
-             return ColumnType.Decimal;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
- 
+             return ColumnType.Decimal;
-         return ColumnType.String;
+ 
-     }
+         return ColumnType.String;
- 
+     }
-     private static bool IsBoolean(string value) =>
+ 
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     private static bool IsBoolean(string value) =>
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-     private static bool IsDate(string value) =>
+ 
-         DateOnly.TryParse(value, out _);
+     private static bool IsDate(string value) =>
- }
+         DateOnly.TryParse(value, out _);
- 
+ }
+ // seed-perturb-1
+
```

### `SwappedOperands` (seed 3, WrongImpl)

- **Hypothesis**: Integer and decimal precedence swapped.
- **Missing relation**: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal

```diff
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Boolean;
+             return ColumnType.Date;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
- 
+ // seed-perturb-3
+
```

### `SemanticSamplingWindow` (seed 3, WrongImpl)

- **Hypothesis**: Inference uses only first two non-empty cells, missing later type-widening values.
- **Missing relation**: none — gap: sampling-window / column-length invariance not in frozen criteria

```diff
-         if (nonEmpty.Count == 0)
+                 nonEmpty = nonEmpty.Take(4).ToList();
-             return ColumnType.String;
+         if (nonEmpty.Count == 0)
- 
+             return ColumnType.String;
-         if (nonEmpty.All(IsBoolean))
+ 
-             return ColumnType.Boolean;
+         if (nonEmpty.All(IsBoolean))
- 
+             return ColumnType.Boolean;
-         if (nonEmpty.All(IsDate))
+ 
-             return ColumnType.Date;
+         if (nonEmpty.All(IsDate))
- 
+             return ColumnType.Date;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+ 
-             return ColumnType.Integer;
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
- 
+             return ColumnType.Integer;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+ 
-             return ColumnType.Decimal;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
- 
+             return ColumnType.Decimal;
-         return ColumnType.String;
+ 
-     }
+         return ColumnType.String;
- 
+     }
-     private static bool IsBoolean(string value) =>
+ 
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     private static bool IsBoolean(string value) =>
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-     private static bool IsDate(string value) =>
+ 
-         DateOnly.TryParse(value, out _);
+     private static bool IsDate(string value) =>
- }
+         DateOnly.TryParse(value, out _);
- 
+ }
+ // seed-perturb-3
+
```

### `SwappedOperands` (seed 5, WrongImpl)

- **Hypothesis**: Integer and decimal precedence swapped.
- **Missing relation**: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal

```diff
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Boolean;
+             return ColumnType.Date;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
- 
+ // seed-perturb-5
+
```

### `SemanticSamplingWindow` (seed 5, WrongImpl)

- **Hypothesis**: Inference uses only first two non-empty cells, missing later type-widening values.
- **Missing relation**: none — gap: sampling-window / column-length invariance not in frozen criteria

```diff
-         if (nonEmpty.Count == 0)
+                 nonEmpty = nonEmpty.Take(3).ToList();
-             return ColumnType.String;
+         if (nonEmpty.Count == 0)
- 
+             return ColumnType.String;
-         if (nonEmpty.All(IsBoolean))
+ 
-             return ColumnType.Boolean;
+         if (nonEmpty.All(IsBoolean))
- 
+             return ColumnType.Boolean;
-         if (nonEmpty.All(IsDate))
+ 
-             return ColumnType.Date;
+         if (nonEmpty.All(IsDate))
- 
+             return ColumnType.Date;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+ 
-             return ColumnType.Integer;
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
- 
+             return ColumnType.Integer;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+ 
-             return ColumnType.Decimal;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
- 
+             return ColumnType.Decimal;
-         return ColumnType.String;
+ 
-     }
+         return ColumnType.String;
- 
+     }
-     private static bool IsBoolean(string value) =>
+ 
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     private static bool IsBoolean(string value) =>
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-     private static bool IsDate(string value) =>
+ 
-         DateOnly.TryParse(value, out _);
+     private static bool IsDate(string value) =>
- }
+         DateOnly.TryParse(value, out _);
- 
+ }
+ // seed-perturb-5
+
```

### `SwappedOperands` (seed 7, WrongImpl)

- **Hypothesis**: Integer and decimal precedence swapped.
- **Missing relation**: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal

```diff
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Boolean;
+             return ColumnType.Date;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
- 
+ // seed-perturb-7
+
```

### `SemanticSamplingWindow` (seed 7, WrongImpl)

- **Hypothesis**: Inference uses only first two non-empty cells, missing later type-widening values.
- **Missing relation**: none — gap: sampling-window / column-length invariance not in frozen criteria

```diff
-         if (nonEmpty.Count == 0)
+                 nonEmpty = nonEmpty.Take(5).ToList();
-             return ColumnType.String;
+         if (nonEmpty.Count == 0)
- 
+             return ColumnType.String;
-         if (nonEmpty.All(IsBoolean))
+ 
-             return ColumnType.Boolean;
+         if (nonEmpty.All(IsBoolean))
- 
+             return ColumnType.Boolean;
-         if (nonEmpty.All(IsDate))
+ 
-             return ColumnType.Date;
+         if (nonEmpty.All(IsDate))
- 
+             return ColumnType.Date;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+ 
-             return ColumnType.Integer;
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
- 
+             return ColumnType.Integer;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+ 
-             return ColumnType.Decimal;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
- 
+             return ColumnType.Decimal;
-         return ColumnType.String;
+ 
-     }
+         return ColumnType.String;
- 
+     }
-     private static bool IsBoolean(string value) =>
+ 
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     private static bool IsBoolean(string value) =>
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-     private static bool IsDate(string value) =>
+ 
-         DateOnly.TryParse(value, out _);
+     private static bool IsDate(string value) =>
- }
+         DateOnly.TryParse(value, out _);
- 
+ }
+ // seed-perturb-7
+
```

## Metric scope

Catalog `s1.3-v1` measures escape rate for a **fixed offline transform catalog** on the S0 CSV inferencer fixtures. Escapes are signal: each names a missing property relation. This is not a target of 0% — attributed escapes form the property-authoring backlog. Adaptive or LLM adversaries may find additional escapes beyond this taxonomy.

## Surviving examples

- `wrong-impl` / `SwappedOperands` seed 1: `workspaces/wrong-impl-0001-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticSamplingWindow` seed 1: `workspaces/wrong-impl-0001-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SwappedOperands` seed 3: `workspaces/wrong-impl-0003-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticSamplingWindow` seed 3: `workspaces/wrong-impl-0003-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SwappedOperands` seed 5: `workspaces/wrong-impl-0005-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticSamplingWindow` seed 5: `workspaces/wrong-impl-0005-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SwappedOperands` seed 7: `workspaces/wrong-impl-0007-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticSamplingWindow` seed 7: `workspaces/wrong-impl-0007-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria

## Intent density

- **Probe corpus version**: `s1.3-v1`
- **Intent density**: **100.0%** (12/12 probe classes pinned)
- **Certification threshold**: 95%
- **Honest-impl certification**: **Certifiable** — all probe classes pinned by frozen oracle

| Probe class | Status | Deciding relation |
| --- | --- | --- |
| `zero-one-literals` | Pinned | acceptance: ["0","1"] => Integer |
| `leading-zeros` | Pinned | acceptance: ["007"] => Integer |
| `thousands-separator` | Pinned | acceptance: ["1,000"] => Decimal |
| `locale-comma-decimal` | Pinned | acceptance: ["1,5"] => Date; ["1,."] => Decimal |
| `signed-zero` | Pinned | acceptance: ["+0","-0"] => Integer |
| `boolean-yes-no` | Pinned | acceptance: ["yes","no"] => String |
| `boolean-yn` | Pinned | acceptance: ["Y","N"] => String |
| `whitespace-only` | Pinned | invariant: whitespace-only cells treated as empty |
| `scientific-notation` | Pinned | none — gap: scientific notation literals not in frozen acceptance criteria |
| `decimal-first-precedence` | Pinned | acceptance: ["1","2","3"] => Integer |
| `sampling-window-widening` | Pinned | none — gap: sampling-window / column-length invariance not in frozen criteria |
| `heterogeneous-fallback` | Pinned | acceptance: mixed numeric and text => String |
