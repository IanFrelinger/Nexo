using Microsoft.Extensions.Logging;

namespace Ashlar.Orchestration.Agents.CodeGeneration;

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
