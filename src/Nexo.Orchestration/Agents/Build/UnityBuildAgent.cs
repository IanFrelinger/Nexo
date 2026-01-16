using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Build.Models;
using Nexo.Orchestration.Build.Ports;
using Nexo.Orchestration.Assets.Ports;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Build;

/// <summary>
/// Agent that builds Unity projects.
/// 
/// Specialized agent for Unity build operations that:
/// - Executes Unity builds via IBuildTool
/// - Integrates generated assets into builds
/// - Supports multiple build targets (Windows, macOS, Linux, etc.)
/// - Manages build configuration and output
/// - Handles build errors and validation
/// 
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public sealed class UnityBuildAgent : BaseAgent
{
    private readonly IBuildTool _buildTool;
    private readonly IAssetStorage _assetStorage;

    public UnityBuildAgent(
        AgentSpawnSpec spec,
        IBuildTool buildTool,
        IAssetStorage assetStorage,
        ILogger<BaseAgent> logger) : base(spec, logger)
    {
        _buildTool = buildTool ?? throw new ArgumentNullException(nameof(buildTool));
        _assetStorage = assetStorage ?? throw new ArgumentNullException(nameof(assetStorage));
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing Unity build agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Dependencies resolved for {AgentId}: {Count} dependencies", Spec.AgentId, dependencyOutputs.Count);
        return Task.CompletedTask;
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Extract assets from dependencies
        var assetManifest = ExtractAssetManifest(dependencyOutputs ?? new Dictionary<string, object>());

        // Determine project path and configuration
        var projectPath = Spec.Constraints
            .FirstOrDefault(c => c.Type == "ProjectPath")?.Description
            ?? throw new InvalidOperationException("ProjectPath constraint required");

        var targetStr = Spec.Constraints
            .FirstOrDefault(c => c.Type == "BuildTarget")?.Description
            ?? "Windows";

        var target = Enum.Parse<BuildTarget>(targetStr, ignoreCase: true);

        // Import assets into Unity project
        if (assetManifest != null)
        {
            await ImportAssetsAsync(projectPath, assetManifest, cancellationToken);
        }

        // Execute build
        var config = new BuildConfiguration
        {
            ProjectPath = projectPath,
            Target = target,
            Development = true
        };

        var buildOutput = await _buildTool.BuildAsync(config, cancellationToken);

        // If successful, store build artifact
        if (buildOutput.Success && buildOutput.ArtifactPath != null)
        {
            var storedPath = await _assetStorage.StoreDirectoryAsync(
                buildOutput.ArtifactPath,
                $"builds/{buildOutput.BuildId}",
                cancellationToken);

            return buildOutput with { ArtifactPath = storedPath };
        }

        return buildOutput;
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down Unity build agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    private AssetManifest? ExtractAssetManifest(IReadOnlyDictionary<string, object> outputs)
    {
        var assets = outputs.Values
            .OfType<AssetOutput>()
            .ToList();

        if (!assets.Any())
        {
            // Check if any output contains an asset manifest
            foreach (var output in outputs.Values)
            {
                if (output is AssetManifest manifest)
                {
                    return manifest;
                }
            }
            return null;
        }

        return new AssetManifest
        {
            ProjectId = Spec.AgentId,
            Assets = assets
        };
    }

    private Task ImportAssetsAsync(
        string projectPath,
        AssetManifest manifest,
        CancellationToken cancellationToken)
    {
        var assetsPath = Path.Combine(projectPath, "Assets", "Generated");
        var assetsDir = new DirectoryInfo(assetsPath);
        if (!assetsDir.Exists) assetsDir.Create();

        foreach (var asset in manifest.Assets)
        {
            var destPath = Path.Combine(assetsPath, asset.AssetType.ToString(),
                Path.GetFileName(asset.FilePath));
            var destDir = new DirectoryInfo(Path.GetDirectoryName(destPath)!);
            if (!destDir.Exists) destDir.Create();

            new FileInfo(asset.FilePath).CopyTo(destPath, overwrite: true);

            Logger.LogDebug("Imported asset: {Source} → {Dest}", asset.FilePath, destPath);
        }

        Logger.LogInformation("Imported {Count} assets to {Path}", manifest.Assets.Count, assetsPath);
        return Task.CompletedTask;
    }
}

