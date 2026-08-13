# Nexo.Analyzers

Build-time rules that keep a brick's **declared** contract and its **actual**
behaviour from drifting apart — the trust loop's fence catalog with a
compiler's eyes (extension spec Part A). The catalog grows monotonically:
each recurring agent failure class diagnosed in CI triage or ledger review is
promoted to a rule here, with the decision recorded in this table.

## Catalog

| Id | Rule | Failure class it was promoted from |
|----|------|-------------------------------------|
| `NEXO0001` | Brick reads an undeclared input key | `preferScaffold`/`qtShims` interface drift |
| `NEXO0002` | Brick writes an undeclared output key | same drift class, output side |
| `NEXO0003` | Brick constructor/initializer performs file I/O | side effects during registration and hot-swap materialization are unattributable and ungated |
| `NEXO0004` | Brick constructor/initializer performs network access | same class; egress additionally bypasses session network policy |
| `NEXO0005` | Service factory resolves its own service type | the `ValidateOnBuild`-passing resolution-time hang observed in `SelfExtendRunnerAdapter` DI wiring (~298 silent factory re-entries) |
| `NEXO0006` | Brick reads `DateTime.Now`/`DateTimeOffset.Now` | determinism-gate rejections traceable to wall-clock reads; `UtcNow` is the named fix |
| `NEXO0007` | Brick uses unseeded randomness (`new Random()`, `Random.Shared`) | determinism-gate rejections traceable to environment seeding |
| `NEXO0008` | Brick declares mutable static state | cross-execution state makes the determinism gate's second run see a different world |
| `NEXO0009` | Empty catch block in brick code | swallowed failures become silent wrong output instead of explained failure |

Every rule ships with a three-case test triad in `Nexo.Analyzers.Tests`: at
least one true positive, one true negative, and one deliberately-unresolvable
case that produces no diagnostic (the honesty discipline below, verified).

## Honesty discipline (applies to every rule)

Rules reason only over statically-resolvable facts. A key, symbol, or target
that cannot be resolved at compile time is left alone, never guessed —
false-positive suppression by guessing is non-conformant for the `NEXO*`
range. Concretely: helper-method indirection is not chased, method-group
factories are not judged, readonly containers are not assumed mutated, and a
compilation that cannot resolve the anchor types produces silence here (the
certification analyzer gate separately fails closed on missing anchors).

## NEXO0001 / NEXO0002 — BrickInterface drift

| Id | Fires when |
|----|------------|
| `NEXO0001` | A brick calls `BrickInput.Get("key")` for a key its `BrickInterface.Inputs` never declares |
| `NEXO0002` | A brick calls `BrickOutput.Set("key", …)` for a key its `BrickInterface.Outputs` never declares |

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
  brick's source. `BrickContractTests` in `Nexo.Tests.Application` is the runtime
  backstop for exactly that gap: it executes bricks and asserts every emitted key
  is declared.
- **The `BrickOutput` indexer (`output["key"] = v`) is not covered** — only the
  `Get`/`Set` methods are.

### Wiring

Referenced as an analyzer (not a library) by the brick-producing projects:

```xml
<ProjectReference Include="..\Nexo.Analyzers\Nexo.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Both rules default to `Warning`. Those projects set
`TreatWarningsAsErrors`, so in practice drift breaks the build.
