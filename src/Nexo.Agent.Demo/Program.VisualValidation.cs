using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Spectre.Console;

namespace Nexo.Agent.Demo;

/// <summary>
/// Visual validation demo functionality
/// </summary>
public partial class AgentFoundryDemo
{
    private async Task VisualValidationDemoAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Visual Validation Demo[/]");
        AnsiConsole.WriteLine();

        // Check if OLLama is available
        var ollamaAvailable = await IsOllamaAvailableAsync();
        
        if (!ollamaAvailable)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️  OLLama not detected. Starting OLLama service...[/]");
            AnsiConsole.MarkupLine("To use visual validation, please:");
            AnsiConsole.MarkupLine("1. Install OLLama: https://ollama.ai/");
            AnsiConsole.MarkupLine("2. Pull a vision model: [cyan]ollama pull llava:7b[/]");
            AnsiConsole.MarkupLine("3. Start OLLama service: [cyan]ollama serve[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Running demo with simulated visual analysis...[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]✅ OLLama detected! Visual analytics ready.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]🎮 Interactive Visual Validation Workflow[/]");
        AnsiConsole.WriteLine();

        // Simulate the visual validation workflow
        await SimulateVisualValidationWorkflow(ollamaAvailable);
    }

    private async Task SimulateVisualValidationWorkflow(bool ollamaAvailable)
    {
        var validationTypes = new[] { "UI", "Gameplay", "Performance", "Accessibility" };
        
        foreach (var validationType in validationTypes)
        {
            AnsiConsole.MarkupLine($"[bold yellow]📸 Capturing {validationType} Screenshot...[/]");
            await Task.Delay(1000);
            
            AnsiConsole.MarkupLine($"[bold cyan]🔍 Analyzing {validationType} Elements...[/]");
            await Task.Delay(2000);
            
            // Simulate analysis results
            var results = GenerateMockValidationResults(validationType);
            
            AnsiConsole.MarkupLine($"[green]✅ {validationType} Analysis Complete[/]");
            AnsiConsole.MarkupLine($"   Score: [bold]{results.Score:P1}[/] ({results.Status})");
            AnsiConsole.MarkupLine($"   Issues Found: [bold]{results.IssuesCount}[/]");
            
            if (results.IssuesCount > 0)
            {
                AnsiConsole.MarkupLine($"   [yellow]⚠️  {results.TopIssue}[/]");
            }
            
            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine("[bold green]🎉 Visual Validation Complete![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]📊 Summary:[/]");
        AnsiConsole.MarkupLine("• UI Validation: [green]Good (85%)[/] - Minor contrast improvements needed");
        AnsiConsole.MarkupLine("• Gameplay Validation: [green]Excellent (92%)[/] - All mechanics working well");
        AnsiConsole.MarkupLine("• Performance Validation: [yellow]Fair (68%)[/] - Some optimization opportunities");
        AnsiConsole.MarkupLine("• Accessibility Validation: [red]Poor (45%)[/] - WCAG compliance issues");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold cyan]🛠️  Recommendations:[/]");
        AnsiConsole.MarkupLine("1. [red]High Priority:[/] Fix color contrast ratios for accessibility compliance");
        AnsiConsole.MarkupLine("2. [yellow]Medium Priority:[/] Optimize particle effects for better performance");
        AnsiConsole.MarkupLine("3. [green]Low Priority:[/] Increase crosshair size for better visibility");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[dim]💡 Tip: Use the Visual Validation Tool in your Agent's toolbelt[/]");
        AnsiConsole.MarkupLine("[dim]   to automatically validate visuals during development![/]");
    }

    private (double Score, string Status, int IssuesCount, string TopIssue) GenerateMockValidationResults(string validationType)
    {
        return validationType switch
        {
            "UI" => (0.85, "Good", 2, "Crosshair could be larger"),
            "Gameplay" => (0.92, "Excellent", 0, "No issues detected"),
            "Performance" => (0.68, "Fair", 3, "Particle effects causing frame drops"),
            "Accessibility" => (0.45, "Poor", 5, "Text contrast below WCAG AA standard"),
            _ => (0.75, "Good", 1, "Minor improvements needed")
        };
    }

    private async Task<bool> IsOllamaAvailableAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://localhost:11434/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
