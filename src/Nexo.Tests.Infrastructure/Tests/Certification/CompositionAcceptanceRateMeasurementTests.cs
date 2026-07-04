using FluentAssertions;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for composition acceptance rate measurement.</summary>
[Trait("Category", "Certification")]
public sealed class CompositionAcceptanceRateMeasurementTests
{
    private static readonly string RecordedBatchPath = Path.Combine(
        AppContext.BaseDirectory,
        "Certification", "Dogfood", "RecordedCompositionProposals",
        "damage-to-health-pipeline-batch.json");

    [Fact]
    public async Task RecordedBatch_EachEntryReproducesRecordedVerdictOnReplay()
    {
        var batch = RecordedCompositionProposalBatch.FromJson(File.ReadAllText(RecordedBatchPath));
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();
        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);

        var result = await CompositionAcceptanceRateMeasurer.MeasureRecordedBatchAsync(
            batch,
            proposerInput,
            witness,
            ctx.CompositionAdmission,
            wiringMetadata: "composition-wiring-v0");

        result.Entries.Should().HaveCount(batch.DeclaredSampleCount);
        foreach (var entry in result.Entries)
        {
            entry.ReplayAdmitted.Should().Be(entry.RecordedAdmitted,
                $"sequence {entry.SequenceIndex} must reproduce recorded gate verdict on replay");
        }
    }

    [Fact]
    public async Task RecordedBatch_ComputedRateMatchesAdmitsOverTotal()
    {
        var batch = RecordedCompositionProposalBatch.FromJson(File.ReadAllText(RecordedBatchPath));
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();
        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);

        var result = await CompositionAcceptanceRateMeasurer.MeasureRecordedBatchAsync(
            batch,
            proposerInput,
            witness,
            ctx.CompositionAdmission,
            wiringMetadata: "composition-wiring-v0");

        result.Admits.Should().Be(result.Entries.Count(e => e.ReplayAdmitted));
        result.Total.Should().Be(batch.DeclaredSampleCount);
        result.AcceptanceRate.Should().Be(result.Admits / (double)result.Total);
        CompositionAcceptanceRateMeasurer.FormatRate(result.AcceptanceRate)
            .Should().Be((result.Admits / (double)result.Total).ToString("F2"));
    }

    [Fact]
    public void ShortBatchGuard_RejectsTruncatedBatch()
    {
        var batch = RecordedCompositionProposalBatch.FromJson(File.ReadAllText(RecordedBatchPath));
        var truncated = batch with
        {
            Entries = batch.Entries.Take(3).ToList()
        };

        var act = () => CompositionAcceptanceRateGuard.EnsureBatchComplete(truncated);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*truncated*");
    }
}
