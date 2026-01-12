using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Adapters;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Agents.AutonomousDev.Bricks;

/// <summary>
/// Compiles and builds the project.
/// </summary>
public class BuildBrick : Brick
{
    private readonly ILogger<BuildBrick>? _logger;
    
    public BuildBrick(ILogger<BuildBrick>? logger = null)
    {
        _logger = logger;
        
        Id = "autonomous-dev.build";
        Name = "Build";
        Version = "1.0.0";
        Icon = "🔨";
        Category = BrickCategory.Control;
        Description = "Compiles and builds the project";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("project", "IProjectAdapter", "Project adapter"),
                new BrickInputDefinition("fullRebuild", "bool", "Full rebuild", required: false, defaultValue: false)
            ],
            Outputs = [
                new BrickOutputDefinition("result", "BuildResult", "Build result")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "build-deterministic",
                Name = "Build",
                Description = "Executes build command",
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
        var project = input.Get<IProjectAdapter>("project");
        var fullRebuild = input.Get<bool>("fullRebuild", false);
        
        var buildResult = await project.BuildAsync(fullRebuild, cancellationToken);
        
        return new BrickOutput
        {
            ["result"] = buildResult,
            Summary = buildResult.Success 
                ? $"Build succeeded ({buildResult.Warnings.Count} warnings)" 
                : $"Build failed: {string.Join(", ", buildResult.Errors.Take(3))}"
        };
    }
}
