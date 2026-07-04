using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Agents.Security;

/// <summary>
/// Severity of a compliance issue.
/// </summary>
public enum ComplianceSeverity
{
    Low,
    Medium,
    High,
    Critical
}
