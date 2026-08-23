using Ashlar.Core.Application.Composition.Models;
using Ashlar.Core.Application.Composition.Ports;

namespace Ashlar.Infrastructure.Composition;

/// <summary>
/// Rule-based composition engine. Matches problem descriptions to component pipelines
/// using keyword rules. Uses registry to resolve available components.
/// </summary>
public sealed class CompositionEngine : ICompositionEngine
{
    private readonly ICapabilityComponentRegistry _registry;
    private static readonly string[] DefaultCapabilities = ["perception", "validation", "reporting", "understanding", "code-analysis"];

    /// <summary>
    /// Weighted intents used for adaptive composition. Multiple intents can match and contribute
    /// to the final pipeline, enabling mixed/nuanced requests instead of first-match behavior.
    /// </summary>
    private static readonly IntentPattern[] IntentPatterns =
    [
        // Strong testing/debug signal.
        new(
            "test-debug",
            AnchorKeywords: ["test", "debug", "failure", "flaky", "regression"],
            SignalKeywords: ["failure", "debug", "flaky", "regression", "error", "investigate"],
            Components: ["perception", "code-analysis", "validation", "reporting"],
            BaseWeight: 12),

        // Test-runner composition lane.
        new(
            "test-runner",
            AnchorKeywords: ["test", "suite", "cli", "ashlar", "matrix", "parallel"],
            SignalKeywords: ["test", "suite", "cli", "ashlar", "matrix", "parallel", "run", "execute"],
            Components: ["test-discovery", "test-execution", "result-aggregation", "reporting"],
            BaseWeight: 14),

        new(
            "analysis",
            AnchorKeywords: ["analyze", "analysis", "architecture", "quality"],
            SignalKeywords: ["analyze", "analysis", "code", "architecture", "quality", "static"],
            Components: ["code-analysis", "validation", "reporting"],
            BaseWeight: 10),

        new(
            "adaptation",
            AnchorKeywords: ["fix", "adapt", "improve", "violation"],
            SignalKeywords: ["fix", "adapt", "improve", "violation", "patch", "repair"],
            Components: ["understanding", "code-analysis", "validation", "reporting"],
            BaseWeight: 9),

        new(
            "observation",
            AnchorKeywords: ["observe", "watch", "monitor", "telemetry"],
            SignalKeywords: ["observe", "watch", "monitor", "telemetry", "signal"],
            Components: ["perception", "understanding", "reporting"],
            BaseWeight: 8),

        new(
            "validation",
            AnchorKeywords: ["validate", "contract", "check"],
            SignalKeywords: ["validate", "contract", "check", "conformance", "verify"],
            Components: ["validation", "reporting"],
            BaseWeight: 8),

        new(
            "compose",
            AnchorKeywords: ["compose", "agent", "pipeline"],
            SignalKeywords: ["compose", "agent", "pipeline", "orchestrate", "capability"],
            Components: ["understanding", "perception", "validation", "reporting"],
            BaseWeight: 7),
    ];

