# Certification evidence ledger

Falsifiable proof ledger for atom certification, general generation, composition certification, and dogfood. Each row cites how it was proven and the CI run (when applicable).

Version pin: `0.1.0` (from `VERSION`)

## Proof index

| Property | Proof mechanism | Result | CI run |
|----------|-----------------|--------|--------|
| Atom portability (spike steps 1–5) | `spikes/portability/run-portability-spike.sh` — generate, certify, pack, external consume, cross-project execute | **PASS** (all steps) | Local spike; re-run `run-portability-spike.sh` for fresh summary |
| Atom gate teeth (strong witness) | `CertificationGateTeethTests.GoodBrick_StrongWitness_Admits_WithZeroEscapeRate` | **ADMIT**, `escape_rate=0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Atom gate teeth (weak witness) | `CertificationGateTeethTests.WeakWitness_AllowsMutantEscapes_RejectsWithTeeth` | **REJECT**, `mutation`, `escape_rate > 0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| General generation 4b (buggy rejects) | `GenerationSafetyTests.BuggyGeneration_StrongWitness_Rejects` | **REJECT**, `correctness` \| `mutation` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| General generation 4a (correct admits) | `GenerationSafetyTests.GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate` | **ADMIT**, `escape_rate=0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Composition gate 4b (broken seam) | `CompositionCertificationGateTeethTests.BrokenComposition_StrongWitness_Rejects` | **REJECT**, `seam` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Composition gate 4c (weak witness) | `CompositionCertificationGateTeethTests.CorrectComposition_WeakWitness_Rejects_WithStructuralTeeth` | **REJECT**, `mutation`, `composition_escape_rate > 0` | [Cert gate 27918340788](https://github.com/IanFrelinger/Nexo/actions/runs/27918340788) |
| Damage-resolver dogfood (honest) | `DamageResolverDogfoodTests.HonestCursorGeneration_Admits_WithZeroEscapeRate` | **ADMIT**, `escape_rate=0`, `signed=true` | [Cert gate 27918244198](https://github.com/IanFrelinger/Nexo/actions/runs/27918244198) @ `802e6d18` |
| Damage-resolver dogfood (buggy) | `DamageResolverDogfoodTests.BuggyCursorGeneration_Rejects` | **REJECT**, `correctness` \| `mutation` | [Cert gate 27918244198](https://github.com/IanFrelinger/Nexo/actions/runs/27918244198) @ `802e6d18` |
| Autonomy first flight (live engine) | `spikes/autonomy-first-flight/run-first-flight.ps1` — one real iteration: attested Docker session → full chain → Tier-0 swap → watch window | **PASS**, `AdmittedAndSwapped`, `escape_rate=0` | Local spike @ `1afac86d`; re-run the script for a fresh flight |
| Autonomy in-session build (P3) | Same flight with `-SessionBuild` — candidate compiles INSIDE the attested `dotnet/sdk:9.0` session over `ExecAsync`, offline | **PASS**, `session-build` input on the certificate | Local spike @ `d71d045f` |
| Autonomy in-session execution (P5a) | Flight with `-SessionExecute` — witness, determinism, and every mutant EXECUTE inside the session; the gate judges raw observations | **PASS**, `session-execution` input, `escape_rate=0` | Local spike @ `bf8821db` |
| Model-proposed candidate (P5b) | Flight with `-Proposed` — recorded model proposal, proposer signature in lineage, full containment; admitted only after two honest mutation REJECTs forced witness hardening | **PASS** after campaign: 2× `BudgetExhausted`, REJECT 0.16, REJECT 0.05, then `AdmittedAndSwapped` | Local spike @ `bf8821db` |
| LIVE model proposal (P6) | Flight with `-Live` — ollama `codellama:7b` called AT FLIGHT TIME, witness-blind prompt, recording committed; the gate judges each sample | **PASS on sample 4** (`AdmittedAndSwapped`, escape 0); measured acceptance 1/4 — 2 mutation REJECTs, 1 swap-host identity hold | Local spike @ `4ad4d05e`; recordings in `spikes/autonomy-first-flight/recordings/` |
| Standing loop, first sweep (S1) | `run-first-flight.ps1 -Sweep` — an objective FILE in the store drives the loop: witness + proposal loaded, attested session, in-session compile, witness judged | **REJECT at `correctness` case 0** (`expected "" got <null>`) — hold mode, nothing swapped | Local spike @ `061c4f83`; example in `samples/autonomy-objectives/` |
| Repair loop to ADMIT (S2) | S1 rejection fed back to the model as repair input; loop re-run under hold + full containment | model fixed its one defective line; **ADMIT, `escape_rate=0` → `CertifiedButHeld`** after two trust-machinery holes were closed (analyzer-dead mutants as kills; `$summary` witnessable) | Local spike @ `7cdf9e88`; sample in `samples/autonomy-objectives/` |
| Repair channel as policy (S3) | `RepairFeedbackPolicy` + ablation on codellama:7b, then the shipped loop path (5 objectives × 2-attempt budget, two temperatures) | **redaction costs nothing (3/3 vs 3/3 in ablation); through the shipped path 3/5 objectives converge within the budget at temp 0.2 and 0.7 alike**; the necessary ingredient was contract precision ("NEVER null"), and single-shot rate on a 7B model swings with formatting noise — the bounded retry is what makes it usable | Local ablation @ this PR |
| Dogfood campaign 1 (S4) | five human-authored objectives, live codellama:7b in the loop, hold mode, four campaigns | **compiled 0/5 → 1/5 → 3/5 → 2/5 as the loop was fixed; first full-chain success `door-lock-transition` CertifiedButHeld (escape_rate=0) on the FIRST proposal; text-slug held on a witness the proposer never saw**; a 7B model re-emits on repair — the loop is model-agnostic and the next lever is the model | `.nexo/campaign/*` recordings; `samples/autonomy-objectives/door-lock-transition.proposal.json` |
| Dogfood campaign 2 (S5) | the same five objectives, witnesses, preamble and hold mode — only `NEXO_OLLAMA_MODEL` varies: `codellama:7b` vs `qwen2.5-coder:7b` vs `qwen3.8:27b` | **certified-held 1/5 → 2/5 → 3/5 on model swap alone; at 27B compiled 5/5 and the failures move down the pipeline** — `semver-parse` survives correctness and is rejected at `mutation` (`escape_rate=0.04`, a witness weakness, not a model one). Repair stays weak at every size: byte-identical re-emission on 6/6 (7B) and 3/4 (27B) repair attempts, and every certified candidate certified on attempt 1 | `.nexo/campaign/*-qwen*` recordings; `samples/autonomy-objectives/rgb-hex-parse.proposal.json` |

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

## Physical-atom certification (Phase 0 — Prototype)

Headless cert + verifier core for binding physical objects to hosted digital-twin assets. Spec: `docs/physical-atom-phase0-spec.md`. Test report: `docs/physical-atom-phase0-test-report.md`.

| Proof | Result |
|-------|--------|
| `PhysicalAtomCertificateVerifierTests` (R1–R7 refusal + A1–A4 admission) | **PASS** — forged sig, hash mismatch, binding-scope violations, geo H3 inconsistency, tampered extensions all refused |
| `BundleCertificationBrickTests` | **PASS** — Design/Instance/Batch issuance; inconsistent inputs refused at issuance |
| `PhysicalAtomSampleCertTests` | **PASS** — committed sample at `samples/physical-atom-cert/` verifies headless |

**Design decision:** `Design` binding_scope with populated `manufacture_meta` is an explicit error (`binding-scope-manufacture-meta-forbidden`), not silently ignored.

**Crypto:** Ed25519 issuer signatures via `Nexo.Certification.Physical` (NSec 25.4.0). Sample issuer key is documentation-only.

cert-gate: **69 tests** @ [run 28486193636](https://github.com/IanFrelinger/Nexo/actions/runs/28486193636) (`conclusion: success`, PR #210).

## Physical-atom asset resolution (Phase 1 — Prototype)

Headless hosting/resolution loop: register assets + certs, resolve by atom/hash, verify bundles. Spec: `docs/physical-atom-phase1-spec.md`.

| Proof | Result |
|-------|--------|
| `PhysicalAtomResolutionVerifierTests` (R1–R4 + A1–A2) | **PASS** — unresolved atom/asset, store byte mismatch, tampered bundle manifest refused |
| `AssetBundleCertificationPipelineTests` | **PASS** — certify/register/resolve end-to-end |
| `PhysicalAtomCertBundleManifestTests` | **PASS** — sample `design-scope.bundle.json` round-trips and verifies |

Sample bundle: `samples/physical-atom-cert/design-scope.bundle.json`.

## Physical-atom tag encoding (Phase 2 — Prototype)

QR/NFC reference encoding for certified atoms. Spec: `docs/physical-atom-phase2-spec.md`.

| Proof | Result |
|-------|--------|
| `PhysicalAtomTagCodecTests` (R1–R6 + A1–A2) | **PASS** — malformed prefix/base64/CRC/version/NDEF type refused |
| `PhysicalAtomTagIssuingTests` | **PASS** — bundle → QR + NFC; missing issuer key refused |
| `PhysicalAtomTagSampleTests` | **PASS** — `design-scope.tag-qr.txt` decodes headless |

Sample QR: `samples/physical-atom-cert/design-scope.tag-qr.txt`.

## Physical-atom orchestration (Phase 3 — Prototype)

HTTP resolution routing + tag→verify orchestration. Spec: `docs/physical-atom-phase3-spec.md`.

| Proof | Result |
|-------|--------|
| `PhysicalAtomTagVerifyOrchestratorTests` | **PASS** — malformed tag, unresolved atom, reference/fingerprint mismatch refused |
| `HttpAssetResolutionRouterTests` | **PASS** — headless GET routes for cert + asset |
| `PhysicalAtomEndToEndFlowTests` | **PASS** — pipeline → HTTP → tag verify |

## Spatial pose arc (P1 — Prototype, #220)

Identity/pose seam landed on `master` via squash merge `4f550b03`. Duplicate `Nexo.Certification.PhysicalAtom` dropped; spatial runtime binds through `TagVerifyResolverAdapter` → `PhysicalAtomTagVerifyOrchestrator`.

| Proof | Result |
|-------|--------|
| `SpatialBindingServiceRejectionTests` + `TagVerifyResolverAdapterSeamTests` | **PASS** — uncertified atom, issuer mismatch, mid-stream asset-hash change refused |
| `ScopedPoseRelayRejectionTests` | **PASS** — host-only publish, non-member subscribe refused, no pose replay for late joiners |
| `PoseStreamConsumerRejectionTests` | **PASS** — confidence/gap/velocity policies downstream of provider |
| `dependency-boundary-gate` | **PASS** — only `Nexo.Spatial.Runtime` references `Nexo.Certification.Physical` |

Projects: `Nexo.Spatial.Contracts`, `Nexo.Spatial.Runtime`, `Nexo.Spatial.Multiplayer`. Doc: `docs/spatial-multiplayer.md`.

**Deferred:** 0c8b hash-chained bundle transitions (use `PhysicalAtomCertBundleVerifier` instead). Native SDK binding on device hosts (ARKit/NRSDK/RealityKit frame delegates) remains a manual hardware follow-up.

## Spatial platform providers (P2 — Prototype)

Real platform shells wired through injectable native-interop seams; headless CI exercises fail-closed paths only.

| Proof | Result |
|-------|--------|
| `ArKitSpatialAnchorProviderRejectionTests` | **PASS** — non-iOS/uninitialized fail-closed; limited→`Occluded`; interruption→`Lost`; unknown atom→`null` |
| `XrealSpatialAnchorProviderRejectionTests` | **PASS** — unsupported host, tether disconnect→`Lost`, unknown atom→`null` |
| `VisionProSpatialAnchorProviderRejectionTests` | **PASS** — pre-immersive-space fail-closed; limited→`Occluded` |
| `SpatialAnchorProviderSelectorRejectionTests` | **PASS** — unsupported host explicit unavailable; deterministic priority tie-break |
| `dependency-boundary-gate` | **PASS** — zero `Nexo.Certification.*` in `Nexo.Spatial.Platform.*`; no sibling platform refs |

Projects: `Nexo.Spatial.Platform.ARKit`, `Nexo.Spatial.Platform.XREAL`, `Nexo.Spatial.Platform.VisionPro`. Selection glue: `SpatialAnchorProviderSelector` in `Nexo.Spatial.Runtime`.

**Architecture notes:** ARKit session lifecycle is host-owned via `IArKitNativeSession`. Vision Pro is a separate package (not an ARKit variant) for visionOS consumer isolation + immersive-space gating. Provider priority: `visionpro` → `arkit` → `xreal` (ordinal tie-break).

## Autonomy first flight (P2)

One real autonomous iteration, end to end, against a **live container engine** — the acceptance
step previously recorded as outstanding under known limitation 5. Flown 2026-08-14 from commit
`1afac86d` via `spikes/autonomy-first-flight/run-first-flight.ps1` (devcontainer image +
`/var/run/docker.sock` pass-through; only committed state flies).

The spike composes `AddCertificationGate` + `AddNexoAutonomy` exactly as a host would
(`ValidateOnBuild`/`ValidateScopes`), hand-authors a Triage objective, and runs one
`AutonomousIterationHarness.RunIterationAsync` with a candidate whose source, witness, and clean
project file mirror the proven gate-teeth shape.

| Step | Result |
|------|--------|
| Objective intake (`Triage` source, touch-set: `spikes/autonomy-first-flight/generated/`) | Tier = `Tier0Autonomous` |
| Sandbox session against live daemon (`alpine:3.20`, no mounts, `NetworkAccess.None`) | Provisioned + attested: digest `sha256:d9e853e87e55526f6b2917df91a2115c36dd7c696a35be12163d44e6e2a4b6bc`, engine `29.7.2`, effective caps `mem=268435456` / `pids=64` / `nanoCpus=1000000000` (matched request — not weaker) |
| Certification chain (analyzer fence + touch-set, witness, mutation, determinism, dependency) | **ADMIT** — `signed=true`, `escape_rate=0` |
| Certificate inputs | `witness`, `image-digest`, `sandbox-spec`, `attestation`, `generation-depth` (depth 1) all recorded |
| Autonomous Tier-0 swap (`AutonomousAdmission` with lineage key) | **`AdmittedAndSwapped`** as generation 1, 4.4 s end to end |
| Post-swap serving | 3/3 invocations correct (`errorCount=1`, first message extracted) — watch window cleared |
| Session teardown | `docker ps -a` on the host daemon shows **zero** `nexo-session-*` containers after the run |
| Digest | `AutonomyDigest` renders the swap-committed event; nothing held |

A `--dry` leg (TestKit fake runner, same wiring) also passed, with an explicit zero-leaked-sessions
assertion. The dry and real runs produced identical outcomes and identical certificate-input kinds;
only the attestation values differ (fake vs. live daemon) — which is exactly the seam's contract.

**Boundary:** the P2 flight's session was provisioning attestation only; the **P3 leg below**
closed the compilation half of that gap. The **real-proposer** leg of old limitation 5 remains
open: every flight candidate was hand-authored to the gate-teeth shape, not produced by a model.

### P3 acceptance: the candidate compiled inside the session

Re-flown 2026-08-14 from commit `d71d045f` with `-SessionBuild`: the loop's iteration required the
candidate to compile **inside** the attested session (`Nexo:Autonomy:BuildCandidateInSession=true`),
against the pinned SDK image on the live daemon.

| Step | Result |
|------|--------|
| Session image | `mcr.microsoft.com/dotnet/sdk:9.0` @ `sha256:35048e3a81e6a07c316e7bbbd80d80d2ba705fe5f23a8ed42b6638c8f4c20d30`, engine `29.7.2`, same effective-caps verification as P2 |
| In-session compile (`SessionCandidateBuild` over `ExecAsync`: base64-chunk uploads, zero-`PackageReference` project, cleared NuGet sources, `NetworkAccess.None`) | **PASS** — the exact `CandidateSourceWrapper.Wrap` bytes every certification-path compile sees |
| Certificate inputs | Six kinds: `witness`, `image-digest`, `sandbox-spec`, `attestation`, **`session-build`**, `generation-depth` |
| Outcome | **`AdmittedAndSwapped`** as generation 1, 7.0 s end to end; 3/3 post-swap invocations; digest rendered; zero `nexo-session-*` containers left on the daemon |

The dry `-Dry -SessionBuild` leg passed identically (fake sessions, same input kinds). In-container
test suite: 20/20 (`AutonomousIterationHarnessTests` including the in-session-build facts,
`AutonomyCompositionTests` including the `BuildCandidateInSession` misconfiguration row).

### P5: execution containment, and the recorded model proposal

Two closures flown 2026-08-14. With `ExecuteCandidateInSession=true`, the gate's witness,
determinism, and mutation legs EXECUTE candidate and mutant code inside the attested session
(`SessionExecutionBackend`: a dumb runner built in-session from embedded source, per-unit load
contexts, raw observations only — the gate judges). With both legs on, untrusted candidate code
never runs in the harness process; what remains on the host is orchestration, Roslyn compilation
of mutants, and judgment.

**Full-containment flight (hand-authored candidate):** `AdmittedAndSwapped`, seven certificate
inputs including `session-execution`, `escape_rate=0` with every mutant executed in-session —
12.3 s at the two-case witness (`3b199a67`), re-flown PASS at the hardened four-case witness
(`bf8821db`).

**The recorded model proposal** — the first candidate in this repository authored by a model
(Claude Fable 5, recorded 2026-08-14; proposer signature `model:claude-fable-5:recorded:2026-08-14`
hash-bound into the `generation-depth` input; authored from the objective and interface contract
only, never shown the witness; deliberately a different implementation shape). **The campaign it
took to admit it is the strongest teeth evidence this ledger has:**

| Flight | Commit | Outcome |
|--------|--------|---------|
| 1 | `3b199a67` | **`BudgetExhausted`** at the 600 s ceiling (R4.6 working as specced): a `mutate-int-literal` mutant turned the proposal's position-advancing loop nonterminating and hung the in-session runner |
| 2 | `80a9edc4` | **`BudgetExhausted`** again: the first fix raced the brick's *returned* task, but a synchronous brick executes inside `MethodInfo.Invoke` — the race never started. Fixed by racing the whole invoke inside `Task.Run` |
| 3 | `56ad61ae` | **REJECT `mutation`**, `escape_rate=0.16`, 3 survivors — boundary-index equivalents the two-case witness could not distinguish (no case pinned an ERROR marker at the start of the text or of a line). 23.6 s |
| 4 | `9e6776c3` | **REJECT `mutation`**, `escape_rate=0.05`, 1 survivor — the leading-newline sentinel. 23.2 s |
| 5 | `bf8821db` | **PASS** — `AdmittedAndSwapped` as generation 1, `escape_rate=0` against the hardened witness, 23.5 s, 3/3 post-swap invocations, zero sessions leaked |

Flights 3–4 are the mutation gate REJECTING a model-produced candidate on a live engine with full
session containment — and the survivors named REAL witness gaps, fixed by contract-derived
hardening cases (edge-positioned markers; leading newline), not by fitting the witness to the
implementation. Flight 1 additionally proved the budget ceiling catches nonterminating mutants,
and produced the runner's per-execution timeout: a timed-out execution is now an observation
(judged as a killed mutant or rejected candidate), and the spinning thread dies with the runner
process — an isolation move the in-process path could never afford.

In-container suites after all of it: 66/66 (gate execution-seam facts, backend orchestration
facts, teeth, campaigns, harness, composition, watch window, digest).

### P6: LIVE model proposals at flight time

The last open leg, flown 2026-08-14: `run-first-flight.ps1 -Live` calls the LOCAL provider —
**ollama `codellama:7b`**, the seam the general-generation arc named as production
(`model:ollama:isolation-enforced`) — at flight time, with a committed witness-blind prompt
(objective + interface contract only), records the raw exchange to
`spikes/autonomy-first-flight/recordings/` (committed evidence artifacts), and mounts the
recording into the flight as a read-only input. The spike extracts the source mechanically and
does no validation beyond locating a class name: **the gate is the judge**, and every `-Live`
run is a fresh acceptance sample.

| Sample | Generation | Verdict |
|--------|-----------|---------|
| 1 (`live-…121237`) | 57 s | **REJECT `mutation`** — `escape_rate=0.08`; the `IndexOf + 5` offset mutant, trim-shadowed by every existing witness message (real witness gap #3) |
| 2 (`live-…121507`) | 8 s | **REJECT `mutation`** — same survivor against the five-case witness (hardening #3, the colon-adjacent case, was necessary but not sufficient) |
| 3 (`live-…121708`) | 7 s | **CERTIFIED — signed, `escape_rate=0` — then HELD by the swap host**: the model omitted the constructor, so the compiled brick self-declared an empty Id and verify-at-load refused the identity mismatch (`brick-id-mismatch`). Defense-in-depth demonstrated on a live candidate: the gate certifies, the host independently re-verifies |
| 4 (`live-…121844`) | 10 s | **PASS — `AdmittedAndSwapped`** as generation 1 in 13.3 s: signed, `escape_rate=0` against the six-case witness, full session containment, 3/3 post-swap invocations, zero sessions leaked |

**Measured live acceptance: 1/4 (0.25) — reported, not targeted** (the P3-S3 discipline). The
campaign forced two further contract-derived witness hardenings (the colon-adjacent marker; the
marker-terminal line, which kills the trim-shadowed offset class outright), and one prompt
reshape (skeleton-completion form — the constructor arrives verbatim, the model contributes the
behavior, which is exactly the A2.3 manifest-scaffold shape).

**The proposer-diversity observation, worth keeping:** every distinct implementation shape that
traversed this loop — hand-authored, recorded model, live model — surfaced a witness gap the
previous shapes could not express. Six witness cases now exist; four were demanded by the gate
rejecting real candidates. Proposer diversity is adversarial witness-hardening.

## S1: the first standing-loop sweep (objective file drives the loop)

Flown 2026-08-15 from `061c4f83` via `run-first-flight.ps1 -Sweep`. Everything before this
hand-constructed its candidate inside a spike; this is the first run where an objective FILE
in the store drove the loop end to end.

| Stage | Result |
|-------|--------|
| Objective read from `IObjectiveStore` | `tag-scan-classifier` (source=Human, priority=10) |
| Witness loaded (human-authored, sibling file) | 5 cases |
| Proposal loaded (live ollama `codellama:7b`, recorded) | witness-blind by construction |
| Session started + attested on the live daemon | `mcr.microsoft.com/dotnet/sdk:9.0` @ `sha256:35048e3a...` |
| Candidate compiled INSIDE the session | PASS, toolchain `9.0.317` |
| Witness judged | **REJECT at `correctness`, case 0** |
| Outcome | `ExplainedFailure`, session torn down, zero leaked containers, nothing swapped |

**What the rejection caught.** The model wrote the codec's `failureCode` straight to its
output; that value is `null` on the success path (`PhysicalAtomTagBinaryCodec.cs:113`) while
the contract and the witness's first case require the empty string. A witness authored from
the contract BEFORE the proposal existed, and never shown to the proposer, caught a real
defect in model-generated code under full session containment.

**Two defects the run exposed in our own work, both fixed:**

1. The first sweep failed earlier, at the in-session build, with `CS1056 Unexpected
   character '003c'`. The proposal-recording tool unescaped `
`, `\"` and `\` but not
   `XXXX`, so the stored source carried literal escape text. The MODEL's output was fine;
   the transcription was not. The loop caught it at the earliest possible gate with an exact
   diagnostic.
2. The rejection message read `expected  got ` - both sides empty, because
   `Convert.ToString` returns `""` for null and a `JsonElement` of kind Null stringifies to
   empty. The verdict was right and the feedback was useless. Since this text is the repair
   channel a proposer reads, it now renders `expected "" got <null>`.

### S2: repair loop to ADMIT — and two holes in the trust machinery it exposed

The rejection message from S1 was fed back to the model as repair input, and the loop
re-run after each fix. Full campaign, all under hold mode with full session containment:

| Run | Verdict | What it taught |
|-----|---------|----------------|
| S1 | REJECT `correctness` case 0 (`expected "" got <null>`) | the witness catches the model's null-vs-empty defect |
| repair | model returns `failureCode ?? string.Empty` — the one line at fault, nothing else | **the repair loop works on the correctness leg with a live model** |
| S2a | correctness PASSES; REJECT `mutation`, escape 0.20, survivor `mutate-string-literal-33` | a survivor no witness could kill — see below |
| fence triage | analyzer-dead mutants now count as kills (`BrickMutationEngine` runs the fence on SURVIVORS only) | mutants that could never certify were inflating escape rates |
| S2b | still REJECT `mutation`, same survivor | that mutant was not analyzer-dead — deeper |
| `$summary` | `WitnessObservableOutput` makes the summary witnessable under a reserved key at all three judge sites | the survivor was the SUMMARY literal, invisible to any witness |
| S2c | **ADMIT, `escape_rate=0` → `CertifiedButHeld`** | the model-repaired candidate is fully certified; hold refuses the swap |

**Two genuine holes in the trust machinery, found by getting to the bottom of one survivor:**

1. **Analyzer-dead mutants counted as escapes.** Mutants were judged only by the witness; the
   analyzer fence never ran on them. So a mutant that rewrites a declared key
   (`Set("firstMatchingLine")` → `"firstMatchingLinX"`, NEXO0002) — which no proposer could ever
   ship — was an "escape" no behavioural witness can observe. Fixed with precedent: a
   non-compiling mutant was already "dead on arrival"; a fence-rejected one is the same case at an
   earlier gate. Triage runs on survivors only and fails toward reporting the survivor.
   **This exposed that `GoodGeneration_WeakWitness_Rejects_WithTeeth` had fake teeth** — all seven of
   its surviving mutants were analyzer-dead; not one touched the logic the weak witness actually
   fails to observe (`firstMatchingLine ??= line`). The catalog had no operator for `??=`. Added
   `degrade-coalesce-assign` (`a ??= b` → `a = b`, "keep first" → "keep last", the buggy fixture's
   exact defect class); the weak witness now rejects for the RIGHT reason.
2. **The summary was unwitnessable.** `BrickOutput.ToDictionary()` excludes `Summary`, and every
   judge compared against the dictionary alone — so a mutated summary literal was unkillable by any
   witness expressible in the language. Reserved key `$summary`, projected identically at all three
   judge sites; the contract-conformance leg reads `ToDictionary()` directly so it can never register
   as an undeclared write.

Also fixed en route: the proposal recorder did not unescape `XXXX` (CS1056 at the in-session build —
the model's output was fine, the transcription was not), and witness failure messages rendered null
and empty identically (`expected  got `), which is useless as repair feedback.

**The design tension made concrete:** useful repair feedback necessarily leaks witness values to
the proposer (`expected ""` IS a witness value). Repair trades generation-blindness for
convergence one message at a time. That was the open question at the end of S2. S3 answers it.

### S3: the repair channel made policy — and measured

The tension is now resolved by construction: `RepairFeedbackPolicy` projects every rejection into
a proposer view at a chosen disclosure level — `CheckOnly` / `OwnOutput` (default) / `Full` — from
STRUCTURED witness findings, never by re-parsing prose. The default shows the proposer the failing
check, case index, key, and its OWN observed value; expected values appear only under `Full`, which
is opt-in and documented as weakening the certificate. Repairs are bounded per objective (default
2), then held for a human. Everything is model-independent configuration; `OllamaProposalSource`
adds prompt style, temperature, tokens and preamble as dials so the same loop serves a 7B local
model or a large hosted one.

**Then it was measured — twice, because the first measurement was too clean to trust.**

Hand-built ablation on `codellama:7b`, 3 samples/arm at temperature 0.2, repairing the S1
null-vs-empty defect:

| Prompt | Expected value shown | "NEVER null" in contract | Repaired |
|--------|----------------------|--------------------------|----------|
| default `OwnOutput` | no | yes | 3/3 |
| `Full` | yes | yes | 3/3 |
| default, contract softened | no | no | 0/3 |
| (13 earlier attempts, softened contract) | mixed | no | 0/13 |

**Redacting the expected value costs nothing**; the necessary ingredient was two words in the
CONTRACT ("NEVER null"). But the SHIPPED prompt — same disclosure, same contract — went 0/3
single-shot, and ~60 further model calls could not isolate a single semantic cause: the swing
between 0/3 and 3/3 tracked formatting noise (a blank line, "; the" vs ", and the",
"valid tag" vs "tag reference"). That is a property of a 7B model at low temperature, and the
loop must be robust to it rather than tuned to it.

So the number that matters is the one users get. Through the shipped code path — default
`OwnOutput` policy, `RepairWithContractOnly`, the loop's 2-attempt budget — **3/5 independent
objectives converge, at temperature 0.2 and at 0.7 alike.** Not the 3/3 of a lucky prompt, not
the 0/3 of an unlucky one. The retry budget is what turns "sometimes" into "usually", and it is
bounded, so a proposer still cannot binary-search the witness.

Three operating lessons, all now encoded as configuration rather than lore: (1) generation-
blindness is free — keep the default; (2) when a small model will not repair, sharpen the
CONTRACT before loosening the disclosure; (3) hand small models the contract alone, not the
objective narrative (`RepairWithContractOnly`, measured 3/3 → 0/3 the other way).

Worked example (objective, witness with `$summary` pinned, and the model-REPAIRED proposal):
`samples/autonomy-objectives/`.

### S4: dogfood campaign 1 — the loop meets five objectives it has never seen

Everything before this section was mechanism with a green gate behind it. What there was no
evidence for was the standing loop meeting more than one real objective. So: five human-authored
objectives with human-authored witnesses (`samples/autonomy-objectives/`) — a classifier, a state
machine, two parsers, and `text-slug`, under-specified ON PURPOSE (its contract never addresses
diacritics; the witness pins them) so the campaign could watch the repair channel hold rather
than converge on a witness the proposer cannot see. A LIVE `codellama:7b` composed INSIDE the
loop (so the loop's own policy-projected, bounded repair is what runs), hold admission on. Every
proposal and the exact projected feedback the model was handed are recorded per attempt
(`run-first-flight.ps1 -SweepLive`).

Four campaigns in an afternoon, each ~2–5 minutes for all five objectives, each fixing what the
previous one exposed:

| Campaign | What changed | Compiled | Judged by the witness | Certified (held) |
|---|---|---|---|---|
| 1 | baseline | 0/5 | 0 | 0 |
| 2 | build failures enter the repair channel | 1/5 (a `// TODO` stub) | 1 | 0 |
| 3 | build repairs carry the whole objective; brick-API operator preamble | 3/5 | 3 | 0 |
| 4 | authoring fixes: `event`→`trigger`, Summary stated, `$summary` worded | 2/5 | 2 | **1** |

**Mechanism findings (fixed as they surfaced):**

1. **Compile failures were terminal.** A session-build failure carried no `CertificationDecision`,
   and the loop repaired only when there was one — so a small model's dominant failure mode (a
   one-line C# slip: `output.Minor = …` for `output.Set("minor", …)`, a `Set(` without its
   receiver, a missing `using System.Linq`) ended the objective after one attempt. Compiler
   diagnostics are the safest feedback there is — they describe the candidate's own text, never
   the witness — and now they ride on `IterationResult.BuildDiagnostics` in the candidate's own
   line coordinates, projected by `RepairFeedback.RenderBuildFailure` under the same policy and
   attempt cap.
2. **Repair prompts stripped what a compile fix needs.** Contract-only repair (measured right for
   the null-vs-empty case) dropped the skeleton and API notes; a build repair now carries the whole
   objective (`RepairContext.Kind`).
3. **A 7B model does not know the brick API** and cannot derive it from diagnostics. The existing
   operator preamble knob now carries it as house rules (`proposer-preamble.md`) — data the
   proposer is handed, never a witness, the same knob a deployment would set. Compiled went 1/5 → 3/5.
4. **The reserved `$summary` key was opaque to a proposer** ("output['$summary'] was not
   produced", three attempts, no repair). It is now worded as "the output Summary (output.Summary)".

**Authoring findings — the campaign is as much about objectives as about the loop:** an input
named `event` is a C# keyword (the model's natural `var event = …` can never compile, and
"; expected" cannot tell a small model why); a witness that pins `$summary` needs a contract that
states the Summary. Both were ours. The contract-precision lesson from S3, again.

**What the model did, honestly.** `codellama:7b` re-emits its previous source almost verbatim on
repair — every objective, both repair kinds, all four campaigns; identical diagnostics at identical
lines. Feedback quality is not the constraint: the projections were precise and redaction held
throughout (`text-slug` saw its own `"café-olé"` and never `"cafe-ole"`, and the loop held). At 7B
the loop reaches the witness for 2–3 of 5 shapes and repair convergence is ~0 for anything beyond
a one-token fix; results also swing between runs (`semver-parse` compiled in campaign 3, not in 4).
The loop is model-agnostic by construction (`NEXO_OLLAMA_MODEL`); the next lever is the model.

**And the first end-to-end success:** in campaign 4 `door-lock-transition`'s FIRST proposal —
9.4 s from a 7B model, on an objective it had never seen — built in the attested session, passed
the analyzer fence, all eight witness cases, mutation (`escape_rate=0`) and determinism, and the
loop held it: `CertifiedButHeld — certified; the operator holds admission … with full evidence on
the record`. Recorded beside the objective as `door-lock-transition.proposal.json`. Nothing was
admitted, in four campaigns and 60 attempts, and every rejection says why.

### S5: dogfood campaign 2 — the same five objectives, three proposer models

S4 ended with a claim rather than a measurement: the loop is model-agnostic by construction, so
the next lever is the model and not the prompt. S5 tests exactly that. Everything else is pinned —
the same five objectives and witnesses, the same operator preamble, hold mode, the same 2-attempt
repair budget, the same attested session — and the only variable is `NEXO_OLLAMA_MODEL`. Two more
proposers, one campaign each, back to back on one box (`run-first-flight.ps1 -SweepLive -Models`).

| Proposer | Compiled | Judged by the witness | Certified (held) | Where the survivors failed | Repair behaviour | s/proposal |
|---|---|---|---|---|---|---|
| `codellama:7b` (S4, campaign 4) | 2/5 | 2 | 1 | compile | re-emits almost verbatim | 9.3 |
| `qwen2.5-coder:7b` | 3/5 | 3 | **2** | 2 compile, 1 correctness | byte-identical on 6/6 repairs | 9.4 |
| `qwen3.8:27b` | **5/5** | **5** | **3** | 1 mutation, 1 correctness | byte-identical on 3/4; one real edit | 117.7 |

**The lever was real, and it moves the thing S4 could not move.** Swapping the model — no prompt
change, no loop change — took certified-held from 1/5 to 2/5 to 3/5. `qwen3.8:27b` certified
`tag-scan-classifier`, `door-lock-transition` and `rgb-hex-parse`, each on its FIRST proposal, each
with `escape_rate=0`. `rgb-hex-parse` is the first parser to go the whole way and is recorded beside
its objective as `rgb-hex-parse.proposal.json`.

**The interesting result is not the count — it is that the failures moved down the pipeline.** At
7B the dominant failure mode is writing C# that compiles against an API the model was just handed:
`codellama` and `qwen2.5-coder` lose 2–3 objectives to `error CS1061` / `CS0103` before the witness
ever runs. At 27B that failure mode disappears outright — 5/5 compiled, and the build-repair path
introduced in S4 never fired once. What is left are the two failures the gate actually exists to
catch:

1. **`semver-parse` passed every correctness case and was rejected at `mutation`** —
   `escape_rate=0.04, survivors=[mutate-int-literal-41]`, three attempts running. That is a finding
   about OUR witness, not about the model: a mutant of the candidate that the witness does not kill.
   At this model size the binding constraint starts to shift from the competence of the proposer to
   the strength of the objectives, and the campaign starts testing us. This is the S3/S4
   contract-precision lesson arriving one layer deeper.
2. **`text-slug` failed at `correctness` on the plain cases**, not on the diacritics case it was
   authored to hold on (`"hello-world"` → `"helo-wrd"`, `"rock-roll"` → `"rock-l"` — the candidate
   eats repeated letters). So this run does not re-demonstrate S4's "held on a witness the proposer
   never saw"; it failed earlier, for an ordinary reason.

**Repair is still the weak channel, and size does not fix it.** This is the S4 finding surviving a
family change and a 4x parameter increase. `qwen2.5-coder:7b` re-emitted **byte-identical** source
on 6 of 6 repair attempts — same hash, same length, with populated feedback on the record every
time. `qwen3.8:27b` did so on 3 of 4; its single genuine edit (`text-slug`, 1631 → 1474 chars)
reproduced the identical failure. Every certified candidate in this campaign, at both sizes,
certified on attempt 1. Bounded repair remains worth its cost as a guard, but on local models it is
not where the wins come from — the first proposal is.

**Redaction held on a second and third model.** `text-slug`'s projected feedback showed the model
only its own output (`"café-ol"`); the expected `"cafe-ole"` appears in no recorded feedback
anywhere in the campaign. The S3 policy is now measured across three proposers of two families.

**Cost, honestly.** `qwen3.8:27b` is 18 GB q4 on a 12 GB card, so it runs partly on CPU: 117.7 s per
proposal against 9.4 s, and 1174.9 s of wall clock for the sweep against 171.9 s — 12.5x per
proposal, 6.8x per campaign. It needs fewer proposals (9 vs 11) because more objectives land first
try, which earns a little of that back. It also cannot be run naively: Qwen3-family models spend the
entire token budget reasoning and return an empty candidate unless `think: false` is sent, which is
what `OllamaProposalOptions.Think` and `-ThinkOff` exist for.

**Nothing was admitted.** Three campaigns, twenty proposals, hold mode throughout; every rejection
names its stage and says why.

## Settled decisions

- **`NetworkAccess.HostServicesOnly` stays a fail-closed refusal — permanently.** Every shipped
  backend refuses the mode rather than approximating it, and this is now the settled posture,
  not a v1 gap. The one workload that seemed to need it — package restore during the in-session
  candidate build — was solved **without** network (P3: offline restore from the SDK's installed
  packs, cleared sources), and a model server, the other imagined consumer, belongs on the
  proposer side of the boundary, not inside a certification session. The enum member stays
  declared (and `AllowedEndpoints` stays attestation-relevant) for a backend that can genuinely
  realize per-session egress rules; none is planned.

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

4. **Session containment is opt-in and covers compilation + execution; the host keeps
   orchestration, mutant compilation, and judgment.** With `BuildCandidateInSession=true`,
   candidates compile inside the attested session (`session-build` input); with
   `ExecuteCandidateInSession=true` additionally, the witness, determinism, and mutation
   legs EXECUTE candidate and mutant code inside that session (`session-execution` input)
   and untrusted candidate code never runs in the harness process. What deliberately stays
   on the host: iteration orchestration, Roslyn compilation of mutants (trusted tooling —
   the mutant never executes here), and ALL judgment — backends return raw observations
   and the gate compares, because a backend that judged its own executions could be lied
   to once and would then lie to the certificate forever. With both flags off (the
   default), sessions remain attestation-only and the in-process path is byte-identical
   to before. The **write** surface is confined separately, by `ProposerConfinement`'s
   single-declaration tool allowlist.

5. **Model proposing: CLOSED for the mechanism; the remaining boundary is scale.** Both
   legs have now flown: a recorded model proposal (P5) and a LIVE local-model proposal
   (P6 — ollama called at flight time, admitted on sample 4 with measured acceptance
   1/4). Every stage the spec demands has real evidence: model identity hash-bound in
   lineage, witness-blind prompting, full session containment, honest gate rejections,
   a swap-host identity hold, and an autonomous Tier-0 swap of live model output. What
   remains is breadth, not mechanism: one objective and one task family so far; a
   standing proposer loop over many objectives with acceptance tracked per lineage is
   host-operations work on seams that all exist.
