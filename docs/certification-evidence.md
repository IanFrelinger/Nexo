# Certification evidence ledger

Falsifiable proof ledger for atom certification, general generation, composition certification, and dogfood. Each row cites how it was proven and the CI run (when applicable).

Version pin: `0.1.0` (from `VERSION`)

## Proof index

| Property | Proof mechanism | Result | CI run |
|----------|-----------------|--------|--------|
| Atom portability (spike steps 1–5) | `spikes/portability/run-portability-spike.sh` — generate, certify, pack, external consume, cross-project execute | **PASS** (all steps) | Local spike; see [portability spike summary](../spikes/portability/spike-run-summary.md) when re-run |
| Atom gate teeth (strong witness) | `CertificationGateTeethTests.GoodBrick_StrongWitness_Admits_WithZeroEscapeRate` | **ADMIT**, `escape_rate=0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Atom gate teeth (weak witness) | `CertificationGateTeethTests.WeakWitness_AllowsMutantEscapes_RejectsWithTeeth` | **REJECT**, `mutation`, `escape_rate > 0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| General generation 4b (buggy rejects) | `GenerationSafetyTests.BuggyGeneration_StrongWitness_Rejects` | **REJECT**, `correctness` \| `mutation` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| General generation 4a (correct admits) | `GenerationSafetyTests.GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate` | **ADMIT**, `escape_rate=0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Composition gate 4b (broken seam) | `CompositionCertificationGateTeethTests.BrokenComposition_StrongWitness_Rejects` | **REJECT**, `seam` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Composition gate 4c (weak witness) | `CompositionCertificationGateTeethTests.CorrectComposition_WeakWitness_Rejects_WithStructuralTeeth` | **REJECT**, `mutation`, `composition_escape_rate > 0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Damage-resolver dogfood (honest) | `DamageResolverDogfoodTests.HonestCursorGeneration_Admits_WithZeroEscapeRate` | **ADMIT**, `escape_rate=0`, `signed=true` | [Cert gate 27918244198](https://github.com/IanFrelinger/Nexo/actions/runs/27918244198) @ `802e6d18` |
| Damage-resolver dogfood (buggy) | `DamageResolverDogfoodTests.BuggyCursorGeneration_Rejects` | **REJECT**, `correctness` \| `mutation` | [Cert gate 27918244198](https://github.com/IanFrelinger/Nexo/actions/runs/27918244198) @ `802e6d18` |

**Dogfood summary:** `honest=ADMIT`, `buggy=REJECT`, `tests_executed=19` — CI-confirmed on PR #191.

### Integration merge verification (2026-06-21)

Branch `cursor/integration-cert-tower-921c` — fast-forward merge of full tower + merge-readiness onto `origin/master` (`5bd1a103`).

Local cert-gate on integration tip (`9baf34a9`):

```
Test Run Successful. Total tests: 19, Passed: 19
cert-gate executed 19 tests (expected>=19, derived from --list-tests).
```

Dogfood on integration: `HonestCursorGeneration_Admits_WithZeroEscapeRate` PASS, `BuggyCursorGeneration_Rejects` PASS.

---

## Atom portability (spike)

Probe brick: `ErrorSummaryExtractor` (deterministic log scanner).

| Step | Description | Result |
|------|-------------|--------|
| 1 | Generate deterministic probe brick via `INewBrickGenerator` | PASS |
| 2 | Certify through S0–S2 gate (signed admission record) | PASS |
| 3 | Pack Nexo.Brick.Contracts + Nexo.Authoring (+ Hosting.Bundle) @ 0.1.0 | PASS |
| 4 | Consume generated brick from external template (package pins only) | PASS |
| 5 | Cross-project HTTP execute assertion | PASS |

**Step 2 detail:** Signed admission record at `spikes/portability/generated/certification-record.json` (`escape_rate=0`).

## Atom gate teeth

Strong witness on the probe brick: ADMIT with `escape_rate=0`, mutants killed by AST operators (`flip-binary-op`, `negate-condition`, `mutate-int-literal`, `remove-statement`, etc.).

Weak witness (unit test `MutationProbeBrick`, errorCount-only): REJECT with `escape_rate > 0`; survivors include AST ids such as `flip-binary-op-*` and `mutate-int-literal-*`.

## General generation

Intent: **line-substring-counter** — extract count of lines containing a given substring (plus `firstMatchingLine` output).

Independent strong witness (human-provided, not authored by generator):

| Input | Expected output |
|-------|-----------------|
| `text="FOO line one\nplain\nFOO line two\n"`, `substring="FOO"` | `matchCount=2`, `firstMatchingLine="FOO line one"` |

Model seam: `IGeneratorModel` with hermetic `FixtureGeneratorModel` (**test double**) in tests; production `ProviderGeneratorModel` (`model:ollama:isolation-enforced`) behind the sealed seam.

### Generate→certify→admit results (fixture test double)

| Case | Variant | Witness | Result | Gate detail |
|------|---------|---------|--------|-------------|
| 4a | `fixture:correct` | Strong | **ADMIT** | `escape_rate=0`, signed admission record |
| 4b | `fixture:buggy` | Strong | **REJECT** | `correctness` — `firstMatchingLine` reports last match (`FOO line two`) instead of first |
| 4c | `fixture:correct` | Weak (`matchCount` only) | **REJECT** | `mutation` — `escape_rate > 0` (AST mutants survive weak witness) |
| 4d | `fixture:dependency-leak` | Strong | **REJECT** | `dependency` — source contains forbidden token `Nexo.Infrastructure` |

Generated manifest carries `GenerationProvenance` (e.g. `fixture:correct`) marking model/fixture origin on the artifact.

## Composition certification

Composition: **error-summary-pipeline** — certified `mutation-probe-brick` → `error-summary-formatter` with independent composition-level witness (not derived from constituent witnesses).

| Case | Setup | Witness | Result | Gate detail |
|------|-------|---------|--------|-------------|
| 4a | Correct wiring | Strong (`errorCount` + `summary`) | **ADMIT** | `composition_escape_rate=0`, signed composition record |
| 4b | Broken seam (`firstErrorMessage` → `errorCount`) | Strong | **REJECT** | `seam` — producer type `string` does not satisfy consumer `int` |
| 4c | Correct wiring | Weak (`errorCount` only) | **REJECT** | `mutation` — `composition_escape_rate > 0`; survivors include `drop-node-format`, `reorder-adjacent-0` |
| 4d | Uncertified `uncertified-brick` constituent | Strong | **REJECT** | `constituents` — no valid atom certification record |
| 4e | Volatile `nonce` output (unwitnessed) | Strong | **REJECT** | `determinism` — composition outputs differ under AuditMode |

Graph mutations only (reorder/drop/redirect/swap brick assignments); constituent brick source is sealed.

## Dogfood run (damage-resolver)

Intent: **damage-resolver** — Cursor-authored `CursorGeneratorModel` (**test double**, `cursor:honest` / `cursor:buggy`); human-authored witness in `DamageResolverDogfoodWitness.Spec` (generation blind).

### CI verification (authoritative)

| Fact | Value |
|------|-------|
| Workflow | [Cert gate run 27918244198](https://github.com/IanFrelinger/Nexo/actions/runs/27918244198) — `conclusion: success` |
| Commit | `802e6d180bcae8cb7538d0497a644f67a5153893` |
| Tests executed | **19** (TRX `Counters total="19" executed="19" passed="19"`) |
| `HonestCursorGeneration_Admits_WithZeroEscapeRate` | **PASS** (CI TRX `outcome="Passed"`, 941 ms on `runnervm7b5n9`) |
| `BuggyCursorGeneration_Rejects` | **PASS** (CI TRX `outcome="Passed"`, 63 ms) |
| Honest brick `escape_rate` | **0** |
| Honest brick `signed` | **true** |
| TRX artifact | `cert-gate-trx` uploaded by workflow (not committed to repo) |

## Composition dogfood run

honest=ADMIT, broken=REJECT, tests_reported=21

| Test | Result |
|------|--------|
| `HonestComposition_StrongWitness_Admits_WithZeroEscapeRate` | **PASS** |
| `BrokenComposition_StrongWitness_Rejects` | **PASS** |
| Honest composition `composition_escape_rate` | **0** |
| Honest composition `signed` | **true** |

Composition: **damage-resolver → health-applier** (`damage-to-health-pipeline`); witness in `CompositionDogfoodWitness.Spec` (6 end-to-end cases); broken wiring redirects `currentHealth` into `health.finalDamage` (rejects on `correctness`).

cert-gate CI: **success**, run [27922375242](https://github.com/IanFrelinger/Nexo/actions/runs/27922375242) — TRX `total="21"`, guard `cert-gate reported 21 tests (expected>=21)`.

## Phase 2: cross-project reuse

trusted-reuse=PASS, tamper-reject=PASS, forged-sig-reject=PASS, tests_reported=24

| Proof | Result |
|-------|--------|
| `HonestCertifiedBrick_ProjectB_TrustsAndRunsUntouched` | **PASS** — verifier TRUSTED, brick runs untouched (`finalDamage=40`) |
| `TamperedBrick_ProjectB_RejectsContentHashMismatch` | **PASS** — `content-hash-mismatch`, refused |
| `ForgedSignature_ProjectB_Rejects` | **PASS** — `signature-invalid`, refused |

Project B (`samples/certified-brick-reuse/ProjectB`) references only `Nexo.Certification.Contracts`, `Nexo.Brick.Contracts`, `Nexo.Authoring`, and the packed `Nexo.Certified.DamageResolver` artifact — **no generator, no gate**. B verifies signature + content hash; it does not re-certify or regenerate.

Content binding: `CertificationRecord.ContentHash` = SHA-256 (UTF-8) of canonical brick source, included in signed HMAC payload (`Nexo.Certification.Contracts`).

Pack/export: `scripts/pack-certified-brick-reuse.sh` → local feed + `certification-record.json` sidecar.

**v0 trust model:** same-owner cross-project reuse via shared dev HMAC key (`NEXO_CERT_DEV_HMAC_KEY`). Cross-organization trust requires PKI (out of scope).

## Agent-composer: propose→certify loop (P3-S1)

bad-proposals=REJECT (all variants), correct-proposal=ADMIT, independence=PASS, tests_reported=33

| Proof | Result |
|-------|--------|
| `BadProposalVariants_AreRejectedByExistingGate` (5 variants) | **REJECT** — `correctness` \| `mutation` \| `seam` \| `constituents` via unchanged `CompositionCertificationGate` |
| `TamperedConstituentCert_RejectedByConstituentIntegrity` | **REJECT** — `constituents` (atom cert does not verify) |
| `CorrectProposal_StrongWitness_Admits_WithZeroEscapeRate` | **ADMIT** — `composition_escape_rate=0`, signed |
| `CorrectProposal_MatchesHandAuthoredCompositionResult` | **ADMIT** — same certified result as hand-authored `CompositionDogfoodFixtures.HonestSpec()` |
| `ProposerInput_StructurallyCannotCarryWitnessCases` | **PASS** — `CompositionProposerInput` has no witness-bearing fields |

**What this proves:** An untrusted proposer (`ICompositionProposer`) receives only target I/O signature + certified-brick catalog. Its wiring proposal traverses the **identical** composition admission gate as hand-authored specs — no agent bypass. Human witness remains in `CompositionDogfoodWitness.Spec`; the controlled proposer (`ControlledCompositionProposer`) is a deterministic CI double, not a real model.

**v0 boundary:** Controlled proposer only; real LLM/agent proposer is the next sprint.

cert-gate CI: **success**, run [28000451847](https://github.com/IanFrelinger/Nexo/actions/runs/28000451847) — TRX `total="33" executed="33" passed="33"`; all 9 `CompositionProposer*` tests `outcome="Passed"` (check-runs API: `cert-gate` conclusion `success` @ `887686a1`).

## Agent-composer: real-model proposer dogfood (P3-S2)

recorded-proposal=ADMIT (first-try), independence=PASS, s1-regression=PASS, tests_reported=37

| Proof | Result |
|-------|--------|
| `RecordedRealProposal_TraversesIdenticalLoop_ReportsHonestGateVerdict` | **ADMIT** — recorded `model:cursor:isolation-enforced` proposal; `firstTryCertified=true` at recording |
| `RecordedRealProposal_WhenAdmitted_MatchesHandAuthoredCompositionResult` | **ADMIT** — same certified result as hand-authored `CompositionDogfoodFixtures.HonestSpec()` |
| `CompositionGeneratorModel_InputPathCannotCarryWitnessCases` | **PASS** — `ICompositionGeneratorModel` accepts `CompositionProposerInput` only |
| `RealProposerPrompt_IsBuiltFromProposerInputOnly_WithNoWitnessValues` | **PASS** — prompt built from target + catalog; no serialized witness cases |
| S1 `CompositionProposerDogfoodTests` (8 tests) | **UNCHANGED** — controlled rejection suite still green |

**What this proves:** A real model proposer (`ProviderCompositionGeneratorModel` over `ICompositionGeneratorModel`, mirroring `IGeneratorModel`) builds prompts from `CompositionProposerInput` only. Cert-gate replays a **recorded** proposal (`RecordedCompositionGeneratorModel`) — no live API in blocking CI. The recorded dogfood honestly reports the gate verdict on the model's actual proposal (`firstTryCertified=true`, ADMIT on damage→health).

**v0 boundary:** Single recorded proposal (record/replay); live provider records locally via `CompositionProposalRecorder`. S1 controlled rejection suite remains authoritative teeth.

cert-gate CI: **success**, run [28028224579](https://github.com/IanFrelinger/Nexo/actions/runs/28028224579) — TRX `total="37" executed="37" passed="37"`; all 4 new P3-S2 tests `outcome="Passed"` (check-runs API: `cert-gate` conclusion `success` @ `ca03c2b1`).

## Agent-composer: acceptance-rate measurement (P3-S3)

acceptance_rate=0.60 (3/5), protocol=N=5 temperature=0.7 discards=none, s1-s2-regression=PASS, tests_reported=41

| Observation | Value |
|-------------|-------|
| **Measured acceptance rate** | **0.60** (3 admits / 5 proposals) — reported, not targeted |
| Protocol | N=5, temperature=0.7, provider=cursor, discards=none |
| Distinctness observed | 3 unique wiring specs (correct×3, reordered×1, dropped×1) |
| Short-batch guard | REJECT on truncated batch (3 < declared 5) |

| Proof | Result |
|-------|--------|
| `RecordedBatch_EachEntryReproducesRecordedVerdictOnReplay` | **PASS** — anti-forgery: replay verdict matches each recorded verdict |
| `RecordedBatch_ComputedRateMatchesAdmitsOverTotal` | **PASS** — arithmetic integrity only (no threshold assertion) |
| `ShortBatchGuard_RejectsTruncatedBatch` | **PASS** — vacuous rate guard bites |
| `RecordedBatch_HoldsExactlyDeclaredIndependentSamples` | **PASS** — exactly N=5 sequence-indexed entries, no padding |
| S1 + S2 tests | **BYTE-UNCHANGED** |

**What this proves:** Raw untrusted proposer acceptance is **measured** (admits/total via unchanged `ProposeAndCertifyCompositionService`), not gated. Full batch recorded locally at temperature > 0 including rejects; CI replays deterministically.

**v0 boundary:** Single task (damage→health), one provider (cursor), record/replay batch.

## Contract-stability gaps

- Generated brick uses Nexo.Core.Domain.* namespaces (shipped via Nexo.Authoring/Nexo.Brick.Contracts but not the pinned package IDs)

## Artifacts

- `spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs`
- `spikes/portability/generated/manifest.json`
- `spikes/portability/generated/certification-record.json`

## Known v0 limitations

1. **Dev HMAC signer, not PKI.** `CertificationRecordSigner` uses a development HMAC key, not a public-key infrastructure. This becomes more load-bearing in the composition phase because trust chains from constituent atom signatures — a forged or weak constituent record undermines the whole composition admission path.

2. **Composition seam check is TYPE-level only.** The seam validator checks producer/consumer type compatibility (e.g. `string` vs `int`) but not semantic mismatches where types align (e.g. file path vs URL, both `string`). Graph-mutation teeth only partially compensate for this gap.

3. **cert-gate expected count is derived at runtime.** The zero-test guard compares executed count against `dotnet test --list-tests` on the cert-gate filter (see `scripts/cert-gate-config.sh`). If the filter or test discovery breaks, the guard fails closed.
