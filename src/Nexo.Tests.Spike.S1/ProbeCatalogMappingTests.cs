using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S1;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.IntentDensity;
using Nexo.Spike.S1.Reporting;
using Nexo.Spike.S1.Transforms;
using Xunit;

namespace Nexo.Tests.Spike.S1;

public sealed class ProbeCatalogMappingTests
{
    [Fact]
    public void Every_wrong_impl_transform_has_at_least_one_probe_class()
    {
        ProbeCatalogMapping.UnmappedTransformTags().Should().BeEmpty(
            because: "corpus must subsume catalog — see docs/spike/probe-catalog-mapping.md");
    }

    [Fact]
    public void Every_probe_class_maps_to_a_catalog_transform()
    {
        ProbeCatalogMapping.ProbeClassesWithoutCatalogTag().Should().BeEmpty();
    }

    [Fact]
    public void Catalog_has_eighteen_wrong_impl_tags_and_eighteen_probes()
    {
        TransformCatalog.WrongImplTags.Should().HaveCount(18);
        ProbeCorpus.All.Should().HaveCount(18);
        ProbeCorpus.All.Select(p => p.DivergentTransform).Should().OnlyHaveUniqueItems();
    }
}

public sealed class CertificationEquivalenceTests
{
    [Fact]
    public void Equivalence_holds_when_density_one_and_escapes_zero()
    {
        var result = CertificationEquivalence.Evaluate(Report(1.0), EscapeReport(0));
        result.EquivalenceHolds.Should().BeTrue();
    }

    [Fact]
    public void Equivalence_fails_when_density_one_but_escapes_positive()
    {
        CertificationEquivalence.Evaluate(Report(1.0), EscapeReport(3)).EquivalenceHolds.Should().BeFalse();
    }

    [Fact]
    public void Equivalence_fails_when_escapes_zero_but_density_below_one()
    {
        CertificationEquivalence.Evaluate(Report(0.5), EscapeReport(0)).EquivalenceHolds.Should().BeFalse();
    }

    private static IntentDensityReport Report(double intentDensity) =>
        new(
            IntentDensityReport.Version,
            ProbeCorpus.ProbeCorpusVersion,
            TransformCatalog.CatalogVersion,
            8,
            intentDensity,
            (int)(intentDensity * 18),
            18 - (int)(intentDensity * 18),
            18,
            0.95,
            new CertificationResult(
                intentDensity >= 0.95 ? CertificationVerdict.Certifiable : CertificationVerdict.NotCertifiable,
                intentDensity, 0.95, "test", [], [], []),
            null, null, []);

    private static EscapeRateReport EscapeReport(int escapes) =>
        new(
            EscapeRateReport.Version,
            TransformCatalog.CatalogVersion,
            "offline",
            8, 0, 1, 144, 0, 144, 0,
            new ToolAvailability(true, true, null),
            new DimensionReport("completed", "PropertyGate", 144, escapes, 144 - escapes, 0, 0,
                escapes / 144.0, 0, new Dictionary<string, TransformBreakdown>()),
            new DimensionReport("skipped", "MutationGate", 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, TransformBreakdown>()),
            null, [], []);
}

public sealed class NegativeControlStrippingTests
{
    [Fact]
    public void Strips_last_metamorphic_method_in_file()
    {
        var source = File.ReadAllText(ResolveMetamorphicFixture());
        NegativeControlStripping.TryRemoveMethod(
                source,
                "InferType_uses_full_column_not_prefix_when_suffix_widens_type",
                out var stripped)
            .Should().BeTrue();

        stripped.Should().NotContain("InferType_uses_full_column_not_prefix_when_suffix_widens_type");
        stripped.Should().Contain("Whitespace_only_cells_are_treated_as_empty");
    }

    [Fact]
    public void ApplyIfNeeded_removes_sampling_window_test_from_workspace()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"nexo-nc-{Guid.NewGuid():N}");
        try
        {
            SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: true);
            var intent = ResolveNegativeControlIntent();
            NegativeControlStripping.ApplyIfNeeded(workspace, intent);

            var metamorphicPath = Path.Combine(
                workspace,
                "CsvColumnInferrer.Properties",
                "MetamorphicPropertyTests.cs");
            File.ReadAllText(metamorphicPath)
                .Should().NotContain("InferType_uses_full_column_not_prefix_when_suffix_widens_type");
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                try { Directory.Delete(workspace, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    [Fact]
    public async Task Negative_control_sampling_window_pass_is_escape_not_vacuous_caught()
    {
        var harness = new EscapeRateHarness();
        var generator = AdversarialGeneratorFactory.Create();
        var candidate = generator.GenerateWrongImplCandidates(8)
            .First(c => c.Tag == TransformTag.SemanticSamplingWindow && c.Seed == 1);
        var workRoot = Path.Combine(Path.GetTempPath(), $"nexo-nc-escape-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workRoot);

        try
        {
            var outcome = await harness.EvaluateWrongImplAsync(
                candidate,
                workRoot,
                ResolveNegativeControlIntent(),
                CancellationToken.None);

            outcome.Kind.Should().Be(CandidateOutcomeKind.Escape);
        }
        finally
        {
            if (Directory.Exists(workRoot))
            {
                try { Directory.Delete(workRoot, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    private static string ResolveMetamorphicFixture() =>
        FindRepoFile(Path.Combine("samples", "spike-s0", "template",
            "CsvColumnInferrer.Properties", "MetamorphicPropertyTests.cs"));

    private static string ResolveNegativeControlIntent() =>
        FindRepoFile(Path.Combine("src", "Nexo.Spike.S1", "Fixtures",
            "honest-csv-inferrer-negative-control.json"));

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

        throw new FileNotFoundException($"Repo file not found: {relativePath}");
    }
}
