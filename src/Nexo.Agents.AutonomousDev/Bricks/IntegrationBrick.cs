using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Adapters;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Agents.AutonomousDev.Bricks;

/// <summary>
/// Applies generated artifacts to the project.
/// </summary>
public class IntegrationBrick : Brick
{
    private readonly ILogger<IntegrationBrick>? _logger;
    
    public IntegrationBrick(ILogger<IntegrationBrick>? logger = null)
    {
        _logger = logger;
        
        Id = "autonomous-dev.integration";
        Name = "Integration";
        Version = "1.0.0";
        Icon = "🔧";
        Category = BrickCategory.Transform;
        Description = "Applies generated artifacts to the project";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("artifacts", "GeneratedArtifact[]", "Artifacts to apply"),
                new BrickInputDefinition("project", "IProjectAdapter", "Project adapter"),
                new BrickInputDefinition("createBackup", "bool", "Create backup", required: false, defaultValue: true)
            ],
            Outputs = [
                new BrickOutputDefinition("result", "IntegrationResult", "Integration result")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "integration-deterministic",
                Name = "File Integration",
                Description = "Applies files to project",
                Executor = "DirectExecutor",
                Config = new Dictionary<string, object>()
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var artifacts = input.Get<GeneratedArtifact[]>("artifacts");
        var project = input.Get<IProjectAdapter>("project");
        var createBackup = input.Get<bool>("createBackup", true);
        
        string? backupPath = null;
        if (createBackup)
        {
            backupPath = await project.CreateBackupAsync(cancellationToken);
        }
        
        var appliedChanges = new List<AppliedChange>();
        var errors = new List<string>();
        
        foreach (var artifact in artifacts)
        {
            try
            {
                var exists = await project.FileExistsAsync(artifact.TargetPath, cancellationToken);
                var changeType = exists ? FileChangeType.Modify : FileChangeType.Create;
                
                if (artifact.IsFullFile)
                {
                    await project.WriteFileAsync(artifact.TargetPath, artifact.Content, cancellationToken);
                }
                else
                {
                    await project.PatchFileAsync(artifact.TargetPath, artifact.Content, cancellationToken);
                }
                
                appliedChanges.Add(new AppliedChange
                {
                    FilePath = artifact.TargetPath,
                    ChangeType = changeType,
                    Success = true
                });
                
                _logger?.LogInformation("Applied {Type} to {Path}", changeType, artifact.TargetPath);
            }
            catch (Exception ex)
            {
                errors.Add($"{artifact.TargetPath}: {ex.Message}");
                appliedChanges.Add(new AppliedChange
                {
                    FilePath = artifact.TargetPath,
                    ChangeType = FileChangeType.Modify,
                    Success = false,
                    Error = ex.Message
                });
            }
        }
        
        return new BrickOutput
        {
            ["result"] = new IntegrationResult
            {
                Success = errors.Count == 0,
                AppliedChanges = appliedChanges,
                Errors = errors,
                BackupPath = backupPath
            },
            Summary = $"Applied {appliedChanges.Count(s => s.Success)}/{appliedChanges.Count} changes"
        };
    }
}

/// <summary>
/// Result of integration.
/// </summary>
public record IntegrationResult
{
    public bool Success { get; init; }
    public IReadOnlyList<AppliedChange> AppliedChanges { get; init; } = Array.Empty<AppliedChange>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public string? BackupPath { get; init; }
}

/// <summary>
/// A change that was applied.
/// </summary>
public record AppliedChange
{
    public required string FilePath { get; init; }
    public required FileChangeType ChangeType { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
}
