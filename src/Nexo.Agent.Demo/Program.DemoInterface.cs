using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Spectre.Console;

namespace Nexo.Agent.Demo;

/// <summary>
/// Interactive demo interface and menu system functionality
/// </summary>
public partial class AgentFoundryDemo
{
    public async Task<int> RunAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Nexo Agent Foundry").Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold green]Welcome to the Nexo Agent Foundry Demo![/]");
        AnsiConsole.MarkupLine("This demo shows an AI agent that can plan tasks, use tools, and grow its toolbelt at runtime.");
        AnsiConsole.WriteLine();

        // Register built-in tools
        await RegisterBuiltInToolsAsync();

        // Main demo loop
        while (true)
        {
            try
            {
                var choice = ShowMainMenu();
                
                switch (choice)
                {
                    case "run":
                        await RunTaskDemoAsync();
                        break;
                    case "mode":
                        await ToggleModeAsync();
                        break;
                    case "tools":
                        await ShowToolsAsync();
                        break;
                    case "generate":
                        await GenerateToolDemoAsync();
                        break;
                    case "break":
                        await BreakPolicyDemoAsync();
                        break;
                    case "visual":
                        await VisualValidationDemoAsync();
                        break;
                    case "quit":
                        AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                        return 0;
                    default:
                        AnsiConsole.MarkupLine("[red]Invalid choice. Please try again.[/]");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in demo loop");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
            AnsiConsole.Clear();
        }
    }

    private string ShowMainMenu()
    {
        var panel = new Panel(new Rows(
            new Text("Agent Foundry Demo Menu", new Style(Color.Blue, Color.Black, Decoration.Bold)),
            new Text(""),
            new Text("[bold cyan]Current Status:[/]"),
            new Text($"  Mode: [yellow]{_currentMode}[/]"),
            new Text($"  Self-Heal: [yellow]{(_selfHealEnabled ? "Enabled" : "Disabled")}[/]"),
            new Text(""),
            new Text("[bold green]Available Commands:[/]"),
            new Text("  [bold white][R][/] - Run Task Demo"),
            new Text("  [bold white][M][/] - Toggle Mode (OFF/HYBRID/EMBEDDED)"),
            new Text("  [bold white][T][/] - Show Available Tools"),
            new Text("  [bold white][G][/] - Generate New Tool"),
            new Text("  [bold white][B][/] - Break Policy Demo"),
            new Text("  [bold white][V][/] - Visual Validation Demo"),
            new Text("  [bold white][Q][/] - Quit")
        ))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("Nexo Agent Foundry", Justify.Center)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an option:")
                .AddChoices("run", "mode", "tools", "generate", "break", "quit")
        );
    }
}
