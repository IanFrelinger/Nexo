# The certification gate, from a package

This is the builder-facing page for Ashlar's headline capability: the gate that decides whether a
brick becomes a **certified artifact**. It is written for someone consuming `Ashlar.*` from
nuget.org with no repository checkout. For writing the brick itself, see
[`AuthoringBricks.md`](AuthoringBricks.md); for the invariants and the spec, see
[`trust-loop/ashlar-trust-loop-spec.md`](trust-loop/ashlar-trust-loop-spec.md); for what has
actually been proven and where it still falls short, see
[`certification-evidence.md`](certification-evidence.md).

> **Not the same as `ashlar verify`.** The CLI's `verify` command judges a *project* — its
> `ashlar.yaml` and `ashlar.policy.yaml` — and prints VERIFIED or CERTIFIED
> ([`OperatorLifecycle.md`](OperatorLifecycle.md)). This page is about the *artifact* gate that
> judges a brick's source. Different subject, different record.

## What a witness is

`README.md` and the trust-loop spec both lean on the word "witness" without defining it. Here it is:

> A **witness** is a list of input → expected-output cases for one brick, authored **before** the
> candidate implementation exists and never shown to whoever (or whatever) writes it. It is not a
> unit test: it is an executable statement of what "correct" means, held by the party doing the
> judging rather than the party doing the proposing.

Two consequences fall out of that separation, and they are the whole point:

- The proposer cannot write to the target. It never sees the cases, so it cannot special-case them.
- The witness itself gets audited. Mutation testing (leg 3) deliberately breaks the candidate and
  requires the witness to notice **every** break. A witness that lets a mutant live is a witness
  without teeth, and the gate rejects the whole submission rather than issuing a certificate the
  witness cannot back up.

In code a witness is `WitnessSpec(string BrickId, IReadOnlyList<WitnessCase> Cases)` with
`WitnessCase(IReadOnlyDictionary<string, object> Input, IReadOnlyDictionary<string, object> ExpectedOutput)`
— both in `Ashlar.Core.Application.Certification.Models`. On disk,
`BrickCertificationProjectLoader` reads this JSON shape:

```json
{
  "brickId": "acme.invoice.late-fee",
  "cases": [
    { "input": { "principalCents": 100000, "daysOverdue": 5, "graceDays": 10, "waived": false },
      "expectedOutput": { "feeCents": 0, "tier": "none" } },
    { "input": { "principalCents": 100000, "daysOverdue": 31, "graceDays": 0, "waived": false },
      "expectedOutput": { "feeCents": 3500, "tier": "delinquent" } }
  ]
}
```

Names in `input` / `expectedOutput` are the brick's declared `BrickInputDefinition` /
`BrickOutputDefinition` names. Numbers arrive as `int`, `long` or `double`; strings and booleans
map straight across (`BrickCertificationProjectLoader.FromJsonElement`).

Write the boundary cases. Every literal and every comparison in the brick becomes a mutant, so a
witness with three happy-path cases will be rejected at the mutation leg, not the correctness leg —
which is the gate telling you the certificate would have been worthless.

## What a certificate means

An artifact carries a certificate **if and only if** it passed every leg below, and the certificate
is signed over the artifact's content hash (SHA-256 of the brick source). Change one character of
the source and the record no longer verifies:
`CertificationTrustVerifier.Verify(record, source)` returns `content-hash-mismatch`.

## The five legs, in order

`CertificationGate.CertifyAsync` runs these in a fixed order and stops at the first failure. The
record's `gatesPassed` array is the ordered prefix that *did* pass, so a rejection tells you exactly
how far the candidate got.

| # | Leg (`failureCheck`) | What it checks | Why it is first/here |
|---|---|---|---|
| 0 | `recursion` | Generation lineage is coherent and under the depth ceiling. Absent lineage = human-authored, depth 0. | Runs before everything: an incoherent depth claim must not even be analyzed. Only relevant to the autonomy loop; hand-authored bricks pass trivially. |
| 1 | `analyzer` | The candidate compiles, and the Ashlar analyzer catalog (plus any constraint-manifest rules) reports nothing at or above the severity floor. | A defect a deterministic analyzer can *name* should never cost a mutation run. |
| 2 | `correctness` | Every witness case's actual output equals its expected output. | Cheap, and mutation testing is meaningless if the unmutated candidate is already wrong. |
| 3 | `mutation` | The engine generates mutants of the source and requires `escape_rate == 0` — every mutant must be killed by some witness case. Zero mutants generated is also a failure. | This is what gives the certificate teeth: it audits the witness, not the candidate. |
| 4 | `determinism` | The same case run twice under `AuditMode` canonicalizes identically. A missing repeat is nondeterminism-by-absence, never vacuous agreement. | A nondeterministic brick cannot be certified against a fixed expected output. |
| 5 | `dependency` | The brick project references no other project and only the two allowed packages (next section), and the source contains none of the forbidden kernel tokens. | Last because it is about *shape*, not behaviour — but it is not optional; see below. |

