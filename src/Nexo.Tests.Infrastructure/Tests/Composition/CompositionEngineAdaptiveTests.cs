using Nexo.Infrastructure.Composition;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Composition;

/// <summary>
/// Verifies adaptive, weighted composition behavior (multi-intent + dependency-aware ordering).
/// </summary>
public sealed class CompositionEngineAdaptiveTests
{
    [Fact]
    public async Task ComposeAsync_MultiIntentProblem_MergesPipelinesByScore()
    {
        var registry = new CapabilityComponentRegistry();
        var engine = new CompositionEngine(registry);
        var available = new[]
        {
            "perception",
            "validation",
            "reporting",
            "understanding",
            "code-analysis",
            "test-discovery",
            "test-execution",
            "result-aggregation"
        };

        var composed = await engine.ComposeAsync(
            "Debug flaky test failures in nexo suite and report a summary.",
            available);

        Assert.NotNull(composed);
        Assert.Contains("test-discovery", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("test-execution", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("result-aggregation", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("code-analysis", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("reporting", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);

        var discoveryIndex = IndexOf(composed.ComponentIds, "test-discovery");
        var executionIndex = IndexOf(composed.ComponentIds, "test-execution");
        var aggregationIndex = IndexOf(composed.ComponentIds, "result-aggregation");
        Assert.True(discoveryIndex < executionIndex, "test-discovery should precede test-execution");
        Assert.True(executionIndex < aggregationIndex, "test-execution should precede result-aggregation");
    }

    [Fact]
    public async Task ComposeAsync_AnalysisAndValidationProblem_PreservesStageOrdering()
    {
        var registry = new CapabilityComponentRegistry();
        var engine = new CompositionEngine(registry);
        var available = new[] { "perception", "validation", "reporting", "understanding", "code-analysis" };

        var composed = await engine.ComposeAsync(
            "Analyze architecture quality, validate contract checks, and provide a report summary.",
            available);

        Assert.NotNull(composed);
        Assert.Contains("code-analysis", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("validation", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("reporting", composed.ComponentIds, StringComparer.OrdinalIgnoreCase);

        var codeAnalysisIndex = IndexOf(composed.ComponentIds, "code-analysis");
        var validationIndex = IndexOf(composed.ComponentIds, "validation");
        var reportingIndex = IndexOf(composed.ComponentIds, "reporting");
        Assert.True(codeAnalysisIndex < validationIndex, "code-analysis should precede validation");
        Assert.True(validationIndex < reportingIndex, "validation should precede reporting");
    }

    [Fact]
    public async Task ComposeAsync_NoIntentMatch_FallsBackToDefaultPipeline()
    {
        var registry = new CapabilityComponentRegistry();
        var engine = new CompositionEngine(registry);
        var available = new[] { "perception", "validation", "reporting" };

        var composed = await engine.ComposeAsync("miscellaneous request with no strong keywords", available);

        Assert.NotNull(composed);
        Assert.Equal(new[] { "perception", "validation", "reporting" }, composed.ComponentIds);
    }

    private static int IndexOf(IReadOnlyList<string> values, string value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return int.MaxValue;
    }
}
