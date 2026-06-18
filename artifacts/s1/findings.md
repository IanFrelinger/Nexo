# S1 Gate Escape Rate

## Headline

- **Catalog version**: `s1.2-v1`
- **Adversary**: `offline` (offline taxonomy; not adaptive/LLM)
- **Seeds**: 8 (144 distinct wrong-impl trials; 144 total runs)
- **Wrong-impl escape rate** (PropertyGate): **50.7%** (73/144 adversarial candidates escaped)
- **Wrong-impl false-reject rate**: 0.0%
- **Weak-test dimension**: completed (MutationGate escape rate: 0.0%)

## Tool availability

- dotnet: available
- dotnet-stryker: available (dotnet stryker --help)

## Wrong-impl per-transform breakdown

| Transform | Total | Escapes | Caught | Escape rate | Attribution |
| --- | ---: | ---: | ---: | ---: | --- |
| `OffByOne` | 8 | 1 | 7 | 12.5% | caught: acceptance: ["1","2","3"] => Integer |
| `BoundaryInclusive` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["2024-01-15"] => Date |
| `NegatedCondition` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["true","false"] => Boolean |
| `DroppedBranch` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["2024-01-15"] => Date |
| `ConstantReturn` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `SwappedOperands` | 8 | 4 | 4 | 50.0% | caught: acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal |
| `SemanticTypePrecedenceDecimalFirst` | 8 | 0 | 8 | 0.0% | caught: acceptance: ["1","2","3"] => Integer |
| `SemanticTypePrecedenceZeroOneBool` | 8 | 8 | 0 | 100.0% | missing: none — gap: 0/1 numeric string literals not in frozen acceptance criteria |
| `SemanticEmptyWhitespaceRetained` | 8 | 8 | 0 | 100.0% | missing: invariant: whitespace-only cells treated as empty |
| `SemanticFormatLeadingZeros` | 8 | 8 | 0 | 100.0% | missing: none — gap: leading-zero format literals not in frozen acceptance criteria |
| `SemanticFormatThousands` | 8 | 8 | 0 | 100.0% | missing: none — gap: thousands-separator format not in frozen acceptance criteria |
| `SemanticFormatScientific` | 8 | 0 | 8 | 0.0% | caught: frozen acceptance criteria (spec-derived) |
| `SemanticFormatLocaleComma` | 8 | 8 | 0 | 100.0% | missing: none — gap: locale comma decimal format not in frozen acceptance criteria |
| `SemanticFormatSignedZero` | 8 | 8 | 0 | 100.0% | missing: none — gap: signed-zero format literals not in frozen acceptance criteria |
| `SemanticSamplingWindow` | 8 | 4 | 4 | 50.0% | caught: metamorphic: value-order invariance |
| `SemanticBooleanYesNo` | 8 | 8 | 0 | 100.0% | missing: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria |
| `SemanticBooleanYn` | 8 | 8 | 0 | 100.0% | missing: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria |
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
| `TautologyReplacement` | never | caught | caught | caught |
| `OverNarrowDomain` | never | caught | caught | caught |
| `TypeOnlyAssert` | never | caught | caught | caught |

## Missing property relations

Each escape names a property-oracle gap to close before self-generated bricks approach the stable surface:

### `SemanticTypePrecedenceZeroOneBool` (seed 0, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "0"
-     private static bool IsDate(string value) =>
+         || value == "1";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-0
+
```

### `SemanticEmptyWhitespaceRetained` (seed 0, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:    */
- 
+ // seed-perturb-0
+
```

### `SemanticFormatLeadingZeros` (seed 0, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:007 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-0
+
```

### `SemanticFormatThousands` (seed 0, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:1,000 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-0
+
```

### `SemanticFormatLocaleComma` (seed 0, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:1,5 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-0
+
```

### `SemanticFormatSignedZero` (seed 0, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:+0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-0
+
```

### `SemanticBooleanYesNo` (seed 0, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("no", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-0
+
```

### `SemanticBooleanYn` (seed 0, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("Y", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("N", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-0
+
```

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

### `SemanticTypePrecedenceZeroOneBool` (seed 1, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "1"
-     private static bool IsDate(string value) =>
+         || value == "0";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-1
+
```

### `SemanticEmptyWhitespaceRetained` (seed 1, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:	 */
- 
+ // seed-perturb-1
+
```

### `SemanticFormatLeadingZeros` (seed 1, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:0012 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-1
+
```

### `SemanticFormatThousands` (seed 1, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:2,500 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-1
+
```

