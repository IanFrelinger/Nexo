# Ashlar.Analyzers

Build-time rules that keep a brick's **declared** contract and its **actual**
behaviour from drifting apart — the trust loop's fence catalog with a
compiler's eyes (extension spec Part A). The catalog grows monotonically:
each recurring agent failure class diagnosed in CI triage or ledger review is
promoted to a rule here, with the decision recorded in this table.

## Catalog

| Id | Rule | Failure class it was promoted from |
|----|------|-------------------------------------|
| `ASHLAR0001` | Brick reads an undeclared input key | `preferScaffold`/`qtShims` interface drift |
| `ASHLAR0002` | Brick writes an undeclared output key | same drift class, output side |
| `ASHLAR0003` | Brick constructor/initializer performs file I/O | side effects during registration and hot-swap materialization are unattributable and ungated |
| `ASHLAR0004` | Brick constructor/initializer performs network access | same class; egress additionally bypasses session network policy |
| `ASHLAR0005` | Service factory resolves its own service type | the `ValidateOnBuild`-passing resolution-time hang observed in `SelfExtendRunnerAdapter` DI wiring (~298 silent factory re-entries) |
| `ASHLAR0006` | Brick reads `DateTime.Now`/`DateTimeOffset.Now` | determinism-gate rejections traceable to wall-clock reads; `UtcNow` is the named fix |
| `ASHLAR0007` | Brick uses unseeded randomness (`new Random()`, `Random.Shared`) | determinism-gate rejections traceable to environment seeding |
| `ASHLAR0008` | Brick declares mutable static state | cross-execution state makes the determinism gate's second run see a different world |
| `ASHLAR0009` | Empty catch block in brick code | swallowed failures become silent wrong output instead of explained failure |
| `ASHLAR0010` | Using directive outside the constraint-manifest allowlist | manifest-derived (A2); prompt-vs-gate drift killed by construction |
| `ASHLAR0011` | Resolved reference matches a forbidden API token | manifest-derived (A2); textual matching was dodgeable by aliasing/qualification |
| `ASHLAR0012` | Resolved reference inside a forbidden namespace | manifest-derived (A2); fully-qualified calls dodge any using-directive check |
| `ASHLAR0013` | Resolved reference outside the objective's declared touch-set | autonomy spec R3.2: the tier was classified from the declaration; reaching beyond it invalidates the classification |
| `ASHLAR0014` | Undeclared resolved reference into the trust kernel | autonomy spec I-1: kernel-touch smuggling forfeits certification outright; reflection/transitive reach is owned by the swap-host and runtime legs of the triple check |

Every rule ships with a three-case test triad in `Ashlar.Analyzers.Tests`: at
least one true positive, one true negative, and one deliberately-unresolvable
case that produces no diagnostic (the honesty discipline below, verified).

## Manifest-derived rules (ASHLAR0010–0012)

Unlike the static catalog, these rules have no fixed configuration:
`BrickConstraintManifestAnalyzer` is **constructed with a
`BrickConstraintManifest` instance** — the same object the proposer's
instructions were rendered from — so the rules enforced and the instruction
text restated in every violation are, by construction, the single source used
twice (extension spec A2.3). The type deliberately carries no
`[DiagnosticAnalyzer]` attribute: compiler discovery never instantiates it;
only the certification `AnalyzerFenceGate` attaches it, per candidate, when
the `CertificationRequest` carries a manifest.

### A2.1 — where each manifest rule is enforced

| Manifest rule | Generation pre-gate (textual validator) | Certification analyzer gate (resolved symbols) | Governing point |
|---|---|---|---|
| `RequiredBaseType` | ✅ | — | validator (structural) |
| `RequiredNamespace` | ✅ | — | validator (structural) |
| `RequiredClassNameSuffix` | ✅ | — | validator (structural) |
| `RequiredDeclarations` | ✅ | — | validator (structural) |
| `AllowedUsings` | ✅ exact text | ✅ `ASHLAR0010`, same normalization | analyzer |
| `ForbiddenApiTokens` | ✅ substring | ✅ `ASHLAR0011`, symbol-resolved | analyzer |
| `ForbiddenNamespaces` | — (not textually checkable without guessing) | ✅ `ASHLAR0012`, symbol-resolved incl. sub-namespaces | analyzer |

Where both run, the overlap is deliberate — the validator gives the proposer
fast repair feedback during generation; the analyzer verdict governs at
certification, because resolved symbols see through aliases
(`using D = System.DateTime; D.Now`) and full qualification, which text
cannot. The analyzer's own honesty limit: a type merely *declared* but never
created or dereferenced produces no operation and is not flagged — the
textual validator still catches that token during generation.

## Honesty discipline (applies to every rule)

Rules reason only over statically-resolvable facts. A key, symbol, or target
that cannot be resolved at compile time is left alone, never guessed —
false-positive suppression by guessing is non-conformant for the `ASHLAR*`
range. Concretely: helper-method indirection is not chased, method-group
factories are not judged, readonly containers are not assumed mutated, and a
compilation that cannot resolve the anchor types produces silence here (the
certification analyzer gate separately fails closed on missing anchors).

## ASHLAR0001 / ASHLAR0002 — BrickInterface drift

| Id | Fires when |
|----|------------|
| `ASHLAR0001` | A brick calls `BrickInput.Get("key")` for a key its `BrickInterface.Inputs` never declares |
| `ASHLAR0002` | A brick calls `BrickOutput.Set("key", …)` for a key its `BrickInterface.Outputs` never declares |

### The bug class this exists for

A brick's contract and its implementation are two independent strings that
nothing forces to agree:

```csharp
Interface = new BrickInterface
{
    Inputs = [new BrickInputDefinition("preferScaffold", "bool", "…")]
};
…
var prefer = input.Get("preferScaffolding", false);   // note the 'ing'
```

A caller reading the published interface sends `preferScaffold`. The brick reads
`preferScaffolding`, misses, and silently takes the default. No exception, no
failing test — the bag is stringly-typed and tolerant, so the feature simply
never turns on. The same shape produced the `qtShims` drift.

### What it deliberately does NOT do

The rule only reports what it can prove, because a rule that cries wolf gets
suppressed project-wide and then protects nothing.

- **Non-constant keys are ignored.** A key built at runtime (variable,
  parameter, interpolation) cannot be checked without guessing.
  `const` keys *are* resolved — they are statically known, which is what makes
  the `GenerativeBrick.ProvenanceOutputKey` pattern work on both sides.
- **A type that declares no `BrickInterface` is skipped entirely.** Its contract
  lives somewhere the analyzer cannot see; reporting would be noise.
- **Declared names are read by parameter binding, not position**, so
  `new BrickOutputDefinition(type: "string", name: "result")` resolves correctly.
- **Cross-class emission is out of reach.** A key written by a base-class helper
  (`GenerativeBrick.EmitProvenance`) never appears as a literal in the derived
  brick's source. `BrickContractTests` in `Ashlar.Tests.Application` is the runtime
  backstop for exactly that gap: it executes bricks and asserts every emitted key
  is declared.
- **The `BrickOutput` indexer (`output["key"] = v`) is not covered** — only the
  `Get`/`Set` methods are.

### Wiring

Referenced as an analyzer (not a library) by the brick-producing projects:

```xml
<ProjectReference Include="..\Ashlar.Analyzers\Ashlar.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Both rules default to `Warning`. Those projects set
`TreatWarningsAsErrors`, so in practice drift breaks the build.
