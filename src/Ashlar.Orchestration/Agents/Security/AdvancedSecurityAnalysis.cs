using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Security;

/// <summary>
/// Advanced security analysis from LLM.
/// </summary>
public sealed record AdvancedSecurityAnalysis
{
    /// <summary>Executive summary of the security assessment.</summary>
    public required string Summary { get; init; }

    /// <summary>Numeric risk score from the analysis.</summary>
    public required int RiskScore { get; init; }

    /// <summary>Recommended remediation actions.</summary>
    public IReadOnlyList<string> Recommendations { get; init; } = new List<string>();
}
