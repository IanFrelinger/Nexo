using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Models;

namespace Nexo.Agent.Contracts;

/// <summary>
/// Factory for creating new tools dynamically using the Feature Factory pipeline.
/// </summary>
public interface IToolFactory
{
    /// <summary>
    /// Creates a new tool based on the tool request.
    /// </summary>
    /// <param name="request">Tool creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool creation result</returns>
    Task<ToolCreationResult> CreateToolAsync(ToolRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tool can be created for the given request.
    /// </summary>
    /// <param name="request">Tool creation request</param>
    /// <returns>True if the tool can be created</returns>
    bool CanCreateTool(ToolRequest request);

    /// <summary>
    /// Gets the estimated time to create a tool.
    /// </summary>
    /// <param name="request">Tool creation request</param>
    /// <returns>Estimated creation time</returns>
    TimeSpan EstimateCreationTime(ToolRequest request);
}

/// <summary>
/// Result of tool creation.
/// </summary>
/// <param name="Success">Whether the tool was created successfully</param>
/// <param name="ToolId">Created tool identifier</param>
/// <param name="Manifest">Tool manifest</param>
/// <param name="Error">Error message if creation failed</param>
/// <param name="Duration">Creation duration</param>
/// <param name="PolicyScore">Policy compliance score (0-100)</param>
/// <param name="RepairAttempts">Number of repair attempts made</param>
public record ToolCreationResult(
    bool Success,
    string? ToolId,
    ToolManifest? Manifest,
    string? Error,
    TimeSpan Duration,
    int PolicyScore,
    int RepairAttempts);
