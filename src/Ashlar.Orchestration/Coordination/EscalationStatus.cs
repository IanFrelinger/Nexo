using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Coordination.Conflicts;

namespace Ashlar.Orchestration.Coordination;

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
