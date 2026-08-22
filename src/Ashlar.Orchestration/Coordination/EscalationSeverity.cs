using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Coordination.Conflicts;

namespace Ashlar.Orchestration.Coordination;

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
