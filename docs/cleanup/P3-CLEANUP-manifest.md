# P3-CLEANUP manifest

Hygiene-only pass off `cursor/acceptance-rate-measurement-6118` (S3 head). No certification behavior changes.

## Pre-cleanup `--list-tests` (cert-gate filter, n=41)

```
    Nexo.Tests.Infrastructure.Tests.Certification.AstMutationEngineTests.CollectMutations_OnNonProbeShape_ProducesApplicableMutants
    Nexo.Tests.Infrastructure.Tests.Certification.AstMutationEngineTests.RunAsync_OnNonProbeShape_GeneratesMutantsAndEvaluates
    Nexo.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.GoodBrick_StrongWitness_Admits_WithZeroEscapeRate
    Nexo.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.BadWitnessBrick_Rejects_OnCorrectness
    Nexo.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.WeakWitness_AllowsMutantEscapes_RejectsWithTeeth
    Nexo.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.NondeterministicBrick_Rejects_OnDeterminism
    Nexo.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.UngatedBrick_IsRejectedByRegistryAdmissionPath
    Nexo.Tests.Infrastructure.Tests.Certification.CertificationGateTeethTests.CertifiedAdmission_OnlyExposesAdmittedBricks
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateMeasurementTests.RecordedBatch_EachEntryReproducesRecordedVerdictOnReplay
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateMeasurementTests.RecordedBatch_ComputedRateMatchesAdmitsOverTotal
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateMeasurementTests.ShortBatchGuard_RejectsTruncatedBatch
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionAcceptanceRateProtocolTests.RecordedBatch_HoldsExactlyDeclaredIndependentSamples
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.CorrectComposition_StrongWitness_Admits_WithZeroEscapeRate
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.BrokenComposition_StrongWitness_Rejects
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.CorrectComposition_WeakWitness_Rejects_WithStructuralTeeth
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.UncertifiedConstituent_Rejects_Constituents
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionCertificationGateTeethTests.NondeterministicComposition_Rejects_Determinism
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionDogfoodTests.HonestComposition_StrongWitness_Admits_WithZeroEscapeRate
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionDogfoodTests.BrokenComposition_StrongWitness_Rejects
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.CorrectProposal_StrongWitness_Admits_WithZeroEscapeRate
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.CorrectProposal_MatchesHandAuthoredCompositionResult
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "reordered-wiring", expectedFailureChecks: ["correctness", "mutation", "seam"])
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "dropped-brick", expectedFailureChecks: ["correctness", "mutation", "seam"])
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "type-mismatch-edge", expectedFailureChecks: ["seam", "correctness"])
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "hallucinated-dependency", expectedFailureChecks: ["constituents"])
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.BadProposalVariants_AreRejectedByExistingGate(variant: "uncertified-constituent", expectedFailureChecks: ["constituents"])
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerDogfoodTests.TamperedConstituentCert_RejectedByConstituentIntegrity
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerIndependenceTests.ProposerInput_StructurallyCannotCarryWitnessCases
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerIndependenceTests.CompositionGeneratorModel_InputPathCannotCarryWitnessCases
    Nexo.Tests.Infrastructure.Tests.Certification.CompositionProposerIndependenceTests.RealProposerPrompt_IsBuiltFromProposerInputOnly_WithNoWitnessValues
    Nexo.Tests.Infrastructure.Tests.Certification.CrossProjectReuseTests.HonestCertifiedBrick_ProjectB_TrustsAndRunsUntouched
    Nexo.Tests.Infrastructure.Tests.Certification.CrossProjectReuseTests.TamperedBrick_ProjectB_RejectsContentHashMismatch
    Nexo.Tests.Infrastructure.Tests.Certification.CrossProjectReuseTests.ForgedSignature_ProjectB_Rejects
    Nexo.Tests.Infrastructure.Tests.Certification.DamageResolverDogfoodTests.HonestCursorGeneration_Admits_WithZeroEscapeRate
    Nexo.Tests.Infrastructure.Tests.Certification.DamageResolverDogfoodTests.BuggyCursorGeneration_Rejects
    Nexo.Tests.Infrastructure.Tests.Certification.RealModelCompositionProposerDogfoodTests.RecordedRealProposal_TraversesIdenticalLoop_ReportsHonestGateVerdict
    Nexo.Tests.Infrastructure.Tests.Certification.RealModelCompositionProposerDogfoodTests.RecordedRealProposal_WhenAdmitted_MatchesHandAuthoredCompositionResult
    Nexo.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate
    Nexo.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.BuggyGeneration_StrongWitness_Rejects
    Nexo.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.GoodGeneration_WeakWitness_Rejects_WithTeeth
    Nexo.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.DependencyLeakGeneration_Rejects_DependencyCheck
```

**Excluded from cert-gate filter (intentional):** `Nexo.Tests.Infrastructure.Tests.LocalFixtures.*` (local fixture regeneration only).

## Post-cleanup `--list-tests`

**IDENTICAL** to pre-cleanup (41 tests, byte-for-byte same fully-qualified names). Verified via `diff` on `cert_gate_list_tests` output after cleanup.

## Files moved (Infrastructure → Nexo.Tests.Infrastructure)

| From | To |
|------|-----|
| `src/Nexo.Infrastructure/Certification/Composition/ControlledCompositionProposer.cs` | `src/Nexo.Tests.Infrastructure/Certification/Doubles/ControlledCompositionProposer.cs` |
| `src/Nexo.Infrastructure/Certification/Composition/RecordedCompositionGeneratorModel.cs` | `src/Nexo.Tests.Infrastructure/Certification/Doubles/RecordedCompositionGeneratorModel.cs` |
| `src/Nexo.Infrastructure/Certification/Composition/DamageHealthCompositionProposals.cs` | `src/Nexo.Tests.Infrastructure/Certification/Doubles/DamageHealthCompositionProposals.cs` |
| `src/Nexo.Infrastructure/Certification/Composition/CompositionProposalRecorder.cs` | `src/Nexo.Tests.Infrastructure/Certification/Doubles/CompositionProposalRecorder.cs` |
| `src/Nexo.Infrastructure/Certification/Composition/RecordedCompositionProposal.cs` | `src/Nexo.Tests.Infrastructure/Certification/Doubles/RecordedCompositionProposal.cs` |

Namespace: `Nexo.Tests.Infrastructure.Certification.Doubles` (was `Nexo.Infrastructure.Certification.Composition`).

## Files removed

| File | Reason | Archive tag |
|------|--------|-------------|
| `src/Nexo.Infrastructure/Certification/Composition/FixedProposalReplayProposer.cs` | Inlined as private nested type in `CompositionAcceptanceRateMeasurer` (identical logic; production replay, not a test double) | `archive/pre-cleanup-fixed-proposal-replay-proposer` |

## Other changes

- `docs/certification-evidence.md` — consolidated agent-composer layers (reorg only)
- `scripts/cert-gate-config.sh` — note on LocalFixtures exclusion
- `.gitignore` — `*.bak` pattern
