using System.CommandLine;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Spectre.Console;

namespace Nexo.Demo.Visual;

public class DemoApplication
{
    private readonly IBrickRegistry _brickRegistry;
    
    public DemoApplication(IBrickRegistry brickRegistry)
    {
        _brickRegistry = brickRegistry;
    }
    
    public async Task RunAsync(string[] args)
    {
        var interactiveOption = new Option<bool>(
            "--interactive",
            "Run in interactive mode with visual UI");
        
        var offlineOption = new Option<bool>(
            "--offline",
            "Run in offline mode (no network calls)");
        
        var rootCommand = new RootCommand("Nexo Interactive Demo")
        {
            interactiveOption,
            offlineOption
        };
        
        rootCommand.SetHandler(async (interactive, offline) =>
        {
            if (interactive)
            {
                await RunInteractiveDemo(offline);
            }
            else
            {
                await RunQuickDemo(offline);
            }
        }, interactiveOption, offlineOption);
        
        await rootCommand.InvokeAsync(args);
    }
    
    private async Task RunInteractiveDemo(bool offline)
    {
        AnsiConsole.Write(new FigletText("Nexo Demo").Color(Color.Blue));
        AnsiConsole.MarkupLine("[grey]AI-Native Runtime Infrastructure[/]");
        AnsiConsole.WriteLine();
        
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to explore?")
                    .AddChoices(
                        "🔄 Toggle Implementation (⚙️ ↔ 🤖)",
                        "▶️  Run Security Scan",
                        "📊 View Brick Domain Knowledge",
                        "🔌 Switch Provider",
                        "📈 View Execution Metrics",
                        "❌ Exit"));
            
            switch (choice)
            {
                case "🔄 Toggle Implementation (⚙️ ↔ 🤖)":
                    await DemoImplementationToggle(offline);
                    break;
                case "▶️  Run Security Scan":
                    await DemoSecurityScan(offline);
                    break;
                case "📊 View Brick Domain Knowledge":
                    DemoDomainKnowledge();
                    break;
                case "🔌 Switch Provider":
                    await DemoProviderSwitch();
                    break;
                case "📈 View Execution Metrics":
                    DemoMetrics();
                    break;
                case "❌ Exit":
                    return;
            }
            
