using FluentAssertions;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;
using Xunit;

namespace Nexo.Tests.Spike.S1;

public sealed class TransformCatalogTests
{
    [Fact]
    public void Catalog_has_versioned_identifier()
    {
        TransformCatalog.CatalogVersion.Should().Be("s1.1-v1");
        TransformAttribution.All.Should().ContainKey(TransformTag.SemanticBooleanYesNo);
    }

    [Theory]
    [MemberData(nameof(WrongImplTags))]
    public void Impl_transforms_change_source_except_honest_noop(TransformTag tag)
    {
        var source = HonestFixtures.Implementation;
        var transformed = TransformCatalog.ApplyImplTransform(tag, source);

        if (tag == TransformTag.HonestNoOp)
            transformed.Should().Be(source);
        else
            transformed.Should().NotBe(source);
    }

    [Theory]
    [MemberData(nameof(SemanticWrongImplTags))]
    public void Semantic_transforms_have_attribution_metadata(TransformTag tag)
    {
        var definition = TransformAttribution.Get(tag);
        definition.Hypothesis.Should().NotBeNullOrWhiteSpace();
        definition.ExpectedRelation.Should().NotBeNullOrWhiteSpace();
        TransformCatalog.BuildImplDiff(tag).Should().NotBe("(no diff)");
    }

    [Theory]
    [MemberData(nameof(WeakTestTags))]
    public void Test_transforms_change_assertions_except_honest_noop(TransformTag tag)
    {
        var source = HonestFixtures.Tests;
        var transformed = TransformCatalog.ApplyTestTransform(tag, source);

        if (tag == TransformTag.HonestNoOp)
            transformed.Should().Be(source);
        else
            transformed.Should().NotBe(source);
    }

    [Fact]
    public void Tautology_transform_compiles_as_self_referential_assertion()
    {
        var transformed = TransformCatalog.ApplyTestTransform(
            TransformTag.TautologyReplacement,
            HonestFixtures.Tests);

        transformed.Should().Contain("__t.Should().Be(__t)");
        transformed.Should().NotContain(".Should().Be(ColumnType.");
    }

    [Fact]
    public void Semantic_heterogeneous_fallback_targets_final_return_only()
    {
        var transformed = TransformCatalog.ApplyImplTransform(
            TransformTag.SemanticHeterogeneousFallback,
            HonestFixtures.Implementation);

        transformed.Should().Contain("nonEmpty.Any(v => int.TryParse(v, out _))");
        transformed.Should().Contain("if (values.Count == 0)");
    }

    public static IEnumerable<object[]> WrongImplTags() =>
        TransformCatalog.WrongImplTags.Select(tag => new object[] { tag });

    public static IEnumerable<object[]> SemanticWrongImplTags() =>
        TransformCatalog.SemanticWrongImplTags.Select(tag => new object[] { tag });

    public static IEnumerable<object[]> WeakTestTags() =>
        TransformCatalog.WeakTestTags.Select(tag => new object[] { tag });
}

public sealed class DefectInjectionGeneratorTests
{
    [Fact]
    public void GenerateWrongImplCandidates_is_catalog_times_seeds()
    {
        var generator = new DefectInjectionGenerator();
        var candidates = generator.GenerateWrongImplCandidates(3);

        candidates.Should().HaveCount(3 * TransformCatalog.WrongImplTags.Count);
        candidates.Select(c => (c.Seed, c.Tag)).Should().OnlyHaveUniqueItems();
        candidates.Should().OnlyContain(c => c.Family == TransformFamily.WrongImpl);
    }

    [Fact]
    public void GenerateWeakTestCandidates_preserves_honest_implementation()
    {
        var generator = new DefectInjectionGenerator();
        var candidates = generator.GenerateWeakTestCandidates(2);

        candidates.Should().OnlyContain(c =>
            c.ImplementationSource == HonestFixtures.Implementation &&
            c.Family == TransformFamily.WeakTest);
    }
}

public sealed class EscapeRateTallyTests
{
    [Fact]
    public void Escape_rate_counts_only_adversarial_outcomes()
    {
        var adversarial = new List<CandidateOutcome>
        {
            Outcome(TransformTag.OffByOne, TransformFamily.WrongImpl, CandidateOutcomeKind.Escape),
            Outcome(TransformTag.BoundaryInclusive, TransformFamily.WrongImpl, CandidateOutcomeKind.Caught),
            Outcome(TransformTag.ConstantReturn, TransformFamily.WrongImpl, CandidateOutcomeKind.Caught)
        };
        var baseline = new List<CandidateOutcome>
        {
            Outcome(TransformTag.HonestNoOp, TransformFamily.HonestBaseline, CandidateOutcomeKind.Accepted),
            Outcome(TransformTag.HonestNoOp, TransformFamily.HonestBaseline, CandidateOutcomeKind.FalseReject)
        };

        var report = EscapeRateTally.BuildDimensionReport(
            "PropertyGate",
            "completed",
            adversarial,
            baseline,
            TransformCatalog.WrongImplTags);

        report.Escapes.Should().Be(1);
        report.Caught.Should().Be(2);
        report.FalseRejects.Should().Be(1);
        report.EscapeRate.Should().BeApproximately(1.0 / 3.0, 1e-9);
    }

    [Fact]
    public void Threshold_sensitivity_records_first_escape_threshold()
    {
        var sweep = EscapeRateTally.BuildThresholdSensitivity(
            [60.0, 75.0, 90.0],
            new Dictionary<TransformTag, IReadOnlyList<CandidateOutcomeKind>>
            {
                [TransformTag.AssertionRemoved] =
                [
                    CandidateOutcomeKind.Escape,
                    CandidateOutcomeKind.Caught,
                    CandidateOutcomeKind.Caught
                ]
            });

        sweep.FirstEscapeThreshold["AssertionRemoved"].Should().Be(60.0);
        sweep.PerTransform["AssertionRemoved"][1].Escapes.Should().Be(0);
    }

    [Fact]
    public void Catalog_version_ratchet_baseline_is_keyed()
    {
        var baselinePath = FindRepoFile("artifacts/s1/escape-rate-baseline.json");
        var baseline = File.ReadAllText(baselinePath);

        baseline.Should().Contain(TransformCatalog.CatalogVersion);
        baseline.Should().Contain("catalogVersions");
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }

    private static CandidateOutcome Outcome(
        TransformTag tag,
        TransformFamily family,
        CandidateOutcomeKind kind) =>
        new(tag, family, 0, kind, null, "hypothesis", "caught", "missing", "diff");
}

public sealed class AdversarialGeneratorFactoryTests
{
    [Fact]
    public void Default_factory_returns_offline_generator()
    {
        Environment.SetEnvironmentVariable("NEXO_S1_ADVERSARY", null);
        var generator = AdversarialGeneratorFactory.Create();
        generator.Should().BeOfType<DefectInjectionGenerator>();
    }
}
