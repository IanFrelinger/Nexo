using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Security;

/// <summary>
/// Result of security analysis.
/// </summary>
public sealed record SecurityAnalysisResult
{
    /// <summary>Detected security vulnerabilities.</summary>
    public IReadOnlyList<Vulnerability> Vulnerabilities { get; init; } = new List<Vulnerability>();

    /// <summary>Detected compliance violations.</summary>
    public IReadOnlyList<ComplianceIssue> ComplianceIssues { get; init; } = new List<ComplianceIssue>();

    /// <summary>Optional LLM-produced advanced analysis.</summary>
    public AdvancedSecurityAnalysis? AdvancedAnalysis { get; init; }

    /// <summary>UTC timestamp when analysis completed.</summary>
    public required DateTimeOffset AnalyzedAt { get; init; }

    /// <summary>Additional analysis metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
