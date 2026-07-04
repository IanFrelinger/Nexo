using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Agents.Security;

/// <summary>
/// A compliance issue.
/// </summary>
public sealed record ComplianceIssue
{
    /// <summary>Compliance standard being evaluated.</summary>
    public required string Standard { get; init; }

    /// <summary>Specific rule or control that was violated.</summary>
    public required string Rule { get; init; }

    /// <summary>Human-readable description of the violation.</summary>
    public required string Description { get; init; }

    /// <summary>Severity of the compliance issue.</summary>
    public required ComplianceSeverity Severity { get; init; }
}
