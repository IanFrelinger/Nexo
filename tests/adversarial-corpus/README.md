# Adversarial corpus

Every attack that ever beat the certification gate, checked in as a fixture with the verdict the
gate **should** give, and replayed on every pull request by
`src/Ashlar.Tests.Infrastructure/Tests/Certification/AdversarialCorpusTests.cs` (inside the
cert-gate filter, `bash scripts/run-cert-gate.sh`).

Nine adversarial rounds produced the bricks in here: a countdown loop that hung the certifier
forever, a recursive helper that killed it with a stack overflow, a candidate that ended the test
host with exit code 0, twin projects whose byte-identical source certified under different
`DefineConstants`, a symbol the SDK defines for free that split the program with no csproj edit,
witnesses with no teeth that a capped catalog could not expose, a helper body the catalog never
mutated, a `Directory.Build.props` that compiled code the certificate never covered. Until this
directory existed they lived under `/tmp` in one dev container and died with it. A fix without its
attack beside it is a claim the next refactor can silently unmake.

## The standard step

**A finding that survives adjudication becomes a fixture here, in the same PR that fixes it.**

The fixture is the attack exactly as it was run against the gate — the brick source, the project
and the witness the adversary used — plus an `expect.json` saying what the gate should have said.
Write the expectation from what the gate *should* do, never from what it currently does: if the
current verdict is wrong, the theory fails until the fix lands, which is the point of putting both in
one PR. Do not encode a wrong verdict to make the build green.

## Layout

```
tests/adversarial-corpus/
  Directory.Build.props        # empty shield: keeps the repo-root props out of every fixture
  README.md
  _shared/                     # material fixtures reference (a smuggled payload); NOT a fixture
  <id>/
    Corpus.<Id>.csproj         # net8.0, PackageReference Ashlar.Brick.Contracts, ManagePackageVersionsCentrally=false
    <Brick>.cs                 # exactly one source file — a brick is one file
    <name>.witness.json        # exactly one witness
    expect.json                # the verdict the gate SHOULD give
    Directory.Build.props      # only when the props file IS the attack (props-injection)
```

Rules the theory enforces:

- Every directory that does not start with `_` or `.` is a fixture, and the test case is named
  after it. A fixture **without `expect.json` fails by name** rather than being skipped: an attack
  nobody has written an expectation for is an attack the gate is not being held to.
- One `*.witness.json` per fixture. A brick under two witnesses is two fixtures.
- The project is the consumer shape from `samples/hello-brick/HelloBrick`: `net8.0`, a single
  `PackageReference` to `Ashlar.Brick.Contracts` (the released version, from nuget.org),
  `ManagePackageVersionsCentrally=false`. The assembly name **must be unique** (`Corpus.<Id>`):
  several fixtures share a brick type name, and a second `Assembly.LoadFrom` of the same simple name
  in one test process hands back the first assembly's types.
- The directory sits outside every project directory, so no test project's `Compile` glob ever
  compiles a fixture. Each fixture is built by `BrickCertificationProjectLoader`, exactly as the gate
  builds a real brick, so the theory needs the .NET SDK and nuget.org like every other loader test.

## `expect.json`

Read strictly: an unknown key fails the fixture, because a misspelt invariant that is silently
ignored is an expectation nobody is held to.

| field | required | meaning |
|---|---|---|
| `id` | yes | The directory name. A copied fixture cannot answer for another. |
| `origin` | yes | `{ "round", "lens", "severity" }` — where the finding came from (round or oracle path), the lens that found it, and the adjudicated severity (`CRITICAL`, `HIGH`, `MEDIUM`, or `control` for the honest twin a fix must not over-reject). |
| `class` | yes | `A` source-set / compiled-program divergence (the legs judged a different program from the build) · `B` author code running on the certifier's own threads · `C` coverage truncation (the catalog or the witness missed code that ships) · `D` drift (a record or schema that stopped meaning what it says). |
| `expect` | yes | `ADMIT`, `REJECT`, `REFUSE`, or `VERDICT` (below). |
| `leg` | for `REJECT` | The leg that must stop it: `fence` (a.k.a. `analyzer`), `correctness`, `mutation`, `determinism`, `dependency`. A string or a list of acceptable legs. Allowed on `VERDICT` too, where it constrains a rejection. |
| `messageContains` | for `REFUSE` | A fragment the loader's refusal message must contain — the refusal names the shape, or the author cannot act on it. |
| `description` | no | What the fixture demonstrates, for the reader. |
| `invariants` | no | Robust facts about the record, below. |
| `knownIssue` | for `VERDICT` | Why the gate's own answer cannot be pinned yet, and what to change when it can. |

