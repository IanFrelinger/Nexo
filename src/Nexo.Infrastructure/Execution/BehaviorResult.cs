namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Result of executing a behavior.
/// </summary>
public class BehaviorResult
{
    public bool Success { get; init; }
    public Dictionary<string, object> Outputs { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

