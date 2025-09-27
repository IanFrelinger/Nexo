using System;
using System.Linq;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Helper methods for SecurityAnalysisAgent.
/// </summary>
public partial class SecurityAnalysisAgent
{
    /// <summary>
    /// Determines security level from agent response
    /// </summary>
    private SecurityLevel DetermineSecurityLevel(AgentResponse response)
    {
        var securityScore = response.GetMetadata<double>("SecurityScore");
        
        return securityScore switch
        {
            >= 8.0 => SecurityLevel.High,
            >= 6.0 => SecurityLevel.Standard,
            >= 4.0 => SecurityLevel.Basic,
            _ => SecurityLevel.Low
        };
    }

    /// <summary>
    /// Extracts platform-specific vulnerabilities from agent response
    /// </summary>
    private SecurityVulnerability[] ExtractPlatformVulnerabilities(AgentResponse response)
    {
        var vulnerabilities = response.GetMetadata<SecurityVulnerability[]>("SecurityVulnerabilities");
        return vulnerabilities ?? Array.Empty<SecurityVulnerability>();
    }
}
