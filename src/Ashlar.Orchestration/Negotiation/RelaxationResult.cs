using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Coordination.Conflicts;
using Ashlar.Orchestration.Negotiation.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Result of constraint relaxation attempt.
/// </summary>
public sealed record RelaxationResult
{
    /// <summary>Whether constraint relaxation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Constraints that were relaxed and their new values.</summary>
    public IReadOnlyDictionary<string, string> RelaxedConstraints { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Explanation of how constraints were relaxed.</summary>
    public string? Explanation { get; init; }

    /// <summary>Reason relaxation failed, when applicable.</summary>
    public string? FailureReason { get; init; }

    public static RelaxationResult Unresolvable(string reason) => new()
    {
        Success = false,
        FailureReason = reason
    };
}
