using Microsoft.Extensions.Logging;

namespace Ashlar.Orchestration.Agents.CodeGeneration;

/// <summary>
/// An issue found in code analysis.
/// </summary>
public sealed record CodeIssue
{
    /// <summary>Issue category (style, security, etc.).</summary>
    public required string Category { get; init; }

    /// <summary>Severity of the issue.</summary>
    public required IssueSeverity Severity { get; init; }

    /// <summary>Human-readable issue description.</summary>
    public required string Message { get; init; }

    /// <summary>Source line number, if available.</summary>
    public int? LineNumber { get; init; }
}
