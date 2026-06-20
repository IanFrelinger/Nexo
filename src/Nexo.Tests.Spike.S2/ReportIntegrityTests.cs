using FluentAssertions;
using Nexo.Spike.S2;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Adversary.Llm;
using Nexo.Spike.S2.Reporting;
using Xunit;

namespace Nexo.Tests.Spike.S2;

public sealed class ReportIntegrityTests
{
    [Fact]
    public void PromoteToCanonical_throws_when_non_vacuity_not_proven()
    {
        var vacuous = BuildReport(
            AdaptiveAdversaryFactory.MockMode,
            trueEscapeCount: 0,
            benignPassCount: 0,
            rejectedCount: 3);

        vacuous.NonVacuityProven.Should().BeFalse();

        var act = () => AdaptiveEscapeReportPublisher.PromoteToCanonical(
            vacuous,
            Path.Combine(Path.GetTempPath(), "nexo-s2-should-not-write"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*vacuous*");
    }

    [Fact]
    public void PromoteToCanonical_throws_for_scripted_stand_in_even_when_non_vacuity_proven()
    {
        var standIn = BuildReport(
            AdaptiveAdversaryFactory.ScriptedStandInMode,
            trueEscapeCount: 2,
            benignPassCount: 3,
            rejectedCount: 3);

        standIn.NonVacuityProven.Should().BeTrue();

        var act = () => AdaptiveEscapeReportPublisher.PromoteToCanonical(
            standIn,
            Path.Combine(Path.GetTempPath(), "nexo-s2-should-not-write"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be promoted*");
    }

    [Fact]
    public async Task PublishRunAsync_writes_invalid_marker_and_skips_canonical_when_vacuous()
    {
        var root = Path.Combine(Path.GetTempPath(), $"nexo-s2-vacuous-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var vacuous = BuildReport(
                AdaptiveAdversaryFactory.LlmMode,
                trueEscapeCount: 0,
                benignPassCount: 0,
                rejectedCount: 8,
                modelId: "gpt-test");

            var published = await AdaptiveEscapeReportPublisher.PublishRunAsync(
                vacuous,
                root,
                attemptCanonicalPromotion: true);

            published.Status.Should().Be(AdaptiveEscapeRunStatus.InvalidVacuous);
            published.PromotedToCanonical.Should().BeFalse();
            File.Exists(Path.Combine(published.RunDirectory, AdaptiveEscapeReportPublisher.InvalidMarkerFileName))
                .Should().BeTrue();
            File.Exists(Path.Combine(root, AdaptiveEscapeReportPublisher.CanonicalReportFileName))
                .Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                try { Directory.Delete(root, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    [Fact]
    public async Task PublishRunAsync_promotes_mock_non_vacuity_run_to_canonical()
    {
        var root = Path.Combine(Path.GetTempPath(), $"nexo-s2-canonical-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var valid = BuildReport(
                AdaptiveAdversaryFactory.MockMode,
                trueEscapeCount: 1,
                benignPassCount: 1,
                rejectedCount: 1);

            var published = await AdaptiveEscapeReportPublisher.PublishRunAsync(valid, root);

            published.PromotedToCanonical.Should().BeTrue();
            File.Exists(Path.Combine(root, AdaptiveEscapeReportPublisher.CanonicalReportFileName))
                .Should().BeTrue();
            File.Exists(Path.Combine(root, AdaptiveEscapeReportPublisher.CanonicalFindingsFileName))
                .Should().BeTrue();
            File.Exists(Path.Combine(published.RunDirectory, AdaptiveEscapeReportPublisher.InvalidMarkerFileName))
                .Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                try { Directory.Delete(root, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    [Fact]
    public async Task Stub_llm_preflight_writes_invalid_run_and_not_canonical()
    {
        var root = Path.Combine(Path.GetTempPath(), $"nexo-s2-stub-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var priorKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var priorModel = Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL");

        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXO_LLM_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXO_S2_LLM_MODEL", null);

            var act = () => LlmRunPreflight.EnsureProviderConfigured();
            act.Should().Throw<InvalidOperationException>();

            var published = await AdaptiveEscapeReportPublisher.PublishStubLlmFailureAsync(
                root,
                "LLM API key is not set.",
                modelHint: null);

            published.Status.Should().Be(AdaptiveEscapeRunStatus.InvalidStubLlm);
            published.PromotedToCanonical.Should().BeFalse();
            File.Exists(Path.Combine(published.RunDirectory, AdaptiveEscapeReportPublisher.InvalidMarkerFileName))
                .Should().BeTrue();
            File.Exists(Path.Combine(root, AdaptiveEscapeReportPublisher.CanonicalReportFileName))
                .Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", priorKey);
            Environment.SetEnvironmentVariable("NEXO_S2_LLM_MODEL", priorModel);

            if (Directory.Exists(root))
            {
                try { Directory.Delete(root, recursive: true); }
                catch { /* best-effort */ }
            }
        }
    }

    [Fact]
    public void RenderFindings_includes_correction_note_for_canonical()
    {
        var report = BuildReport(AdaptiveAdversaryFactory.MockMode, 1, 1, 1);
        var md = AdaptiveEscapeReportWriter.RenderFindings(report, AdaptiveEscapeFindingsOptions.ForCanonical());
        md.Should().Contain("## CORRECTION");
        md.Should().Contain("empty stub");
    }

    [Fact]
    public void RenderFindings_includes_scripted_stand_in_banner()
    {
        var report = BuildReport(AdaptiveAdversaryFactory.ScriptedStandInMode, 2, 3, 3);
        var md = AdaptiveEscapeReportWriter.RenderFindings(
            report,
            AdaptiveEscapeFindingsOptions.ForScriptedStandInRun(AdaptiveEscapeRunStatus.Valid));
        md.Should().Contain("Scripted stand-in (non-adaptive)");
        md.Should().Contain("no novel signal");
    }

    private static AdaptiveEscapeReport BuildReport(
        string backend,
        int trueEscapeCount,
        int benignPassCount,
        int rejectedCount,
        string? modelId = null)
    {
        var total = trueEscapeCount + benignPassCount + rejectedCount;
        return new AdaptiveEscapeReport(
            AdaptiveEscapeReport.Version,
            "s2.0-v1",
            backend,
            modelId,
            new EffortBudget(1, total),
            AdaptiveEscapeHarness.ScopeNote,
            total == 0 ? 0 : (double)trueEscapeCount / total,
            total == 0 ? 0 : (double)benignPassCount / total,
            total == 0 ? 0 : (double)rejectedCount / total,
            trueEscapeCount,
            benignPassCount,
            rejectedCount,
            trueEscapeCount > 0 ? [1] : [null],
            [],
            []);
    }
}
