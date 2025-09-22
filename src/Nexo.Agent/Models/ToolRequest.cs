using System.Text.Json.Serialization;
using Nexo.Agent.Contracts;

namespace Nexo.Agent.Models;

/// <summary>
/// Request to create a new tool.
/// </summary>
/// <param name="Id">Desired tool identifier</param>
/// <param name="Name">Human-readable tool name</param>
/// <param name="Description">Tool description</param>
/// <param name="InputSchema">JSON schema for input validation</param>
/// <param name="OutputSchema">JSON schema for output validation</param>
/// <param name="RequiredPermissions">Required permissions</param>
/// <param name="Capabilities">Desired capabilities</param>
/// <param name="QualityTargets">Quality targets for generation</param>
/// <param name="BreakPolicy">Whether to intentionally break policy for testing</param>
/// <param name="Timeout">Tool execution timeout</param>
/// <param name="CreatedAt">Request timestamp</param>
public record ToolRequest(
    string Id,
    string Name,
    string Description,
    string InputSchema,
    string OutputSchema,
    ToolPermissions RequiredPermissions,
    IReadOnlyList<string> Capabilities,
    ToolQualityTargets QualityTargets,
    bool BreakPolicy = false,
    TimeSpan? Timeout = null,
    DateTime? CreatedAt = null)
{
    /// <summary>
    /// Creates a tool request with default values.
    /// </summary>
    public static ToolRequest Create(
        string id,
        string name,
        string description,
        ToolPermissions requiredPermissions = ToolPermissions.None,
        IReadOnlyList<string>? capabilities = null,
        ToolQualityTargets? qualityTargets = null,
        bool breakPolicy = false)
    {
        return new ToolRequest(
            Id: id,
            Name: name,
            Description: description,
            InputSchema: "{}",
            OutputSchema: "{}",
            RequiredPermissions: requiredPermissions,
            Capabilities: capabilities ?? Array.Empty<string>(),
            QualityTargets: qualityTargets ?? ToolQualityTargets.Default,
            BreakPolicy: breakPolicy,
            Timeout: TimeSpan.FromMinutes(5),
            CreatedAt: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Quality targets for tool generation.
/// </summary>
/// <param name="MinPolicyScore">Minimum policy compliance score (0-100)</param>
/// <param name="MaxRepairAttempts">Maximum repair attempts</param>
/// <param name="RequireTests">Whether to require unit tests</param>
/// <param name="RequireDocumentation">Whether to require documentation</param>
public record ToolQualityTargets(
    int MinPolicyScore = 80,
    int MaxRepairAttempts = 3,
    bool RequireTests = true,
    bool RequireDocumentation = true)
{
    /// <summary>
    /// Default quality targets.
    /// </summary>
    public static ToolQualityTargets Default => new();
}
