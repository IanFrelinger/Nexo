using Ashlar.Tests.Infrastructure.Certification.Doubles;
using Ashlar.Tests.Infrastructure.Certification.Dogfood;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.LocalFixtures;

/// <summary>
/// Local-only helper to regenerate recorded batch fixtures. Not part of cert-gate.
/// </summary>
public sealed class CompositionAcceptanceRateBatchFixtureGeneratorTests
{
    [Fact(Skip = "Run locally to regenerate damage-to-health-pipeline-batch.json")]
    public async Task WriteDamageHealthBatchFixture()
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();
        var batch = await CompositionAcceptanceRateBatchBuilder.BuildDamageHealthBatchAsync(ctx, witness);

        var outputDir = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "Certification", "Dogfood", "RecordedCompositionProposals");
        var outputPath = Path.Combine(outputDir, "damage-to-health-pipeline-batch.json");
        await CompositionProposalRecorder.SaveBatchAsync(outputPath, batch);
    }
}
