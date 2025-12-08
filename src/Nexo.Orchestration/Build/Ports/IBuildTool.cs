using Nexo.Orchestration.Build.Models;

namespace Nexo.Orchestration.Build.Ports;

/// <summary>
/// Port for build tools (Unity, Unreal, etc.).
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

