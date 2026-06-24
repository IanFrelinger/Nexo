using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Certification.Doubles;
using Nexo.Tests.Infrastructure.Certification.Fixtures;

namespace Nexo.Tests.Infrastructure.Certification.Dogfood;

/// <summary>
/// Builds honest recorded batches by running proposals through the real gate (local capture only).
/// </summary>
internal static class CompositionAcceptanceRateBatchBuilder
{
    public const double RecordedTemperature = 0.7;
    public const int RecordedSampleCount = 5;
    public const string RecordedProvider = "cursor";
    public static readonly DateTimeOffset BatchRecordedAt = new(2026, 6, 23, 14, 30, 0, TimeSpan.Zero);

    public static async Task<RecordedCompositionProposalBatch> BuildDamageHealthBatchAsync(
        CertifiedCompositionTestContext ctx,
        CompositionWitnessSpec witness)
    {
        var target = Nexo.Core.Application.Certification.CompositionProposerInputBuilder.TargetFromSpec(
            CompositionDogfoodFixtures.HonestSpec());

        var proposals = new[]
        {
            DamageHealthCompositionProposals.Correct(target),
            DamageHealthCompositionProposals.Correct(target),
            DamageHealthCompositionProposals.ReorderedWiring(target),
            DamageHealthCompositionProposals.Correct(target),
            DamageHealthCompositionProposals.DroppedBrick(target)
        };

        var entries = new List<RecordedCompositionBatchEntry>();
        for (var i = 0; i < proposals.Length; i++)
        {
            var spec = proposals[i];
            var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
            {
                Spec = spec with { CompositionId = witness.CompositionId },
                Witness = witness,
                WiringMetadata = "composition-wiring-v0"
            }).ConfigureAwait(false);

            entries.Add(new RecordedCompositionBatchEntry(
                i,
                $"model:{RecordedProvider}:isolation-enforced",
                BatchRecordedAt.AddSeconds(i + 1),
                decision.Admitted,
                decision.Admitted ? "ADMIT" : "REJECT",
                decision.Admitted ? null : decision.FailureCheck,
                spec));
        }

        return new RecordedCompositionProposalBatch(
            RecordedProvider,
            RecordedTemperature,
            RecordedSampleCount,
            BatchRecordedAt,
            Discards: "none",
            entries);
    }
}
