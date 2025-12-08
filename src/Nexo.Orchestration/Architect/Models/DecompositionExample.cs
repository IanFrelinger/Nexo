namespace Nexo.Orchestration.Architect.Models;

/// <summary>
/// A stored example of a decomposition for RAG retrieval.
/// </summary>
public sealed record DecompositionExample
{
    /// <summary>
    /// Unique identifier for this example.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The original request that was decomposed.
    /// </summary>
    public required string Request { get; init; }

    /// <summary>
    /// The decomposition result.
    /// </summary>
    public required DecompositionResult Result { get; init; }

    /// <summary>
    /// Domain tags for this example (e.g., "Combat", "Economy", "AI").
    /// </summary>
    public IReadOnlyList<string> DomainTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Keywords extracted from the request for semantic matching.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// When this example was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Quality score (0.0 to 1.0) indicating how well this example worked.
    /// </summary>
    public double QualityScore { get; init; } = 1.0;
}

