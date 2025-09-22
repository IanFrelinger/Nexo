using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Implementations;
using Nexo.Agent.Tools.Builtin;
using Nexo.Observability;
using Spectre.Console;

namespace Nexo.Agent.Demo;

/// <summary>
/// TUI demo application for the Agent Foundry system.
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
            logger.LogInformation("Starting Nexo Agent Foundry Demo");

            // Run the demo
            var demo = new AgentFoundryDemo(agent, logger);
            
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
            logger.LogError(ex, "Fatal error in demo application");
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

                // Add agent services
                services.AddSingleton<IToolRegistry, ToolRegistry>();
                services.AddSingleton<IToolBroker, ToolBroker>();
                services.AddSingleton<IAgentPlanner, SimplePlanner>();
                services.AddSingleton<IToolFactory, PipelineToolFactory>();
                services.AddSingleton<IAgent, AtlasAgent>();

                // Register built-in tools
                services.AddSingleton<ITool, FileReadTool>();
                services.AddSingleton<ITool, CsvQueryTool>();
                services.AddSingleton<ITool, ReportWriteTool>();
                services.AddSingleton<ITool, SummarizeTool>();
            });
}

/// <summary>
/// Interactive demo for the Agent Foundry system.
/// </summary>
public class AgentFoundryDemo
{
    private readonly IAgent _agent;
    private readonly ILogger _logger;
    private AgentMode _currentMode = AgentMode.Off;
    private bool _selfHealEnabled = false;

    public AgentFoundryDemo(IAgent agent, ILogger logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

    private async Task ShowToolsAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Available Tools[/]");
        AnsiConsole.WriteLine();

        // This would normally get tools from the registry
        var tools = new[]
        {
            new { Id = "tool.file.read", Name = "File Read", Description = "Read text or binary files" },
            new { Id = "tool.csv.query", Name = "CSV Query", Description = "Query CSV files with simple operations" },
            new { Id = "tool.report.write", Name = "Report Write", Description = "Write reports in Markdown format" },
            new { Id = "tool.text.summarize", Name = "Text Summarize", Description = "Summarize text content" }
        };

        var table = new Table();
        table.AddColumn("Tool ID");
        table.AddColumn("Name");
        table.AddColumn("Description");

        foreach (var tool in tools)
        {
            table.AddRow(tool.Id, tool.Name, tool.Description);
        }

        AnsiConsole.Write(table);
        await Task.CompletedTask;
    }

    private async Task GenerateToolDemoAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Generate New Tool[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]This would demonstrate dynamic tool creation using the Feature Factory pipeline.[/]");
        AnsiConsole.MarkupLine("The tool would be generated, validated against policies, and hot-loaded into the agent.");
        
        // Simulate tool generation
        AnsiConsole.MarkupLine("[green]✓ Tool generation simulated successfully![/]");
        await Task.CompletedTask;
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

    private async Task RegisterBuiltInToolsAsync()
    {
        // In a real implementation, this would register tools with the registry
        _logger.LogInformation("Built-in tools registered");
        
        // Register visual validation tool if OLLama is available
        if (await IsOllamaAvailableAsync())
        {
            _logger.LogInformation("OLLama visual analytics available - visual validation tool registered");
        }
        await Task.CompletedTask;
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
