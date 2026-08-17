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

    /// <summary>
    /// Every mutation carries the edit that produced it, not just a kind and a line. The id
    /// alone cannot distinguish a weak witness from an equivalent mutant, and ledger S5 spent
    /// two campaigns' worth of forensics on exactly that ambiguity.
    /// </summary>
    [Fact]
    public void CollectMutations_CarriesTheEditThatProducedEachMutant()
    {
        // The shape from ledger S5: a redundant length guard whose int-literal mutant
        // (0 => 2) is semantically equivalent, so no witness case could ever kill it.
        // Only ExecuteAsync (and private instance helpers) are in the mutation scope.
        const string source = """
            public sealed class SemverParseBrick
            {
                public bool ExecuteAsync(string version)
                {
                    bool isValid = false;
                    if (version.Length > 0)
                    {
                        isValid = version.Split('.').Length == 3;
                    }

                    return isValid;
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source);

        var guardMutant = mutations.Should().ContainSingle(m =>
            m.Id.StartsWith("mutate-int-literal", StringComparison.Ordinal)
            && m.Site.LineText.Contains("version.Length", StringComparison.Ordinal))
            .Subject;

        guardMutant.Id.Should().Be($"mutate-int-literal-{guardMutant.Site.Line}",
            "the id must keep encoding the line it points at");
        guardMutant.Site.OriginalText.Should().Be("0");
        guardMutant.Site.MutatedText.Should().Be("2");
        guardMutant.Site.LineText.Should().Be("if (version.Length > 0)",
            "an operator adjudicates a survivor from the line it landed on");
        guardMutant.Site.Column.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Survivors reach the caller with their sites, so a rejection can say WHAT changed.
    /// </summary>
    [Fact]
    public void MutationSurvivor_Describe_NamesLocationAndEdit()
    {
        var survivor = new MutationSurvivor(
            "mutate-int-literal-41",
            new MutationSite(41, 13, "0", "2", "if (version.Length > 0)"));

        var described = survivor.Describe();

        described.Should().Contain("mutate-int-literal-41");
        described.Should().Contain("line 41");
        described.Should().Contain("0 -> 2");
        described.Should().Contain("if (version.Length > 0)");
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