### `SemanticFormatLocaleComma` (seed 1, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:2,75 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-1
+
```

### `SemanticFormatSignedZero` (seed 1, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:-0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
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

### `SemanticBooleanYesNo` (seed 1, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("no", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-1
+
```

### `SemanticBooleanYn` (seed 1, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("N", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("Y", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-1
+
```

### `SemanticTypePrecedenceZeroOneBool` (seed 2, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "0"
-     private static bool IsDate(string value) =>
+         || value == "1";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-2
+
```

### `SemanticEmptyWhitespaceRetained` (seed 2, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:  	   */
- 
+ // seed-perturb-2
+
```

### `SemanticFormatLeadingZeros` (seed 2, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:00042 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-2
+
```

### `SemanticFormatThousands` (seed 2, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:10,000 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-2
+
```

### `SemanticFormatLocaleComma` (seed 2, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:0,5 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-2
+
```

### `SemanticFormatSignedZero` (seed 2, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:+0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-2
+
```

### `SemanticBooleanYesNo` (seed 2, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("YES", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("no", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-2
+
```

### `SemanticBooleanYn` (seed 2, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("y", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("N", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-2
+
```

### `OffByOne` (seed 3, WrongImpl)

- **Hypothesis**: Integer branch requires too many cells (off-by-one on count threshold).
- **Missing relation**: acceptance: ["1","2","3"] => Integer

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.Count >= 3 && nonEmpty.All(v => int.TryParse(v, out _)))
- 
+ // seed-perturb-3
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

### `SemanticTypePrecedenceZeroOneBool` (seed 3, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "1"
-     private static bool IsDate(string value) =>
+         || value == "0";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-3
+
```

### `SemanticEmptyWhitespaceRetained` (seed 3, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness: 	 */
- 
+ // seed-perturb-3
+
```

### `SemanticFormatLeadingZeros` (seed 3, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:0800 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-3
+
```

### `SemanticFormatThousands` (seed 3, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:12,345 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-3
+
```

### `SemanticFormatLocaleComma` (seed 3, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:3,14 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-3
+
```

### `SemanticFormatSignedZero` (seed 3, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:-0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
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

### `SemanticBooleanYesNo` (seed 3, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("NO", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-3
+
```

### `SemanticBooleanYn` (seed 3, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("n", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("Y", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-3
+
```

### `SemanticTypePrecedenceZeroOneBool` (seed 4, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "0"
-     private static bool IsDate(string value) =>
+         || value == "1";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-4
+
```

### `SemanticEmptyWhitespaceRetained` (seed 4, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:		 */
- 
+ // seed-perturb-4
+
```

### `SemanticFormatLeadingZeros` (seed 4, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:0001 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-4
+
```

### `SemanticFormatThousands` (seed 4, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:9,999 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-4
+
```

### `SemanticFormatLocaleComma` (seed 4, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:9,99 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-4
+
```

### `SemanticFormatSignedZero` (seed 4, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:+0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-4
+
```

### `SemanticBooleanYesNo` (seed 4, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("Yes", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("no", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-4
+
```

### `SemanticBooleanYn` (seed 4, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("Y", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("N", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-4
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

### `SemanticTypePrecedenceZeroOneBool` (seed 5, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "1"
-     private static bool IsDate(string value) =>
+         || value == "0";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-5
+
```

### `SemanticEmptyWhitespaceRetained` (seed 5, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:    */
- 
+ // seed-perturb-5
+
```

### `SemanticFormatLeadingZeros` (seed 5, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:0123 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-5
+
```

### `SemanticFormatThousands` (seed 5, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:1,234 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-5
+
```

### `SemanticFormatLocaleComma` (seed 5, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:1,25 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-5
+
```

### `SemanticFormatSignedZero` (seed 5, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:-0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
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

### `SemanticBooleanYesNo` (seed 5, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("No", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-5
+
```

### `SemanticBooleanYn` (seed 5, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("N", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("Y", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-5
+
```

### `SemanticTypePrecedenceZeroOneBool` (seed 6, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "0"
-     private static bool IsDate(string value) =>
+         || value == "1";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-6
+
```

### `SemanticEmptyWhitespaceRetained` (seed 6, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:	  */
- 
+ // seed-perturb-6
+
```

### `SemanticFormatLeadingZeros` (seed 6, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:00099 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-6
+
```

### `SemanticFormatThousands` (seed 6, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:50,000 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-6
+
```

### `SemanticFormatLocaleComma` (seed 6, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:4,50 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-6
+
```

### `SemanticFormatSignedZero` (seed 6, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:+0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-6
+
```

