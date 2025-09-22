using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Implementations;
using Nexo.Agent.Tools.Builtin;
using Nexo.Observability;
using Spectre.Console;
using System.Text.Json;

namespace Nexo.Agent.Demo.ProjectManager;

/// <summary>
/// Interactive Project Manager/Designer demo where the user tasks the Agent with project generation and validation.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create host
        var host = CreateHostBuilder(args).Build();

        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var agent = host.Services.GetRequiredService<IAgent>();

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

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add observability
                services.AddNexoObservability(context.Configuration);

                // Add Agent services
                services.AddSingleton<IAgentPlanner, SimplePlanner>();
                services.AddSingleton<IToolRegistry, ToolRegistry>();
                services.AddSingleton<IToolBroker, ToolBroker>();
                services.AddSingleton<IToolFactory, PipelineToolFactory>();
                services.AddSingleton<IAgent, AtlasAgent>();

                // Add built-in tools
                services.AddSingleton<FileReadTool>();
                services.AddSingleton<CsvQueryTool>();
                services.AddSingleton<ReportWriteTool>();
                services.AddSingleton<SummarizeTool>();
            });
}

/// <summary>
/// Project Manager demo where the user acts as PM/Designer and tasks the Agent.
/// </summary>
public class ProjectManagerDemo
{
    private readonly IAgent _agent;
    private readonly ILogger _logger;
    private readonly List<ProjectTask> _projectTasks = new();
    private readonly List<ProjectValidation> _validations = new();

    public ProjectManagerDemo(IAgent agent, ILogger logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> RunAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Nexo Project Manager").Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold green]Welcome to the Nexo Project Manager Demo![/]");
        AnsiConsole.MarkupLine("You are the [bold cyan]Project Manager/Designer[/]. Task the Agent with project generation and validation.");
        AnsiConsole.WriteLine();

