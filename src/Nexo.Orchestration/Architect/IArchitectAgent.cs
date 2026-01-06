namespace Nexo.Orchestration.Architect;

using Models;

/// <summary>
/// Interface for the Architect Agent that decomposes requests into validated agent specifications.
/// 
/// The Architect Agent is the entry point of orchestration:
/// - Takes high-level user requests
/// - Decomposes them into agent specifications
/// - Validates specifications
/// - Returns structured decomposition results
/// 
/// Supports context-aware decomposition with examples and domain hints.
/// </summary>
public interface IArchitectAgent
{
    /// <summary>
    /// Decomposes a request into a set of agent specifications.
    /// </summary>
    /// <param name="request">The user request to decompose.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A decomposition result containing agent specifications.</returns>
    Task<DecompositionResult> DecomposeAsync(string request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decomposes a request with additional context (e.g., previous decompositions, domain hints).
    /// </summary>
    /// <param name="request">The user request to decompose.</param>
    /// <param name="context">Additional context to aid decomposition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A decomposition result containing agent specifications.</returns>
    Task<DecompositionResult> DecomposeAsync(
        string request,
        DecompositionContext context,
        CancellationToken cancellationToken = default);
}

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
}

/// <summary>
/// Resource budget available for agent execution.
/// </summary>
public sealed record ResourceBudget
{
    /// <summary>
    /// Maximum total compute time in seconds.
    /// </summary>
    public int? MaxComputeSeconds { get; init; }

    /// <summary>
    /// Maximum total context tokens.
    /// </summary>
    public int? MaxContextTokens { get; init; }

    /// <summary>
    /// Maximum total memory in MB.
    /// </summary>
    public int? MaxMemoryMB { get; init; }
}

