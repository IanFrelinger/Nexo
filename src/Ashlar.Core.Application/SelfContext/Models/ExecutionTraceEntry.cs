namespace Ashlar.Core.Application.SelfContext.Models;

/// <summary>Entry for an execution trace (operation start/end, path, outcome).</summary>
public record ExecutionTraceEntry
{
    /// <summary>Stable trace entry identifier.</summary>
    public required string Id { get; init; }

    /// <summary>UTC timestamp when the operation was recorded.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Operation name or phase label.</summary>
    public required string Operation { get; init; }

    /// <summary>File or resource path involved, when applicable.</summary>
    public string? Path { get; init; }

    /// <summary>Outcome label (success, failure, skipped, etc.).</summary>
    public string? Outcome { get; init; }

    /// <summary>Additional contextual key-value data.</summary>
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