### `SemanticBooleanYesNo` (seed 6, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("yEs", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("no", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-6
+
```

### `SemanticBooleanYn` (seed 6, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("y", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("N", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-6
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

### `SemanticTypePrecedenceZeroOneBool` (seed 7, WrongImpl)

- **Hypothesis**: Numeric 0/1 literals classified as Boolean instead of String.
- **Missing relation**: none — gap: 0/1 numeric string literals not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value == "1"
-     private static bool IsDate(string value) =>
+         || value == "0";
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-7
+
```

### `SemanticEmptyWhitespaceRetained` (seed 7, WrongImpl)

- **Hypothesis**: Whitespace-only cells kept in sample instead of treated as empty.
- **Missing relation**: invariant: whitespace-only cells treated as empty

```diff
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+         var nonEmpty = values.Select(v => v ?? string.Empty).ToList(); /* witness:   */
- 
+ // seed-perturb-7
+
```

### `SemanticFormatLeadingZeros` (seed 7, WrongImpl)

- **Hypothesis**: Leading-zero numerics (e.g. "007") rejected from Integer branch.
- **Missing relation**: none — gap: leading-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _) && !HasLeadingZero(v)))
-     private static bool IsDate(string value) =>
+         private static bool HasLeadingZero(string value) =>
-         DateOnly.TryParse(value, out _);
+         value.Length > 1 && value[0] == '0';
- }
+ 
- 
+     private static bool IsDate(string value) => /* witness:0042 */
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-7
+
```

### `SemanticFormatThousands` (seed 7, WrongImpl)

- **Hypothesis**: Thousands separators stripped before numeric parse ("1,000" -> Integer).
- **Missing relation**: none — gap: thousands-separator format not in frozen acceptance criteria

```diff
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v.Replace(",", ""), out _))) /* witness:3,333 */
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(",", ""), out _)))
- 
+ // seed-perturb-7
+
```

### `SemanticFormatLocaleComma` (seed 7, WrongImpl)

- **Hypothesis**: Locale comma decimals parsed ("1,5" -> Decimal).
- **Missing relation**: none — gap: locale comma decimal format not in frozen acceptance criteria

```diff
- namespace CsvColumnInferrer;
+ using System.Globalization;
- public enum ColumnType
+ namespace CsvColumnInferrer;
- {
+ 
-     String,
+ public enum ColumnType
-     Integer,
+ {
-     Decimal,
+     String,
-     Boolean,
+     Integer,
-     Date
+     Decimal,
- }
+     Boolean,
- 
+     Date
- public static class ColumnTypeInferrer
+ }
- {
+ 
-     public static ColumnType InferType(IReadOnlyList<string> values)
+ public static class ColumnTypeInferrer
-     {
+ {
-         if (values.Count == 0)
+     public static ColumnType InferType(IReadOnlyList<string> values)
-             return ColumnType.String;
+     {
- 
+         if (values.Count == 0)
-         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
+             return ColumnType.String;
-         if (nonEmpty.Count == 0)
+ 
-             return ColumnType.String;
+         var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
- 
+         if (nonEmpty.Count == 0)
-         if (nonEmpty.All(IsBoolean))
+             return ColumnType.String;
-             return ColumnType.Boolean;
+ 
- 
+         if (nonEmpty.All(IsBoolean))
-         if (nonEmpty.All(IsDate))
+             return ColumnType.Boolean;
-             return ColumnType.Date;
+ 
- 
+         if (nonEmpty.All(IsDate))
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+             return ColumnType.Date;
-             return ColumnType.Integer;
+ 
- 
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+             return ColumnType.Integer;
-             return ColumnType.Decimal;
+ 
- 
+         if (nonEmpty.All(v => decimal.TryParse(v.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) /* witness:6,75 */))
-         return ColumnType.String;
+             return ColumnType.Decimal;
-     }
+ 
- 
+         return ColumnType.String;
-     private static bool IsBoolean(string value) =>
+     }
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+ 
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+     private static bool IsBoolean(string value) =>
- 
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-7
+
```

### `SemanticFormatSignedZero` (seed 7, WrongImpl)

- **Hypothesis**: Signed-zero literals ("-0", "+0") forced to String.
- **Missing relation**: none — gap: signed-zero format literals not in frozen acceptance criteria

```diff
-         if (nonEmpty.Count == 0)
+                 if (nonEmpty.Any(v => v is "+0" or "-0"))
-             return ColumnType.String;
+             return ColumnType.String; /* witness:-0 */
-         if (nonEmpty.All(IsBoolean))
+         if (nonEmpty.Count == 0)
-             return ColumnType.Boolean;
+             return ColumnType.String;
-         if (nonEmpty.All(IsDate))
+         if (nonEmpty.All(IsBoolean))
-             return ColumnType.Date;
+             return ColumnType.Boolean;
-         if (nonEmpty.All(v => int.TryParse(v, out _)))
+         if (nonEmpty.All(IsDate))
-             return ColumnType.Integer;
+             return ColumnType.Date;
-         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
+         if (nonEmpty.All(v => int.TryParse(v, out _)))
-             return ColumnType.Decimal;
+             return ColumnType.Integer;
-         return ColumnType.String;
+         if (nonEmpty.All(v => decimal.TryParse(v, out _)))
-     }
+             return ColumnType.Decimal;
-     private static bool IsBoolean(string value) =>
+         return ColumnType.String;
-         value.Equals("true", StringComparison.OrdinalIgnoreCase)
+     }
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+ 
- 
+     private static bool IsBoolean(string value) =>
-     private static bool IsDate(string value) =>
+         value.Equals("true", StringComparison.OrdinalIgnoreCase)
-         DateOnly.TryParse(value, out _);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
- }
+ 
- 
+     private static bool IsDate(string value) =>
+         DateOnly.TryParse(value, out _);
+ }
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

