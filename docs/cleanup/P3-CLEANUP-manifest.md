# P3-CLEANUP manifest

Hygiene-only pass off `cursor/acceptance-rate-measurement-6118` (S3 head). No certification behavior changes.

## Pre-cleanup `--list-tests` (cert-gate filter, n=41)

```
    Ashlar.Tests.Infrastructure.Tests.Certification.AstMutationEngineTests.CollectMutations_OnNonProbeShape_ProducesApplicableMutants
    Ashlar.Tests.Infrastructure.Tests.Certification.AstMutationEngineTests.RunAsync_OnNonProbeShape_GeneratesMutantsAndEvaluates
    Ashlar.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.GoodBrick_StrongWitness_Admits_WithZeroEscapeRate
    Ashlar.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.BadWitnessBrick_Rejects_OnCorrectness
    Ashlar.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.WeakWitness_AllowsMutantEscapes_RejectsWithTeeth
    Ashlar.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.NondeterministicBrick_Rejects_OnDeterminism
    Ashlar.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.UngatedBrick_IsRejectedByRegistryAdmissionPath
    Ashlar.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.CertifiedAdmission_OnlyExposesAdmittedBricks
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateMeasurementTests.RecordedBatch_EachEntryReproducesRecordedVerdictOnReplay
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateMeasurementTests.RecordedBatch_ComputedRateMatchesAdmitsOverTotal
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateMeasurementTests.ShortBatchGuard_RejectsTruncatedBatch
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateProtocolTests.RecordedBatch_HoldsExactlyDeclaredIndependentSamples
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.CorrectComposition_StrongWitness_Admits_WithZeroEscapeRate
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.BrokenComposition_StrongWitness_Rejects
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.CorrectComposition_WeakWitness_Rejects_WithStructuralTeeth
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.UncertifiedConstituent_Rejects_Constituents
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.NondeterministicComposition_Rejects_Determinism
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionDogfoodTests.HonestComposition_StrongWitness_Admits_WithZeroEscapeRate
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionDogfoodTests.BrokenComposition_StrongWitness_Rejects
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.CorrectProposal_StrongWitness_Admits_WithZeroEscapeRate
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.CorrectProposal_MatchesHandAuthoredCompositionResult
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "reordered-wiring", expectedFailureChecks: ["correctness", "mutation", "seam"])
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "dropped-brick", expectedFailureChecks: ["correctness", "mutation", "seam"])
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "type-mismatch-edge", expectedFailureChecks: ["seam", "correctness"])
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "hallucinated-dependency", expectedFailureChecks: ["constituents"])
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "uncertified-constituent", expectedFailureChecks: ["constituents"])
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.TamperedConstituentCert_RejectedByConstituentIntegrity
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerIndependenceTests.ProposerInput_StructurallyCannotCarryWitnessCases
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerIndependenceTests.CompositionGeneratorModel_InputPathCannotCarryWitnessCases
    Ashlar.Tests.Infrastructure.Tests.Certification.CompositionProposerIndependenceTests.RealProposerPrompt_IsBuiltFromProposerInputOnly_WithNoWitnessValues
    Ashlar.Tests.Infrastructure.Tests.Certification.CrossProjectReuseTests.HonestCertifiedBrick_ProjectB_TrustsAndRunsUntouched
    Ashlar.Tests.Infrastructure.Tests.Certification.CrossProjectReuseTests.TamperedBrick_ProjectB_RejectsContentHashMismatch
    Ashlar.Tests.Infrastructure.Tests.Certification.CrossProjectReuseTests.ForgedSignature_ProjectB_Rejects
    Ashlar.Tests.Infrastructure.Tests.Certification.DamageResolverDogfoodTests.HonestCursorGeneration_Admits_WithZeroEscapeRate
    Ashlar.Tests.Infrastructure.Tests.Certification.DamageResolverDogfoodTests.BuggyCursorGeneration_Rejects
    Ashlar.Tests.Infrastructure.Tests.Certification.RealModelCompositionProposerDogfoodTests.RecordedRealProposal_TraversesIdenticalLoop_ReportsHonestGateVerdict
    Ashlar.Tests.Infrastructure.Tests.Certification.RealModelCompositionProposerDogfoodTests.RecordedRealProposal_WhenAdmitted_MatchesHandAuthoredCompositionResult
    Ashlar.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate
    Ashlar.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.BuggyGeneration_StrongWitness_Rejects
    Ashlar.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.GoodGeneration_WeakWitness_Rejects_WithTeeth
    Ashlar.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.DependencyLeakGeneration_Rejects_DependencyCheck
```

**Excluded from cert-gate filter (intentional):** `Ashlar.Tests.Infrastructure.Tests.LocalFixtures.*` (local fixture regeneration only).

## Post-cleanup `--list-tests`

**IDENTICAL** to pre-cleanup (41 tests, byte-for-byte same fully-qualified names). Verified via `diff` on `cert_gate_list_tests` output after cleanup.

## Files moved (Infrastructure → Ashlar.Tests.Infrastructure)

| From | To |
|------|-----|
| `src/Ashlar.Infrastructure/Certification/Composition/ControlledCompositionProposer.cs` | `src/Ashlar.Tests.Infrastructure/Certification/Doubles/ControlledCompositionProposer.cs` |
| `src/Ashlar.Infrastructure/Certification/Composition/RecordedCompositionGeneratorModel.cs` | `src/Ashlar.Tests.Infrastructure/Certification/Doubles/RecordedCompositionGeneratorModel.cs` |
| `src/Ashlar.Infrastructure/Certification/Composition/DamageHealthCompositionProposals.cs` | `src/Ashlar.Tests.Infrastructure/Certification/Doubles/DamageHealthCompositionProposals.cs` |
| `src/Ashlar.Infrastructure/Certification/Composition/CompositionProposalRecorder.cs` | `src/Ashlar.Tests.Infrastructure/Certification/Doubles/CompositionProposalRecorder.cs` |
| `src/Ashlar.Infrastructure/Certification/Composition/RecordedCompositionProposal.cs` | `src/Ashlar.Tests.Infrastructure/Certification/Doubles/RecordedCompositionProposal.cs` |

Namespace: `Ashlar.Tests.Infrastructure.Certification.Doubles` (was `Ashlar.Infrastructure.Certification.Composition`).

## Files removed

| File | Reason | Archive tag |
|------|--------|-------------|
| `src/Ashlar.Infrastructure/Certification/Composition/FixedProposalReplayProposer.cs` | Inlined as private nested type in `CompositionAcceptanceRateMeasurer` (identical logic; production replay, not a test double) | `archive/pre-cleanup-fixed-proposal-replay-proposer` |

## Other changes

- `docs/certification-evidence.md` — consolidated agent-composer layers (reorg only)
- `scripts/cert-gate-config.sh` — note on LocalFixtures exclusion
- `.gitignore` — `*.bak` pattern
