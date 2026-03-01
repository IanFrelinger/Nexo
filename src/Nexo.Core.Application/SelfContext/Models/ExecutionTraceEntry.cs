namespace Nexo.Core.Application.SelfContext.Models;

/// <summary>
/// Entry for an execution trace (operation start/end, path, outcome).
/// </summary>
public record ExecutionTraceEntry
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Operation { get; init; }
    public string? Path { get; init; }
    public string? Outcome { get; init; }
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
