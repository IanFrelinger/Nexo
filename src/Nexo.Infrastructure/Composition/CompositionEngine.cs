using Nexo.Core.Application.Composition.Models;
using Nexo.Core.Application.Composition.Ports;

namespace Nexo.Infrastructure.Composition;

/// <summary>
/// Rule-based composition engine. Matches problem descriptions to component pipelines
/// using keyword rules. Uses registry to resolve available components.
/// </summary>
public sealed class CompositionEngine : ICompositionEngine
{
    private readonly ICapabilityComponentRegistry _registry;
    private static readonly string[] DefaultCapabilities = ["perception", "validation", "reporting", "understanding", "code-analysis"];

    /// <summary>
    /// Rules: (keywords that must match, component pipeline). First matching rule wins.
    /// Phase D: test-runner pipeline preferred when test-discovery/test-execution/result-aggregation available.
    /// </summary>
    private static readonly (string[] Keywords, string[] Pipeline)[] Rules =
    [
        (["test", "failure", "debug"], ["perception", "code-analysis", "validation", "reporting"]),
        (["test", "nexo"], ["test-discovery", "test-execution", "result-aggregation"]),
        (["test", "nexo"], ["perception", "validation", "reporting"]),
        (["test", "cli"], ["test-discovery", "test-execution", "result-aggregation"]),
        (["test", "cli"], ["perception", "validation", "reporting"]),
        (["test", "suite"], ["test-discovery", "test-execution", "result-aggregation"]),
        (["test"], ["test-discovery", "test-execution", "result-aggregation"]),
        (["test"], ["perception", "validation", "reporting"]),
        (["analyze", "code"], ["code-analysis", "validation", "reporting"]),
        (["analyze", "architecture"], ["code-analysis", "validation", "reporting"]),
        (["analyze"], ["code-analysis", "validation", "reporting"]),
        (["fix", "violation"], ["code-analysis", "understanding", "validation"]),
        (["adapt", "brick"], ["understanding", "code-analysis", "validation"]),
        (["improve"], ["code-analysis", "understanding", "validation"]),
        (["observe"], ["perception", "understanding"]),
        (["validate", "contract"], ["validation", "reporting"]),
        (["validate"], ["validation", "reporting"]),
        (["report", "summary"], ["reporting"]),
        (["compose", "agent"], ["understanding", "perception", "validation", "reporting"]),
    ];

    public CompositionEngine(ICapabilityComponentRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public Task<ComposedAgent?> ComposeAsync(string problemDescription, IReadOnlyList<string> availableCapabilities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var desc = problemDescription.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(desc))
            return Task.FromResult<ComposedAgent?>(null);

        var available = availableCapabilities.Count > 0
            ? availableCapabilities
            : DefaultCapabilities;
        var componentIds = ResolvePipeline(desc, available);
        if (componentIds.Count == 0)
            return Task.FromResult<ComposedAgent?>(null);

        return Task.FromResult<ComposedAgent?>(new ComposedAgent
        {
            Id = Guid.NewGuid().ToString("N"),
            ProblemDescription = problemDescription,
            ComponentIds = componentIds,
        });
    }

    private static IReadOnlyList<string> ResolvePipeline(string desc, IReadOnlyList<string> available)
    {
        var availableSet = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
        foreach (var (keywords, pipeline) in Rules)
        {
            if (keywords.All(k => desc.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                var filtered = pipeline.Where(id => availableSet.Contains(id)).ToList();
                if (filtered.Count > 0)
                    return filtered;
            }
        }
        var defaultPipeline = new[] { "perception", "validation", "reporting" };
        return defaultPipeline.Where(id => availableSet.Contains(id)).ToList();
    }

}
