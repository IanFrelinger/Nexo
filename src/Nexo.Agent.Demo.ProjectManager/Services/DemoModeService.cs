using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Nexo.Agent.Demo.ProjectManager.Services;

/// <summary>
/// Handles demo mode execution and sample data generation.
/// </summary>
public class DemoModeService
{
    private readonly ILogger _logger;
    private readonly ProjectTaskManager _taskManager;
    private readonly ProjectStatusReporter _statusReporter;
    private readonly ProjectTemplateService _templateService;

    public DemoModeService(
        ILogger logger,
        ProjectTaskManager taskManager,
        ProjectStatusReporter statusReporter,
        ProjectTemplateService templateService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        _statusReporter = statusReporter ?? throw new ArgumentNullException(nameof(statusReporter));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
    }

    public async Task<int> RunDemoModeAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]🤖 Running Project Manager Demo in Non-Interactive Mode[/]");
        AnsiConsole.WriteLine();

        try
        {
            // 1. Show natural language requirements input
            AnsiConsole.MarkupLine("[bold cyan]📝 Natural Language Project Requirements:[/]");
            await _templateService.ShowNaturalLanguageRequirementsAsync();
            AnsiConsole.WriteLine();

            // 2. Show project templates
            AnsiConsole.MarkupLine("[bold cyan]📋 Available Project Templates:[/]");
            await _templateService.ShowProjectTemplatesAsync();
            AnsiConsole.WriteLine();

            // 3. Create sample project tasks
            AnsiConsole.MarkupLine("[bold cyan]📋 Creating Sample Project Tasks...[/]");
            await CreateSampleTasksAsync();
            AnsiConsole.WriteLine();

            // 4. Assign tasks to agent
            AnsiConsole.MarkupLine("[bold cyan]🎯 Assigning Tasks to Agent...[/]");
            await AssignSampleTasksToAgentAsync();
            AnsiConsole.WriteLine();

            // 5. Review project status
            AnsiConsole.MarkupLine("[bold cyan]📊 Reviewing Project Status...[/]");
            await _statusReporter.ReviewProjectStatusAsync(_taskManager.Tasks);
            AnsiConsole.WriteLine();

            // 6. Run validation tests
            AnsiConsole.MarkupLine("[bold cyan]🔍 Running Validation Tests...[/]");
            await _statusReporter.RunValidationTestsAsync();
            AnsiConsole.WriteLine();

            // 7. Generate project report
            AnsiConsole.MarkupLine("[bold cyan]📈 Generating Project Report...[/]");
            await _statusReporter.GenerateProjectReportAsync(_taskManager.Tasks);
            AnsiConsole.WriteLine();

            // 8. Summary
            AnsiConsole.MarkupLine("[bold green]🎉 FPS Character Controller Project Demo Complete![/]");
            AnsiConsole.MarkupLine("The Project Manager successfully demonstrated:");
            AnsiConsole.MarkupLine("  • Natural language requirements capture and analysis");
            AnsiConsole.MarkupLine("  • FPS Character Controller task creation and assignment");
            AnsiConsole.MarkupLine("  • Nexo ecosystem integration planning");
            AnsiConsole.MarkupLine("  • Agent task execution and monitoring");
            AnsiConsole.MarkupLine("  • Real-time project status tracking and reporting");
            AnsiConsole.MarkupLine("  • Validation testing and quality assurance");
            AnsiConsole.MarkupLine("  • Comprehensive project reporting with actionable insights");
            AnsiConsole.WriteLine();
            
            AnsiConsole.MarkupLine("[bold cyan]🎮 FPS Character Controller Features Delivered:[/]");
            AnsiConsole.MarkupLine("  • Core movement, look, and jump mechanics");
            AnsiConsole.MarkupLine("  • Unity Input System integration");
            AnsiConsole.MarkupLine("  • Advanced physics and collision handling");
            AnsiConsole.MarkupLine("  • Full Nexo Agent ecosystem integration");
            AnsiConsole.MarkupLine("  • Nexo aggregator tool calling capabilities");
            AnsiConsole.MarkupLine("  • Performance optimization and validation");
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
                Name = "FPS Character Controller Core",
                Description = "Implement FPS character controller with movement, look, jump, and physics integration",
                Priority = "🔴 Critical",
                Type = "🏗️ Development",
                EstimatedEffort = 12,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-002",
                Name = "Nexo Ecosystem Integration",
                Description = "Integrate FPS controller with Nexo Agent ecosystem and aggregator for tool calling",
                Priority = "🔴 Critical",
                Type = "🏗️ Development",
                EstimatedEffort = 8,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-003",
                Name = "Input System Integration",
                Description = "Implement Unity Input System with customizable key bindings and gamepad support",
                Priority = "🟡 High",
                Type = "🏗️ Development",
                EstimatedEffort = 6,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-004",
                Name = "Physics & Collision System",
                Description = "Implement advanced physics with slope handling, ground detection, and collision response",
                Priority = "🟡 High",
                Type = "🏗️ Development",
                EstimatedEffort = 10,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-005",
                Name = "Nexo Tool Validation",
                Description = "Create validation tools for FPS controller testing and optimization via Nexo aggregator",
                Priority = "🟢 Medium",
                Type = "🔍 Validation",
                EstimatedEffort = 6,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                AssignedTo = "Agent"
            },
            new ProjectTask
            {
                Id = "task-006",
                Name = "Performance & Optimization",
                Description = "Optimize FPS controller performance and ensure smooth 60+ FPS gameplay",
                Priority = "🟡 High",
                Type = "📊 Analysis",
                EstimatedEffort = 8,
                Status = "📝 Created",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                AssignedTo = "Agent"
            }
        };

        foreach (var task in sampleTasks)
        {
            _taskManager.AddTask(task);
            AnsiConsole.MarkupLine($"  ✅ Created: {task.Name} ({task.Priority})");
            await Task.Delay(500);
        }
    }

    private async Task AssignSampleTasksToAgentAsync()
    {
        var tasksToAssign = _taskManager.Tasks.Where(t => t.Status == "📝 Created").ToList();
        
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
                _statusReporter.AddValidation(new ProjectValidation
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
