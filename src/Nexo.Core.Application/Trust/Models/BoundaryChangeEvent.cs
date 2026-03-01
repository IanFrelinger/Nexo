namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// Event raised when an access boundary setting changes.
/// </summary>
public sealed class BoundaryChangeEvent
{
    /// <summary>What changed: category, source, project, or pause.</summary>
    public required string ChangeType { get; init; }

    /// <summary>Category name (when ChangeType is category).</summary>
    public string? Category { get; init; }

    /// <summary>Source ID (when ChangeType is source).</summary>
    public string? SourceId { get; init; }

    /// <summary>Project path (when ChangeType is project).</summary>
    public string? ProjectPath { get; init; }

    /// <summary>Previous state (e.g. allowed/blocked, paused/resumed).</summary>
    public string? PreviousState { get; init; }

    /// <summary>New state.</summary>
    public required string NewState { get; init; }

    /// <summary>When the change occurred.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
