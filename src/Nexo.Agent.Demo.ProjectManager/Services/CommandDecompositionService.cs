using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Spectre.Console;

namespace Nexo.Agent.Demo.ProjectManager.Services;

/// <summary>
/// Handles command decomposition and analysis for project requirements.
/// </summary>
public partial class CommandDecompositionService
{
    private readonly ILogger _logger;
    private readonly ITaskExecutionAgent _agent;

    public CommandDecompositionService(ILogger logger, ITaskExecutionAgent agent)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public async Task<CommandDecompositionResult> DecomposeRequirementsIntoCommandsAsync(ProjectRequirements requirements)
    {
        AnsiConsole.MarkupLine("[bold cyan]🔧 Decomposing Requirements into Reusable Commands...[/]");
        AnsiConsole.WriteLine();
        
        // Use the existing Agent system to decompose requirements
        var context = new CommandDecompositionContext
        {
            TargetRuntime = "Unity",
            TargetFramework = ".NET",
            AvailableLibraries = new[] { "Unity", "Nexo.Agent", "Unity.InputSystem" },
            DocumentationSources = new[] { "Unity Documentation", "Nexo Documentation" },
            RuntimeParameters = new Dictionary<string, object>
            {
                ["Complexity"] = "High",
                ["TargetPlatform"] = "Unity"
            }
        };

        // Use the existing Agent planner to decompose requirements
        var decomposition = await _agent.ExecuteTaskAsync(
            $"Decompose requirements: {requirements.Description}",
            $"Context: {requirements.Context}, Priority: {requirements.Priority}, Timeline: {requirements.Timeline}");

        // For demo purposes, simulate the decomposition result
        // In a real implementation, this would use the actual Agent planner
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
                Description = "Integrates a component with the Nexo Agent ecosystem and aggregator for tool calling",
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

        return new CommandDecompositionResult
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
                    ["TargetRuntime"] = "Unity",
                    ["Framework"] = ".NET",
                    ["Complexity"] = "High"
                }
            },
            ReuseAnalysis = reuseAnalysis,
            DocumentationReferences = documentationReferences,
            GeneratedCodeFiles = new[] { "FPSCharacterController.cs", "NexoIntegration.cs", "InputSystem.cs", "PhysicsSystem.cs" },
            EstimatedExecutionTime = TimeSpan.FromHours(10),
            Dependencies = new[] { "Unity", "Nexo.Agent", "Unity.InputSystem" }
        };
    }

    public async Task ViewCommandDecompositionAsync(CommandDecompositionResult decomposition)
    {
        await Task.CompletedTask;
        
        AnsiConsole.MarkupLine("[bold cyan]🔧 Command Decomposition Analysis[/]");
        AnsiConsole.WriteLine();
        
        // Show original prompt
        AnsiConsole.MarkupLine($"[bold]Original Prompt:[/] {decomposition.OriginalPrompt}");
        AnsiConsole.WriteLine();
        
        // Show command sequence
        AnsiConsole.MarkupLine($"[bold cyan]📋 Command Sequence: {decomposition.Commands.Name}[/]");
        AnsiConsole.MarkupLine($"[dim]{decomposition.Commands.Description}[/]");
        AnsiConsole.WriteLine();
        
        // Show commands table
        var commandsTable = new Table();
        commandsTable.AddColumn("Command");
        commandsTable.AddColumn("Category");
        commandsTable.AddColumn("Reuse Count");
        commandsTable.AddColumn("Last Used");
        
        foreach (var command in decomposition.Commands.Commands)
        {
            commandsTable.AddRow(
                command.Name,
                command.Category,
                command.ReuseCount.ToString(),
                command.LastUsed.ToString("yyyy-MM-dd")
            );
        }
        
        AnsiConsole.Write(commandsTable);
        AnsiConsole.WriteLine();
        
        // Show reuse analysis
        AnsiConsole.MarkupLine("[bold cyan]📊 Command Reuse Analysis[/]");
        AnsiConsole.MarkupLine($"• Total Commands: [bold]{decomposition.ReuseAnalysis.TotalCommands}[/]");
        AnsiConsole.MarkupLine($"• Reused Commands: [green]{decomposition.ReuseAnalysis.ReusedCommands}[/]");
        AnsiConsole.MarkupLine($"• New Commands: [yellow]{decomposition.ReuseAnalysis.NewCommands}[/]");
        AnsiConsole.MarkupLine($"• Reuse Percentage: [bold]{decomposition.ReuseAnalysis.ReusePercentage:P1}[/]");
        AnsiConsole.WriteLine();
        
        // Show reused command details
        if (decomposition.ReuseAnalysis.ReusedCommandDetails.Length > 0)
        {
            AnsiConsole.MarkupLine("[bold cyan]🔄 Reused Command Details[/]");
            foreach (var reuse in decomposition.ReuseAnalysis.ReusedCommandDetails)
            {
                AnsiConsole.MarkupLine($"[bold]{reuse.CommandName}[/] (Used {reuse.PreviousUseCount} times)");
                AnsiConsole.MarkupLine($"  Previous Projects: {string.Join(", ", reuse.PreviousProjects)}");
                AnsiConsole.MarkupLine($"  Similar Commands: {string.Join(", ", reuse.SimilarCommands)}");
                AnsiConsole.MarkupLine($"  Similarity Score: {reuse.SimilarityScore:P1}");
                AnsiConsole.WriteLine();
            }
        }
        
        // Show documentation cross-references
        if (decomposition.DocumentationReferences.Length > 0)
        {
            AnsiConsole.MarkupLine("[bold cyan]📚 Documentation Cross-References[/]");
            foreach (var docRef in decomposition.DocumentationReferences)
            {
                AnsiConsole.MarkupLine($"[bold]{docRef.Section}[/]");
                AnsiConsole.MarkupLine($"  Source: {docRef.DocumentationSource}");
                AnsiConsole.MarkupLine($"  URL: {docRef.DocumentationUrl}");
                AnsiConsole.MarkupLine($"  Topics: {string.Join(", ", docRef.RelatedTopics)}");
                AnsiConsole.WriteLine();
            }
        }
        
        // Show generated code files
        AnsiConsole.MarkupLine("[bold cyan]💻 Generated Code Files[/]");
        foreach (var file in decomposition.GeneratedCodeFiles)
        {
            AnsiConsole.MarkupLine($"• {file}");
        }
        AnsiConsole.WriteLine();
        
        // Show execution summary
        AnsiConsole.MarkupLine("[bold cyan]⏱️ Execution Summary[/]");
        AnsiConsole.MarkupLine($"• Estimated Time: [bold]{decomposition.EstimatedExecutionTime.TotalHours:F1} hours[/]");
        AnsiConsole.MarkupLine($"• Dependencies: {string.Join(", ", decomposition.Dependencies)}");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[green]✅ Command decomposition shows {reusePercentage:P1} reuse of existing commands from the library![/]",
            decomposition.ReuseAnalysis.ReusePercentage);
    }
}
