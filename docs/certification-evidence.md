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

## State log binding (foundation + portable substrate)

Attested state log binding: certify transition **rules/schema** and bind mutable state via hash-chained provenance (`Nexo.Certification.State`). Tier-1 structural verification via `StateLogVerifier`; Tier-2 replay via optional `ITransitionReplayer` seam.

| Proof | Result |
|-------|--------|
| `AttestedStateLogBindingTests` R1–R10 | **REFUSE** — distinct codes: `behavior-cert-unresolved`, `behavior-cert-untrusted`, `schema-violation`, `chain-prev-entry-break`, `chain-gap`, `chain-reorder`, `injected-behavior-cert-unresolved`, `replay-mismatch`, `live-state-mismatch` |
| `A1_FullyValidLog_IsTrusted` | **TRUSTED** — `VerifiedTransitions == 3` |
| `A2_FullyValidLogWithReplay_IsTrusted` | **TRUSTED** — Tier-2 admission with fake replayer |
| `A3_FullyValidLogWithBrickReplay_IsTrusted` | **TRUSTED** — Tier-2 admission via `BrickTransitionReplayer` (Roslyn brick execution) |
| `A4_FullyValidLogWithLiveHeadBinding_IsTrusted` | **TRUSTED** — Tier-3 admission via `ILiveStateHashReader` head binding |
| `G1_ZeroMutantGuard_VerifiedTransitionCountMatchesLogLength` | **PASS** — count > 0 and equals N |
| `CrossProjectStateLogReuseTests` | **PASS** — Project C bundled JSON + file catalog, no Infrastructure in consumer csproj |
| `AttestedStateLogWireFormatTests` | **PASS** — bundled sample deserializes and trusts Tier-1 |
| `CertifiedBehaviorCatalogTests` | **PASS** — file catalog resolves trusted behaviors |
| `AttestedStateLogTrustGateTests` | **PASS** — DI trust gate trusts bundled sample (Tier-1, Tier-2 brick replay, Tier-3 live head) |

Project C (`samples/certified-state-log-reuse/ProjectC`) references only `Nexo.Certification.State` + `Nexo.Certification.Contracts`. Bundled artifacts under `Nexo.Certified.PhaseWitness/` (`state-schema.json`, `attested-state-log.json`, compilable brick sources under `bricks/`, behavior sidecars). Pack: `scripts/pack-certified-state-log-reuse.sh`.

**Tier split:** Tier-1 catches cert provenance, schema format, hash-chain integrity (R1–R7). Tier-2 replay required for swapped-valid-cert (R8) and out-of-band state tamper with valid schema hashes (R9). Tier-3 live head binding required when log and live store diverge (R10). Production replay: `BrickTransitionReplayer` + `IBrickTransitionReplayerFactory`; live binding: `ILiveStateHashReader` + `InMemoryLiveStateHashReader` in `Nexo.Infrastructure`.

## Agent-composer (proposer seam → real model → acceptance rate)

Cumulative evidence from P3-S1 (controlled proposer), P3-S2 (real-model record/replay), and P3-S3 (acceptance-rate measurement). cert-gate: **41 tests** @ [run 28067778575](https://github.com/IanFrelinger/Nexo/actions/runs/28067778575) (`conclusion: success` @ `7f9cbdc3`).

### Proposer seam (P3-S1)

| Proof | Result |
|-------|--------|
| `BadProposalVariants_AreRejectedByExistingGate` (5 variants) | **REJECT** — `correctness` \| `mutation` \| `seam` \| `constituents` via unchanged `CompositionCertificationGate` |
| `TamperedConstituentCert_RejectedByConstituentIntegrity` | **REJECT** — `constituents` |
| `CorrectProposal_StrongWitness_Admits_WithZeroEscapeRate` | **ADMIT** — `composition_escape_rate=0`, signed |
| `CorrectProposal_MatchesHandAuthoredCompositionResult` | **ADMIT** — matches hand-authored `CompositionDogfoodFixtures.HonestSpec()` |
| `ProposerInput_StructurallyCannotCarryWitnessCases` | **PASS** — no witness-bearing fields on `CompositionProposerInput` |

**What this proves:** Untrusted proposer (`ICompositionProposer`) receives target I/O signature + certified-brick catalog only. Wiring traverses the **identical** composition admission gate — no agent bypass. Human witness in `CompositionDogfoodWitness.Spec`; `ControlledCompositionProposer` is a deterministic CI double.

**v0 boundary:** Controlled proposer only for rejection teeth; real model is next layer below.

CI: [run 28000451847](https://github.com/IanFrelinger/Nexo/actions/runs/28000451847) — 33 tests, 9 `CompositionProposer*` passed.

### Real-model proposer dogfood (P3-S2)

| Proof | Result |
|-------|--------|
| `RecordedRealProposal_TraversesIdenticalLoop_ReportsHonestGateVerdict` | **ADMIT** — `model:cursor:isolation-enforced`; `firstTryCertified=true` |
| `RecordedRealProposal_WhenAdmitted_MatchesHandAuthoredCompositionResult` | **ADMIT** — same result as hand-authored spec |
| `CompositionGeneratorModel_InputPathCannotCarryWitnessCases` | **PASS** |
| `RealProposerPrompt_IsBuiltFromProposerInputOnly_WithNoWitnessValues` | **PASS** — no serialized witness cases in prompt |
| S1 controlled rejection suite (8 tests) | **UNCHANGED** |

**What this proves:** `ProviderCompositionGeneratorModel` over `ICompositionGeneratorModel` builds prompts from `CompositionProposerInput` only. Cert-gate replays a **recorded** proposal — no live API. Honest first-try ADMIT on damage→health.

**v0 boundary:** Single recorded proposal (record/replay); live capture via `CompositionProposalRecorder` locally. S1 controlled rejection remains authoritative teeth.

CI: [run 28028224579](https://github.com/IanFrelinger/Nexo/actions/runs/28028224579) — 37 tests.

### Acceptance-rate measurement (P3-S3)

| Observation | Value |
|-------------|-------|
| **Measured acceptance rate** | **0.60** (3 admits / 5 proposals) — reported, not targeted |
| Protocol | N=5, temperature=0.7, provider=cursor, discards=none |
| Distinctness observed | 3 unique wiring specs (correct×3, reordered×1, dropped×1) |

| Proof | Result |
|-------|--------|
| `RecordedBatch_EachEntryReproducesRecordedVerdictOnReplay` | **PASS** — anti-forgery |
| `RecordedBatch_ComputedRateMatchesAdmitsOverTotal` | **PASS** — arithmetic integrity only |
| `ShortBatchGuard_RejectsTruncatedBatch` | **PASS** |
| `RecordedBatch_HoldsExactlyDeclaredIndependentSamples` | **PASS** — exactly N=5, no padding |
| S1 + S2 tests | **BYTE-UNCHANGED** |

**What this proves:** Raw proposer acceptance is **measured** (admits/total via unchanged `ProposeAndCertifyCompositionService`), not gated. Full batch recorded at temperature > 0 including rejects; CI replays deterministically.

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
