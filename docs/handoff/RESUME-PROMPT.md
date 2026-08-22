# Resume prompt

Paste the block below into a fresh Claude Code session on your local machine, from
the repository root, once you are on `claude/nexo-explanation-q4xnbr`.

It carries the decisions and the audit findings that are not recoverable from the
source alone. Everything else it can read for itself.

---

```
Read docs/handoff/HANDOFF.md first — it is the handoff from a cloud session that
had no .NET SDK, and it explains what is done, what is deliberately undone, and why.

Context you cannot recover from the source:

- This repo was fully mapped and adversarially audited. Two decisions came out of
  it: (1) rename Nexo -> Ashlar across everything — namespaces, assemblies,
  packages, directories, filenames; (2) extract the game-specific code to its own
  repository that consumes the kernel as a package.
- Ashlar is a masonry term. The brand vocabulary is already fixed by design work:
  `ashlar verify ./build` is the CLI verb, verification stages are "courses", the
  signed certificate is the "bonding stone", and the states are Submitted / Held /
  Certified / Failed. Gold (#C08B2C) marks verification and nothing else.
- The audit's central conclusion: the certification gate is the genuinely
  differentiated, load-bearing subsystem. Almost everything else is either
  mid-migration, thinner than its documentation, or duplicated. Protect the gate.
  `cert-gate` is the ONLY required status check on master.
- Do not relocate the DamageResolver / HealthApplier certification fixtures. They
  are gate fixtures, not game features, and the evidence ledger cites them.

I have a .NET 10 SDK here, so the first thing to do is establish a baseline:

    dotnet build Nexo.Kernel.sln
    bash scripts/run-cert-gate.sh

Then work through the sequence in HANDOFF.md section 3. Commit the extraction and
the rename as separate commits — the rename touches ~4,000 files and is
unreviewable if mixed with anything else.

Start by confirming the baseline is green and telling me what you find. Do not
apply either script until I confirm.
```

---

## If you want to go straight to the cleanup work instead

The rename and extraction are independent of the seven-wave cleanup plan. Waves 1–3
(documentation truth pass, making silent failures loud, sealing the write-allowlist
escape) need no decisions and no rename. To do those first, say so — HANDOFF.md
section 6 lists the three highest-value items with exact file paths and line numbers.
