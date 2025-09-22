using System.Text.Json.Serialization;
using Nexo.Agent.Contracts;

namespace Nexo.Agent.Models;

/// <summary>
/// Manifest describing a tool's capabilities and metadata.
/// </summary>
/// <param name="Id">Unique tool identifier</param>
/// <param name="Name">Human-readable tool name</param>
/// <param name="Version">Tool version</param>
/// <param name="Description">Tool description</param>
/// <param name="Author">Tool author</param>
/// <param name="InputSchema">JSON schema for input validation</param>
/// <param name="OutputSchema">JSON schema for output validation</param>
/// <param name="RequiredPermissions">Required permissions to execute this tool</param>
/// <param name="Capabilities">Tool capability tags</param>
/// <param name="Timeout">Default timeout for tool execution</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record ToolManifest(
    string Id,
    string Name,
    string Version,
    string Description,
    string Author,
    string InputSchema,
    string OutputSchema,
    ToolPermissions RequiredPermissions,
    IReadOnlyList<string> Capabilities,
    TimeSpan Timeout,
    DateTime CreatedAt)
{
    /// <summary>
    /// Creates a tool manifest with default values.
    /// </summary>
    public static ToolManifest Create(
        string id,
        string name,
        string description,
        ToolPermissions requiredPermissions = ToolPermissions.None,
        IReadOnlyList<string>? capabilities = null,
        TimeSpan? timeout = null)
    {
        return new ToolManifest(
            Id: id,
            Name: name,
            Version: "1.0.0",
            Description: description,
            Author: "Nexo Agent",
            InputSchema: "{}",
            OutputSchema: "{}",
            RequiredPermissions: requiredPermissions,
            Capabilities: capabilities ?? Array.Empty<string>(),
            Timeout: timeout ?? TimeSpan.FromMinutes(5),
            CreatedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Validates that the required permissions are available.
    /// </summary>
    /// <param name="availablePermissions">Available permissions</param>
    /// <returns>True if all required permissions are available</returns>
    public bool HasRequiredPermissions(ToolPermissions availablePermissions)
    {
        return (availablePermissions & RequiredPermissions) == RequiredPermissions;
    }

    /// <summary>
    /// Checks if the tool has a specific capability.
    /// </summary>
    /// <param name="capability">Capability to check</param>
    /// <returns>True if the tool has the capability</returns>
    public bool HasCapability(string capability)
    {
        return Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
    }
}
