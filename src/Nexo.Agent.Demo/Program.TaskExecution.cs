using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Spectre.Console;

namespace Nexo.Agent.Demo;

/// <summary>
/// Task execution and agent interaction functionality
/// </summary>
public partial class AgentFoundryDemo
{
    private async Task RunTaskDemoAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Running Task Demo[/]");
        AnsiConsole.WriteLine();

        var goal = AnsiConsole.Ask<string>("Enter your task goal:");
        var context = AnsiConsole.Ask<string>("Enter additional context (optional):", "");

        if (string.IsNullOrEmpty(context))
            context = null;

        AnsiConsole.MarkupLine("[yellow]Executing task...[/]");
        
        var result = await _agent.ExecuteTaskAsync(goal, context);
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine("[green]✓ Task completed successfully![/]");
            AnsiConsole.MarkupLine($"[green]Duration: {result.Duration.TotalSeconds:F2}s[/]");
            
            if (result.Outputs.Count > 0)
            {
                AnsiConsole.MarkupLine("[bold]Outputs:[/]");
                foreach (var output in result.Outputs)
                {
                    AnsiConsole.MarkupLine($"[dim]{output}[/]");
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Task failed: {result.Message}[/]");
        }
    }

    private async Task ToggleModeAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Toggle Agent Mode[/]");
        AnsiConsole.WriteLine();

        var newMode = AnsiConsole.Prompt(
            new SelectionPrompt<AgentMode>()
                .Title("Select agent mode:")
                .AddChoices(AgentMode.Off, AgentMode.Hybrid, AgentMode.Embedded)
        );

        _agent.SetMode(newMode);
        _currentMode = newMode;

        AnsiConsole.MarkupLine($"[green]Agent mode changed to: {newMode}[/]");
        await Task.CompletedTask;
    }
}
