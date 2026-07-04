using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Security;

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
