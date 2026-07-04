using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Coordination.Conflicts;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Severity levels for escalations (maps to ConflictSeverity).
/// </summary>
public enum EscalationSeverity
{
    Low,
    Medium,
    High,
    Critical
}
