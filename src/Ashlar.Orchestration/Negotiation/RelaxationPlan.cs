using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Coordination.Conflicts;
using Ashlar.Orchestration.Negotiation.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Plan for relaxing constraints.
/// </summary>
internal sealed record RelaxationPlan
{
    public IReadOnlyDictionary<string, string> RelaxedConstraints { get; init; } =
        new Dictionary<string, string>();
    public string? Explanation { get; init; }
}
