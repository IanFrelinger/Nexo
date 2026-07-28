# Nexo.Analyzers

Build-time rules that keep a brick's **declared** contract and its **actual**
behaviour from drifting apart.

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
