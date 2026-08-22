namespace Ashlar.Orchestration.Architect;

using Models;

/// <summary>
/// Additional context for decomposition.
/// </summary>
public sealed record DecompositionContext
{
    /// <summary>
    /// Previous decomposition examples that are similar to the current request.
    /// </summary>
    public IReadOnlyList<DecompositionResult> SimilarExamples { get; init; } = Array.Empty<DecompositionResult>();

    /// <summary>
    /// Domain hints or patterns to guide decomposition.
    /// </summary>
    public IReadOnlyList<string> DomainHints { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Available resource budget (compute, context, etc.).
    /// </summary>
    public ResourceBudget? ResourceBudget { get; init; }

    /// <summary>
    /// Correlation identifier propagated from orchestration.
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Barrier level used for endpoint routing.
    /// </summary>
    public string? BarrierLevel { get; init; }

    /// <summary>
    /// Optional preferred region for endpoint routing.
    /// </summary>
    public string? PreferredRegion { get; init; }
}