Verdict classes:

- **`ADMIT`** — the gate admits; the record is signed and verifies.
- **`REJECT`** — the gate rejects at the named leg; the record is unsigned.
- **`REFUSE`** — `BrickCertificationProjectLoader.LoadAsync` refuses before the gate runs (what
  `tools/Ashlar.CertifyBrick` reports as exit 3). Not a verdict about the brick: the loader could not
  build an honest certification request from the project.
- **`VERDICT`** — either signed outcome. For a brick whose only remaining defect is a *known harness
  one*: the fixture exists to prove the certifier survives the brick and still says something. Only
  honest with a `knownIssue`, and the theory refuses it without one.

Invariants (all optional; each is checked only when present):

| invariant | asserts |
|---|---|
| `timedOutMutantsMin` | at least this many mutants were stopped by the wall clock (`timedOutMutants`) |
| `crashedMutantsMin` | at least this many mutants took their process down (`crashedMutants`) |
| `survivorsMin` / `survivorsMax` | bounds on `survivingMutantIds` |
| `totalMutantsMin` / `totalMutantsMax` | bounds on `totalMutants` (`totalMutantsMax: 0` says the brick never reached the mutation leg) |
| `wallSecondsMax` | `CertificationGate.CertifyAsync` returned inside this many seconds (the build is not counted). A hung mutant is killed, not waited for. |
| `reasonContains` / `reasonNotContains` | fragment(s) the rejection reason must / must not contain — the survivor line text, the rule id, the crash mechanism. |
| `compileOptionsContains` / `compileOptionsNotContains` | fragment(s) of the record's `compile-options` input — the symbols and options the legs judged under (`ASHLAR_EVIL`, `NET8_0`, `checkOverflow=true`). |
| `gatePassConfigurationContains` | a fragment some `gatesPassed[].configuration` must contain (`perCaseTimeoutMs`). |

Independently of the fixture, every verdict is checked for the round-8 accounting: `killedMutants`,
`timedOutMutants`, `crashedMutants` and `survivingMutantIds` partition `totalMutants`, and a
wall-clock or process-death kill is never filed as a witness kill.

**Robustness rule.** An expectation must survive the catalog widening. Never a mutant id, a hash, a
line number or an exact count. Say *which code* a survivor sits in (`reasonContains: "Resolve("`),
*which branch* it must not name (`reasonNotContains: "baseDamage - armor"`), *which options* the
record must disclose, *which leg* must fire. The twin fixtures (`fifthway-a`/`-b`, `-ex`/`-ey`,
`-d1`/`-d2`, `cap-*`, `static-*`, `payroll-*`) exist because the honest half pins that the fix did
not over-reject.

## Fixtures

