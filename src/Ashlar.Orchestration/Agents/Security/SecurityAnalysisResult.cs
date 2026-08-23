using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Security;

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
