using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

/// <summary>
/// Synthesizes playtest feedback into actionable design changes.
/// </summary>
public sealed class FeedbackSynthesizerAgent : BaseAgent
{
    private readonly IModel _model;

    public FeedbackSynthesizerAgent(
        AgentSpawnSpec spec,
        IModel model,
        ILogger<BaseAgent> logger) : base(spec, logger)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing feedback synthesizer agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Dependencies resolved for {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Get playtest report from dependencies
        var report = dependencyOutputs?.Values
            .OfType<PlaytestReport>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No playtest report found");

        // Get original design documents
        var designDocs = dependencyOutputs?
            .Where(kvp => !kvp.Value.GetType().Name.Contains("Playtest"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>();

        // Generate change requests for each affected domain
        var changeRequests = new List<DesignChangeRequest>();

        foreach (var issue in report.Issues.Where(i => i.Severity >= BalanceSeverity.Moderate))
        {
            foreach (var domain in issue.AffectedDomains)
            {
                var changeRequest = await GenerateChangeRequestAsync(
                    issue,
                    domain,
                    designDocs,
                    cancellationToken);

                if (changeRequest != null)
                {
                    changeRequests.Add(changeRequest);
                }
            }
        }

        return new FeedbackSynthesis
        {
            SynthesisId = Guid.NewGuid().ToString(),
            SourceReportId = report.ReportId,
            GeneratedAt = DateTimeOffset.UtcNow,
            ChangeRequests = changeRequests,
            IterationRecommendation = GenerateIterationRecommendation(changeRequests)
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down feedback synthesizer agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    private async Task<DesignChangeRequest?> GenerateChangeRequestAsync(
        BalanceIssue issue,
        string domain,
        IDictionary<string, object> designDocs,
        CancellationToken cancellationToken)
    {
        var relevantDoc = designDocs
            .FirstOrDefault(kvp => kvp.Key.Contains(domain, StringComparison.OrdinalIgnoreCase))
            .Value;

        var fixesText = string.Join("\n", issue.SuggestedFixes.Select(f => $"- {f.Description} (confidence: {f.Confidence:P0})"));
        var designJson = relevantDoc != null 
            ? JsonSerializer.Serialize(relevantDoc, new JsonSerializerOptions { WriteIndented = true })
            : "{}";
        var jsonExample = $"{{\"domain\": \"{domain}\", \"changeType\": \"modify|add|remove\", \"targetElement\": \"specific element to change\", \"currentValue\": \"what it is now\", \"proposedValue\": \"what it should be\", \"rationale\": \"why this change addresses the issue\"}}";
        
        var prompt = $"""
            Given this balance issue:

            {issue.Title}
            {issue.Description}

            Suggested fixes:
            {fixesText}

            And this current design for {domain}:
            {designJson}

            Generate a specific design change request as JSON:
            {jsonExample}
            """;

        var output = await _model.CompleteAsync(new ModelInput(new[]
        {
            ("system", "You are a game designer translating balance feedback into design changes."),
            ("user", prompt)
        }), cancellationToken);

        return ParseChangeRequest(output.Text, issue.IssueId);
    }

    private DesignChangeRequest? ParseChangeRequest(string output, string issueId)
    {
        try
        {
            var json = output;
            if (json.Contains("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    json = json.Substring(start, end - start + 1);
                }
            }

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new DesignChangeRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                SourceIssueId = issueId,
                Domain = root.GetProperty("domain").GetString() ?? "",
                ChangeType = Enum.Parse<ChangeType>(root.GetProperty("changeType").GetString() ?? "Modify", true),
                TargetElement = root.GetProperty("targetElement").GetString() ?? "",
                CurrentValue = root.TryGetProperty("currentValue", out var cv) ? cv.GetString() : null,
                ProposedValue = root.GetProperty("proposedValue").GetString() ?? "",
                Rationale = root.GetProperty("rationale").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse change request");
            return null;
        }
    }

    private IterationRecommendation GenerateIterationRecommendation(IReadOnlyList<DesignChangeRequest> changes)
    {
        var criticalChanges = changes.Count(c => c.ChangeType == ChangeType.Modify);
        var newFeatures = changes.Count(c => c.ChangeType == ChangeType.Add);
        var removals = changes.Count(c => c.ChangeType == ChangeType.Remove);

        return new IterationRecommendation
        {
            ShouldIterate = changes.Any(),
            Priority = changes.Count > 5 ? IterationPriority.High : IterationPriority.Normal,
            EstimatedEffort = TimeSpan.FromHours(changes.Count * 2),
            Summary = $"Recommended {changes.Count} changes: {criticalChanges} modifications, {newFeatures} additions, {removals} removals"
        };
    }
}

/// <summary>
/// Result of feedback synthesis.
/// </summary>
public sealed record FeedbackSynthesis
{
    public required string SynthesisId { get; init; }
    public required string SourceReportId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<DesignChangeRequest> ChangeRequests { get; init; } = Array.Empty<DesignChangeRequest>();
    public IterationRecommendation? IterationRecommendation { get; init; }
}

/// <summary>
/// A request to change a design element.
/// </summary>
public sealed record DesignChangeRequest
{
    public required string RequestId { get; init; }
    public required string SourceIssueId { get; init; }
    public required string Domain { get; init; }
    public required ChangeType ChangeType { get; init; }
    public required string TargetElement { get; init; }
    public string? CurrentValue { get; init; }
    public required string ProposedValue { get; init; }
    public required string Rationale { get; init; }
}

/// <summary>
/// Recommendation for iteration.
/// </summary>
public sealed record IterationRecommendation
{
    public bool ShouldIterate { get; init; }
    public IterationPriority Priority { get; init; }
    public TimeSpan EstimatedEffort { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Types of design changes.
/// </summary>
public enum ChangeType { Modify, Add, Remove }

/// <summary>
/// Priority levels for iteration.
/// </summary>
public enum IterationPriority { Low, Normal, High, Critical }

