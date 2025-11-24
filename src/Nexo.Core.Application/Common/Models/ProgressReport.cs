namespace Nexo.Core.Application.Common.Models;

/// <summary>
/// Represents a progress update for long-running operations.
/// </summary>
public record ProgressReport
{
    /// <summary>
    /// Current progress percentage (0-100).
    /// </summary>
    public int Percentage { get; init; }

    /// <summary>
    /// Current status message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Current step number (if applicable).
    /// </summary>
    public int? CurrentStep { get; init; }

    /// <summary>
    /// Total number of steps (if applicable).
    /// </summary>
    public int? TotalSteps { get; init; }

    /// <summary>
    /// Additional context data.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

