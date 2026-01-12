using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev;
using Nexo.Agents.AutonomousDev.Configuration;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Spectre.Console;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command handler for demo operations showcasing Universal Testing Agent and Autonomous Development Agent.
/// </summary>
public class DemoCommand
{
    private readonly ILogger<DemoCommand> _logger;
    private readonly IProviderFactory _providerFactory;
    
    public DemoCommand(ILogger<DemoCommand> logger, IProviderFactory providerFactory)
    {
        _logger = logger;
        _providerFactory = providerFactory;
    }
    
    /// <summary>
    /// Creates the demo command group with all subcommands.
    /// </summary>
    public static Command CreateCommand(IServiceProvider serviceProvider, Option<bool> jsonOpt, Option<bool> verboseOpt)
    {
        var demoCmd = new Command("demo", "Demo operations for Universal Testing and Autonomous Development");
        
        var logger = serviceProvider.GetRequiredService<ILogger<DemoCommand>>();
        var providerFactory = serviceProvider.GetRequiredService<IProviderFactory>();
        var command = new DemoCommand(logger, providerFactory);
        
        // nexo demo test - Universal Testing Agent
        var testCmd = new Command("test", "Run Universal Testing Agent to test any application");
        testCmd.AddArgument(new Argument<string>("target", "Target to test (URL, file path, API endpoint, etc.)"));
        testCmd.AddArgument(new Argument<string>("goal", "Testing goal in natural language"));
        testCmd.AddOption(new Option<string?>("--type", "Target type (WebApp, Game, Api, Cli, DesktopApp, Auto)"));
        testCmd.AddOption(new Option<string?>("--depth", "Testing depth (Quick, Standard, Thorough, Exhaustive)"));
        testCmd.AddOption(new Option<string?>("--max-duration", "Maximum duration (e.g., '10m', '1h')"));
        testCmd.AddOption(new Option<string?>("--output", "Output file for test report"));
        
        var testTargetArg = testCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException();
        var testGoalArg = testCmd.Arguments[1] as Argument<string> ?? throw new InvalidOperationException();
        var testTypeOpt = testCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException();
        var testDepthOpt = testCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException();
        var testDurationOpt = testCmd.Options[2] as Option<string?> ?? throw new InvalidOperationException();
        var testOutputOpt = testCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException();
        
        testCmd.SetHandler(async (string target, string goal, string? type, string? depth, string? maxDuration, string? output, bool json, bool verbose) =>
        {
            var exitCode = await command.RunUniversalTestAsync(target, goal, type, depth, maxDuration, output, json, verbose);
            Environment.Exit(exitCode);
        }, testTargetArg, testGoalArg, testTypeOpt, testDepthOpt, testDurationOpt, testOutputOpt, jsonOpt, verboseOpt);
        
        // nexo demo dev - Autonomous Development Agent
        var devCmd = new Command("dev", "Run Autonomous Development Agent to build features autonomously");
        devCmd.AddArgument(new Argument<string>("task", "Development task in natural language"));
        devCmd.AddArgument(new Argument<string>("project-path", "Path to the project"));
        devCmd.AddOption(new Option<string?>("--type", "Project type (UnityGame, DotNetApp, ReactApp, etc.)"));
        devCmd.AddOption(new Option<string?>("--acceptance", "Acceptance criteria"));
        devCmd.AddOption(new Option<int>("--max-iterations", () => 10, "Maximum iterations"));
        devCmd.AddOption(new Option<string?>("--persona", "Test persona (Novice, Average, PowerUser, Adversarial, etc.)"));
        devCmd.AddOption(new Option<string?>("--autonomy", "Autonomy level (Supervised, SemiAutonomous, FullyAutonomous)"));
        devCmd.AddOption(new Option<string?>("--output", "Output file for session report"));
        
        var devTaskArg = devCmd.Arguments[0] as Argument<string> ?? throw new InvalidOperationException();
        var devPathArg = devCmd.Arguments[1] as Argument<string> ?? throw new InvalidOperationException();
        var devTypeOpt = devCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException();
        var devAcceptanceOpt = devCmd.Options[1] as Option<string?> ?? throw new InvalidOperationException();
        var devIterationsOpt = devCmd.Options[2] as Option<int> ?? throw new InvalidOperationException();
        var devPersonaOpt = devCmd.Options[3] as Option<string?> ?? throw new InvalidOperationException();
        var devAutonomyOpt = devCmd.Options[4] as Option<string?> ?? throw new InvalidOperationException();
        var devOutputOpt = devCmd.Options[5] as Option<string?> ?? throw new InvalidOperationException();
        
        devCmd.SetHandler(async (string task, string projectPath, string? type, string? acceptance, int maxIterations, string? persona, string? autonomy, string? output, bool json, bool verbose) =>
        {
            var exitCode = await command.RunAutonomousDevAsync(task, projectPath, type, acceptance, maxIterations, persona, autonomy, output, json, verbose);
            Environment.Exit(exitCode);
        }, devTaskArg, devPathArg, devTypeOpt, devAcceptanceOpt, devIterationsOpt, devPersonaOpt, devAutonomyOpt, devOutputOpt, jsonOpt, verboseOpt);
        
        demoCmd.AddCommand(testCmd);
        demoCmd.AddCommand(devCmd);
        
        return demoCmd;
    }
    
