using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Adaptation.Generation;
using Nexo.Infrastructure.Certification;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for ast mutation engine.</summary>
[Trait("Category", "Certification")]
public sealed class AstMutationEngineTests
{
    [Fact]
    public void CollectMutations_OnNonProbeShape_ProducesApplicableMutants()
    {
        var signature = new WitnessSignature(
            "line-substring-counter",
            [new WitnessIoField("text", "string"), new WitnessIoField("substring", "string")],
            [new WitnessIoField("matchCount", "int")]);

        var source = LineSubstringCounterSources.Correct(signature);
        source.Should().NotContain("errorCount");
        source.Should().NotContain("ErrorSummary");

        var mutations = AstMutationCatalog.CollectMutations(source);
        mutations.Should().NotBeEmpty("AST engine must derive mutants from arbitrary brick source");
        mutations.Select(m => m.Id).Should().Contain(id =>
            id.StartsWith("negate-condition", StringComparison.Ordinal)
            || id.StartsWith("remove-statement", StringComparison.Ordinal)
            || id.StartsWith("flip-binary-op", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_OnNonProbeShape_GeneratesMutantsAndEvaluates()
    {
        var signature = new WitnessSignature(
            "line-substring-counter",
            [new WitnessIoField("text", "string"), new WitnessIoField("substring", "string")],
            [new WitnessIoField("matchCount", "int")]);

        var source = LineSubstringCounterSources.Correct(signature);
        var witness = new WitnessSpec(
            "line-substring-counter",
            [
                new WitnessCase(
                    new Dictionary<string, object>
                    {
                        ["text"] = "FOO one\nplain\nFOO two",
                        ["substring"] = "FOO"
                    },
                    new Dictionary<string, object> { ["matchCount"] = 2 })
            ]);

        var engine = new BrickMutationEngine();
        var result = await engine.RunAsync(
            source,
            "Nexo.Certified.DamageResolver.LineSubstringCounterBrick",
            witness,
            CompilationReferences(),
            CancellationToken.None);

        result.TotalMutants.Should().BeGreaterThan(0, "zero-mutant guard requires applicable AST mutations");
    }

    /// <summary>Compilation references.</summary>
    private static List<string> CompilationReferences() =>
    [
        typeof(Nexo.Core.Domain.Bricks.Brick).Assembly.Location,
        typeof(Nexo.Core.Domain.Execution.BrickInput).Assembly.Location
    ];
}