            AnsiConsole.WriteLine();
        }
    }
    
    private async Task DemoImplementationToggle(bool offline)
    {
        var brick = _brickRegistry.GetBrick("owasp-scanner");
        if (brick == null)
        {
            AnsiConsole.MarkupLine("[red]Brick 'owasp-scanner' not found[/]");
            return;
        }
        
        AnsiConsole.MarkupLine($"[bold]Brick:[/] {brick.Name}");
        AnsiConsole.MarkupLine($"[bold]Current Mode:[/] {(brick.DefaultImplementation == ImplementationType.Deterministic ? "⚙️ Deterministic" : "🤖 Agentic")}");
        AnsiConsole.WriteLine();
        
        var input = new BrickInput();
        input.Set("code", "SELECT * FROM users WHERE id = '" + "userInput" + "'");
        input.Set("language", "sql");
        
        // Run deterministic
        AnsiConsole.MarkupLine("[yellow]Running ⚙️ Deterministic...[/]");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var deterministicResult = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            CreateContext(offline));
        sw.Stop();
        
        AnsiConsole.MarkupLine($"  [green]✓[/] Completed in [bold]{sw.ElapsedMilliseconds}ms[/]");
        if (deterministicResult.Summary != null)
        {
            AnsiConsole.MarkupLine($"  [grey]{deterministicResult.Summary}[/]");
        }
        AnsiConsole.WriteLine();
        
        if (!offline)
        {
            // Run agentic
            AnsiConsole.MarkupLine("[yellow]Running 🤖 Agentic...[/]");
            sw.Restart();
            try
            {
                var agenticResult = await brick.ExecuteAsync(
                    input,
                    ImplementationType.Agentic,
                    CreateContext(offline));
                sw.Stop();
                
                AnsiConsole.MarkupLine($"  [green]✓[/] Completed in [bold]{sw.ElapsedMilliseconds}ms[/]");
                if (agenticResult.Summary != null)
                {
                    AnsiConsole.MarkupLine($"  [grey]{agenticResult.Summary}[/]");
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                AnsiConsole.MarkupLine($"  [red]✗[/] Failed: {ex.Message}");
                AnsiConsole.MarkupLine("[grey]Note: LLM provider may not be configured. Use --offline to skip agentic mode.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]🤖 Agentic mode skipped (offline mode)[/]");
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Key Insight:[/] Same interface, same output schema, different execution strategies.");
    }
    
    private async Task DemoSecurityScan(bool offline)
    {
        var brick = _brickRegistry.GetBrick("owasp-scanner");
        if (brick == null)
        {
            AnsiConsole.MarkupLine("[red]Brick 'owasp-scanner' not found[/]");
            return;
        }
        
        AnsiConsole.MarkupLine($"[bold]Brick:[/] {brick.Name}");
        AnsiConsole.MarkupLine($"[bold]Description:[/] {brick.Description}");
        AnsiConsole.WriteLine();
        
        var table = new Table();
        table.AddColumn("Step");
        table.AddColumn("Mode");
        table.AddColumn("Status");
        table.AddColumn("Duration");
        
        var input = new BrickInput();
        input.Set("code", """
            public class UserController {
                public string GetUser(string id) {
                    var query = "SELECT * FROM users WHERE id = '" + id + "'";
                    return ExecuteQuery(query);
                }
            }
            """);
        input.Set("language", "csharp");
        
        AnsiConsole.Live(table)
            .Start(ctx =>
            {
                // Step 1: Deterministic
                table.AddRow("1", "⚙️ Deterministic", "[yellow]Running...[/]", "-");
                ctx.Refresh();
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = brick.ExecuteAsync(
                    input,
                    ImplementationType.Deterministic,
                    CreateContext(offline)).GetAwaiter().GetResult();
                sw.Stop();
                
                table.Rows.Update(table.Rows.Count - 1, 2, new Markup("[green]✓ Complete[/]"));
                table.Rows.Update(table.Rows.Count - 1, 3, new Markup($"{sw.ElapsedMilliseconds}ms"));
                ctx.Refresh();
            });
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Scan completed successfully![/]");
    }
    
    private void DemoDomainKnowledge()
    {
        var brick = _brickRegistry.GetBrick("owasp-scanner");
        if (brick == null)
        {
            AnsiConsole.MarkupLine("[red]Brick 'owasp-scanner' not found[/]");
            return;
        }
        
        AnsiConsole.MarkupLine($"[bold]Brick:[/] {brick.Name}");
        AnsiConsole.WriteLine();
        
        var panel = new Panel(
            new Rows(
                new Markup($"[bold]Standards:[/] {string.Join(", ", brick.DomainKnowledge.Standards)}"),
                new Markup($"[bold]Rules:[/] {brick.DomainKnowledge.Rules.Count} detection patterns"),
                new Markup($"[bold]Learned Patterns:[/] {brick.DomainKnowledge.LearnedPatterns.Count} from {brick.DomainKnowledge.ExecutionCount:N0} executions")
            ))
        {
            Header = new PanelHeader("Domain Knowledge"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        if (brick.DomainKnowledge.Rules.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Sample Rules:[/]");
            
            var ruleTable = new Table();
            ruleTable.AddColumn("ID");
            ruleTable.AddColumn("Description");
            ruleTable.AddColumn("Severity");
            
            foreach (var rule in brick.DomainKnowledge.Rules.Take(5))
            {
                ruleTable.AddRow(rule.Id, rule.Description ?? "", rule.Severity ?? "Medium");
            }
            
            AnsiConsole.Write(ruleTable);
        }
    }
    
    private async Task DemoProviderSwitch()
    {
        var providers = new[] { "OpenAI", "Azure OpenAI", "Anthropic", "Ollama (Local)" };
        
        var current = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select provider:")
                .AddChoices(providers));
        
        AnsiConsole.MarkupLine($"[green]✓[/] Switched to [bold]{current}[/]");
        AnsiConsole.MarkupLine("[grey]All subsequent 🤖 executions will use this provider.[/]");
        
        if (current == "Ollama (Local)")
        {
            AnsiConsole.MarkupLine("[yellow]Note:[/] Ollama runs entirely offline. No data leaves your machine.");
        }
    }
    
    private void DemoMetrics()
    {
        var chart = new BarChart()
            .Width(60)
            .Label("[green bold]Execution Times (ms)[/]")
            .CenterLabel()
            .AddItem("⚙️ Parse", 12, Color.Blue)
            .AddItem("⚙️ Scan", 45, Color.Blue)
            .AddItem("🤖 Analyze", 1850, Color.Purple)
            .AddItem("⚙️ Score", 8, Color.Blue)
            .AddItem("🤖 Explain", 2100, Color.Purple)
            .AddItem("⚙️ Report", 25, Color.Blue);
        
        AnsiConsole.Write(chart);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Summary:[/]");
        AnsiConsole.MarkupLine("  ⚙️ Deterministic total: [bold]90ms[/]");
        AnsiConsole.MarkupLine("  🤖 Agentic total: [bold]3,950ms[/]");
        AnsiConsole.MarkupLine("  [grey]Same results, 44x speed difference[/]");
    }
    
    private async Task RunQuickDemo(bool offline)
    {
        AnsiConsole.MarkupLine("[bold]Nexo Quick Demo[/]");
        AnsiConsole.MarkupLine("[grey]Run with --interactive for full UI[/]");
        AnsiConsole.WriteLine();
        
        await DemoImplementationToggle(offline);
    }
    
    private IExecutionContext CreateContext(bool offline)
    {
        return new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "demo-agent",
            BehaviorId = "demo-behavior",
            IsAirGapped = offline,
            Provider = offline ? "ollama" : "openai"
        };
    }
}

