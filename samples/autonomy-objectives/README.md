# Autonomy objectives — worked example

The objective store lives at `.nexo/runtime-studio/objectives/`, which is gitignored
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

```bash
cp samples/autonomy-objectives/tag-scan-classifier.* \
   .nexo/runtime-studio/objectives/pending/
```

Then compose a host with `AddNexoAutonomy` + `AddNexoAutonomyLoop`. Keep
`HoldAdmission` at its default (`true`) — the loop will certify fully and admit nothing,
which is what you want until you have read a few digests and trust what the witnesses pin.

Sessions are required: the loop builds and executes candidates inside an attested container,
so a container engine must be reachable. Model-proposed code never runs in the host process.

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
