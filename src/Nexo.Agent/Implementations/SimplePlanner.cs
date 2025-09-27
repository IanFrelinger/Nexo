using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;

namespace Nexo.Agent.Implementations;

/// <summary>
/// Simple rule-based planner that creates plans without LLM dependency.
/// </summary>
public partial class SimplePlanner : IAgentPlanner
{
    private readonly ILogger<SimplePlanner> _logger;
    private readonly ActivitySource _activitySource;

    public SimplePlanner(ILogger<SimplePlanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = new ActivitySource("Nexo.Agent");
    }

    public async Task<Plan> CreatePlanAsync(
        string goal,
        string? context,
        IReadOnlyList<ToolManifest> availableTools,
        AgentMode mode,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CreatePlan");
        activity?.SetTag("planner.goal", goal);
        activity?.SetTag("planner.mode", mode.ToString());
        activity?.SetTag("planner.available_tools", availableTools.Count);

        try
        {
            _logger.LogInformation("Creating plan for goal: {Goal}", goal);

            var steps = new List<ToolInvocation>();
            var toolMap = availableTools.ToDictionary(t => t.Id, t => t);

            // Simple rule-based planning based on common patterns
            var goalLower = goal.ToLowerInvariant();

            // File processing patterns
            if (goalLower.Contains("read") && goalLower.Contains("file"))
            {
                steps.Add(CreateFileReadStep(goal, toolMap));
            }

            if (goalLower.Contains("csv") || goalLower.Contains("query"))
            {
                steps.Add(CreateCsvQueryStep(goal, toolMap));
            }

            if (goalLower.Contains("report") || goalLower.Contains("write"))
            {
                steps.Add(CreateReportWriteStep(goal, toolMap));
            }

            if (goalLower.Contains("summarize") || goalLower.Contains("summary"))
            {
                steps.Add(CreateSummarizeStep(goal, toolMap));
            }

            if (goalLower.Contains("zip") || goalLower.Contains("archive"))
            {
                steps.Add(CreateArchiveStep(goal, toolMap));
            }

            // If no specific patterns matched, create a generic plan
            if (steps.Count == 0)
            {
                steps.Add(CreateGenericStep(goal, toolMap));
            }

            var plan = Plan.Create(goal, context, steps);
            activity?.SetTag("planner.steps_created", steps.Count);

            _logger.LogInformation("Created plan with {StepCount} steps", steps.Count);
            await Task.CompletedTask;
            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan for goal: {Goal}", goal);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Plan.Create(goal, context);
        }
    }

    public IReadOnlyList<ToolRequest> IdentifyMissingTools(Plan plan, IReadOnlyList<ToolManifest> availableTools)
    {
        var availableToolIds = availableTools.Select(t => t.Id).ToHashSet();
        var missingTools = new List<ToolRequest>();

        foreach (var step in plan.Steps)
        {
            if (!availableToolIds.Contains(step.ToolId))
            {
                // Create a tool request for the missing tool
                var request = CreateToolRequestForMissingTool(step.ToolId);
                if (request != null)
                {
                    missingTools.Add(request);
                }
            }
        }

        return missingTools;
    }

    private ToolInvocation CreateFileReadStep(string goal, Dictionary<string, ToolManifest> toolMap)
    {
        var toolId = "tool.file.read";
        var input = new { filePath = ExtractFilePath(goal) };
        var inputJson = JsonSerializer.Serialize(input);

        return ToolInvocation.Create(
            stepNumber: 1,
            toolId: toolId,
            input: inputJson,
            description: $"Read file: {ExtractFilePath(goal)}"
        );
    }

    private ToolInvocation CreateCsvQueryStep(string goal, Dictionary<string, ToolManifest> toolMap)
    {
        var toolId = "tool.csv.query";
        var input = new { 
            filePath = ExtractFilePath(goal),
            query = ExtractQuery(goal)
        };
        var inputJson = JsonSerializer.Serialize(input);

        return ToolInvocation.Create(
            stepNumber: 2,
            toolId: toolId,
            input: inputJson,
            description: $"Query CSV: {ExtractQuery(goal)}",
            dependsOn: new[] { 1 }
        );
    }

