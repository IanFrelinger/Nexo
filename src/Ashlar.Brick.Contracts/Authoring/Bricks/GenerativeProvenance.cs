namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// How an artifact was produced. Domain-neutral — profiles map their own
/// deterministic/template/model paths onto these values.
/// </summary>
public enum GenerationStrategy
{
    /// <summary>Purely rule/algorithmic generation with no model call.</summary>
    Deterministic = 0,

    /// <summary>Filled template / scaffold with structural placeholders.</summary>
    Templated = 1,

    /// <summary>Produced by a generative model.</summary>
    Model = 2
}

/// <summary>
/// Structured provenance emitted by <see cref="GenerativeBrick"/> outputs.
/// Replaces magic-string sentinels for “was a model called?”.
/// </summary>
public sealed class GenerativeProvenance
{
    /// <summary>How the artifact was produced.</summary>
    public GenerationStrategy Strategy { get; init; }

    /// <summary>True when post-validators accepted the artifact.</summary>
    public bool Verified { get; init; }

    /// <summary>Optional confidence score in [0, 1].</summary>
    public double? Confidence { get; init; }

    /// <summary>Non-fatal warnings for operators/models.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Structural assumptions baked into the draft.</summary>
    public IReadOnlyList<string> Assumptions { get; init; } = Array.Empty<string>();

    /// <summary>References the drafter could not ground.</summary>
    public IReadOnlyList<string> UnsupportedReferences { get; init; } = Array.Empty<string>();

    /// <summary>When true, route through an approval gate before side effects.</summary>
    public bool RequiresHumanReview { get; init; }

    /// <summary>Factory with empty-list defaults.</summary>
    public static GenerativeProvenance Create(
        GenerationStrategy strategy,
        bool verified = false,
        double? confidence = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? assumptions = null,
        IReadOnlyList<string>? unsupportedReferences = null,
        bool requiresHumanReview = false) =>
        new()
        {
            Strategy = strategy,
            Verified = verified,
            Confidence = confidence,
            Warnings = warnings ?? Array.Empty<string>(),
            Assumptions = assumptions ?? Array.Empty<string>(),
            UnsupportedReferences = unsupportedReferences ?? Array.Empty<string>(),
            RequiresHumanReview = requiresHumanReview
        };
}
