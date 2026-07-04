namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Normalized output from a stage executor.
/// </summary>
public sealed record PipelineStageExecutionResult
{
    /// <summary>Whether the stage attempt completed without error.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Whether the orchestrator may retry this stage on failure.</summary>
    public bool Retryable { get; init; } = true;

    /// <summary>Identifier of the worker that produced this result.</summary>
    public string? WorkerId { get; init; }

    /// <summary>Serialized stage output when <see cref="Succeeded"/> is true.</summary>
    public string? Output { get; init; }

    /// <summary>Error description when <see cref="Succeeded"/> is false.</summary>
    public string? Error { get; init; }
}
