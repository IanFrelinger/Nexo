using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Adapters;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.AutonomousDev.Bricks;

/// <summary>
/// Generates code, assets, and configurations.
/// </summary>
public class GenerationBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<GenerationBrick>? _logger;
    
    public GenerationBrick(IProviderFactory providerFactory, ILogger<GenerationBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "autonomous-dev.generation";
        Name = "Generation";
        Version = "1.0.0";
        Icon = "✨";
        Category = BrickCategory.Generation;
        Description = "Generates code, assets, and configurations";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("task", "DevelopmentTask", "Task to implement"),
                new BrickInputDefinition("plan", "DevelopmentPlan", "Development plan"),
                new BrickInputDefinition("project", "IProjectAdapter", "Project adapter"),
                new BrickInputDefinition("previousArtifacts", "GeneratedArtifact[]", "Previous artifacts", required: false),
                new BrickInputDefinition("previousFeedback", "TestFeedback", "Previous feedback", required: false)
            ],
            Outputs = [
                new BrickOutputDefinition("artifacts", "GeneratedArtifact[]", "Generated artifacts")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "generation-deterministic",
                Name = "Template Generation",
                Description = "Generates safe placeholder artifacts without LLM",
                Executor = "TemplateExecutor",
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "< 100ms",
                    Deterministic = true,
                    RequiresNetwork = false,
                    ResourceUsage = ResourceUsage.Low
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "generation-agentic",
                Name = "AI Generation",
                Description = "Uses LLM to generate code",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are implementing a development task.",
                    Temperature = 0.2,
                    MaxTokens = 8000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "10-30s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.High
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = new[] { ImplementationType.Agentic, ImplementationType.Deterministic };
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var task = input.Get<DevelopmentTask>("task");
        var plan = input.Get<DevelopmentPlan>("plan");
        var project = input.Get<IProjectAdapter>("project");
        var previousArtifacts = input.Get<GeneratedArtifact[]>("previousArtifacts", Array.Empty<GeneratedArtifact>()) ?? Array.Empty<GeneratedArtifact>();
        var previousFeedback = input.Get<TestFeedback?>("previousFeedback", null);

        if (implementation == ImplementationType.Deterministic || context.IsAirGapped)
        {
            var deterministicArtifacts = BuildDeterministicArtifacts(task);
            return new BrickOutput
            {
                ["artifacts"] = deterministicArtifacts.ToArray(),
                Summary = $"Generated (deterministic) {deterministicArtifacts.Count} artifact(s)"
            };
        }
        
        // Get existing file content if modifying
        var existingCode = new Dictionary<string, string>();
        foreach (var targetFile in task.TargetFiles)
        {
            var content = await project.ReadFileAsync(targetFile, cancellationToken);
            if (content != null)
                existingCode[targetFile] = content;
        }
        
        var prompt = BuildGenerationPrompt(task, existingCode, previousFeedback);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are implementing a development task.",
            prompt,
            new { },
            cancellationToken);
        
        var artifacts = ParseArtifacts(response, task.Id);
        
        return new BrickOutput
        {
            ["artifacts"] = artifacts.ToArray(),
            Summary = $"Generated {artifacts.Count} artifact(s)"
        };
    }

    private static List<GeneratedArtifact> BuildDeterministicArtifacts(DevelopmentTask task)
    {
        // Safe, non-breaking fallback: write/update a markdown notes file only.
        var content =
            "# Nexo Demo Notes\n\n" +
            "Generated by deterministic fallback.\n\n" +
            $"Task: {task.Title}\n\n";

        return new List<GeneratedArtifact>
        {
            new()
            {
                Id = "artifact1",
                TaskId = task.Id,
                Type = ArtifactType.Documentation,
                TargetPath = "NEXO_AGENT_NOTES.md",
                Content = content,
                Description = "Deterministic fallback artifact",
                Language = "markdown",
                IsFullFile = true,
                Confidence = 0.8,
                Uncertainties = Array.Empty<string>(),
            }
        };
    }
    
    private static string BuildGenerationPrompt(
        DevelopmentTask task,
        Dictionary<string, string> existingCode,
        TestFeedback? previousFeedback)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("## Task");
        sb.AppendLine($"{task.Title}");
        sb.AppendLine($"{task.Description}");
        sb.AppendLine();
        sb.AppendLine("Steps:");
        foreach (var step in task.Steps)
            sb.AppendLine($"- {step}");
        sb.AppendLine();
        
        sb.AppendLine("## Target Files");
        foreach (var file in task.TargetFiles)
            sb.AppendLine($"- {file}");
        sb.AppendLine();
        
        if (existingCode.Count > 0)
        {
            sb.AppendLine("## Existing Code");
            foreach (var (path, content) in existingCode)
            {
                sb.AppendLine($"// {path}");
                sb.AppendLine(content.Substring(0, Math.Min(1000, content.Length)));
                sb.AppendLine("---");
            }
        }
        
        if (previousFeedback != null)
        {
            sb.AppendLine();
            sb.AppendLine("## Previous Iteration Feedback (MUST ADDRESS)");
            foreach (var item in previousFeedback.ActionableItems)
                sb.AppendLine($"- [{item.Priority}] {item.Problem}: {item.SuggestedAction}");
        }
        
        sb.AppendLine();
        sb.AppendLine(@"## Your Task

Generate complete file content for each target file. Respond with JSON array:

[
  {
    ""targetPath"": ""path/to/file.cs"",
    ""content"": ""complete file content here"",
    ""description"": ""what this does"",
    ""language"": ""csharp"",
    ""isFullFile"": true,
    ""confidence"": 0.9,
    ""uncertainties"": [""any assumptions""]
  }
]");
        
        return sb.ToString();
    }
    
    private static List<GeneratedArtifact> ParseArtifacts(string json, string taskId)
    {
        var artifacts = new List<GeneratedArtifact>();
        
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                artifacts.Add(new GeneratedArtifact
                {
                    Id = Guid.NewGuid().ToString(),
                    TaskId = taskId,
                    Type = ArtifactType.SourceCode,
                    TargetPath = el.GetProperty("targetPath").GetString() ?? "",
                    Content = el.GetProperty("content").GetString() ?? "",
                    Description = el.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    Language = el.TryGetProperty("language", out var lang) ? lang.GetString() ?? "text" : "text",
                    IsFullFile = el.TryGetProperty("isFullFile", out var full) && full.GetBoolean(),
                    Confidence = el.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.8,
                    Uncertainties = el.TryGetProperty("uncertainties", out var unc)
                        ? unc.EnumerateArray().Select(u => u.GetString()!).ToList()
                        : Array.Empty<string>()
                });
            }
        }
        catch
        {
            // Return empty on parse error
        }
        
        return artifacts;
    }
}
