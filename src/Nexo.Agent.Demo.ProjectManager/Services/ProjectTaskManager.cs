using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Spectre.Console;

namespace Nexo.Agent.Demo.ProjectManager.Services;

/// <summary>
/// Manages project tasks including creation, assignment, and tracking.
/// </summary>
public class ProjectTaskManager
{
    private readonly ILogger _logger;
    private readonly List<ProjectTask> _projectTasks = new();

    public ProjectTaskManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<ProjectTask> Tasks => _projectTasks.AsReadOnly();

    public async Task CreateProjectTaskAsync()
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

    public async Task AssignTaskToAgentAsync(ITaskExecutionAgent agent)
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

    public void AddTask(ProjectTask task)
    {
        _projectTasks.Add(task);
    }
}
