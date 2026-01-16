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
/// Parses and understands development requirements.
/// </summary>
public class SpecificationBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<SpecificationBrick>? _logger;
    
    public SpecificationBrick(IProviderFactory providerFactory, ILogger<SpecificationBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "autonomous-dev.specification";
        Name = "Specification";
        Version = "1.0.0";
        Icon = "📋";
        Category = BrickCategory.Analysis;
        Description = "Parses and understands development requirements";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("task", "string", "Task description"),
                new BrickInputDefinition("detailedSpec", "string", "Detailed specification", required: false),
                new BrickInputDefinition("references", "string[]", "Reference materials", required: false),
                new BrickInputDefinition("acceptanceCriteria", "string", "Acceptance criteria", required: false),
                new BrickInputDefinition("project", "IProjectAdapter", "Project adapter")
            ],
            Outputs = [
                new BrickOutputDefinition("specification", "Specification", "Parsed specification")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "specification-deterministic",
                Name = "Heuristic Specification",
                Description = "Creates a minimal spec from inputs without LLM",
                Executor = "RuleEngineExecutor",
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
                Id = "specification-agentic",
                Name = "AI Specification",
                Description = "Uses LLM to understand requirements",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are analyzing a development task to create a clear specification.",
                    Temperature = 0.3,
                    MaxTokens = 3000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "3-10s",
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
        var task = input.Get<string>("task");
        var detailedSpec = input.Get<string?>("detailedSpec", null);
        var references = input.Get<string[]>("references", Array.Empty<string>()) ?? Array.Empty<string>();
        var acceptanceCriteria = input.Get<string?>("acceptanceCriteria", null);
        var project = input.Get<IProjectAdapter>("project");

        if (implementation == ImplementationType.Deterministic || context.IsAirGapped)
        {
            var deterministicSpec = BuildDeterministicSpec(task, acceptanceCriteria);
            return new BrickOutput
            {
                ["specification"] = deterministicSpec,
                Summary = $"Specification (deterministic): {deterministicSpec.Summary} ({deterministicSpec.ChangeType}, complexity: {deterministicSpec.EstimatedComplexity})"
            };
        }
        
        // Get project context
        var projectContext = await project.GetProjectContextAsync(cancellationToken);
        
        var prompt = BuildSpecificationPrompt(task, detailedSpec, references, acceptanceCriteria, projectContext);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are analyzing a development task to create a clear specification.",
            prompt,
            new { },
            cancellationToken);
        
        var spec = ParseSpecification(response);
        
        return new BrickOutput
        {
            ["specification"] = spec,
            Summary = $"Specification: {spec.Summary} ({spec.ChangeType}, complexity: {spec.EstimatedComplexity})"
        };
    }

    private static Specification BuildDeterministicSpec(string task, string? acceptanceCriteria)
    {
        return new Specification
        {
            Summary = task.Length > 120 ? task[..120] + "…" : task,
            ChangeType = ChangeType.Test,
            FunctionalRequirements = new List<Requirement>
            {
                new() { Id = "req1", Description = "Make a small safe change.", Priority = RequirementPriority.Must, IsMandatory = true }
            },
            NonFunctionalRequirements = new List<Requirement>(),
            AcceptanceCriteria = new List<AcceptanceCriterion>
            {
                new()
                {
                    Id = "ac1",
                    Description = string.IsNullOrWhiteSpace(acceptanceCriteria) ? "Project builds successfully." : acceptanceCriteria!,
                    TestDescription = "Run build successfully",
                    IsAutomatable = true
                }
            },
            AffectedAreas = new List<string> { "." },
            Risks = new List<string> { "Deterministic spec is coarse; wire agentic mode for richer planning." },
            EstimatedComplexity = 1,
            Confidence = 0.7,
            OpenQuestions = new List<string>()
        };
    }
    
    private static string BuildSpecificationPrompt(
        string task,
        string? detailedSpec,
        string[] references,
        string? acceptanceCriteria,
        ProjectContext projectContext)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("## Project Context");
        sb.AppendLine($"Type: {projectContext.ProjectType}");
        sb.AppendLine($"Language: {projectContext.PrimaryLanguage}");
        if (!string.IsNullOrEmpty(projectContext.Framework))
            sb.AppendLine($"Framework: {projectContext.Framework}");
        sb.AppendLine($"Key Files: {string.Join(", ", projectContext.KeyFiles.Take(10))}");
        sb.AppendLine();
        
        sb.AppendLine("## Task Description");
        sb.AppendLine(task);
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(detailedSpec))
        {
            sb.AppendLine("## Detailed Specification");
            sb.AppendLine(detailedSpec);
            sb.AppendLine();
        }
        
        if (!string.IsNullOrEmpty(acceptanceCriteria))
        {
            sb.AppendLine("## Acceptance Criteria (provided)");
            sb.AppendLine(acceptanceCriteria);
            sb.AppendLine();
        }
        
        if (references.Length > 0)
        {
            sb.AppendLine("## Reference Materials");
            foreach (var reference in references)
                sb.AppendLine($"- {reference}");
            sb.AppendLine();
        }
        
        sb.AppendLine(@"## Your Task

Analyze this request and produce a structured specification in JSON:

{
  ""summary"": ""Clear, actionable one-liner"",
  ""changeType"": ""NewFeature|BugFix|Refactor|Performance|UI_UX|Security|Configuration|Documentation|Test"",
  ""functionalRequirements"": [
    { ""id"": ""req1"", ""description"": ""..."", ""priority"": ""Must|Should|Could|Wont"", ""isMandatory"": true }
  ],
  ""nonFunctionalRequirements"": [...],
  ""acceptanceCriteria"": [
    { ""id"": ""ac1"", ""description"": ""..."", ""testDescription"": ""..."", ""isAutomatable"": true }
  ],
  ""affectedAreas"": [""path/to/file1"", ""path/to/file2""],
  ""risks"": [""risk1"", ""risk2""],
  ""complexity"": 5,
  ""confidence"": 0.8,
  ""openQuestions"": [""question1""]
}");
        
        return sb.ToString();
    }
    
    private static Specification ParseSpecification(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new Specification
            {
                Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                ChangeType = Enum.TryParse<ChangeType>(
                    root.TryGetProperty("changeType", out var ct) ? ct.GetString() : "NewFeature",
                    true, out var changeType) ? changeType : ChangeType.NewFeature,
                FunctionalRequirements = ParseRequirements(root, "functionalRequirements"),
                NonFunctionalRequirements = ParseRequirements(root, "nonFunctionalRequirements"),
                AcceptanceCriteria = ParseAcceptanceCriteria(root),
                AffectedAreas = ParseStringArray(root, "affectedAreas"),
                Risks = ParseStringArray(root, "risks"),
                EstimatedComplexity = root.TryGetProperty("complexity", out var comp) ? comp.GetInt32() : 5,
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.8,
                OpenQuestions = ParseStringArray(root, "openQuestions")
            };
        }
        catch (Exception ex)
        {
            // Fallback specification
            return new Specification
            {
                Summary = "Failed to parse specification",
                ChangeType = ChangeType.NewFeature,
                EstimatedComplexity = 5,
                Confidence = 0.1,
                OpenQuestions = new[] { $"Parse error: {ex.Message}" }
            };
        }
    }
    
    private static IReadOnlyList<Requirement> ParseRequirements(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return Array.Empty<Requirement>();
        
        return prop.EnumerateArray().Select(r => new Requirement
        {
            Id = r.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            Description = r.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            Priority = Enum.TryParse<RequirementPriority>(
                r.TryGetProperty("priority", out var p) ? p.GetString() : "Must",
                true, out var priority) ? priority : RequirementPriority.Must,
            IsMandatory = r.TryGetProperty("isMandatory", out var m) && m.GetBoolean()
        }).ToList();
    }
    
    private static IReadOnlyList<AcceptanceCriterion> ParseAcceptanceCriteria(JsonElement root)
    {
        if (!root.TryGetProperty("acceptanceCriteria", out var prop))
            return Array.Empty<AcceptanceCriterion>();
        
        return prop.EnumerateArray().Select(ac => new AcceptanceCriterion
        {
            Id = ac.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            Description = ac.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            TestDescription = ac.TryGetProperty("testDescription", out var test) ? test.GetString() ?? "" : "",
            IsAutomatable = ac.TryGetProperty("isAutomatable", out var auto) && auto.GetBoolean()
        }).ToList();
    }
    
    private static IReadOnlyList<string> ParseStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return Array.Empty<string>();
        
        return prop.EnumerateArray().Select(e => e.GetString()!).Where(s => s != null).ToList();
    }
}