Only after all five does the gate sign the record and return `Admitted = true`.

**The gate reasons about its own epistemics.** Two of the refusals above exist because silence
would be misread as a pass, not because a problem was found:

- Given source with an undefined symbol, the analyzer leg says
  `candidate does not compile, so analyzer silence would be meaningless: CS0103 ...` — it refuses
  because it cannot know.
- Given a compilation that cannot resolve `Ashlar.Core.Domain.Bricks.Brick`, it says
  `analyzer anchor type '...' is not resolvable from the candidate compilation references; the brick
  rules would silently no-op`. Every brick-scoped rule anchors on that type; without it they would
  all pass vacuously.

Both are the reason you must supply `CompilationReferences` — see "Things that will bite you".

## The two-package rule (and the one-project rule)

This is a hard design constraint, enforced by
`src/Ashlar.Infrastructure/Certification/BrickDependencyChecker.cs`, and it is the leg most
first-time authors hit:

- **A certifiable brick project may reference exactly two NuGet packages:**
  `Ashlar.Brick.Contracts` and `Ashlar.Authoring`. Any other `PackageReference` — including a
  logging package, a JSON package, or another `Ashlar.*` package — is a violation:

  ```
  PackageReference 'Serilog' is not allowed (only Ashlar.Brick.Contracts + Ashlar.Authoring,
  plus Ashlar.Analyzers referenced build-time-only with ExcludeAssets="runtime;compile")
  ```

- **One exception: `Ashlar.Analyzers`, build-time-only.** Running the fence locally is worth doing:
  it is the same rule catalogue the gate's analyzer leg attaches to your candidate. It does not
  count against the two-package rule — but only in a reference shape that keeps it out of the
  built brick. This is the shape:

  ```xml
  <PackageReference Include="Ashlar.Analyzers" Version="0.1.1" ExcludeAssets="runtime;compile" />
  ```

  **`PrivateAssets="all"` is not enough, and is refused.** This is the one place the rule is
  counter-intuitive, so it is worth being exact about. `PrivateAssets` controls what flows
  *onward*, to projects that reference **yours**; it does nothing to your own project, which still
  receives the package's compile and runtime assets. `Ashlar.Analyzers` deliberately ships a
  `lib/` leg beside `analyzers/dotnet/cs/` (the Ashlar runtime consumes the same assembly as an
  ordinary library), so under `PrivateAssets="all"` the analyzer DLL is copied into your brick's
  output and your brick can bind to analyzer types and still certify — and then fail with
  `FileNotFoundException` in a consumer's process, because the packed brick declares no such
  dependency. `ExcludeAssets="runtime;compile"` leaves the `analyzers` asset group untouched, so
  the rules still **run** in your build, while the assembly stays out of your output and off your
  compile-time reference set. `ExcludeAssets="all"` is also accepted, but it switches the
  analyzers off, which defeats the point of adding the reference.

  ```
  PackageReference 'Ashlar.Analyzers' is allowed only build-time-only, and this one is not.
  Add ExcludeAssets="runtime;compile" to it: ...
  ```

- **A certifiable brick must live in its own project.** *Any* `ProjectReference` is refused
  outright:

  ```
  ProjectReference forbidden: ../../src/Ashlar.Core.Domain/Ashlar.Core.Domain.csproj
  ```

- **The source may not name the kernel.** The strings `Ashlar.Infrastructure`,
  `Ashlar.Core.Application`, `ProjectReference`, `src/Ashlar` and `/workspace` are refused wherever
  they appear in the candidate source:

  ```
  Source contains forbidden token 'Ashlar.Core.Application'
  ```

- **One brick, one `.cs`, one `.csproj`.** The certificate binds a SHA-256 of a single source
  file, and the analyzer and mutation legs compile that file as one compilation unit — so the brick
  must BE one authored source file. `BrickCertificationProjectLoader` refuses a project whose
  authored C# spans several files rather than picking one and signing as though it were the whole
  brick. It also takes the first `.csproj` in the directory and the first non-abstract
  `DomainBrick` type in the built assembly, so give each brick its own directory.

The reason is that the certificate binds a content hash of one source file. If the brick could pull
in arbitrary code, the hash would cover a fraction of what actually runs, and "certified" would be a
claim about a file rather than about behaviour.

