using Ashlar.Orchestration.Build.Models;

namespace Ashlar.Orchestration.Build.Ports;

/// <summary>
/// Port for build tools (Unity, Unreal, etc.).
/// 
/// Defines the contract for build tool adapters:
/// - Execute builds with configuration
/// - Support multiple build targets
/// - Provide tool identification
/// 
/// Implementations (UnityBuildTool, etc.) provide specific build logic.
/// Used by UnityBuildAgent and other build agents.
/// </summary>
public interface IBuildTool
{
    /// <summary>
    /// Executes a build with the given configuration.
    /// </summary>
    Task<BuildOutput> BuildAsync(
        BuildConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported build targets for this tool.
    /// </summary>
    IReadOnlyList<BuildTarget> SupportedTargets { get; }

    /// <summary>
    /// Gets the tool name (e.g., "Unity", "Unreal").
    /// </summary>
    string ToolName { get; }
}

