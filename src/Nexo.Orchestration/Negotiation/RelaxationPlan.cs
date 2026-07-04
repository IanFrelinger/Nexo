using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Negotiation.Models;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Plan for relaxing constraints.
/// </summary>
internal sealed record RelaxationPlan
{
    public IReadOnlyDictionary<string, string> RelaxedConstraints { get; init; } =
        new Dictionary<string, string>();
    public string? Explanation { get; init; }
}