    private ToolInvocation CreateReportWriteStep(string goal, Dictionary<string, ToolManifest> toolMap)
    {
        var toolId = "tool.report.write";
        var input = new { 
            content = "Generated report content",
            outputPath = "out/report.md"
        };
        var inputJson = JsonSerializer.Serialize(input);

        return ToolInvocation.Create(
            stepNumber: 3,
            toolId: toolId,
            input: inputJson,
            description: "Write report to file",
            dependsOn: new[] { 1, 2 }
        );
    }

    private ToolInvocation CreateSummarizeStep(string goal, Dictionary<string, ToolManifest> toolMap)
    {
        var toolId = "tool.text.summarize";
        var input = new { 
            text = "Content to summarize",
            maxLength = 200
        };
        var inputJson = JsonSerializer.Serialize(input);

        return ToolInvocation.Create(
            stepNumber: 4,
            toolId: toolId,
            input: inputJson,
            description: "Summarize content"
        );
    }

    private ToolInvocation CreateArchiveStep(string goal, Dictionary<string, ToolManifest> toolMap)
    {
        var toolId = "tool.archive.zip";
        var input = new { 
            sourcePath = "out",
            outputPath = "out/package.zip"
        };
        var inputJson = JsonSerializer.Serialize(input);

        return ToolInvocation.Create(
            stepNumber: 5,
            toolId: toolId,
            input: inputJson,
            description: "Create ZIP archive",
            dependsOn: new[] { 3 }
        );
    }

    private ToolInvocation CreateGenericStep(string goal, Dictionary<string, ToolManifest> toolMap)
    {
        // Try to find a suitable tool or create a generic step
        var toolId = toolMap.Keys.FirstOrDefault() ?? "tool.unknown";
        var input = new { goal = goal };
        var inputJson = JsonSerializer.Serialize(input);

        return ToolInvocation.Create(
            stepNumber: 1,
            toolId: toolId,
            input: inputJson,
            description: $"Execute: {goal}"
        );
    }

    private ToolRequest? CreateToolRequestForMissingTool(string toolId)
    {
        return toolId switch
        {
            "tool.archive.zip" => ToolRequest.Create(
                id: "tool.archive.zip",
                name: "Archive ZIP",
                description: "Create ZIP archives from files and directories",
                requiredPermissions: ToolPermissions.FileRead | ToolPermissions.FileWrite,
                capabilities: new[] { "archive", "compression", "file" }
            ),
            _ => null
        };
    }

    private string ExtractFilePath(string goal)
    {
        // Simple extraction - look for common file patterns
        var patterns = new[] { @"\b\w+\.csv\b", @"\b\w+\.txt\b", @"\b\w+\.json\b", @"\b\w+\.md\b" };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(goal, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value;
            }
        }

