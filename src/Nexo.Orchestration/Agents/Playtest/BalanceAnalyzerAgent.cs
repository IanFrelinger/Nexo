using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using Nexo.Orchestration.Playtest.Ports;
using System.Text;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

/// <summary>
/// Analyzes playtest telemetry to identify balance issues.
/// 
/// Responsibilities:
/// - Analyzes playtest sessions from ITelemetryStore
/// - Identifies balance issues (overpowered/underpowered mechanics)
/// - Uses LLM (IModel) for advanced analysis
/// - Generates balance reports with severity ratings
/// - Provides recommendations for fixes
/// 
/// Used in automated playtesting to detect game balance problems.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public sealed class BalanceAnalyzerAgent : BaseAgent
{
    private readonly ITelemetryStore _telemetryStore;
    private readonly IModel _model;

    public BalanceAnalyzerAgent(
        AgentSpawnSpec spec,
        ITelemetryStore telemetryStore,
        IModel model,
        ILogger<BaseAgent> logger) : base(spec, logger)
    {
        _telemetryStore = telemetryStore ?? throw new ArgumentNullException(nameof(telemetryStore));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing balance analyzer agent: {AgentId}", Spec.AgentId);
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
        // Get playtest session from dependencies
        var sessions = dependencyOutputs?.Values
            .OfType<PlaytestSession>()
            .ToList() ?? new List<PlaytestSession>();

        if (!sessions.Any())
        {
            throw new InvalidOperationException("No playtest sessions found in dependencies");
        }

        var allIssues = new List<BalanceIssue>();

        foreach (var session in sessions)
        {
            // Load telemetry for this session
            var events = await _telemetryStore.GetEventsAsync(session.SessionId, cancellationToken);

            // Run statistical analysis
            var combatIssues = AnalyzeCombatBalance(events);
            var economyIssues = AnalyzeEconomyBalance(events);
            var progressionIssues = AnalyzeProgressionBalance(events);
            var performanceIssues = AnalyzePerformance(events);

            allIssues.AddRange(combatIssues);
            allIssues.AddRange(economyIssues);
            allIssues.AddRange(progressionIssues);
            allIssues.AddRange(performanceIssues);
        }

        // Use LLM to synthesize findings and suggest fixes
        var enrichedIssues = await EnrichWithSuggestionsAsync(allIssues, cancellationToken);

        return new PlaytestReport
        {
            ReportId = Guid.NewGuid().ToString(),
            SessionIds = sessions.Select(s => s.SessionId).ToList(),
            GeneratedAt = DateTimeOffset.UtcNow,
            Issues = enrichedIssues,
            Summary = await GenerateSummaryAsync(enrichedIssues, cancellationToken)
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down balance analyzer agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    private IReadOnlyList<BalanceIssue> AnalyzeCombatBalance(IReadOnlyList<TelemetryEvent> events)
    {
        var issues = new List<BalanceIssue>();

        // Time-to-kill analysis
        var killEvents = events.Where(e => e.EventType == TelemetryEventTypes.PlayerKilled).ToList();
        var damageEvents = events.Where(e => e.EventType == TelemetryEventTypes.PlayerDamaged).ToList();

        if (killEvents.Any())
        {
            // Calculate average TTK per weapon
            var weaponTTK = damageEvents
                .GroupBy(e => e.Data.GetValueOrDefault("weapon")?.ToString() ?? "unknown")
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(e => Convert.ToDouble(e.Data.GetValueOrDefault("ttk", 0))));

            foreach (var (weapon, ttk) in weaponTTK)
            {
                if (ttk < 0.3) // Too fast
                {
                    issues.Add(new BalanceIssue
                    {
                        IssueId = Guid.NewGuid().ToString(),
                        Category = BalanceCategory.Combat,
                        Severity = BalanceSeverity.Major,
                        Title = $"TTK too low for {weapon}",
                        Description = $"Average time-to-kill with {weapon} is {ttk:F2}s, which is below the 0.3s minimum threshold",
                        Evidence = new[]
                        {
                            new StatisticalEvidence
                            {
                                Metric = "TTK",
                                ObservedValue = ttk,
                                ExpectedValue = 0.5,
                                SampleSize = damageEvents.Count(e => e.Data.GetValueOrDefault("weapon")?.ToString() == weapon)
                            }
                        },
                        AffectedDomains = new[] { "Combat" }
                    });
                }
            }
        }

        return issues;
    }

    private IReadOnlyList<BalanceIssue> AnalyzeEconomyBalance(IReadOnlyList<TelemetryEvent> events)
    {
        var issues = new List<BalanceIssue>();

        var earnEvents = events.Where(e => e.EventType == TelemetryEventTypes.CurrencyEarned).ToList();
        var spendEvents = events.Where(e => e.EventType == TelemetryEventTypes.CurrencySpent).ToList();

        if (earnEvents.Any() && spendEvents.Any())
        {
            var totalEarned = earnEvents.Sum(e => Convert.ToDouble(e.Data.GetValueOrDefault("amount", 0)));
            var totalSpent = spendEvents.Sum(e => Convert.ToDouble(e.Data.GetValueOrDefault("amount", 0)));

            var inflationRate = totalEarned > 0 ? (totalEarned - totalSpent) / totalEarned : 0;

            if (inflationRate > 0.3) // Too much currency accumulation
            {
                issues.Add(new BalanceIssue
                {
                    IssueId = Guid.NewGuid().ToString(),
                    Category = BalanceCategory.Economy,
                    Severity = BalanceSeverity.Moderate,
                    Title = "Currency inflation detected",
                    Description = $"Players are accumulating currency at {inflationRate:P0} rate, indicating not enough spending opportunities",
                    Evidence = new[]
                    {
                        new StatisticalEvidence
                        {
                            Metric = "InflationRate",
                            ObservedValue = inflationRate,
                            ExpectedValue = 0.1,
                            SampleSize = earnEvents.Count + spendEvents.Count
                        }
                    },
                    AffectedDomains = new[] { "Economy" }
                });
            }
        }

        return issues;
    }

    private IReadOnlyList<BalanceIssue> AnalyzeProgressionBalance(IReadOnlyList<TelemetryEvent> events)
    {
        var issues = new List<BalanceIssue>();

        var objectiveEvents = events
            .Where(e => e.EventType.StartsWith("objective."))
            .ToList();

        var startedCount = objectiveEvents.Count(e => e.EventType == TelemetryEventTypes.ObjectiveStarted);
        var completedCount = objectiveEvents.Count(e => e.EventType == TelemetryEventTypes.ObjectiveCompleted);

        var completionRate = startedCount > 0
            ? completedCount / (double)startedCount
            : 0;

        if (completionRate < 0.5 && objectiveEvents.Count > 5)
        {
            issues.Add(new BalanceIssue
            {
                IssueId = Guid.NewGuid().ToString(),
                Category = BalanceCategory.Progression,
                Severity = BalanceSeverity.Major,
                Title = "Low objective completion rate",
                Description = $"Only {completionRate:P0} of started objectives are completed, indicating difficulty or engagement issues",
                Evidence = new[]
                {
                    new StatisticalEvidence
                    {
                        Metric = "ObjectiveCompletionRate",
                        ObservedValue = completionRate,
                        ExpectedValue = 0.7,
                        SampleSize = objectiveEvents.Count
                    }
                },
                AffectedDomains = new[] { "Gameplay", "Progression" }
            });
        }

        return issues;
    }

    private IReadOnlyList<BalanceIssue> AnalyzePerformance(IReadOnlyList<TelemetryEvent> events)
    {
        var issues = new List<BalanceIssue>();

        var frameTimeEvents = events.Where(e => e.EventType == TelemetryEventTypes.FrameTime).ToList();

        if (frameTimeEvents.Any())
        {
            var avgFrameTime = frameTimeEvents.Average(e => Convert.ToDouble(e.Data.GetValueOrDefault("ms", 16)));
            var avgFps = 1000.0 / avgFrameTime;

            if (avgFps < 30)
            {
                issues.Add(new BalanceIssue
                {
                    IssueId = Guid.NewGuid().ToString(),
                    Category = BalanceCategory.Performance,
                    Severity = avgFps < 20 ? BalanceSeverity.Critical : BalanceSeverity.Major,
                    Title = "Low frame rate detected",
                    Description = $"Average FPS of {avgFps:F1} is below acceptable threshold of 30 FPS",
                    Evidence = new[]
                    {
                        new StatisticalEvidence
                        {
                            Metric = "AverageFPS",
                            ObservedValue = avgFps,
                            ExpectedValue = 60,
                            SampleSize = frameTimeEvents.Count
                        }
                    },
                    AffectedDomains = new[] { "Infrastructure" }
                });
            }
        }

        return issues;
    }

    private async Task<IReadOnlyList<BalanceIssue>> EnrichWithSuggestionsAsync(
        IReadOnlyList<BalanceIssue> issues,
        CancellationToken cancellationToken)
    {
        var enriched = new List<BalanceIssue>();

        foreach (var issue in issues)
        {
            var evidenceText = string.Join(", ", issue.Evidence.Select(e => $"{e.Metric}: {e.ObservedValue} (expected {e.ExpectedValue})"));
            var jsonExample = "[{\"description\": \"fix description\", \"confidence\": 0.0-1.0, \"parameters\": {}}]";
            var prompt = $"""
                Given this game balance issue:

                Category: {issue.Category}
                Title: {issue.Title}
                Description: {issue.Description}
                Evidence: {evidenceText}

                Suggest 2-3 specific fixes with confidence levels.
                Respond as JSON array:
                {jsonExample}
                """;

            var output = await _model.CompleteAsync(new ModelInput(new[]
            {
                ("system", "You are a game balance expert. Suggest specific, actionable fixes."),
                ("user", prompt)
            }), cancellationToken);

            var fixes = ParseSuggestedFixes(output.Text);

            enriched.Add(issue with { SuggestedFixes = fixes });
        }

        return enriched;
    }

    private IReadOnlyList<SuggestedFix> ParseSuggestedFixes(string output)
    {
        try
        {
            var json = output;
            if (json.Contains("```"))
            {
                var start = json.IndexOf('[');
                var end = json.LastIndexOf(']');
                if (start >= 0 && end > start)
                {
                    json = json.Substring(start, end - start + 1);
                }
            }

            var fixes = JsonSerializer.Deserialize<List<SuggestedFix>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (IReadOnlyList<SuggestedFix>)(fixes ?? new List<SuggestedFix>());
        }
        catch
        {
            return Array.Empty<SuggestedFix>();
        }
    }

    private async Task<string> GenerateSummaryAsync(
        IReadOnlyList<BalanceIssue> issues,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            Summarize these game balance issues in 2-3 paragraphs:

            {string.Join("\n", issues.Select(i => $"- [{i.Severity}] {i.Title}: {i.Description}"))}

            Focus on the most impactful issues and overall patterns.
            """;

        var output = await _model.CompleteAsync(new ModelInput(new[]
        {
            ("system", "You write concise game balance reports for developers."),
            ("user", prompt)
        }), cancellationToken);

        return output.Text.Trim();
    }
}