### `SemanticBooleanYesNo` (seed 7, WrongImpl)

- **Hypothesis**: Colloquial yes/no tokens classified as Boolean.
- **Missing relation**: none — gap: yes/no boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("nO", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-7
+
```

### `SemanticBooleanYn` (seed 7, WrongImpl)

- **Hypothesis**: Single-letter Y/N tokens classified as Boolean.
- **Missing relation**: none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

```diff
-         || value.Equals("false", StringComparison.OrdinalIgnoreCase);
+         || value.Equals("false", StringComparison.OrdinalIgnoreCase)
- 
+         || value.Equals("n", StringComparison.OrdinalIgnoreCase)
-     private static bool IsDate(string value) =>
+         || value.Equals("Y", StringComparison.OrdinalIgnoreCase);
-         DateOnly.TryParse(value, out _);
+ 
- }
+     private static bool IsDate(string value) =>
- 
+         DateOnly.TryParse(value, out _);
+ }
+ // seed-perturb-7
+
```

## Metric scope

Catalog `s1.2-v1` measures escape rate for a **fixed offline transform catalog** on the S0 CSV inferencer fixtures. Escapes are signal: each names a missing property relation. This is not a target of 0% — attributed escapes form the property-authoring backlog. Adaptive or LLM adversaries may find additional escapes beyond this taxonomy.

## Surviving examples

- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 0: `workspaces/wrong-impl-0000-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 0: `workspaces/wrong-impl-0000-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 0: `workspaces/wrong-impl-0000-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 0: `workspaces/wrong-impl-0000-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 0: `workspaces/wrong-impl-0000-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 0: `workspaces/wrong-impl-0000-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 0: `workspaces/wrong-impl-0000-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 0: `workspaces/wrong-impl-0000-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SwappedOperands` seed 1: `workspaces/wrong-impl-0001-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 1: `workspaces/wrong-impl-0001-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 1: `workspaces/wrong-impl-0001-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 1: `workspaces/wrong-impl-0001-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 1: `workspaces/wrong-impl-0001-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 1: `workspaces/wrong-impl-0001-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 1: `workspaces/wrong-impl-0001-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticSamplingWindow` seed 1: `workspaces/wrong-impl-0001-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 1: `workspaces/wrong-impl-0001-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 1: `workspaces/wrong-impl-0001-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 2: `workspaces/wrong-impl-0002-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 2: `workspaces/wrong-impl-0002-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 2: `workspaces/wrong-impl-0002-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 2: `workspaces/wrong-impl-0002-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 2: `workspaces/wrong-impl-0002-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 2: `workspaces/wrong-impl-0002-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 2: `workspaces/wrong-impl-0002-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 2: `workspaces/wrong-impl-0002-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `OffByOne` seed 3: `workspaces/wrong-impl-0003-OffByOne` — acceptance: ["1","2","3"] => Integer
- `wrong-impl` / `SwappedOperands` seed 3: `workspaces/wrong-impl-0003-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 3: `workspaces/wrong-impl-0003-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 3: `workspaces/wrong-impl-0003-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 3: `workspaces/wrong-impl-0003-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 3: `workspaces/wrong-impl-0003-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 3: `workspaces/wrong-impl-0003-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 3: `workspaces/wrong-impl-0003-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticSamplingWindow` seed 3: `workspaces/wrong-impl-0003-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 3: `workspaces/wrong-impl-0003-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 3: `workspaces/wrong-impl-0003-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 4: `workspaces/wrong-impl-0004-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 4: `workspaces/wrong-impl-0004-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 4: `workspaces/wrong-impl-0004-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 4: `workspaces/wrong-impl-0004-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 4: `workspaces/wrong-impl-0004-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 4: `workspaces/wrong-impl-0004-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 4: `workspaces/wrong-impl-0004-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 4: `workspaces/wrong-impl-0004-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SwappedOperands` seed 5: `workspaces/wrong-impl-0005-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 5: `workspaces/wrong-impl-0005-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 5: `workspaces/wrong-impl-0005-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 5: `workspaces/wrong-impl-0005-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 5: `workspaces/wrong-impl-0005-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 5: `workspaces/wrong-impl-0005-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 5: `workspaces/wrong-impl-0005-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticSamplingWindow` seed 5: `workspaces/wrong-impl-0005-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 5: `workspaces/wrong-impl-0005-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 5: `workspaces/wrong-impl-0005-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 6: `workspaces/wrong-impl-0006-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 6: `workspaces/wrong-impl-0006-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 6: `workspaces/wrong-impl-0006-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 6: `workspaces/wrong-impl-0006-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 6: `workspaces/wrong-impl-0006-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 6: `workspaces/wrong-impl-0006-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 6: `workspaces/wrong-impl-0006-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 6: `workspaces/wrong-impl-0006-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SwappedOperands` seed 7: `workspaces/wrong-impl-0007-SwappedOperands` — acceptance: ["1","2","3"] => Integer; ["1.5","2.0"] => Decimal
- `wrong-impl` / `SemanticTypePrecedenceZeroOneBool` seed 7: `workspaces/wrong-impl-0007-SemanticTypePrecedenceZeroOneBool` — none — gap: 0/1 numeric string literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticEmptyWhitespaceRetained` seed 7: `workspaces/wrong-impl-0007-SemanticEmptyWhitespaceRetained` — invariant: whitespace-only cells treated as empty
- `wrong-impl` / `SemanticFormatLeadingZeros` seed 7: `workspaces/wrong-impl-0007-SemanticFormatLeadingZeros` — none — gap: leading-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatThousands` seed 7: `workspaces/wrong-impl-0007-SemanticFormatThousands` — none — gap: thousands-separator format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatLocaleComma` seed 7: `workspaces/wrong-impl-0007-SemanticFormatLocaleComma` — none — gap: locale comma decimal format not in frozen acceptance criteria
- `wrong-impl` / `SemanticFormatSignedZero` seed 7: `workspaces/wrong-impl-0007-SemanticFormatSignedZero` — none — gap: signed-zero format literals not in frozen acceptance criteria
- `wrong-impl` / `SemanticSamplingWindow` seed 7: `workspaces/wrong-impl-0007-SemanticSamplingWindow` — none — gap: sampling-window / column-length invariance not in frozen criteria
- `wrong-impl` / `SemanticBooleanYesNo` seed 7: `workspaces/wrong-impl-0007-SemanticBooleanYesNo` — none — gap: yes/no boolean ambiguity not in frozen acceptance criteria
- `wrong-impl` / `SemanticBooleanYn` seed 7: `workspaces/wrong-impl-0007-SemanticBooleanYn` — none — gap: Y/N boolean ambiguity not in frozen acceptance criteria