    /// <summary>
    /// Dependency edges used to keep composed pipelines coherent. This gives graph-style assembly
    /// while still outputting a linear executable order.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> ComponentDependencies =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["test-execution"] = ["test-discovery"],
            ["result-aggregation"] = ["test-execution"],
            ["validation"] = ["perception"],
            ["reporting"] = ["validation"]
        };

    /// <summary>
    /// Stable stage order for final execution pipeline.
    /// </summary>
    private static readonly string[] StageOrder =
    [
        "perception",
        "understanding",
        "code-analysis",
        "test-discovery",
        "test-execution",
        "validation",
        "result-aggregation",
        "reporting"
    ];

    /// <summary>Initializes a new composition engine.</summary>
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

        var validationIssues = _registry.ValidateSelection(componentIds, available);
        if (validationIssues.Count > 0)
        {
            var message = string.Join(
                "; ",
                validationIssues.Select(issue => $"{issue.Code} ({issue.ComponentId}): {issue.Message}"));
            throw new InvalidOperationException($"Composition validation failed: {message}");
        }

        return Task.FromResult<ComposedAgent?>(new ComposedAgent
        {
            Id = Guid.NewGuid().ToString("N"),
            ProblemDescription = problemDescription,
            ComponentIds = componentIds,
        });
    }

    private IReadOnlyList<string> ResolvePipeline(string desc, IReadOnlyList<string> available)
    {
        var availableSet = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
        var componentScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in IntentPatterns)
        {
            var score = ScoreIntent(desc, pattern);
            if (score <= 0)
                continue;

            for (var i = 0; i < pattern.Components.Length; i++)
            {
                var component = pattern.Components[i];
                if (!IsAvailableComponent(component, availableSet))
                    continue;

                // Early components in each pattern get a slight boost to preserve intentional ordering.
                var positionalBoost = (pattern.Components.Length - i) * 0.5d;
                componentScores[component] = componentScores.TryGetValue(component, out var existing)
                    ? existing + score + positionalBoost
                    : score + positionalBoost;
            }
        }

        if (componentScores.Count == 0)
        {
            var defaultPipeline = new[] { "perception", "validation", "reporting" };
            return defaultPipeline.Where(id => IsAvailableComponent(id, availableSet)).ToList();
        }

        var selected = new HashSet<string>(
            componentScores
                .Where(kv => kv.Value >= 6d)
                .Select(kv => kv.Key),
            StringComparer.OrdinalIgnoreCase);

        if (selected.Count == 0)
        {
            selected = new HashSet<string>(
                componentScores
                    .OrderByDescending(kv => kv.Value)
                    .Take(3)
                    .Select(kv => kv.Key),
                StringComparer.OrdinalIgnoreCase);
        }

        AddDependencies(selected, availableSet);
        return OrderPipeline(selected, componentScores);
    }

    private static double ScoreIntent(string desc, IntentPattern pattern)
    {
        var anchorHits = pattern.AnchorKeywords.Count(k => desc.Contains(k, StringComparison.OrdinalIgnoreCase));
        if (anchorHits == 0)
            return 0d;

        var signalHits = pattern.SignalKeywords.Count(k => desc.Contains(k, StringComparison.OrdinalIgnoreCase));
        return pattern.BaseWeight + (anchorHits * 4d) + (signalHits * 2d);
    }

    private void AddDependencies(HashSet<string> selected, HashSet<string> available)
    {
        var queue = new Queue<string>(selected);
        while (queue.Count > 0)
        {
            var component = queue.Dequeue();
            if (!ComponentDependencies.TryGetValue(component, out var deps))
                continue;

            foreach (var dep in deps)
            {
                if (!IsAvailableComponent(dep, available))
                    continue;
                if (!selected.Add(dep))
                    continue;
                queue.Enqueue(dep);
            }
        }
    }

    private static IReadOnlyList<string> OrderPipeline(HashSet<string> selected, Dictionary<string, double> scores)
    {
        var ordered = new List<string>();
        foreach (var stage in StageOrder)
        {
            if (selected.Remove(stage))
                ordered.Add(stage);
        }

        if (selected.Count > 0)
        {
            ordered.AddRange(
                selected
                    .OrderByDescending(id => scores.TryGetValue(id, out var score) ? score : 0d)
                    .ThenBy(id => id, StringComparer.OrdinalIgnoreCase));
        }

        return ordered;
    }

    private bool IsAvailableComponent(string componentId, HashSet<string> availableSet)
    {
        if (availableSet.Contains(componentId))
            return true;

        var descriptor = _registry.GetById(componentId);
        if (descriptor != null && availableSet.Contains(descriptor.Capability))
            return true;

        return false;
    }

    private sealed record IntentPattern(
        string Name,
        string[] AnchorKeywords,
        string[] SignalKeywords,
        string[] Components,
        double BaseWeight);
}