        // Show available project templates
        await ShowProjectTemplatesAsync();

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
                    "❌ Exit"
                }));

            switch (choice)
            {
                case "📋 Create New Project Task":
                    await CreateProjectTaskAsync();
                    break;
                case "🎯 Assign Task to Agent":
                    await AssignTaskToAgentAsync();
                    break;
                case "📊 Review Project Status":
                    await ReviewProjectStatusAsync();
                    break;
                case "🔍 Run Validation Tests":
                    await RunValidationTestsAsync();
                    break;
                case "📈 Generate Project Report":
                    await GenerateProjectReportAsync();
                    break;
                case "🛠️ Configure Agent Settings":
                    await ConfigureAgentSettingsAsync();
                    break;
                case "📁 View Project History":
                    await ViewProjectHistoryAsync();
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

    private async Task ShowProjectTemplatesAsync()
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

    private async Task CreateProjectTaskAsync()
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]📋 Create New Project Task[/]");
        AnsiConsole.WriteLine();

        var taskName = AnsiConsole.Ask<string>("[bold]Task Name:[/]");
        var taskDescription = AnsiConsole.Ask<string>("[bold]Task Description:[/]");
        var priority = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Priority:[/]")
            .AddChoices("🔴 Critical", "🟡 High", "🟢 Medium", "🔵 Low"));

        var taskType = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Task Type:[/]")
            .AddChoices("🏗️ Development", "🧪 Testing", "📊 Analysis", "🔍 Validation", "📝 Documentation"));

        var estimatedEffort = AnsiConsole.Ask<int>("[bold]Estimated Effort (hours):[/]");

        var task = new ProjectTask
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = taskName,
            Description = taskDescription,
            Priority = priority,
            Type = taskType,
            EstimatedEffort = estimatedEffort,
            Status = "📝 Created",
            CreatedAt = DateTime.UtcNow,
            AssignedTo = "Agent"
        };

        _projectTasks.Add(task);

        AnsiConsole.MarkupLine($"[green]✅ Task created successfully![/]");
        AnsiConsole.MarkupLine($"[dim]Task ID: {task.Id}[/]");
        AnsiConsole.MarkupLine($"[dim]Status: {task.Status}[/]");
    }

    private async Task AssignTaskToAgentAsync()
    {
        if (!_projectTasks.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No tasks available. Create a task first.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold cyan]🎯 Assign Task to Agent[/]");
        AnsiConsole.WriteLine();

        var task = AnsiConsole.Prompt(new SelectionPrompt<ProjectTask>()
            .Title("[bold]Select task to assign:[/]")
            .UseConverter(t => $"{t.Name} ({t.Priority}) - {t.Status}")
            .AddChoices(_projectTasks.Where(t => t.Status == "📝 Created")));

        if (task == null)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No unassigned tasks available.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Assigning task: {task.Name}[/]");
        AnsiConsole.WriteLine();

        // Simulate agent task execution
        await SimulateAgentTaskExecutionAsync(task);
    }

    private async Task SimulateAgentTaskExecutionAsync(ProjectTask task)
    {
        AnsiConsole.MarkupLine("[bold cyan]🤖 Agent is working on the task...[/]");
        AnsiConsole.WriteLine();

        // Update task status
        task.Status = "🔄 In Progress";
        task.StartedAt = DateTime.UtcNow;

        // Simulate different types of work based on task type
        switch (task.Type)
        {
            case "🏗️ Development":
                await SimulateDevelopmentWorkAsync(task);
                break;
            case "🧪 Testing":
                await SimulateTestingWorkAsync(task);
                break;
            case "📊 Analysis":
                await SimulateAnalysisWorkAsync(task);
                break;
            case "🔍 Validation":
                await SimulateValidationWorkAsync(task);
                break;
            case "📝 Documentation":
                await SimulateDocumentationWorkAsync(task);
                break;
        }

        // Mark task as completed
        task.Status = "✅ Completed";
        task.CompletedAt = DateTime.UtcNow;
        task.ActualEffort = (int)(DateTime.UtcNow - task.StartedAt.Value).TotalMinutes / 60;

        AnsiConsole.MarkupLine($"[green]✅ Task completed successfully![/]");
        AnsiConsole.MarkupLine($"[dim]Actual effort: {task.ActualEffort} hours[/]");
    }

    private async Task SimulateDevelopmentWorkAsync(ProjectTask task)
    {
        var steps = new[]
        {
            "🔍 Analyzing requirements",
            "🏗️ Setting up project structure",
            "💻 Writing code",
            "🔧 Configuring dependencies",
            "🧪 Running unit tests",
            "📦 Building project"
        };

        foreach (var step in steps)
        {
            AnsiConsole.MarkupLine($"[yellow]{step}...[/]");
            await Task.Delay(1000);
        }
    }

    private async Task SimulateTestingWorkAsync(ProjectTask task)
    {
        var steps = new[]
        {
            "🧪 Writing test cases",
            "🔍 Running automated tests",
            "👁️ Visual validation testing",
            "📊 Performance testing",
            "🔒 Security testing",
            "📋 Generating test report"
        };

        foreach (var step in steps)
        {
            AnsiConsole.MarkupLine($"[yellow]{step}...[/]");
            await Task.Delay(1000);
        }
    }

    private async Task SimulateAnalysisWorkAsync(ProjectTask task)
    {
        var steps = new[]
        {
            "📊 Collecting metrics",
            "🔍 Analyzing code quality",
            "📈 Performance analysis",
            "🎯 Identifying bottlenecks",
            "💡 Generating recommendations",
            "📋 Creating analysis report"
        };

        foreach (var step in steps)
        {
            AnsiConsole.MarkupLine($"[yellow]{step}...[/]");
            await Task.Delay(1000);
        }
    }

    private async Task SimulateValidationWorkAsync(ProjectTask task)
    {
        var steps = new[]
        {
            "👁️ Visual validation with OLLama",
            "🎮 Gameplay testing",
            "♿ Accessibility validation",
            "⚡ Performance validation",
            "🔒 Security validation",
            "📋 Validation report generation"
        };

        foreach (var step in steps)
        {
            AnsiConsole.MarkupLine($"[yellow]{step}...[/]");
            await Task.Delay(1000);
        }

        // Add validation result
        _validations.Add(new ProjectValidation
        {
            TaskId = task.Id,
            Type = "Comprehensive",
            Score = 0.85,
            Issues = new[] { "Minor UI contrast issue", "Performance optimization needed" },
            Recommendations = new[] { "Improve color contrast", "Optimize particle effects" },
            Timestamp = DateTime.UtcNow
        });
    }

    private async Task SimulateDocumentationWorkAsync(ProjectTask task)
    {
        var steps = new[]
        {
            "📝 Writing technical documentation",
            "🎯 Creating user guides",
            "🔧 API documentation",
            "📊 Generating diagrams",
            "🔍 Reviewing documentation",
            "📦 Publishing documentation"
        };

        foreach (var step in steps)
        {
            AnsiConsole.MarkupLine($"[yellow]{step}...[/]");
            await Task.Delay(1000);
        }
    }

    private async Task ReviewProjectStatusAsync()
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]📊 Project Status Review[/]");
        AnsiConsole.WriteLine();

        if (!_projectTasks.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No tasks created yet.[/]");
            return;
        }

        // Create status table
        var table = new Table();
        table.AddColumn("Task");
        table.AddColumn("Type");
        table.AddColumn("Priority");
        table.AddColumn("Status");
        table.AddColumn("Effort");

        foreach (var task in _projectTasks)
        {
            table.AddRow(
                task.Name,
                task.Type,
                task.Priority,
                task.Status,
                $"{task.ActualEffort ?? 0}/{task.EstimatedEffort}h"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Show summary statistics
        var totalTasks = _projectTasks.Count;
        var completedTasks = _projectTasks.Count(t => t.Status == "✅ Completed");
        var inProgressTasks = _projectTasks.Count(t => t.Status == "🔄 In Progress");
        var totalEffort = _projectTasks.Sum(t => t.ActualEffort ?? 0);

        AnsiConsole.MarkupLine("[bold cyan]📈 Summary:[/]");
        AnsiConsole.MarkupLine($"• Total Tasks: [bold]{totalTasks}[/]");
        AnsiConsole.MarkupLine($"• Completed: [green]{completedTasks}[/]");
        AnsiConsole.MarkupLine($"• In Progress: [yellow]{inProgressTasks}[/]");
        AnsiConsole.MarkupLine($"• Total Effort: [bold]{totalEffort} hours[/]");
        AnsiConsole.MarkupLine($"• Completion Rate: [bold]{(double)completedTasks / totalTasks:P1}[/]");
    }

    private async Task RunValidationTestsAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]🔍 Running Validation Tests[/]");
        AnsiConsole.WriteLine();

        var validationTypes = new[]
        {
            "👁️ Visual Validation",
            "🎮 Gameplay Testing",
            "♿ Accessibility Testing",
            "⚡ Performance Testing",
            "🔒 Security Testing"
        };

        foreach (var validationType in validationTypes)
        {
            AnsiConsole.MarkupLine($"[yellow]Running {validationType}...[/]");
            await Task.Delay(1500);

            // Simulate validation results
            var score = Random.Shared.NextDouble() * 0.4 + 0.6; // 60-100%
            var status = score switch
            {
                >= 0.9 => "✅ Excellent",
                >= 0.8 => "🟢 Good",
                >= 0.7 => "🟡 Fair",
                _ => "🔴 Poor"
            };

            AnsiConsole.MarkupLine($"[green]✅ {validationType} Complete: {status} ({score:P1})[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]📊 Validation Summary:[/]");
        AnsiConsole.MarkupLine("• Overall Score: [bold]85%[/]");
        AnsiConsole.MarkupLine("• Critical Issues: [red]0[/]");
        AnsiConsole.MarkupLine("• High Priority Issues: [yellow]2[/]");
        AnsiConsole.MarkupLine("• Recommendations: [bold]5[/]");
    }

    private async Task GenerateProjectReportAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]📈 Generating Project Report[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]📊 Collecting project metrics...[/]");
        await Task.Delay(1000);

        AnsiConsole.MarkupLine("[yellow]📋 Analyzing task completion...[/]");
        await Task.Delay(1000);

        AnsiConsole.MarkupLine("[yellow]🔍 Reviewing validation results...[/]");
        await Task.Delay(1000);

        AnsiConsole.MarkupLine("[yellow]📝 Generating recommendations...[/]");
        await Task.Delay(1000);

        // Generate report
        var report = new ProjectReport
        {
            ProjectName = "Nexo Agent Foundry Demo",
            GeneratedAt = DateTime.UtcNow,
            TotalTasks = _projectTasks.Count,
            CompletedTasks = _projectTasks.Count(t => t.Status == "✅ Completed"),
            TotalEffort = _projectTasks.Sum(t => t.ActualEffort ?? 0),
            AverageValidationScore = _validations.Any() ? _validations.Average(v => v.Score) : 0.85,
            TopRecommendations = new[]
            {
                "Improve UI contrast ratios for better accessibility",
                "Optimize particle effects for better performance",
                "Add more comprehensive error handling",
                "Implement automated testing pipeline",
                "Enhance documentation coverage"
            }
        };

        AnsiConsole.MarkupLine("[green]✅ Project Report Generated![/]");
        AnsiConsole.WriteLine();

        // Display report
        AnsiConsole.MarkupLine($"[bold cyan]📋 Project Report: {report.ProjectName}[/]");
        AnsiConsole.MarkupLine($"[dim]Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]📊 Project Metrics:[/]");
        AnsiConsole.MarkupLine($"• Total Tasks: [bold]{report.TotalTasks}[/]");
        AnsiConsole.MarkupLine($"• Completed: [green]{report.CompletedTasks}[/]");
        AnsiConsole.MarkupLine($"• Total Effort: [bold]{report.TotalEffort} hours[/]");
        AnsiConsole.MarkupLine($"• Validation Score: [bold]{report.AverageValidationScore:P1}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]💡 Top Recommendations:[/]");
        for (int i = 0; i < report.TopRecommendations.Length; i++)
        {
            AnsiConsole.MarkupLine($"{i + 1}. {report.TopRecommendations[i]}");
        }
    }

    private async Task ConfigureAgentSettingsAsync()
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]🛠️ Configure Agent Settings[/]");
        AnsiConsole.WriteLine();

        var currentMode = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Current Agent Mode:[/]")
            .AddChoices("OFF", "HYBRID", "EMBEDDED"));

        var autoValidation = AnsiConsole.Confirm("[bold]Enable Auto-Validation:[/]");
        var visualAnalytics = AnsiConsole.Confirm("[bold]Enable Visual Analytics:[/]");
        var selfHealing = AnsiConsole.Confirm("[bold]Enable Self-Healing:[/]");

        AnsiConsole.MarkupLine("[green]✅ Agent settings updated![/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold cyan]🔧 Current Configuration:[/]");
        AnsiConsole.MarkupLine($"• Mode: [bold]{currentMode}[/]");
        AnsiConsole.MarkupLine($"• Auto-Validation: [bold]{(autoValidation ? "Enabled" : "Disabled")}[/]");
        AnsiConsole.MarkupLine($"• Visual Analytics: [bold]{(visualAnalytics ? "Enabled" : "Disabled")}[/]");
        AnsiConsole.MarkupLine($"• Self-Healing: [bold]{(selfHealing ? "Enabled" : "Disabled")}[/]");
    }

    private async Task ViewProjectHistoryAsync()
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]📁 Project History[/]");
        AnsiConsole.WriteLine();

        if (!_projectTasks.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No project history available.[/]");
            return;
        }

        var historyTable = new Table();
        historyTable.AddColumn("Timestamp");
        historyTable.AddColumn("Action");
        historyTable.AddColumn("Details");

        foreach (var task in _projectTasks.OrderBy(t => t.CreatedAt))
        {
            historyTable.AddRow(
                task.CreatedAt.ToString("HH:mm:ss"),
                "Task Created",
                task.Name
            );

            if (task.StartedAt.HasValue)
            {
                historyTable.AddRow(
                    task.StartedAt.Value.ToString("HH:mm:ss"),
                    "Task Started",
                    $"Assigned to Agent"
                );
            }

            if (task.CompletedAt.HasValue)
            {
                historyTable.AddRow(
                    task.CompletedAt.Value.ToString("HH:mm:ss"),
                    "Task Completed",
                    $"Effort: {task.ActualEffort}h"
                );
            }
        }

        AnsiConsole.Write(historyTable);
    }

    /// <summary>
    /// Runs the demo in non-interactive mode, showcasing the full project management workflow.
    /// </summary>
    public async Task<int> RunDemoModeAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]🤖 Running Project Manager Demo in Non-Interactive Mode[/]");
        AnsiConsole.WriteLine();

        try
        {
            // 1. Show project templates
            AnsiConsole.MarkupLine("[bold cyan]📋 Available Project Templates:[/]");
            await ShowProjectTemplatesAsync();
            AnsiConsole.WriteLine();

            // 2. Create sample project tasks
            AnsiConsole.MarkupLine("[bold cyan]📋 Creating Sample Project Tasks...[/]");
            await CreateSampleTasksAsync();
            AnsiConsole.WriteLine();

            // 3. Assign tasks to agent
            AnsiConsole.MarkupLine("[bold cyan]🎯 Assigning Tasks to Agent...[/]");
            await AssignSampleTasksToAgentAsync();
            AnsiConsole.WriteLine();

            // 4. Review project status
            AnsiConsole.MarkupLine("[bold cyan]📊 Reviewing Project Status...[/]");
            await ReviewProjectStatusAsync();
            AnsiConsole.WriteLine();

            // 5. Run validation tests
            AnsiConsole.MarkupLine("[bold cyan]🔍 Running Validation Tests...[/]");
            await RunValidationTestsAsync();
            AnsiConsole.WriteLine();

            // 6. Generate project report
            AnsiConsole.MarkupLine("[bold cyan]📈 Generating Project Report...[/]");
            await GenerateProjectReportAsync();
            AnsiConsole.WriteLine();

            // 7. Summary
            AnsiConsole.MarkupLine("[bold green]🎉 Project Manager Demo Complete![/]");
            AnsiConsole.MarkupLine("The Project Manager successfully demonstrated:");
            AnsiConsole.MarkupLine("  • Task creation and assignment");
            AnsiConsole.MarkupLine("  • Agent task execution and monitoring");
            AnsiConsole.MarkupLine("  • Project status tracking and reporting");
            AnsiConsole.MarkupLine("  • Validation testing and quality assurance");
            AnsiConsole.MarkupLine("  • Comprehensive project reporting");
            AnsiConsole.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Demo failed: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task CreateSampleTasksAsync()
    {
        var sampleTasks = new[]
        {
            new ProjectTask
            {
                Id = "task-001",
                Name = "Create Player Controller",
                Description = "Implement FPS player movement and controls",
                Priority = "🔴 Critical",
                Type = "🏗️ Development",
                EstimatedEffort = 8,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-002",
                Name = "Implement Enemy AI",
                Description = "Create NavMesh-based enemy AI with patrol and chase behaviors",
                Priority = "🟡 High",
                Type = "🏗️ Development",
                EstimatedEffort = 12,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-003",
                Name = "UI Validation Testing",
                Description = "Test UI elements for accessibility and usability",
                Priority = "🟢 Medium",
                Type = "🔍 Validation",
                EstimatedEffort = 4,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-004",
                Name = "Performance Optimization",
                Description = "Optimize game performance and reduce frame drops",
                Priority = "🟡 High",
                Type = "📊 Analysis",
                EstimatedEffort = 6,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                AssignedTo = "Agent"
            }
        };

        foreach (var task in sampleTasks)
        {
            _projectTasks.Add(task);
            AnsiConsole.MarkupLine($"  ✅ Created: {task.Name} ({task.Priority})");
            await Task.Delay(500);
        }
    }

    private async Task AssignSampleTasksToAgentAsync()
    {
        var tasksToAssign = _projectTasks.Where(t => t.Status == "📝 Created").ToList();
        
        foreach (var task in tasksToAssign)
        {
            AnsiConsole.MarkupLine($"[yellow]🤖 Agent working on: {task.Name}...[/]");
            
            // Simulate agent work
            task.Status = "🔄 In Progress";
            task.StartedAt = DateTime.UtcNow;
            
            await Task.Delay(2000);
            
            // Mark as completed
            task.Status = "✅ Completed";
            task.CompletedAt = DateTime.UtcNow;
            task.ActualEffort = task.EstimatedEffort + Random.Shared.Next(-2, 3); // ±2 hours variance
            
            AnsiConsole.MarkupLine($"[green]  ✅ Completed: {task.Name} (Effort: {task.ActualEffort}h)[/]");
            
            // Add validation result for validation tasks
            if (task.Type == "🔍 Validation")
            {
                _validations.Add(new ProjectValidation
                {
                    TaskId = task.Id,
                    Type = "Comprehensive",
                    Score = 0.85 + Random.Shared.NextDouble() * 0.1, // 85-95%
                    Issues = new[] { "Minor UI contrast issue", "Performance optimization needed" },
                    Recommendations = new[] { "Improve color contrast", "Optimize particle effects" },
                    Timestamp = DateTime.UtcNow
                });
            }
        }
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
