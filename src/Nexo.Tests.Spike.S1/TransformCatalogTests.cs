using FluentAssertions;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;
using Xunit;

namespace Nexo.Tests.Spike.S1;

public sealed class TransformCatalogTests
{
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
    public void Type_only_transform_removes_value_assertions()
    {
        var transformed = TransformCatalog.ApplyTestTransform(
            TransformTag.TypeOnlyAssert,
            HonestFixtures.Tests);

        transformed.Should().NotContain(".Should().Be(ColumnType.");
        transformed.Should().Contain("ColumnTypeInferrer.InferType(");
    }

    public static IEnumerable<object[]> WrongImplTags() =>
        TransformCatalog.WrongImplTags.Select(tag => new object[] { tag });

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

    [Fact]
    public void GenerateHonestBaseline_is_noop_transform()
    {
        var generator = new DefectInjectionGenerator();
        var baseline = generator.GenerateHonestBaseline(0);

        baseline.Tag.Should().Be(TransformTag.HonestNoOp);
        baseline.ImplementationSource.Should().Be(HonestFixtures.Implementation);
        baseline.TestSource.Should().Be(HonestFixtures.Tests);
    }
}

public sealed class EscapeRateTallyTests
{
    [Fact]
    public void Escape_rate_counts_only_adversarial_outcomes()
    {
        var adversarial = new List<CandidateOutcome>
        {
            new(TransformTag.OffByOne, TransformFamily.WrongImpl, 0, CandidateOutcomeKind.Escape, null),
            new(TransformTag.BoundaryInclusive, TransformFamily.WrongImpl, 0, CandidateOutcomeKind.Caught, null),
            new(TransformTag.ConstantReturn, TransformFamily.WrongImpl, 1, CandidateOutcomeKind.Caught, null)
        };
        var baseline = new List<CandidateOutcome>
        {
            new(TransformTag.HonestNoOp, TransformFamily.HonestBaseline, 0, CandidateOutcomeKind.Accepted, null),
            new(TransformTag.HonestNoOp, TransformFamily.HonestBaseline, 1, CandidateOutcomeKind.FalseReject, null)
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
        report.FalseRejectRate.Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void Per_transform_breakdown_is_attributable()
    {
        var outcomes = new List<CandidateOutcome>
        {
            new(TransformTag.OffByOne, TransformFamily.WrongImpl, 0, CandidateOutcomeKind.Escape, null),
            new(TransformTag.OffByOne, TransformFamily.WrongImpl, 1, CandidateOutcomeKind.Caught, null)
        };

        var row = EscapeRateTally.TallyTransform(outcomes, TransformTag.OffByOne);
        row.Total.Should().Be(2);
        row.Escapes.Should().Be(1);
        row.Caught.Should().Be(1);
        row.EscapeRate.Should().BeApproximately(0.5, 1e-9);
    }
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

    [Fact]
    public void Llm_mode_without_credentials_throws()
    {
        Environment.SetEnvironmentVariable("NEXO_S1_ADVERSARY", "llm");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        Environment.SetEnvironmentVariable("NEXO_LLM_API_KEY", null);

        var act = () => AdversarialGeneratorFactory.Create();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires*NEXO_S1_ADVERSARY=llm*");
    }
}
