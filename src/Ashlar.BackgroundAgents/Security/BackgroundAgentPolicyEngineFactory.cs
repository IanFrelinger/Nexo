using System.Collections.Generic;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.Runtime;
using Ashlar.BackgroundAgents.Registry;

namespace Ashlar.BackgroundAgents.Security;

/// <summary>
/// Builds a PolicyEngine for background agent execution that includes exfiltration prevention.
/// Use when creating AgentHost or tool runtime for background agents so tool calls are approved by DataExfiltrationPolicy.
/// </summary>
public static class BackgroundAgentPolicyEngineFactory
{
    /// <summary>
    /// Creates a PolicyEngine with DataExfiltrationPolicy and optional additional policies.
    /// </summary>
    /// <param name="registry">Background agent registry.</param>
    /// <param name="sensitivityRegistry">Sensitivity level registry.</param>
    /// <param name="additionalPolicies">Optional additional policies (e.g. AllowAllPolicy, sandbox policies).</param>
    /// <returns>A PolicyEngine that enforces exfiltration policy for background agent tool calls.</returns>
    public static PolicyEngine Create(
        IBackgroundAgentRegistry registry,
        IDataSensitivityRegistry sensitivityRegistry,
        IEnumerable<IPolicy>? additionalPolicies = null)
    {
        var policies = new List<IPolicy>
        {
            new DataExfiltrationPolicy(registry, sensitivityRegistry)
        };
        if (additionalPolicies != null)
        {
            policies.AddRange(additionalPolicies);
        }
        return new PolicyEngine(policies);
    }
}
