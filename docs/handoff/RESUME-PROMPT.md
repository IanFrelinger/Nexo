# Resume prompt

Paste the block below into a fresh Claude Code session on your local machine, from
the repository root, once you are on `claude/nexo-explanation-q4xnbr`.

It carries the decisions, the brand vocabulary and the audit findings that are not
recoverable from the source alone. Everything else it can read for itself.

---

```
Read docs/handoff/HANDOFF.md first. It is at revision 2: revision 1 was written
without a .NET SDK, and three of its load-bearing claims turned out to be wrong
when actually compiled. Section 2 records what was wrong and how it was caught.

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

Known state, already measured — do not re-derive:

- THE RENAME IS DONE. Commit a0609ebe: 4,000 files by content, 228 filenames, 99
  directories; 25,155 insertions and 25,155 deletions, exactly symmetric.
  verify-rename.sh prints PASS, dotnet build Ashlar.Kernel.sln is 0/0, and the cert
  gate is 169/169. Everything is Ashlar now EXCEPT scripts/handoff/ and
  docs/handoff/, which are deliberately excluded because they are documentation
  ABOUT the rename — rewriting them destroyed the verifier on the first attempt.
  Do not "fix" the remaining Nexo tokens in those two directories.
- Anything that EXECUTES build output must run in the dev container. Windows
  Application Control blocks loading freshly-built unsigned test assemblies on this
  host (0x800711C7), which hits all 26 scripts under scripts/ that call dotnet test,
  not just the cert gate. Use:
      bash scripts/handoff/devbox.sh bash scripts/run-cert-gate.sh
  Measured: 169/169 cert-gate tests pass in the container in 1.4-2.3 minutes. That is a
  real green baseline. Do not attempt to change the host security setting — it is
  not yours to change and it is no longer on the critical path.
- Run EVERYTHING through devbox.sh, including the file-heavy passes that do not
  strictly need it. Measured twice each, same script and tree: rename-to-ashlar.sh
  dry run takes 495s / 514s on the host and 57s / 57s in the container — about 9x
  faster, because Windows per-file I/O plus real-time AV scanning across 4,005
  files costs far more than the bind mount does. Results are identical in both
  (4005 / 228 / 99, same 3 blockers). There is no step better off on the host.
- `dotnet build Nexo.sln` fails at restore with NU1201 on a clean tree
  (GameDirector.Host targets net8.0, references net10.0-only Nexo.API). Pre-existing
  and unrelated. Do not treat it as something you caused.
- The extraction is BLOCKED. Playtest and TileMapRenderTool were misfiled as Tier 1;
  they are Tier 3 and need IDomainAgentProvider / IDomainPatternProvider plus a tool
  registry first. extract-game-layer.sh now refuses --apply and tells you why.

The rename is done, so the next step is the provider refactor: turn AgentFactory
and DomainRecognizer into IDomainAgentProvider / IDomainPatternProvider registries,
and make RepoFsToolboxFactory take its tools from a registry instead of newing up
concrete types. HANDOFF.md section 4 Step 2 has the detail. extract-game-layer.sh
reports 0 blockers when that work is finished — that check IS the definition of done.

Start by confirming the tree is still green:
    bash scripts/handoff/devbox.sh 'bash scripts/run-cert-gate.sh'
then read HANDOFF.md section 4 Step 2 and tell me your plan before writing code.
```

---

## If you want to go straight to the cleanup work instead

The rename and the extraction are independent of the seven-wave cleanup plan.
Waves 1–3 (documentation truth pass, making silent failures loud, sealing the
write-allowlist escape) need no decisions and no rename.

HANDOFF.md section 7 lists four items with exact file paths and line numbers. The
write-allowlist escape is the one to do first — and it is worth
doing *before* the rename, since it touches all 14 `Tools.Dev` schemas that the
rename is about to rewrite anyway, and its diff stays readable in the vocabulary
you already know.
