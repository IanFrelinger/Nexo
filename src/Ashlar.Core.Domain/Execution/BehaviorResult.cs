using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Workflows;

namespace Ashlar.Core.Domain.Execution;

/// <summary>
/// Result of executing a behavior.
/// </summary>
public class BehaviorResult
{
    /// <summary>Whether the behavior completed according to its success criteria.</summary>
    public bool Success { get; init; }

    /// <summary>Named output values produced by the behavior.</summary>
    public Dictionary<string, object> Outputs { get; init; } = new();

    /// <summary>Error messages collected during execution.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Wall-clock execution duration.</summary>
    public TimeSpan Duration { get; init; }
}
