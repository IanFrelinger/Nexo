namespace Nexo.Core.Application.SelfImprovement.Models;

/// <summary>
/// Report from a self-improvement loop run.
/// Distinguishes work items sourced from test failures vs observed patterns.
/// </summary>
public record SelfImprovementReport(
    DateTimeOffset RunAt,
    int FailuresProcessed,
    int FixesGenerated,
    int FixesValidated,
    int FixesPromoted,
    int FixesRejected,
    IReadOnlyList<string> PromotedAdaptationIds,
    IReadOnlyList<string> RejectedReasons,
    int PatternsProcessed = 0,
    int PatternSourcedPromoted = 0,
    IReadOnlyList<string>? PatternSourcedPromotedIds = null,
    int? HoldoutPassed = null,
    int? HoldoutTotal = null,
    double? HoldoutPassRate = null);
