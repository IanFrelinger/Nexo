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
/// Breaks specification into actionable development tasks.
/// </summary>
public class PlanningBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<PlanningBrick>? _logger;
    
    public PlanningBrick(IProviderFactory providerFactory, ILogger<PlanningBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "autonomous-dev.planning";
        Name = "Planning";
        Version = "1.0.0";
        Icon = "📝";
        Category = BrickCategory.Control;
        Description = "Breaks specification into actionable development tasks";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("specification", "Specification", "Parsed specification"),
                new BrickInputDefinition("project", "IProjectAdapter", "Project adapter"),
                new BrickInputDefinition("testPersona", "MockUserPersona", "Test persona")
            ],
            Outputs = [
                new BrickOutputDefinition("plan", "DevelopmentPlan", "Development plan")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "planning-deterministic",
                Name = "Heuristic Planning",
                Description = "Creates a minimal safe plan without LLM",
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
                Id = "planning-agentic",
                Name = "AI Planning",
                Description = "Uses LLM to create development plan",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are planning how to implement a feature.",
                    Temperature = 0.3,
                    MaxTokens = 4000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "5-15s",
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
        var specification = input.Get<Specification>("specification");
        var project = input.Get<IProjectAdapter>("project");
        var testPersona = input.Get<MockUserPersona>("testPersona");

        if (implementation == ImplementationType.Deterministic || context.IsAirGapped)
        {
            var deterministicPlan = BuildDeterministicPlan(specification);
            return new BrickOutput
            {
                ["plan"] = deterministicPlan,
                Summary = $"Plan (deterministic): {deterministicPlan.Tasks.Count} task(s)"
            };
        }
        
        var relevantCode = await project.GetRelevantCodeAsync(specification.AffectedAreas.ToArray(), cancellationToken);
        
        var prompt = BuildPlanningPrompt(specification, relevantCode, testPersona);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are planning how to implement a feature.",
            prompt,
            new { },
            cancellationToken);
        
        var plan = ParseDevelopmentPlan(response, specification);
        
        return new BrickOutput
        {
            ["plan"] = plan,
            Summary = $"Plan: {plan.Tasks.Count} tasks, {plan.EstimatedDuration.TotalMinutes:F1} min estimated"
        };
    }

    private static DevelopmentPlan BuildDeterministicPlan(Specification spec)
    {
        // Minimal safe plan: make a small change + build.
        var specId = $"spec:{(spec.Summary ?? "").GetHashCode():x}";
        return new DevelopmentPlan
        {
            SpecificationId = specId,
            Tasks = new List<DevelopmentTask>
            {
                new()
                {
                    Id = "task1",
                    Title = "Apply safe demo change",
                    Description = "Create/update a small non-breaking doc file (deterministic fallback).",
                    Type = TaskType.ModifyFile,
                    Steps = new List<string> { "Write demo note file", "Build project" },
                    TargetFiles = new List<string> { "NEXO_AGENT_NOTES.md" },
                    VerificationMethod = "dotnet build succeeds"
                }
            },
            Dependencies = new List<TaskDependency>(),
            PlannedChanges = new List<PlannedFileChange>
            {
                new()
                {
                    FilePath = "NEXO_AGENT_NOTES.md",
                    ChangeType = FileChangeType.Modify,
                    Description = "Add deterministic plan note"
                }
            },
            TestStrategy = new TestingStrategy
            {
                TestCases = new List<string> { "Build succeeds" },
                Persona = MockUserPersona.Average,
                UserFlows = new List<string>(),
                MinimumPassRate = 0.9
            },
            EstimatedDuration = TimeSpan.FromMinutes(1)
        };
    }
    
    private static string BuildPlanningPrompt(
        Specification spec,
        IReadOnlyList<CodeContext> relevantCode,
        MockUserPersona persona)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("## Specification");
        sb.AppendLine(JsonSerializer.Serialize(spec, new JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine();
        
        if (relevantCode.Count > 0)
        {
            sb.AppendLine("## Existing Code Context");
            foreach (var code in relevantCode.Take(5))
            {
                sb.AppendLine($"// {code.Path}");
                sb.AppendLine(code.Content.Substring(0, Math.Min(500, code.Content.Length)));
                sb.AppendLine("---");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine($@"## Your Task

Create a detailed development plan in JSON:

{{
  ""tasks"": [
    {{
      ""id"": ""task1"",
      ""title"": ""..."",
      ""description"": ""..."",
      ""type"": ""CreateFile|ModifyFile|..."",
      ""steps"": [""step1"", ""step2""],
      ""targetFiles"": [""path/to/file.cs""],
      ""verificationMethod"": ""...""
    }}
  ],
  ""dependencies"": [
    {{ ""taskId"": ""task2"", ""dependsOnTaskId"": ""task1"" }}
  ],
  ""plannedChanges"": [
    {{ ""filePath"": ""..."", ""changeType"": ""Create|Modify"", ""description"": ""..."" }}
  ],
  ""testStrategy"": {{
    ""testCases"": [""..."", ""...""],
    ""persona"": ""{persona}"",
    ""userFlows"": [""..."", ""...""],
    ""minimumPassRate"": 0.9
  }},
  ""estimatedDuration"": ""00:30:00""
}}");
        
        return sb.ToString();
    }
    
    private static DevelopmentPlan ParseDevelopmentPlan(string json, Specification spec)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new DevelopmentPlan
            {
                SpecificationId = Guid.NewGuid().ToString(),
                Tasks = ParseTasks(root),
                Dependencies = ParseDependencies(root),
                PlannedChanges = ParsePlannedChanges(root),
                TestStrategy = ParseTestStrategy(root),
                EstimatedDuration = ParseDuration(root)
            };
        }
        catch
        {
            // Fallback plan
            return new DevelopmentPlan
            {
                SpecificationId = Guid.NewGuid().ToString(),
                Tasks = new[]
                {
                    new DevelopmentTask
                    {
                        Id = "task1",
                        Title = "Implement feature",
                        Description = spec.Summary,
                        Type = TaskType.CreateFile,
                        Steps = new[] { "Generate code", "Test", "Fix issues" }
                    }
                },
                TestStrategy = new TestingStrategy { Persona = MockUserPersona.Average }
            };
        }
    }
    
    private static IReadOnlyList<DevelopmentTask> ParseTasks(JsonElement root)
    {
        if (!root.TryGetProperty("tasks", out var tasks))
            return Array.Empty<DevelopmentTask>();
        
        return tasks.EnumerateArray().Select(t => new DevelopmentTask
        {
            Id = t.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            Title = t.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Description = t.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            Type = Enum.TryParse<TaskType>(t.TryGetProperty("type", out var type) ? type.GetString() : "CreateFile", true, out var taskType) ? taskType : TaskType.CreateFile,
            Steps = t.TryGetProperty("steps", out var steps) ? steps.EnumerateArray().Select(s => s.GetString()!).ToList() : Array.Empty<string>(),
            TargetFiles = t.TryGetProperty("targetFiles", out var files) ? files.EnumerateArray().Select(f => f.GetString()!).ToList() : Array.Empty<string>(),
            VerificationMethod = t.TryGetProperty("verificationMethod", out var ver) ? ver.GetString() : null
        }).ToList();
    }
    
    private static IReadOnlyList<TaskDependency> ParseDependencies(JsonElement root)
    {
        if (!root.TryGetProperty("dependencies", out var deps))
            return Array.Empty<TaskDependency>();
        
        return deps.EnumerateArray().Select(d => new TaskDependency
        {
            TaskId = d.GetProperty("taskId").GetString() ?? "",
            DependsOnTaskId = d.GetProperty("dependsOnTaskId").GetString() ?? ""
        }).ToList();
    }
    
    private static IReadOnlyList<PlannedFileChange> ParsePlannedChanges(JsonElement root)
    {
        if (!root.TryGetProperty("plannedChanges", out var changes))
            return Array.Empty<PlannedFileChange>();
        
        return changes.EnumerateArray().Select(c => new PlannedFileChange
        {
            FilePath = c.GetProperty("filePath").GetString() ?? "",
            ChangeType = Enum.TryParse<FileChangeType>(c.TryGetProperty("changeType", out var ct) ? ct.GetString() : "Create", true, out var changeType) ? changeType : FileChangeType.Create,
            Description = c.TryGetProperty("description", out var desc) ? desc.GetString() : null
        }).ToList();
    }
    
    private static TestingStrategy ParseTestStrategy(JsonElement root)
    {
        if (!root.TryGetProperty("testStrategy", out var strategy))
            return new TestingStrategy { Persona = MockUserPersona.Average };
        
        return new TestingStrategy
        {
            TestCases = strategy.TryGetProperty("testCases", out var cases) ? cases.EnumerateArray().Select(c => c.GetString()!).ToList() : Array.Empty<string>(),
            Persona = Enum.TryParse<MockUserPersona>(strategy.TryGetProperty("persona", out var p) ? p.GetString() : "Average", true, out var persona) ? persona : MockUserPersona.Average,
            UserFlows = strategy.TryGetProperty("userFlows", out var flows) ? flows.EnumerateArray().Select(f => f.GetString()!).ToList() : Array.Empty<string>(),
            MinimumPassRate = strategy.TryGetProperty("minimumPassRate", out var rate) ? rate.GetDouble() : 0.9
        };
    }
    
    private static TimeSpan ParseDuration(JsonElement root)
    {
        if (root.TryGetProperty("estimatedDuration", out var dur))
        {
            if (dur.ValueKind == JsonValueKind.String && TimeSpan.TryParse(dur.GetString(), out var ts))
                return ts;
        }
        return TimeSpan.FromMinutes(30);
    }
}
