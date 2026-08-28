# Autonomy objectives — worked example

The objective store lives at `.ashlar/runtime-studio/objectives/`, which is gitignored
because it is runtime state. This directory holds a tracked copy of one complete objective
so the loop can be exercised reproducibly.

## The three files

| File | Authored by | Purpose |
|------|-------------|---------|
| `tag-scan-classifier.md` | human | The objective: intent, contract, and declared touch-set |
| `tag-scan-classifier.witness.json` | human | Acceptance criteria — **written before any proposal existed** |
| `tag-scan-classifier.proposal.json` | model | A candidate, recorded for replay |

The ordering in that table is the discipline, not a coincidence. The witness is authored
from the objective's contract *before* a proposal exists, and the proposer never sees it —
`ProposalRequest` carries the objective's declarations and deliberately not its acceptance
cases. A proposer that could see the witness could satisfy the cases without satisfying the
contract, and the certificate would then be a claim about nothing.

## Running it

The shortest working path is the first-flight script, which composes everything below for
you (a container engine must be reachable; a live proposer is optional):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File spikes/autonomy-first-flight/run-first-flight.ps1 -SweepLive
```

To compose the loop in your own host, every one of these is load-bearing — the loop is
fail-closed at each step, so leaving one out yields "nothing happens" or an explained
refusal, never a quiet in-process run:

```bash
cp samples/autonomy-objectives/tag-scan-classifier.* \
   .ashlar/runtime-studio/objectives/pending/
```

```csharp
services.AddLogging();
services.AddCertificationGate();                       // the real gate + signer; without it AddAshlarAutonomy has no ICertificationGate to resolve
                                                      // pass recordStorePath: "<dir>" for records that survive the process; parameterless keeps the in-memory store
services.AddAshlarAutonomy(configuration);              // binds Ashlar:Autonomy — the loop and the harness both read it
services.AddAshlarAutonomyLoop(loop =>
{
    loop.IntervalSeconds = 300;                        // 0 (the default) means the loop never sweeps
    loop.MaxObjectivesPerSweep = 5;
    // CompilationReferences defaults to the brick contract assemblies (DomainBrick, BrickInput);
    // add the assembly of anything the candidate delegates to, e.g. the physical-atom codec:
    loop.CompilationReferences = AutonomyLoopSettings.DefaultCompilationReferences()
        .Append(typeof(PhysicalAtomQrTagCodec).Assembly.Location).ToArray();
});
```

with, in the `configuration` you handed to `AddAshlarAutonomy` — `Ashlar:Autonomy` is a
host-composed section, so it reads whatever that configuration contains (an in-memory set as
the first-flight spike uses, `appsettings.json`, or `Ashlar__Autonomy__*` environment variables;
see "How `Ashlar:*` options bind" in `docs/Configuration.md`):

```text
Ashlar:Autonomy:Enabled=true                    # master switch; false = the timer never starts
Ashlar:Autonomy:UseSandboxSessions=true         # otherwise no SessionSpec is built at all
Ashlar:Autonomy:BuildCandidateInSession=true    # compile inside the attested session
Ashlar:Autonomy:ExecuteCandidateInSession=true  # witness/mutation/determinism inside it too
Ashlar:Autonomy:SessionImage=mcr.microsoft.com/dotnet/sdk:9.0   # must already be present on the engine (--pull never)
Ashlar:Autonomy:HoldAdmission=true              # the default: certify fully, admit nothing
```

Why the trio matters here specifically: the loop hands the gate an identity-only handle for
the proposed brick (`ProposedBrickHandle`) — the real candidate exists only as source until
the session builds it. With `ExecuteCandidateInSession=false` the gate would execute that
handle in-process, it refuses (as it must), and every objective ends as an
`ExplainedFailure` at `correctness` that the loop reports as host wiring and never hands to a
proposer as repair feedback. `AddAshlarAutonomy` warns at composition when an enabled loop is
missing the execution leg.

Keep `HoldAdmission` at its default (`true`) — the loop certifies fully and admits nothing,
which is what you want until you have read a few digests and trust what the witnesses pin.
The hold is enforced by the harness `AddAshlarAutonomy` composes; the loop reports that same
value and has no dial of its own.

Model-proposed code never runs in the host process under this configuration.

## What this example demonstrates

The recorded proposal is a real ollama (`codellama:7b`) response to this objective, kept
verbatim including its defect. It delegates correctly to `PhysicalAtomQrTagCodec.TryDecode`
rather than re-implementing base64url handling — and then writes the codec's `failureCode`
straight to the output.

On the success path that value is `null` (`PhysicalAtomTagBinaryCodec.cs`), while the
contract and the witness's first case require the empty string. So the candidate should be
rejected on the correctness leg.

That is the example's point: a witness written from the contract catches a defect a
plausible-looking implementation introduced, and it catches it because it existed first.

## The campaign set

Since dogfood campaign 1 (evidence ledger S4) this directory holds five objectives, chosen for
shape: `tag-scan-classifier` (a classifier over an existing codec), `door-lock-transition` (a
state machine), `semver-parse` and `rgb-hex-parse` (parsers with integer outputs), and
`text-slug`, which is under-specified **on purpose** — its contract never addresses diacritics
while its witness pins them, so a run shows the repair channel holding rather than converging on
a witness the proposer cannot see. Every objective states what a proposer must produce
(namespace, class, `Id`, interface) with a skeleton; `proposer-preamble.md` is the brick API as
operator house rules, passed to the proposer through `OllamaProposalOptions.SystemPreamble`.

`door-lock-transition.proposal.json` is a real `codellama:7b` proposal — its FIRST attempt in
campaign 4 — that built in the attested session, passed the analyzer fence, all eight witness
cases, mutation (`escape_rate=0`) and determinism, and was held (`CertifiedButHeld`).

`rgb-hex-parse.proposal.json` is the same thing from `qwen3.8:27b` in campaign 2 (S5) — the first
parser to go the whole way, also on its first attempt. Both are recorded for replay; neither was
admitted.

## Running the campaign

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File spikes/autonomy-first-flight/run-first-flight.ps1 -SweepLive
```

One sweep of the standing loop with a live ollama proposer composed inside it, all objectives
seeded, hold admission on. The host-mounted campaign directory (`.ashlar/campaign/<stamp>/`) keeps
the objectives, every recorded proposal with the exact projected feedback the model was handed
(`proposals/{id}.attempt{N}.json`), and the full log — the ledger's raw material.
`ASHLAR_OLLAMA_MODEL` selects the model; the loop is model-agnostic by construction.
