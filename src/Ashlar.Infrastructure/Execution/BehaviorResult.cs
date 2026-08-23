namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Result of executing a behavior.
/// </summary>
public class BehaviorResult
{
    /// <summary>Whether execution completed successfully.</summary>
    public bool Success { get; init; }
    /// <summary>Behavior output values keyed by name.</summary>
    public Dictionary<string, object> Outputs { get; init; } = new();
    /// <summary>Execution error messages, if any.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
    /// <summary>Elapsed execution time.</summary>
    public TimeSpan Duration { get; init; }
}

