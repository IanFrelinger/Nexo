using FluentAssertions;
using Nexo.Infrastructure.Certification.Composition;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CompositionAcceptanceRateProtocolTests
{
    private static readonly string RecordedBatchPath = Path.Combine(
        AppContext.BaseDirectory,
        "Certification", "Dogfood", "RecordedCompositionProposals",
        "damage-to-health-pipeline-batch.json");

    [Fact]
    public void RecordedBatch_HoldsExactlyDeclaredIndependentSamples()
    {
        var batch = RecordedCompositionProposalBatch.FromJson(File.ReadAllText(RecordedBatchPath));

        batch.Entries.Should().HaveCount(batch.DeclaredSampleCount,
            "batch must hold exactly N samples — no padding, no truncation");
        batch.Entries.Select(e => e.SequenceIndex).Should().BeEquivalentTo(
            Enumerable.Range(0, batch.DeclaredSampleCount),
            opts => opts.WithStrictOrdering());
        batch.Discards.Should().Be("none");
        batch.Temperature.Should().BeGreaterThan(0, "recording protocol uses temperature > 0 for variation");
    }
}
