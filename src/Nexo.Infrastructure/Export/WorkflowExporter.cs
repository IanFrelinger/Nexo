using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Infrastructure.Execution;

namespace Nexo.Infrastructure.Export;

/// <summary>
/// Exports workflows in various formats and modes.
/// </summary>
public class WorkflowExporter : IWorkflowExporter
{
    private readonly IClusterRegistry _clusterRegistry;
    private readonly IBrickRegistry _brickRegistry;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IContentGenerator _contentGenerator;
    private readonly ILogger<WorkflowExporter> _logger;
    
    /// <summary>Initializes a new workflow exporter.</summary>
    public WorkflowExporter(
        IClusterRegistry clusterRegistry,
        IBrickRegistry brickRegistry,
        ICodeGenerator codeGenerator,
        IContentGenerator contentGenerator,
        ILogger<WorkflowExporter> logger)
    {
        _clusterRegistry = clusterRegistry;
        _brickRegistry = brickRegistry;
        _codeGenerator = codeGenerator;
        _contentGenerator = contentGenerator;
        _logger = logger;
    }
    
    /// <summary>
    /// Exports a workflow in the specified mode and target format.
    /// Supports three export modes: pure deterministic, AI-generated then deterministic, and runtime AI.
    /// </summary>
    /// <param name="workflow">The workflow to export.</param>
    /// <param name="config">Export configuration specifying mode, target, and options.</param>
    /// <param name="ct">Cancellation token to cancel the export operation.</param>
    /// <returns>Export result containing generated files, requirements, and metadata.</returns>
    public async Task<ExportResult> ExportAsync(
        Workflow workflow,
        ExportConfig config,
        CancellationToken ct = default)
    {
        return config.Mode switch
        {
            ExportMode.PureDeterministic => await ExportDeterministicAsync(workflow, config, ct),
            ExportMode.AIGeneratedThenDeterministic => await GenerateThenExportAsync(workflow, config, ct),
            ExportMode.WithRuntimeAI => await ExportWithRuntimeAsync(workflow, config, ct),
            _ => throw new ArgumentException($"Unknown export mode: {config.Mode}")
        };
    }
    
    /// <summary>
    /// Exports workflow as pure deterministic code with no AI runtime dependencies.
    /// Only bricks with deterministic implementations are included.
    /// </summary>
    /// <param name="workflow">The workflow to export.</param>
    /// <param name="config">Export configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result with generated code files.</returns>
    private async Task<ExportResult> ExportDeterministicAsync(
        Workflow workflow,
        ExportConfig config,
        CancellationToken ct)
    {
        var files = new List<ExportedFile>();
        var messages = new List<string>();
        
        // Flatten all clusters into bricks
        var flattenedBricks = FlattenWorkflow(workflow);
        
        // Generate code for each brick (deterministic implementation only)
        foreach (var brick in flattenedBricks)
        {
            if (!brick.Implementations.HasDeterministic)
            {
                messages.Add($"Warning: DomainBrick '{brick.Name}' has no deterministic implementation. Using stub.");
            }
            
            var code = await _codeGenerator.GenerateDeterministicAsync(brick, config.Target, ct);
            files.Add(new ExportedFile
            {
                Path = GetFilePath(brick, config.Target),
                Content = code,
                Language = config.Target.ToString().ToLower()
            });
        }
        
        // Generate orchestration code
        var orchestrationCode = await _codeGenerator.GenerateOrchestrationAsync(workflow, config.Target, ct);
        files.Add(new ExportedFile
        {
            Path = "Orchestration" + GetExtension(config.Target),
            Content = orchestrationCode,
            Language = config.Target.ToString().ToLower()
        });
        
        // Generate project files if needed
        if (config.Output.Format == Nexo.Core.Domain.Export.OutputFormat.Project)
        {
            files.AddRange(GenerateProjectFiles(config.Target, config.Output.Namespace));
        }
        
        return new ExportResult
        {
            Success = true,
            Mode = ExportMode.PureDeterministic,
            Target = config.Target,
            Files = files,
            RuntimeRequirements = [],
            Messages = messages
        };
    }
    
    /// <summary>
    /// Generates AI content at export time, then exports deterministic code that uses the generated data.
    /// This mode allows AI-assisted content generation while producing fully deterministic runtime code.
    /// </summary>
    /// <param name="workflow">The workflow to export.</param>
    /// <param name="config">Export configuration with generation settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result with generated files and generation summary.</returns>
    private async Task<ExportResult> GenerateThenExportAsync(
        Workflow workflow,
        ExportConfig config,
        CancellationToken ct)
    {
        var generatedItems = new List<GeneratedItem>();
        var files = new List<ExportedFile>();
        
        // Find all agentic bricks that should generate content
        var flattenedBricks = FlattenWorkflow(workflow);
        var agenticBricks = flattenedBricks
            .Where(b => b.Implementations.HasAgentic)
            .Where(b => config.GenerationBrickIds?.Contains(b.Id) ?? true)
            .ToList();
        
        // Run AI generation NOW
        foreach (var brick in agenticBricks)
        {
            var generated = await _contentGenerator.GenerateAsync(
                brick,
                config.GenerationConfig!,
                ct);
            
            generatedItems.Add(new GeneratedItem
            {
                BrickId = brick.Id,
                ItemType = brick.Category.ToString(),
                VariationCount = generated.Variations.Count,
                Reviewed = !config.GenerationConfig!.RequireReview
            });
            
            // Export generated content as static data
            var dataFile = await _codeGenerator.GenerateStaticDataAsync(
                brick,
                generated,
                config.Target,
                ct);
            files.Add(dataFile);
        }
        
        // Generate deterministic code that uses the generated data
        foreach (var brick in flattenedBricks)
        {
            var code = await _codeGenerator.GenerateDeterministicWithDataAsync(
                brick,
                config.Target,
                ct);
            files.Add(new ExportedFile
            {
                Path = GetFilePath(brick, config.Target),
                Content = code,
                Language = config.Target.ToString().ToLower()
            });
        }
        
        return new ExportResult
        {
            Success = true,
            Mode = ExportMode.AIGeneratedThenDeterministic,
            Target = config.Target,
            Files = files,
            GenerationSummary = new GenerationSummary
            {
                ItemsGenerated = generatedItems.Count,
                VariationsCreated = generatedItems.Sum(i => i.VariationCount),
                Items = generatedItems
            },
            RuntimeRequirements = [],
            Messages = []
        };
    }
    
