# S3 intent matching rule

**Scope:** deterministic capability lookup for `SkillRegistry.Lookup(intentDescriptor)`.
No semantic embeddings or model calls.

## Lookup key

1. If `CapabilityKey` is non-empty → normalize it (trim + lowercase) and use as the lookup key.
2. Otherwise → `normalize(IntentKey) + "|" + sorted-normalized-tags-joined-by-comma`.

Normalization: trim whitespace, lowercase invariant.

## Match rule

`Lookup(query)` returns the first admitted registry entry whose stored `IntentDescriptor`
produces the **same lookup key** as `query`.

## Examples

| Query IntentKey | Query CapabilityKey | Stored CapabilityKey | Match? |
| --- | --- | --- | --- |
| `csv-column-type-inference` | `csv-type-inference` | `csv-type-inference` | Yes |
| `etl-pipeline-csv-inference` | `csv-type-inference` | `csv-type-inference` | Yes (cross-context reuse) |
| `csv-constant-type-guess` | `csv-constant-guess` | `csv-type-inference` | No |

## Non-goals

- Fuzzy / semantic similarity
- Embedding nearest-neighbor
- Automatic gap detection (explicit `EnsureSkill` calls only in S3.0)
