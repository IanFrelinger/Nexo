using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Implementations;
using Nexo.Agent.Models;
using Nexo.Agent.Tools.Builtin;
using Nexo.Observability;
using Nexo.Agent.Demo.ProjectManager.Services;
using Spectre.Console;

namespace Nexo.Agent.Demo.ProjectManager;

/// <summary>
/// Interactive Project Manager/Designer demo where the user tasks the Agent with project generation and validation.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create host
        var host = ApplicationBootstrap.CreateHost(args);

        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var agent = host.Services.GetRequiredService<ITaskExecutionAgent>();

        try
        {
            logger.LogInformation("Starting Nexo Project Manager Demo");

            // Run the project manager demo
            var demo = new ProjectManagerDemo(agent, logger);
            
            // Check for demo mode flag
            if (args.Length > 0 && args[0] == "--demo")
            {
                return await demo.RunDemoModeAsync();
            }
            else
            {
                return await demo.RunAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in project manager demo");
            return 1;
        }
        finally
        {
            await host.StopAsync();
        }
    }
}

/// <summary>
/// Project Manager demo where the user acts as PM/Designer and tasks the Agent.
/// </summary>
public class ProjectManagerDemo
{
    private readonly ITaskExecutionAgent _agent;
    private readonly ILogger _logger;
    private readonly ProjectTaskManager _taskManager;
    private readonly ProjectStatusReporter _statusReporter;
    private readonly ProjectTemplateService _templateService;
    private readonly CommandDecompositionService _commandService;
    private readonly AgentConfigurationService _configService;
    private readonly DemoModeService _demoService;

    public ProjectManagerDemo(ITaskExecutionAgent agent, ILogger logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize services
        _taskManager = new ProjectTaskManager(logger);
        _statusReporter = new ProjectStatusReporter(logger);
        _templateService = new ProjectTemplateService(logger);
        _commandService = new CommandDecompositionService(logger, agent);
        _configService = new AgentConfigurationService(logger);
        _demoService = new DemoModeService(logger, _taskManager, _statusReporter, _templateService);
    }

    public async Task<int> RunAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Nexo Project Manager").Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold green]Welcome to the Nexo Project Manager Demo![/]");
        AnsiConsole.MarkupLine("You are the [bold cyan]Project Manager/Designer[/]. Task the Agent with project generation and validation.");
        AnsiConsole.WriteLine();

        // Get natural language project requirements
        var projectRequirements = await _templateService.GetProjectRequirementsAsync();
        
        // Decompose requirements into reusable commands using existing Agent system
        var commandDecomposition = await _commandService.DecomposeRequirementsIntoCommandsAsync(projectRequirements);
        
        // Show available project templates
        await _templateService.ShowProjectTemplatesAsync();

        // Main project management loop
        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[bold cyan]What would you like to do?[/]")
                .AddChoices(new[]
                {
                    "📋 Create New Project Task",
                    "🎯 Assign Task to Agent",
                    "📊 Review Project Status",
                    "🔍 Run Validation Tests",
                    "📈 Generate Project Report",
                    "🛠️ Configure Agent Settings",
                    "📁 View Project History",
                    "🔧 View Command Decomposition",
                    "❌ Exit"
                }));

            switch (choice)
            {
                case "📋 Create New Project Task":
                    await _taskManager.CreateProjectTaskAsync();
                    break;
                case "🎯 Assign Task to Agent":
                    await _taskManager.AssignTaskToAgentAsync(_agent);
                    break;
                case "📊 Review Project Status":
                    await _statusReporter.ReviewProjectStatusAsync(_taskManager.Tasks);
                    break;
                case "🔍 Run Validation Tests":
                    await _statusReporter.RunValidationTestsAsync();
                    break;
                case "📈 Generate Project Report":
                    await _statusReporter.GenerateProjectReportAsync(_taskManager.Tasks);
                    break;
                case "🛠️ Configure Agent Settings":
                    await _configService.ConfigureAgentSettingsAsync();
                    break;
                case "📁 View Project History":
                    await _statusReporter.ViewProjectHistoryAsync(_taskManager.Tasks);
                    break;
                case "🔧 View Command Decomposition":
                    await _commandService.ViewCommandDecompositionAsync(commandDecomposition);
                    break;
                case "❌ Exit":
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    return 0;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
        }
    }

    /// <summary>
    /// Runs the demo in non-interactive mode, showcasing the full project management workflow.
    /// </summary>
    public async Task<int> RunDemoModeAsync()
    {
        return await _demoService.RunDemoModeAsync();
    }
}

// Data models
public class ProjectTask
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int EstimatedEffort { get; set; }
    public int? ActualEffort { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ProjectTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ProjectTemplate(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

public class ProjectValidation
{
    public string TaskId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Score { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; }
}

public class ProjectReport
{
    public string ProjectName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalEffort { get; set; }
    public double AverageValidationScore { get; set; }
    public string[] TopRecommendations { get; set; } = Array.Empty<string>();
}

public class ProjectRequirements
{
    public string Description { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Timeline { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Command decomposition models
public class Command
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int ReuseCount { get; set; }
    public DateTime LastUsed { get; set; }
}

public class CommandSequence
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Command[] Commands { get; set; } = Array.Empty<Command>();
    public CommandDependency[] Dependencies { get; set; } = Array.Empty<CommandDependency>();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class CommandDependency
{
    public string CommandId { get; set; } = string.Empty;
    public string DependencyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CommandReuseAnalysis
{
    public int TotalCommands { get; set; }
    public int ReusedCommands { get; set; }
    public int NewCommands { get; set; }
    public double ReusePercentage { get; set; }
    public CommandReuse[] ReusedCommandDetails { get; set; } = Array.Empty<CommandReuse>();
    public CommandGap[] IdentifiedGaps { get; set; } = Array.Empty<CommandGap>();
}

public class CommandReuse
{
    public string CommandId { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public int PreviousUseCount { get; set; }
    public string[] PreviousProjects { get; set; } = Array.Empty<string>();
    public string[] SimilarCommands { get; set; } = Array.Empty<string>();
    public double SimilarityScore { get; set; }
}

public class CommandGap
{
    public string GapType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public class DocumentationCrossReference
{
    public string CommandId { get; set; } = string.Empty;
    public string DocumentationSource { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string[] RelatedTopics { get; set; } = Array.Empty<string>();
    public string[] CodeExamples { get; set; } = Array.Empty<string>();
}

public class CommandDecompositionContext
{
    public string TargetRuntime { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public string[] AvailableLibraries { get; set; } = Array.Empty<string>();
    public string[] DocumentationSources { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> RuntimeParameters { get; set; } = new();
}

public class CommandDecompositionResult
{
    public string OriginalPrompt { get; set; } = string.Empty;
    public CommandSequence Commands { get; set; } = new();
    public CommandReuseAnalysis ReuseAnalysis { get; set; } = new();
    public DocumentationCrossReference[] DocumentationReferences { get; set; } = Array.Empty<DocumentationCrossReference>();
    public string[] GeneratedCodeFiles { get; set; } = Array.Empty<string>();
    public TimeSpan EstimatedExecutionTime { get; set; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
}
