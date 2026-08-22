using FluentAssertions;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Core.Application.Generation;
using Ashlar.Core.Domain.Bricks.Ports;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Generation-side analyzer fence (the deferred half of the analyzer-gate series): the
/// proposer gets the certification gate's verbatim feedback during repair iterations,
/// while the authority stays with certification — this validator only makes candidates
/// better before they get there.
/// </summary>
[Trait("Category", "Certification")]
public sealed class AnalyzerFencePostValidatorTests
{
    private static readonly string[] ContractReferences =
    {
        typeof(Ashlar.Core.Domain.Bricks.Brick).Assembly.Location,
    };

    [Fact]
    public async Task DirtyCandidate_GetsTheGateFeedback_DuringGeneration()
    {
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = ["DateTime.Now"] };
        var validator = new AnalyzerFencePostValidator(ContractReferences, manifest);
        var dirty = MutationProbeBrickSource.Code.Replace(
            "var errorCount = errorLines.Count;",
            "var errorCount = errorLines.Count; var t = DateTime.Now; if (t.Ticks < 0) errorLines.Clear();");

        var (ok, reason) = await validator.ValidateAsync(
            new GeneratedArtifact { Content = dirty }, CancellationToken.None);

        ok.Should().BeFalse();
        reason.Should().Contain("ASHLAR0006", "the static catalog speaks during generation too")
            .And.Contain(manifest.ForbiddenApiInstruction("DateTime.Now"),
                "the manifest instruction is restated verbatim, same as at certification");
    }

    [Fact]
    public async Task CleanCandidate_PassesWithTouchSetAttached()
    {
        var validator = new AnalyzerFencePostValidator(
            ContractReferences,
            touchSet: new TouchSet { Namespaces = ["Ashlar.Tests.Infrastructure.Certification.Fixtures"] });

        var (ok, reason) = await validator.ValidateAsync(
            new GeneratedArtifact { Content = MutationProbeBrickSource.Code }, CancellationToken.None);

        ok.Should().BeTrue(reason);
    }

    [Fact]
    public async Task EmptyArtifact_IsRejectedBeforeAnyCompile()
    {
        var validator = new AnalyzerFencePostValidator(ContractReferences);

        var (ok, reason) = await validator.ValidateAsync(
            new GeneratedArtifact { Content = "" }, CancellationToken.None);

        ok.Should().BeFalse();
        reason.Should().Contain("empty");
    }
}