    public async Task<int> RunUniversalTestAsync(
        string target,
        string goal,
        string? type,
        string? depth,
        string? maxDuration,
        string? output,
        bool json,
        bool verbose)
    {
        try
        {
            if (!json)
            {
                AnsiConsole.Write(new FigletText("Universal Tester").Color(Color.Blue));
                AnsiConsole.MarkupLine("[grey]AI-powered testing for any application[/]");
                AnsiConsole.WriteLine();
            }
            
            var config = new UniversalTesterConfig
            {
                Target = target,
                Goal = goal,
                TargetType = ParseTargetType(type),
                Depth = ParseTestingDepth(depth),
                MaxDuration = ParseDuration(maxDuration)
            };
            
            if (!json)
            {
                AnsiConsole.MarkupLine($"[bold]Target:[/] {target}");
                AnsiConsole.MarkupLine($"[bold]Goal:[/] {goal}");
                AnsiConsole.MarkupLine($"[bold]Type:[/] {config.TargetType}");
                AnsiConsole.MarkupLine($"[bold]Depth:[/] {config.Depth}");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Starting test session...[/]");
            }
            
            var context = CreateExecutionContext();
            var tester = new UniversalTesterAgent(_providerFactory, _logger);
            
            var report = await tester.ExecuteAsync(config, context, CancellationToken.None);
            
            if (json)
            {
                var jsonOutput = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonOutput);
                
                if (!string.IsNullOrEmpty(output))
                {
                    await File.WriteAllTextAsync(output, jsonOutput);
                }
            }
            else
            {
                DisplayTestReport(report);
                
                if (!string.IsNullOrEmpty(output))
                {
                    var jsonOutput = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(output, jsonOutput);
                    AnsiConsole.MarkupLine($"[green]✓[/] Report saved to: {output}");
                }
            }
            
            return report.Summary.OverallScore >= 80 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running universal test");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
            return 1;
        }
    }
    
    public async Task<int> RunAutonomousDevAsync(
        string task,
        string projectPath,
        string? type,
        string? acceptance,
        int maxIterations,
        string? persona,
        string? autonomy,
        string? output,
        bool json,
        bool verbose)
    {
        try
        {
            if (!json)
            {
                AnsiConsole.Write(new FigletText("Autonomous Dev").Color(Color.Green));
                AnsiConsole.MarkupLine("[grey]AI-driven development with mock user testing[/]");
                AnsiConsole.WriteLine();
            }
            
            var config = new DevTaskConfig
            {
                Task = task,
                ProjectPath = projectPath,
                ProjectType = ParseProjectType(type),
                AcceptanceCriteria = acceptance,
                MaxIterations = maxIterations,
                TestPersona = ParsePersona(persona),
                Autonomy = ParseAutonomy(autonomy)
            };
            
            if (!json)
            {
                AnsiConsole.MarkupLine($"[bold]Task:[/] {task}");
                AnsiConsole.MarkupLine($"[bold]Project:[/] {projectPath}");
                AnsiConsole.MarkupLine($"[bold]Persona:[/] {config.TestPersona}");
                AnsiConsole.MarkupLine($"[bold]Autonomy:[/] {config.Autonomy}");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Starting autonomous development...[/]");
            }
            
            var context = CreateExecutionContext();
            var tester = new UniversalTesterAgent(_providerFactory, _logger);
            var devAgent = new AutonomousDevAgent(_providerFactory, tester, _logger);
            
            var session = await devAgent.ExecuteAsync(config, context, CancellationToken.None);
            
            if (json)
            {
                var jsonOutput = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonOutput);
                
                if (!string.IsNullOrEmpty(output))
                {
                    await File.WriteAllTextAsync(output, jsonOutput);
                }
            }
            else
            {
                DisplayDevSession(session);
                
                if (!string.IsNullOrEmpty(output))
                {
                    var jsonOutput = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(output, jsonOutput);
                    AnsiConsole.MarkupLine($"[green]✓[/] Session report saved to: {output}");
                }
            }
            
            return session.Status == Models.SessionStatus.Completed ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running autonomous dev");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
            return 1;
        }
    }
    
    private static TargetType ParseTargetType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return TargetType.Auto;
        return Enum.TryParse<TargetType>(type, true, out var result) ? result : TargetType.Auto;
    }
    
    private static TestingDepth ParseTestingDepth(string? depth)
    {
        if (string.IsNullOrEmpty(depth)) return TestingDepth.Standard;
        return Enum.TryParse<TestingDepth>(depth, true, out var result) ? result : TestingDepth.Standard;
    }
    
    private static TimeSpan ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration)) return TimeSpan.FromMinutes(10);
        
        if (duration.EndsWith("m", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(duration[..^1], out var minutes))
                return TimeSpan.FromMinutes(minutes);
        }
        else if (duration.EndsWith("h", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(duration[..^1], out var hours))
                return TimeSpan.FromHours(hours);
        }
        
        return TimeSpan.FromMinutes(10);
    }
    
    private static ProjectType ParseProjectType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return ProjectType.Auto;
        return Enum.TryParse<ProjectType>(type, true, out var result) ? result : ProjectType.Auto;
    }
    
    private static MockUserPersona ParsePersona(string? persona)
    {
        if (string.IsNullOrEmpty(persona)) return MockUserPersona.Average;
        return Enum.TryParse<MockUserPersona>(persona, true, out var result) ? result : MockUserPersona.Average;
    }
    
    private static AutonomyLevel ParseAutonomy(string? autonomy)
    {
        if (string.IsNullOrEmpty(autonomy)) return AutonomyLevel.Supervised;
        return Enum.TryParse<AutonomyLevel>(autonomy, true, out var result) ? result : AutonomyLevel.Supervised;
    }
    
    private static IExecutionContext CreateExecutionContext()
    {
        return new ExecutionContext
        {
            AgentId = "demo-agent",
            BehaviorId = "demo-behavior",
            IsAirGapped = false,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
    }
    
    private static void DisplayTestReport(TestReport report)
    {
        var summary = report.Summary;
        
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Test Report[/]").RuleStyle("green"));
        
        var scoreColor = summary.OverallScore >= 90 ? "green" : summary.OverallScore >= 70 ? "yellow" : "red";
        AnsiConsole.MarkupLine($"[bold]Overall Score:[/] [{scoreColor}]{summary.OverallScore:F0}%[/]");
        AnsiConsole.MarkupLine($"[bold]Duration:[/] {summary.Duration.TotalMinutes:F1} minutes");
        AnsiConsole.MarkupLine($"[bold]Tests:[/] {summary.Passed}/{summary.TotalTests} passed");
        AnsiConsole.MarkupLine($"[bold]Issues:[/] {summary.Failed} failed, {summary.Warnings} warnings");
        AnsiConsole.WriteLine();
        
        if (summary.KeyFindings.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Key Findings:[/]");
            foreach (var finding in summary.KeyFindings)
            {
                AnsiConsole.MarkupLine($"  • {finding}");
            }
            AnsiConsole.WriteLine();
        }
        
        if (summary.Recommendations.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Recommendations:[/]");
            foreach (var rec in summary.Recommendations)
            {
                AnsiConsole.MarkupLine($"  → {rec}");
            }
        }
    }
    
    private static void DisplayDevSession(DevelopmentSession session)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Development Session[/]").RuleStyle("green"));
        
        var statusColor = session.Status switch
        {
            Models.SessionStatus.Completed => "green",
            Models.SessionStatus.Partial => "yellow",
            Models.SessionStatus.Failed => "red",
            _ => "grey"
        };
        
        AnsiConsole.MarkupLine($"[bold]Status:[/] [{statusColor}]{session.Status}[/]");
        AnsiConsole.MarkupLine($"[bold]Duration:[/] {(session.EndTime - session.StartTime).TotalMinutes:F1} minutes");
        AnsiConsole.MarkupLine($"[bold]Iterations:[/] {session.Iterations.Count}");
        
        if (session.Specification != null)
        {
            AnsiConsole.MarkupLine($"[bold]Feature:[/] {session.Specification.Summary}");
        }
        
        AnsiConsole.WriteLine();
        
        if (session.Iterations.Count > 0)
        {
            var table = new Table();
            table.AddColumn("Iteration");
            table.AddColumn("Score");
            table.AddColumn("Issues");
            table.AddColumn("Status");
            
            foreach (var iteration in session.Iterations)
            {
                var score = iteration.Feedback.AcceptanceScore;
                var scoreColor = score >= 90 ? "green" : score >= 70 ? "yellow" : "red";
                table.AddRow(
                    iteration.Number.ToString(),
                    $"[{scoreColor}]{score:F0}%[/]",
                    iteration.Feedback.Issues.Count.ToString(),
                    iteration.Feedback.OverallSuccess ? "[green]✓[/]" : "[red]✗[/]"
                );
            }
            
            AnsiConsole.Write(table);
        }
    }
}
