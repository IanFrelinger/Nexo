namespace Ashlar.Core.Application.Generation;

/// <summary>Structured outcome of a generation repair loop.</summary>
/// <typeparam name="TArtifact">Generated artifact type.</typeparam>
/// <param name="Artifact">Last drafted artifact (present even on failure).</param>
/// <param name="Succeeded">True when the validator chain passed.</param>
/// <param name="Attempts">Number of draft attempts performed.</param>
/// <param name="Feedback">Feedback from the final failing attempt (empty on success).</param>
/// <param name="FailureSummary">Concatenated reasons when unsuccessful; otherwise null.</param>
public sealed record GenerationRepairResult<TArtifact>(
    TArtifact? Artifact,
    bool Succeeded,
    int Attempts,
    IReadOnlyList<ValidationFeedback> Feedback,
    string? FailureSummary);
