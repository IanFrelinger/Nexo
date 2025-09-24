using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Nexo.Agent.Demo.ProjectManager.Services;

/// <summary>
/// Handles project status reporting and analytics.
/// </summary>
public class ProjectStatusReporter
{
    private readonly ILogger _logger;
    private readonly List<ProjectValidation> _validations = new();

    public ProjectStatusReporter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddValidation(ProjectValidation validation)
    {
        _validations.Add(validation);
    }

    public async Task ReviewProjectStatusAsync(IReadOnlyList<ProjectTask> tasks)
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]📊 Project Status Review[/]");
        AnsiConsole.WriteLine();

        if (!tasks.Any())
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

        foreach (var task in tasks)
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
        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == "✅ Completed");
        var inProgressTasks = tasks.Count(t => t.Status == "🔄 In Progress");
        var totalEffort = tasks.Sum(t => t.ActualEffort ?? 0);

        AnsiConsole.MarkupLine("[bold cyan]📈 Summary:[/]");
        AnsiConsole.MarkupLine($"• Total Tasks: [bold]{totalTasks}[/]");
        AnsiConsole.MarkupLine($"• Completed: [green]{completedTasks}[/]");
        AnsiConsole.MarkupLine($"• In Progress: [yellow]{inProgressTasks}[/]");
        AnsiConsole.MarkupLine($"• Total Effort: [bold]{totalEffort} hours[/]");
        AnsiConsole.MarkupLine($"• Completion Rate: [bold]{(double)completedTasks / totalTasks:P1}[/]");
    }

    public async Task RunValidationTestsAsync()
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

    public async Task GenerateProjectReportAsync(IReadOnlyList<ProjectTask> tasks)
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
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == "✅ Completed"),
            TotalEffort = tasks.Sum(t => t.ActualEffort ?? 0),
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

    public async Task ViewProjectHistoryAsync(IReadOnlyList<ProjectTask> tasks)
    {
        await Task.CompletedTask;
        AnsiConsole.MarkupLine("[bold cyan]📁 Project History[/]");
        AnsiConsole.WriteLine();

        if (!tasks.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No project history available.[/]");
            return;
        }

        var historyTable = new Table();
        historyTable.AddColumn("Timestamp");
        historyTable.AddColumn("Action");
        historyTable.AddColumn("Details");

        foreach (var task in tasks.OrderBy(t => t.CreatedAt))
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
}
