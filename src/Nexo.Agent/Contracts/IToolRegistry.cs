using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Models;

namespace Nexo.Agent.Contracts;

/// <summary>
/// Registry for managing available tools.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    IReadOnlyList<ToolManifest> GetAllTools();

    /// <summary>
    /// Gets a tool by its ID.
    /// </summary>
    /// <param name="toolId">Tool identifier</param>
    /// <returns>Tool manifest or null if not found</returns>
    ToolManifest? GetTool(string toolId);

    /// <summary>
    /// Registers a new tool.
    /// </summary>
    /// <param name="tool">Tool instance to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if registered successfully</returns>
    Task<bool> RegisterToolAsync(ITool tool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a tool by ID.
    /// </summary>
    /// <param name="toolId">Tool identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unregistered successfully</returns>
    Task<bool> UnregisterToolAsync(string toolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    /// <param name="toolId">Tool identifier</param>
    /// <returns>True if the tool is registered</returns>
    bool IsToolRegistered(string toolId);

    /// <summary>
    /// Scans a directory for tool assemblies and registers them.
    /// </summary>
    /// <param name="directoryPath">Directory to scan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tools registered</returns>
    Task<int> ScanAndRegisterToolsAsync(string directoryPath, CancellationToken cancellationToken = default);
}
