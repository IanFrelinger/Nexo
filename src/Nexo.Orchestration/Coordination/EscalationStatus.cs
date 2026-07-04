using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Coordination.Conflicts;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Status of an escalation.
/// </summary>
public enum EscalationStatus
{
    Pending,
    InProgress,
    Resolved,
    Dismissed
}
