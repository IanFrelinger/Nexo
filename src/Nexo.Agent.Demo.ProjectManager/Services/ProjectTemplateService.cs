using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Nexo.Agent.Demo.ProjectManager.Services;

/// <summary>
/// Handles project templates and requirements management.
/// </summary>
public partial class ProjectTemplateService
{
    private readonly ILogger _logger;

    public ProjectTemplateService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ShowProjectTemplatesAsync()
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]📋 Available Project Templates:[/]");
        AnsiConsole.WriteLine();

        var templates = new[]
        {
            new ProjectTemplate("🎮 Unity Game Demo", "Create a complete Unity game with player, enemies, and UI"),
            new ProjectTemplate("🌐 Web Application", "Build a full-stack web application with API and frontend"),
            new ProjectTemplate("📱 Mobile App", "Develop a cross-platform mobile application"),
            new ProjectTemplate("🤖 AI Service", "Create an AI-powered service with ML models"),
            new ProjectTemplate("📊 Data Pipeline", "Build a data processing and analytics pipeline"),
            new ProjectTemplate("🔧 DevOps Tool", "Create automation and deployment tools")
        };

        var table = new Table();
        table.AddColumn("Template");
        table.AddColumn("Description");

        foreach (var template in templates)
        {
            table.AddRow(template.Name, template.Description);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public async Task<ProjectRequirements> GetProjectRequirementsAsync()
    {
        await Task.CompletedTask;
        
        AnsiConsole.MarkupLine("[bold cyan]📝 Project Requirements Input[/]");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]Please describe your project requirements in natural language:[/]");
        AnsiConsole.MarkupLine("[dim]Example: 'Create a fully integrated FPS Character Controller that can be called via the Nexo aggregator'[/]");
        AnsiConsole.WriteLine();
        
        var requirements = AnsiConsole.Ask<string>("[bold]Project Description:[/]");
        
        AnsiConsole.MarkupLine("[bold]Additional Context (optional):[/]");
        AnsiConsole.MarkupLine("[dim]Example: 'Include movement, look controls, jump, physics, and full Nexo ecosystem integration'[/]");
        var context = AnsiConsole.Ask<string>("[bold]Context:[/]");
        
        AnsiConsole.MarkupLine("[bold]Priority Level:[/]");
        var priority = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .AddChoices("🔴 Critical", "🟡 High", "🟢 Medium", "🔵 Low"));
        
        AnsiConsole.MarkupLine("[bold]Estimated Timeline:[/]");
        var timeline = AnsiConsole.Ask<string>("[bold]Timeline:[/]");
        
        var projectReq = new ProjectRequirements
        {
            Description = requirements,
            Context = context,
            Priority = priority,
            Timeline = timeline,
            CreatedAt = DateTime.UtcNow
        };
        
        AnsiConsole.MarkupLine($"[green]✅ Project requirements captured![/]");
        AnsiConsole.WriteLine();
        
        // Display the captured requirements
        AnsiConsole.MarkupLine("[bold cyan]📋 Captured Requirements:[/]");
        AnsiConsole.MarkupLine($"[bold]Description:[/] {projectReq.Description}");
        AnsiConsole.MarkupLine($"[bold]Context:[/] {projectReq.Context}");
        AnsiConsole.MarkupLine($"[bold]Priority:[/] {projectReq.Priority}");
        AnsiConsole.MarkupLine($"[bold]Timeline:[/] {projectReq.Timeline}");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[yellow]🤖 The Nexo Agent will now analyze these requirements and create a detailed project plan...[/]");
        AnsiConsole.WriteLine();
        
        return projectReq;
    }

    public async Task ShowNaturalLanguageRequirementsAsync()
    {
        await Task.CompletedTask;
        
        AnsiConsole.MarkupLine("[bold]Example Natural Language Requirements:[/]");
        AnsiConsole.WriteLine();
        
        var examples = new[]
        {
            new { 
                Description = "Create a fully integrated FPS Character Controller that can be called via the Nexo aggregator",
                Context = "Include movement, look controls, jump, physics, and full Nexo ecosystem integration",
                Priority = "🔴 Critical",
                Timeline = "2 weeks"
            },
            new { 
                Description = "Build a multiplayer game lobby system with real-time chat",
                Context = "Support up to 100 concurrent users with voice chat integration",
                Priority = "🟡 High", 
                Timeline = "3 weeks"
            },
            new { 
                Description = "Develop an AI-powered level generation system",
                Context = "Procedural dungeon generation with difficulty scaling and loot distribution",
                Priority = "🟢 Medium",
                Timeline = "4 weeks"
            }
        };
        
        foreach (var example in examples)
        {
            AnsiConsole.MarkupLine($"[bold cyan]📝 Example Requirements:[/]");
            AnsiConsole.MarkupLine($"[bold]Description:[/] {example.Description}");
            AnsiConsole.MarkupLine($"[bold]Context:[/] {example.Context}");
            AnsiConsole.MarkupLine($"[bold]Priority:[/] {example.Priority}");
            AnsiConsole.MarkupLine($"[bold]Timeline:[/] {example.Timeline}");
            AnsiConsole.WriteLine();
        }
        
        AnsiConsole.MarkupLine("[yellow]🤖 The Nexo Agent analyzes these natural language requirements and automatically creates detailed project plans, tasks, and execution strategies![/]");
    }
}
