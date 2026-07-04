using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Severity of a code issue.
/// </summary>
public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}