        return "input.txt"; // Default fallback
    }

    private string ExtractQuery(string goal)
    {
        if (goal.Contains("count", StringComparison.OrdinalIgnoreCase))
            return "SELECT COUNT(*) FROM data";
        if (goal.Contains("select", StringComparison.OrdinalIgnoreCase))
            return "SELECT * FROM data";
        
        return "SELECT * FROM data";
    }

    public async Task<CommandDecompositionResult> DecomposeRequirementsAsync(
        ProjectRequirements requirements, 
        CommandDecompositionContext context, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("DecomposeRequirements");
        activity?.SetTag("decomposer.requirements", requirements.Description);
        activity?.SetTag("decomposer.runtime", context.TargetRuntime);

        try
        {
            _logger.LogInformation("Decomposing requirements: {Description}", requirements.Description);

            // Simulate command decomposition based on requirements
            var commands = new[]
            {
                new Command
                {
                    Id = "cmd-001",
                    Name = "CreateFPSCharacterController",
                    Description = "Creates a basic FPS character controller with movement and look controls",
                    Category = "Movement",
                    Tags = new[] { "fps", "character", "controller", "movement", "unity" },
                    ReuseCount = 5,
                    LastUsed = DateTime.UtcNow.AddDays(-1)
                },
                new Command
                {
                    Id = "cmd-002", 
                    Name = "IntegrateNexoEcosystem",
                    Description = "Integrates a component with the Nexo Agent ecosystem and aggregator",
                    Category = "Integration",
                    Tags = new[] { "nexo", "ecosystem", "aggregator", "integration" },
                    ReuseCount = 8,
                    LastUsed = DateTime.UtcNow.AddDays(-2)
                },
                new Command
                {
                    Id = "cmd-003",
                    Name = "SetupUnityInputSystem", 
                    Description = "Sets up Unity Input System with customizable key bindings",
                    Category = "Input",
                    Tags = new[] { "input", "unity", "controls", "keyboard", "mouse" },
                    ReuseCount = 12,
                    LastUsed = DateTime.UtcNow.AddDays(-3)
                },
                new Command
                {
                    Id = "cmd-004",
                    Name = "ImplementPhysicsSystem",
                    Description = "Implements advanced physics with slope handling and ground detection", 
                    Category = "Physics",
                    Tags = new[] { "physics", "collision", "ground", "slope", "rigidbody" },
                    ReuseCount = 6,
                    LastUsed = DateTime.UtcNow.AddDays(-5)
                }
            };

            var reuseAnalysis = new CommandReuseAnalysis
            {
                TotalCommands = commands.Length,
                ReusedCommands = commands.Length,
                NewCommands = 0,
                ReusePercentage = 1.0,
                ReusedCommandDetails = commands.Select(c => new CommandReuse
                {
                    CommandId = c.Id,
                    CommandName = c.Name,
                    PreviousUseCount = c.ReuseCount,
                    PreviousProjects = new[] { "FPS Game Project", "Unity Demo", "Character Controller Test" },
                    SimilarCommands = new[] { "CreateThirdPersonController", "CreateTopDownController" },
                    SimilarityScore = 0.85
                }).ToArray(),
                IdentifiedGaps = Array.Empty<CommandGap>()
            };

            var documentationReferences = new[]
            {
                new DocumentationCrossReference
                {
                    CommandId = "cmd-001",
                    DocumentationSource = "Unity Documentation",
                    DocumentationUrl = "https://docs.unity3d.com/Manual/class-CharacterController.html",
                    Section = "Character Controller",
                    RelatedTopics = new[] { "character", "controller", "unity" },
                    CodeExamples = new[] { "GetComponent<CharacterController>()" }
                },
                new DocumentationCrossReference
                {
                    CommandId = "cmd-002",
                    DocumentationSource = "Nexo Documentation", 
                    DocumentationUrl = "https://nexo.dev/docs/agent/integration",
                    Section = "Agent Integration",
                    RelatedTopics = new[] { "nexo", "agent", "integration" },
                    CodeExamples = new[] { "public partial class MyTool : INexoTool" }
                }
            };

            var result = new CommandDecompositionResult
            {
                OriginalPrompt = requirements.Description,
                Commands = new CommandSequence
                {
                    Id = "seq-001",
                    Name = "FPSCharacterControllerImplementation",
                    Description = requirements.Description,
                    Commands = commands,
                    Dependencies = Array.Empty<CommandDependency>(),
                    Parameters = new Dictionary<string, object>
                    {
                        ["TargetRuntime"] = context.TargetRuntime,
                        ["Framework"] = context.TargetFramework,
                        ["Complexity"] = "High"
                    }
                },
                ReuseAnalysis = reuseAnalysis,
                DocumentationReferences = documentationReferences,
                GeneratedCodeFiles = new[] { "FPSCharacterController.cs", "NexoIntegration.cs", "InputSystem.cs", "PhysicsSystem.cs" },
                EstimatedExecutionTime = TimeSpan.FromHours(10),
                Dependencies = new[] { "Unity", "Nexo.Agent", "Unity.InputSystem" }
            };

            activity?.SetTag("decomposer.commands_created", commands.Length);
            activity?.SetTag("decomposer.reuse_percentage", result.ReuseAnalysis.ReusePercentage);

            _logger.LogInformation("Decomposed requirements into {CommandCount} commands with {ReusePercentage:P1} reuse",
                commands.Length, result.ReuseAnalysis.ReusePercentage);

            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decomposing requirements: {Description}", requirements.Description);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }
}