| id | origin | class | expect | demonstrates |
|---|---|---|---|---|
| `fifthway-a` | round 5, `/tmp/fifthway/A` | A | ADMIT | Honest twin of `-b`; must keep certifying. |
| `fifthway-b` | round 5, `/tmp/fifthway/B` | A | REJECT mutation | Same source, `ASHLAR_EVIL` switches the computed branch; the legs used to mutate the dead branch and name a survivor the assembly does not contain. |
| `fifthway-c0` | round 5, `/tmp/fifthway/C0` | A | REJECT fence | Unguarded clock read, undeclared outputs, file write: the fence catches it (control for `-c1`). |
| `fifthway-c1` | round 5, `/tmp/fifthway/C1` | A | REJECT fence | The same lines under `#if NET`, which the SDK defines: the fence used to see a clean brick and the gate ADMITTED it. |
| `fifthway-d1` | round 5, `/tmp/fifthway/D1` | A | REJECT mutation | Unchecked build; the dead `catch` literal is unobservable, and the record discloses `checkOverflow=false`. |
| `fifthway-d2` | round 5, `/tmp/fifthway/D2` | A | ADMIT | Checked build; mutants used to be compiled unchecked and were killed by a wrap the shipped brick does not have. Record discloses `checkOverflow=true`. |
| `fifthway-ex` | round 5, `/tmp/fifthway/EX` | A | ADMIT | Clean twin of `-ey`. |
| `fifthway-ey` | round 5, `/tmp/fifthway/EY` | A | REJECT mutation | Identical contentHash, `DefineConstants` compiles a `File.WriteAllText` backdoor; both twins used to ADMIT. |
| `ifnet8-backdoor` | round 8, `CompiledProgramParityTests` | A | REJECT mutation | `-ey` with no csproj edit: `#if NET8_0` is defined by the SDK for every net8.0 project. |
| `props-injection` | source-set bypass (`Directory.Build.props`) | A | REFUSE | A props file beside the brick compiles `../_shared/InjectedPayload.cs`; the csproj never mentions it. |
| `hang-gt` | adv-mut, `/tmp/adv-mut/fx/hang-gt` | B | ADMIT | `while (n > 0)` → `while (n >= 0)` never returns; the certifier spun forever. Now `timedOutMutants ≥ 1`, bounded wall clock. |
| `recur-brick` | round 9, `/tmp/adv9/recur/RecurBrick` | B | VERDICT | A mutated recursion step overflows the stack; the certifier died with exit 134. Now `crashedMutants ≥ 1`. Known issue below. |
| `dec-brick` | skeptic, `/tmp/skep-recur/DecBrick` | B | VERDICT | Same, reached by a literal mutation alone. Known issue below. |
| `exit-zero` | round 8, `BoundedCandidateExecutionTests` | B | REJECT correctness | `Environment.Exit(0)` in `ExecuteAsync` ended the test host with exit code 0. |
| `cap-minus` / `cap-plus` | adv-mut, `/tmp/adv-mut/fx/cap-*` | C | REJECT mutation | Witness sets `discount` to 0 everywhere; the per-kind mutant cap never generated the swap that shows it. |
| `static-minus` / `static-plus` | adv-mut, `/tmp/adv-mut/fx/static-*` | C | REJECT mutation | The operator lives in a private static helper the catalog never mutated. |
| `internal-minus` | adv-mut, `/tmp/adv-mut/fx/internal-minus` | C | REJECT mutation | Same, `internal` instance helper. |
| `payroll-minus` / `payroll-plus` | skeptic-1, `/tmp/skep1-cap/fx/*` (witness `payroll2`) | C | REJECT mutation | Both twins ADMITTED with 13 mutants and 0 survivors under the capped catalog; `deduction` is 0 in every case. |

No class `D` fixture yet. A record or schema that stopped meaning what it says belongs there.

### Known issues pinned by `VERDICT`

- **Equivalent-mutant false rejection** (`recur-brick`, `dec-brick`). `shift-relational-boundary`
  turns the factorial base case `n <= 1` into `n < 1`, which computes the same value for every
  input (`1 * Factorial(0) == 1`). No witness can kill it, so the gate rejects an honest brick. When
  the catalog recognises the equivalence, change both fixtures to `expect: "ADMIT"` and delete the
  `knownIssue`.

### Deviations from the oracles

Recorded so the fixture can be traced back to what was actually run:

- `fifthway-ex`, `fifthway-ey`, `ifnet8-backdoor`: the backdoor wrote to `/tmp/fifthway/out/`, a
  directory that exists in one container. It writes under `Path.GetTempPath()` here so the leg the
  fixture reaches does not depend on that directory (`CompiledProgramParityTests` does the same).
- `payroll-*`: the oracle directory holds two witnesses; the fixture carries `payroll2`, the one
  under which both twins admitted.
- Every project is renamed `Corpus.<Id>.csproj` for a unique assembly name, and the oracles'
  `TreatWarningsAsErrors=false` / `NoWarn` lines (belt and braces against a props chain that the
  shield here makes unnecessary) are dropped. Source files are otherwise verbatim.

## Running one fixture by hand

```bash
dotnet run --project tools/Ashlar.CertifyBrick -- tests/adversarial-corpus/<id> tests/adversarial-corpus/<id>/<name>.witness.json /tmp/<id>-record.json
# exit 0 ADMIT · 1 REJECT · 3 refused before the gate ran
```

Or the theory alone:

```bash
dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj -f net8.0 \
  --filter "FullyQualifiedName~AdversarialCorpusTests"
```

The class carries `Category=Certification` (the cert-gate filter) and `Tier=Build` (it spawns a
build and child processes per case), so a fast-tier run can leave it out without leaving the
cert-gate.