    /// <summary>
    /// Exports workflow as a runtime package that calls AI providers at execution time.
    /// Includes workflow definition, brick definitions, and runtime bootstrap code.
    /// </summary>
    /// <param name="workflow">The workflow to export.</param>
    /// <param name="config">Export configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result with runtime package files and provider requirements.</returns>
    private async Task<ExportResult> ExportWithRuntimeAsync(
        Workflow workflow,
        ExportConfig config,
        CancellationToken ct)
    {
        var files = new List<ExportedFile>();
        var requirements = new List<string> { "Nexo.Runtime" };
        
        // Export workflow definition as JSON
        var workflowJson = JsonSerializer.Serialize(workflow, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        files.Add(new ExportedFile
        {
            Path = "workflow.json",
            Content = workflowJson,
            Language = "json"
        });
        
        // Export brick definitions
        var flattenedBricks = FlattenWorkflow(workflow);
        foreach (var brick in flattenedBricks)
        {
            var brickJson = JsonSerializer.Serialize(brick);
            files.Add(new ExportedFile
            {
                Path = $"bricks/{brick.Id}.json",
                Content = brickJson,
                Language = "json"
            });
        }
        
        // Generate runtime bootstrap code
        var bootstrapCode = await _codeGenerator.GenerateRuntimeBootstrapAsync(
            workflow,
            config.Target,
            config.IncludeFallbacks,
            ct);
        files.Add(new ExportedFile
        {
            Path = "Program" + GetExtension(config.Target),
            Content = bootstrapCode,
            Language = config.Target.ToString().ToLower()
        });
        
        // Add provider requirements
        var providers = flattenedBricks
            .Where(b => b.Implementations.HasAgentic)
            .SelectMany(b => b.Implementations.Agentic!.ProviderMappings.Keys)
            .Distinct();
        
        foreach (var provider in providers)
        {
            requirements.Add($"Nexo.Provider.{provider}");
        }
        
        // Add Ollama if any brick supports offline
        if (flattenedBricks.Any(b => 
            b.Implementations.Agentic?.ProviderMappings.ContainsKey("ollama") ?? false))
        {
            requirements.Add("Ollama (local installation)");
        }
        
        return new ExportResult
        {
            Success = true,
            Mode = ExportMode.WithRuntimeAI,
            Target = config.Target,
            Files = files,
            RuntimeRequirements = requirements,
            Messages = []
        };
    }
    
    /// <summary>
    /// Flattens a workflow by extracting all bricks from all cluster instances.
    /// Removes duplicates and returns a unique list of all bricks used in the workflow.
    /// </summary>
    /// <param name="workflow">The workflow to flatten.</param>
    /// <returns>List of unique bricks used in the workflow.</returns>
    private IReadOnlyList<DomainBrick> FlattenWorkflow(Workflow workflow)
    {
        var bricks = new HashSet<DomainBrick>();
        
        foreach (var instance in workflow.Instances)
        {
            var cluster = _clusterRegistry.Get(instance.ClusterId);
            if (cluster == null) continue;
            
            foreach (var clusterBrick in cluster.Bricks)
            {
                var brick = _brickRegistry.GetBrick(clusterBrick.BrickId);
                if (brick != null)
                {
                    bricks.Add(brick);
                }
            }
        }
        
        return bricks.ToList();
    }
    
    private string GetFilePath(DomainBrick brick, ExportTarget target)
    {
        var ext = GetExtension(target);
        return $"{brick.Category}/{brick.Name.Replace(" ", "")}{ext}";
    }
    
    private string GetExtension(ExportTarget target) => target switch
    {
        ExportTarget.CSharp => ".cs",
        ExportTarget.Python => ".py",
        ExportTarget.TypeScript => ".ts",
        ExportTarget.BehaviorTree => ".bt.json",
        ExportTarget.StateMachine => ".fsm.json",
        _ => ".txt"
    };
    
    private IReadOnlyList<ExportedFile> GenerateProjectFiles(ExportTarget target, string namespaceName)
    {
        var files = new List<ExportedFile>();
        
        switch (target)
        {
            case ExportTarget.CSharp:
                files.Add(new ExportedFile
                {
                    Path = "Generated.csproj",
                    Content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>{namespaceName}</RootNamespace>
  </PropertyGroup>
</Project>",
                    Language = "xml"
                });
                break;
                
            case ExportTarget.TypeScript:
                files.Add(new ExportedFile
                {
                    Path = "package.json",
                    Content = $@"{{
  ""name"": ""{namespaceName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""type"": ""module""
}}",
                    Language = "json"
                });
                break;
        }
        
        return files;
    }
}