**The canonical sample does not satisfy this.** `samples/hello-brick/` — which
[`AuthoringBricks.md`](AuthoringBricks.md) calls the primary path — uses a `ProjectReference` into
`src/Ashlar.Core.Domain`, so the gate rejects it at the dependency leg. That is fine for what it is
(the smallest way to learn the `Brick` API inside a checkout) and wrong as a starting point for
anything you intend to certify. The shape to copy is
`samples/certified-brick-reuse/Ashlar.Certified.DamageResolver/`, which is its own packable project
referencing only `Ashlar.Brick.Contracts`.

A certifiable brick project, complete:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>true</IsPackable>
    <PackageId>Acme.Bricks.LateFee</PackageId>
    <!-- Required if any ancestor directory has a Directory.Packages.props. -->
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <!-- The loader collects compilation references from the build output folder;
         package assemblies are not copied there by default. -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.1" />
  </ItemGroup>
</Project>
```

## Driving the gate as a package-only consumer

Two shapes, both written against published packages only. Neither needs a checkout.

### Shape A — a standalone certifier over a brick directory

Best when the brick is a separate project on disk and you want a signed
`certification-record.json` on the other side. This is the shape a CI job wants.

Project references (a *host* project, not the brick project — the two-package rule applies to the
brick, not to the tool that certifies it):

```xml
<PackageReference Include="Ashlar.Infrastructure" Version="0.1.1" />
<PackageReference Include="Ashlar.Certification.Contracts" Version="0.1.1" />
```

```csharp
using System.Text.Json;
using Ashlar.Certification.Contracts;
using Ashlar.Infrastructure.Certification;

// args: <brick-project-dir> <witness.json> <output-record.json>
var request = await BrickCertificationProjectLoader.LoadAsync(args[0], args[1]);

var gate = new CertificationGate(new CertificationRecordSigner());
var decision = await gate.CertifyAsync(request);

if (!decision.Admitted)
{
    Console.Error.WriteLine($"REJECTED at {decision.FailureCheck}: {decision.Record.Reason}");
    foreach (var finding in decision.ProbeFindings)
        Console.Error.WriteLine($"  probe: {finding}");
    return 2;
}

var data = CertificationRecordMapper.ToData(decision.Record);
await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(data, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
}));

// The certificate is content-bound: this is the check a downstream consumer repeats.
var verify = CertificationTrustVerifier.Verify(data, request.SourceCode);
Console.WriteLine($"trusted={verify.Trusted} contentHash={data.ContentHash}");
return verify.Trusted ? 0 : 3;
```

`BrickCertificationProjectLoader.LoadAsync` shells out to `dotnet build -c Release` on the brick
project, so the machine running this needs a .NET SDK, not just a runtime. Point it at a specific
NuGet config with the `ASHLAR_CERT_NUGET_CONFIG` environment variable if the default source list is
not what you want.

### Shape B — the gate from DI, inside your own tests

Best when you want a brick's certifiability asserted by `dotnet test` in the same solution that
builds it. Resolve `ICertificationGate` from a normal Ashlar host
(`AddAshlar()` registers it via `AddCertificationInfrastructure`), and build the
`CertificationRequest` yourself:

```csharp
using Ashlar.Authoring;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Hosting;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAshlarBrick<LateFeeBrick>();                      // before AddAshlar()
services.AddAshlar(o => o.RegisterBackgroundAgentHostedService = false);
using var provider = services.BuildServiceProvider();

var gate = provider.GetRequiredService<ICertificationGate>();

var decision = await gate.CertifyAsync(new CertificationRequest
{
    Brick = new LateFeeBrick(),
    BrickTypeName = typeof(LateFeeBrick).FullName,
    SourceCode = await File.ReadAllTextAsync(pathToLateFeeBrickCs),
    ProjectPath = pathToLateFeeBrickCsproj,
    CompilationReferences = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(a => a.Location)
        .Distinct()
        .ToList(),
    Witness = witness,
});

Assert.True(decision.Admitted, $"REJECTED at {decision.FailureCheck}: {decision.Record.Reason}");
```

The test project itself is not the brick project, so it may reference whatever it likes
(`Ashlar.Hosting.Bundle`, xUnit, and so on). `ProjectPath` must point at the **brick's** `.csproj`,
because that file is what the dependency leg reads.

## What a rejection looks like

`CertificationDecision` carries three things worth reading:

- `FailureCheck` — one word, the leg that stopped it (`recursion`, `analyzer`, `correctness`,
  `mutation`, `determinism`, `dependency`).
- `Record.Reason` — the human-facing evidence. This is where the detail lives.
- `Record.GatesPassed` — the ordered prefix of legs that *did* pass, so you can see how far it got.

Real reason strings, by leg:

```
analyzer      candidate does not compile, so analyzer silence would be meaningless: CS0103 ...
correctness   Correctness check failed: <per-case actual vs expected>
mutation      Mutation escape check failed: escape_rate=0.14, survivors=[negate-condition-51];
              <operator, edit and line for each survivor>
