namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Result of a single model inference request.
/// </summary>
public sealed record InferenceResult
{
    /// <summary>Generated text or structured output from the model.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>Number of tokens produced in the response.</summary>
    public int TokenCount { get; init; }

    /// <summary>Wall-clock time spent on inference.</summary>
    public TimeSpan Duration { get; init; }
}
