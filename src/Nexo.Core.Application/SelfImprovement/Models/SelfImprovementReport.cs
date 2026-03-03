namespace Nexo.Core.Application.SelfImprovement.Models;

/// <summary>
/// Report from a self-improvement loop run.
/// </summary>
public record SelfImprovementReport(
    DateTimeOffset RunAt,
    int FailuresProcessed,
    int FixesGenerated,
    int FixesValidated,
    int FixesPromoted,
    int FixesRejected,
    IReadOnlyList<string> PromotedAdaptationIds,
    IReadOnlyList<string> RejectedReasons);
