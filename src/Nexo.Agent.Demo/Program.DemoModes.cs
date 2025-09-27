using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Spectre.Console;

namespace Nexo.Agent.Demo;

/// <summary>
/// Demo modes and policy demonstration functionality
/// </summary>
public partial class AgentFoundryDemo
{
    /// <summary>
    /// Runs the demo in non-interactive mode, showcasing key features automatically.
    /// </summary>
    public async Task<int> RunDemoModeAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]🤖 Running Agent Foundry Demo in Non-Interactive Mode[/]");
        AnsiConsole.WriteLine();

        try
        {
            // 1. Show current status
            AnsiConsole.MarkupLine("[bold cyan]📊 Current Status:[/]");
            AnsiConsole.MarkupLine("  Mode: [yellow]OFF[/] (Offline by default)");
            AnsiConsole.MarkupLine("  Self-Heal: [yellow]Disabled[/]");
            AnsiConsole.WriteLine();

            // 2. Show available tools
            AnsiConsole.MarkupLine("[bold cyan]🔧 Available Tools:[/]");
            await ShowToolsAsync();
            AnsiConsole.WriteLine();

            // 3. Run a sample task
            AnsiConsole.MarkupLine("[bold cyan]🎯 Running Sample Task:[/]");
            AnsiConsole.MarkupLine("Task: 'Redact PII in customers.csv, write report, and ZIP outputs'");
            AnsiConsole.WriteLine();

            // Simulate task execution
            AnsiConsole.MarkupLine("[yellow]📋 Creating plan...[/]");
            await Task.Delay(1000);
            AnsiConsole.MarkupLine("  ✓ Step 1: Read customers.csv");
            AnsiConsole.MarkupLine("  ✓ Step 2: Redact PII data");
            AnsiConsole.MarkupLine("  ✓ Step 3: Write report");
            AnsiConsole.MarkupLine("  ⚠ Step 4: ZIP outputs (tool missing - Archive.Zip)");
            AnsiConsole.WriteLine();

            // 4. Generate missing tool
            AnsiConsole.MarkupLine("[bold cyan]🛠️ Generating Missing Tool:[/]");
            await GenerateToolDemoAsync();
            AnsiConsole.WriteLine();

            // 5. Complete task
            AnsiConsole.MarkupLine("[bold cyan]✅ Completing Task:[/]");
            AnsiConsole.MarkupLine("  ✓ All tools available");
            AnsiConsole.MarkupLine("  ✓ Executing plan...");
            await Task.Delay(1500);
            AnsiConsole.MarkupLine("  ✓ Task completed successfully!");
            AnsiConsole.MarkupLine("  📁 Outputs: out/redacted.csv, out/report.md, out/package.zip");
            AnsiConsole.WriteLine();

            // 6. Show policy demo
            AnsiConsole.MarkupLine("[bold cyan]🛡️ Policy & Self-Healing Demo:[/]");
            await BreakPolicyDemoAsync();
            AnsiConsole.WriteLine();

            // 7. Show visual validation demo
            AnsiConsole.MarkupLine("[bold cyan]👁️ Visual Validation Demo:[/]");
            await VisualValidationDemoAsync();
            AnsiConsole.WriteLine();

            // 8. Summary
            AnsiConsole.MarkupLine("[bold green]🎉 Demo Complete![/]");
            AnsiConsole.MarkupLine("The Agent Foundry successfully demonstrated:");
            AnsiConsole.MarkupLine("  • Task planning and execution");
            AnsiConsole.MarkupLine("  • Dynamic tool generation");
            AnsiConsole.MarkupLine("  • Policy validation and self-healing");
            AnsiConsole.MarkupLine("  • Visual validation with OLLama integration");
            AnsiConsole.MarkupLine("  • Offline operation with offline parity");
            AnsiConsole.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Demo failed: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task BreakPolicyDemoAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Break Policy Demo[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]This would demonstrate policy failure and self-healing.[/]");
        AnsiConsole.MarkupLine("A tool would be generated with a policy violation, fail validation, then be repaired and re-validated.");
        
        // Simulate policy break and repair
        AnsiConsole.MarkupLine("[red]✗ Policy violation detected![/]");
        AnsiConsole.MarkupLine("[yellow]Attempting repair...[/]");
        AnsiConsole.MarkupLine("[green]✓ Repair successful! Tool now passes policy validation.[/]");
        await Task.CompletedTask;
    }
}