mutation      Mutation escape check failed: no mutants were generated from catalog
determinism   Determinism check failed: outputs differ under AuditMode
dependency    Dependency-cleanliness failed: PackageReference 'Serilog' is not allowed
              (only Ashlar.Brick.Contracts + Ashlar.Authoring)
```

On rejection the gate also runs diagnostic probes matched to the failing leg and attaches
`ProbeFindings`. A probe holds no authority — it cannot change a verdict, and a probe that throws is
logged and skipped — so treat findings as extra evidence, never as the verdict.

**Surviving mutants need a human.** `escape_rate > 0` has two opposite causes: a weak witness (add
cases) or an **equivalent mutant** — a rewrite that cannot change behaviour on any input, which no
case can kill and which does not mean the candidate is wrong. Equivalence is undecidable, so the
gate rejects either way and names the operator, the edit and the line for each survivor so you can
tell them apart at a glance. Ledger row S5 in
[`certification-evidence.md`](certification-evidence.md) is this happening for real.

## The record, and verifying it downstream

An admitted record (`CertificationRecordData`, camelCase JSON) carries `status`, `admitted`,
`signed`, `brickId`, `contentHash`, `escapeRate`, `totalMutants`, `killedMutants`,
`survivingMutantIds`, `gatesPassed[]` (name, version, configuration), `signature`, `gate` and
`schemaVersion`. A consumer who wants to *use* a certified brick without re-certifying it needs only
`Ashlar.Certification.Contracts`:

```csharp
var result = CertificationTrustVerifier.Verify(record, brickSource);
// result.Trusted, or result.FailureCode: content-hash-mismatch | signature-invalid | ...
```

`samples/certified-brick-reuse/` is the worked example of that split: Project A certifies, Project B
verifies and runs the brick untouched, referencing no gate and no generator.

> **Read this before trusting a signature.** With no explicit key and no
> `ASHLAR_CERT_DEV_HMAC_KEY`, `CertificationRecordSigner` signs with a **committed development key**
> that anyone with the source can reproduce. The constructor logs a warning in that state and
> exposes it as `UsesDevKey`. A certificate under the dev key proves integrity against accident, not
> against an adversary. The known limits of the signing story — including two ways a forged record
> can be made to verify — are numbered in
> [`certification-evidence.md`](certification-evidence.md) under "Known v0 limitations".

## Things that will bite you

- **`CompilationReferences` is required, and its absence fails closed.** Supply the assemblies the
  candidate compiles against or the analyzer leg refuses with `analyzer anchor type ... is not
  resolvable`. In a test, `AppDomain.CurrentDomain.GetAssemblies()` filtered to non-dynamic
  assemblies with a `Location` is enough; `BrickCertificationProjectLoader` collects them from the
  build output folder instead.
- **`CopyLocalLockFileAssemblies=true` on the brick project** when using the loader — otherwise the
  package assemblies are not in the build output and the reference set the loader collects is
  incomplete.
- **`ManagePackageVersionsCentrally=false`** on any project under a directory that has a
  `Directory.Packages.props`, or restore will refuse your inline `Version` attributes.
- **The brick must be deterministic by construction.** No clock, no I/O, no floating point, no
  randomness, no ambient culture. Leg 4 will catch it, but it is cheaper to not write it.
- **A gate run is not fast.** It builds the project, compiles the candidate, then compiles and runs
  a mutant per mutable site. A brick with twenty-odd mutants is seconds, not milliseconds.

## Where this is proven

- `src/Ashlar.Tests.Infrastructure/Tests/Certification/CertificationGateTeethTests.cs` — the pair
  that defines the gate: `GoodBrick_StrongWitness_Admits_WithZeroEscapeRate` and
  `WeakWitness_AllowsMutantEscapes_RejectsWithTeeth`.
- `.github/workflows/cert-gate.yml` → `bash scripts/run-cert-gate.sh` — the only required status
  check on `master`. Reproduce it locally from a checkout with that same command.
- [`certification-evidence.md`](certification-evidence.md) — one ledger row per proven ADMIT/REJECT,
  each citing the test or spike and the CI run, with "Known v0 limitations" at the end.

---

*Code references on this page were read from `src/` at the `0.1.1` line; the consumer snippets are
transcriptions of shapes that were driven end to end against published packages. The commands were
not re-executed while this page was written.*