## Intent density

- **Probe corpus version**: `s1.2-v1`
- **Intent density**: **33.3%** (4/12 probe classes pinned)
- **Certification threshold**: 95%
- **Honest-impl certification**: **NotCertifiable** — acceptance criteria too sparse to certify

| Probe class | Status | Deciding relation |
| --- | --- | --- |
| `zero-one-literals` | Unpinned | silent |
| `leading-zeros` | Unpinned | silent |
| `thousands-separator` | Unpinned | silent |
| `locale-comma-decimal` | Unpinned | silent |
| `signed-zero` | Unpinned | silent |
| `boolean-yes-no` | Unpinned | silent |
| `boolean-yn` | Unpinned | silent |
| `whitespace-only` | Unpinned | silent |
| `scientific-notation` | Pinned | none — gap: scientific notation literals not in frozen acceptance criteria |
| `decimal-first-precedence` | Pinned | acceptance: ["1","2","3"] => Integer |
| `sampling-window-widening` | Pinned | none — gap: sampling-window / column-length invariance not in frozen criteria |
| `heterogeneous-fallback` | Pinned | acceptance: mixed numeric and text => String |

### Densification backlog (unpinned probe classes)

- `zero-one-literals`
- `leading-zeros`
- `thousands-separator`
- `locale-comma-decimal`
- `signed-zero`
- `boolean-yes-no`
- `boolean-yn`
- `whitespace-only`
