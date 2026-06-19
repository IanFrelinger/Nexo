using FluentAssertions;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.IntentDensity;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;
using Xunit;

namespace Nexo.Tests.Spike.S1;

public sealed class TransformCatalogTests
{
    [Fact]
    public void Catalog_has_versioned_identifier()
    {
        TransformCatalog.CatalogVersion.Should().Be("s1.3-v1");
        ProbeCorpus.ProbeCorpusVersion.Should().Be("s1.3-v1");
        TransformAttribution.All.Should().ContainKey(TransformTag.SemanticBooleanYesNo);
    }

    [Theory]
    [MemberData(nameof(WrongImplTags))]
    public void Impl_transforms_change_source_except_honest_noop(TransformTag tag)
    {
        var source = HonestFixtures.Implementation;
        var transformed = TransformCatalog.ApplyImplTransform(tag, source, 0);

        if (tag == TransformTag.HonestNoOp)
            transformed.Should().Be(source);
        else
            transformed.Should().NotBe(source);
    }

    [Theory]
    [MemberData(nameof(WrongImplTags))]
    public void Different_seeds_yield_distinct_impl_candidates(TransformTag tag)
    {
        var seed0 = TransformCatalog.ApplyImplTransform(tag, HonestFixtures.Implementation, 0);
        var seed1 = TransformCatalog.ApplyImplTransform(tag, HonestFixtures.Implementation, 1);
        seed0.Should().NotBe(seed1, because: $"seed perturbation for {tag}");
    }

    [Fact]
    public void Same_seed_yields_identical_wrong_impl_candidates()
    {
        var generator = new DefectInjectionGenerator();
        var first = generator.GenerateWrongImplCandidates(4);
        var second = generator.GenerateWrongImplCandidates(4);
        first.Should().BeEquivalentTo(second);
    }

    [Theory]
    [MemberData(nameof(SemanticWrongImplTags))]
    public void Semantic_transforms_have_attribution_metadata(TransformTag tag)
    {
        var definition = TransformAttribution.Get(tag);
        definition.Hypothesis.Should().NotBeNullOrWhiteSpace();
        definition.ExpectedRelation.Should().NotBeNullOrWhiteSpace();
        TransformCatalog.BuildImplDiff(tag, 0).Should().NotBe("(no diff)");
    }

    [Theory]
    [MemberData(nameof(WeakTestTags))]
    public void Test_transforms_change_assertions_except_honest_noop(TransformTag tag)
    {
        var source = HonestFixtures.Tests;
        var transformed = TransformCatalog.ApplyTestTransform(tag, source, 0);

        if (tag == TransformTag.HonestNoOp)
            transformed.Should().Be(source);
        else
            transformed.Should().NotBe(source);
    }

    [Fact]
    public void Type_only_assert_checks_enum_membership_not_value()
    {
        var transformed = TransformCatalog.ApplyTestTransform(
            TransformTag.TypeOnlyAssert,
            HonestFixtures.Tests,
            2);

        transformed.Should().Contain("BeOneOf(Enum.GetValues<ColumnType>())");
        transformed.Should().Contain("using System;");
        transformed.Should().NotBe(TransformCatalog.ApplyTestTransform(TransformTag.AssertionRemoved, HonestFixtures.Tests, 2));
    }

    [Fact]
    public void Semantic_heterogeneous_fallback_targets_final_return_only()
    {
        var transformed = TransformCatalog.ApplyImplTransform(
            TransformTag.SemanticHeterogeneousFallback,
            HonestFixtures.Implementation,
            0);

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
    public void Distinct_trial_count_equals_catalog_times_seeds()
    {
        var generator = new DefectInjectionGenerator();
        var candidates = generator.GenerateWrongImplCandidates(8);
        var distinct = candidates.Select(c => (c.Tag, c.ImplementationSource)).Distinct().Count();
        distinct.Should().Be(TransformCatalog.WrongImplTags.Count * 8);
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
    public void Catalog_version_ratchet_baseline_is_keyed()
    {
        var baselinePath = FindRepoFile("artifacts/s1/escape-rate-baseline.json");
        var baseline = File.ReadAllText(baselinePath);

        baseline.Should().Contain(ProbeCorpus.ProbeCorpusVersion);
        baseline.Should().Contain("catalogVersions");
        baseline.Should().Contain("intentDensity");
    }

    private static CandidateOutcome Outcome(
        TransformTag tag,
        TransformFamily family,
        CandidateOutcomeKind kind) =>
        new(tag, family, 0, kind, null, "hypothesis", "caught", "missing", "diff");

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
}

public sealed class IntentDensityTests
{
    [Fact]
    public void Probe_corpus_includes_known_escape_and_control_classes()
    {
        var ids = ProbeCorpus.All.Select(p => p.Id).ToHashSet();
        ids.Should().Contain("zero-one-literals");
        ids.Should().Contain("whitespace-only");
        ids.Should().Contain("scientific-notation");
        ids.Should().Contain("heterogeneous-fallback");
        ProbeCorpus.All.Should().HaveCount(12);
    }

    [Fact]
    public void Certification_gate_marks_sparse_spec_not_certifiable()
    {
        var probes = new List<ProbeClassResult>
        {
            Probe("leading-zeros", ProbePinStatus.Unpinned),
            Probe("scientific-notation", ProbePinStatus.Pinned)
        };

        var result = CertificationGate.Evaluate(0.5, probes, threshold: 0.95);
        result.Verdict.Should().Be(CertificationVerdict.NotCertifiable);
        result.Message.Should().Contain("sparse");
        result.UnpinnedClasses.Should().Contain("leading-zeros");
    }

    [Fact]
    public void Certification_gate_lists_unpinned_as_out_of_scope_when_above_threshold()
    {
        var probes = Enumerable.Range(0, 10)
            .Select(i => Probe($"p{i}", ProbePinStatus.Pinned))
            .Append(Probe("gap", ProbePinStatus.Unpinned))
            .ToList();

        var result = CertificationGate.Evaluate(0.91, probes, threshold: 0.9);
        result.Verdict.Should().Be(CertificationVerdict.CertifiableWithScope);
        result.OutOfScopeClasses.Should().Contain("gap");
    }

    [Fact]
    public void Certification_gate_marks_fully_pinned_spec_certifiable()
    {
        var probes = ProbeCorpus.All
            .Select(p => Probe(p.Id, ProbePinStatus.Pinned))
            .ToList();

        var result = CertificationGate.Evaluate(1.0, probes, threshold: 0.95);
        result.Verdict.Should().Be(CertificationVerdict.Certifiable);
        result.UnpinnedClasses.Should().BeEmpty();
    }

    [Fact]
    public void All_probe_classes_expected_pinned_after_s1_3_densification()
    {
        ProbeCorpus.All.Should().OnlyContain(p => p.ExpectedPinned);
    }

    private static ProbeClassResult Probe(string id, ProbePinStatus status) =>
        new(id, id, status, status == ProbePinStatus.Pinned ? "acceptance" : "silent", TransformTag.OffByOne, true, status == ProbePinStatus.Unpinned);
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
