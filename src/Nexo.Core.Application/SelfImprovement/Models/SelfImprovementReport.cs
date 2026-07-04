namespace Nexo.Core.Application.SelfImprovement.Models;

/// <summary>
/// Report from a self-improvement loop run.
/// Distinguishes work items sourced from test failures vs observed patterns.
/// </summary>
/// <param name="RunAt">UTC timestamp when the loop run completed.</param>
/// <param name="FailuresProcessed">Number of test failures processed in this run.</param>
/// <param name="FixesGenerated">Number of adaptation fixes generated.</param>
/// <param name="FixesValidated">Number of fixes that passed validation.</param>
/// <param name="FixesPromoted">Number of fixes promoted to production.</param>
/// <param name="FixesRejected">Number of fixes rejected during validation or promotion.</param>
/// <param name="PromotedAdaptationIds">Identifiers of adaptations promoted from test-failure work items.</param>
/// <param name="RejectedReasons">Human-readable reasons for rejected fixes.</param>
/// <param name="PatternsProcessed">Number of observed patterns processed in this run.</param>
/// <param name="PatternSourcedPromoted">Number of pattern-sourced adaptations promoted.</param>
/// <param name="PatternSourcedPromotedIds">Identifiers of adaptations promoted from observed patterns.</param>
/// <param name="HoldoutPassed">Holdout tests passed, when holdout validation ran.</param>
/// <param name="HoldoutTotal">Total holdout tests run, when holdout validation ran.</param>
/// <param name="HoldoutPassRate">Holdout pass rate (0–1), when holdout validation ran.</param>
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
