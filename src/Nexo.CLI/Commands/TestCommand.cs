using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.CLI.Output;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for running the Universal Testing Agent.
/// </summary>
public class TestCommand : Command
{
    public TestCommand() : base("test", "Run Universal Testing Agent to test any application")
    {
        var targetOption = new Option<string>(
            "--target",
            "What to test (URL, executable path, api://, cli://)")
        { IsRequired = true };
        
        var goalOption = new Option<string>(
            "--goal",
            "What to test for (natural language)")
        { IsRequired = true };
        
        var depthOption = new Option<string>(
            "--depth",
            () => "standard",
            "Testing depth: quick, standard, thorough, exhaustive");
        
        var personaOption = new Option<string>(
            "--persona",
            () => "average",
            "Mock user persona: novice, average, power-user, adversarial, accessibility");
        
        var durationOption = new Option<string>(
            "--max-duration",
            () => "10m",
            "Maximum testing duration (e.g., '10m', '1h')");
        
        var outputOption = new Option<FileInfo?>(
            "--output",
            "Output report file path");
        
        var modeOption = new Option<string>(
            "--mode",
            () => "mixed",
            "Execution mode: deterministic, agentic, mixed");
        
        var verboseOption = new Option<bool>(
            "--verbose",
            () => false,
            "Show detailed progress");
        
        var jsonOption = new Option<bool>(
            "--format-json",
            () => false,
            "Emit machine-readable JSON");
        
        AddOption(targetOption);
        AddOption(goalOption);
        AddOption(depthOption);
        AddOption(personaOption);
        AddOption(durationOption);
        AddOption(outputOption);
        AddOption(modeOption);
        AddOption(verboseOption);
        AddOption(jsonOption);
        
        this.SetHandler(ExecuteAsync,
            targetOption, goalOption, depthOption, personaOption,
            durationOption, outputOption, modeOption, verboseOption, jsonOption);
    }
    
    private async Task ExecuteAsync(
        string target,
        string goal,
        string depth,
        string persona,
        string maxDuration,
        FileInfo? output,
        string mode,
        bool verbose,
        bool json)
    {
        var console = json ? null : new CliConsole(verbose);
        
        if (!json && console != null)
        {
            console.WriteHeader("🧪 Nexo Universal Testing Agent");
            console.WritePair("Target", target);
            console.WritePair("Goal", goal);
            console.WritePair("Depth", depth);
            console.WritePair("Persona", persona);
            console.WritePair("Mode", mode);
            console.WriteLine();
        }
        
        try
        {
            // Build configuration
            var config = new UniversalTesterConfig
            {
                Target = target,
                Goal = goal,
                Depth = ParseTestingDepth(depth),
                MaxDuration = ParseDuration(maxDuration)
            };
            
            // Create services and agent
            var services = BuildServices(mode);
            var logger = services.GetRequiredService<ILogger<TestCommand>>();
            var providerFactory = services.GetRequiredService<IProviderFactory>();
            var agent = new UniversalTesterAgent(providerFactory, logger);
            
            // Create execution context
            var context = CreateExecutionContext(mode);
            
            // Set up progress reporting
            if (!json && console != null)
            {
                console.StartSpinner("Testing...");
            }
            
            // Run the agent
            var report = await agent.ExecuteAsync(config, context, CancellationToken.None);
            
            if (!json && console != null)
            {
                console.StopSpinner();
            }
            
            if (json)
            {
                var jsonOutput = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonOutput);
                
                if (output != null)
                {
                    await File.WriteAllTextAsync(output.FullName, jsonOutput);
                }
            }
            else if (console != null)
            {
                DisplayTestResults(console, report);
                
                if (output != null)
                {
                    await SaveReportAsync(output, report);
                    console.WriteSuccess($"Report saved to {output.FullName}");
                }
            }
            
            // Exit code based on results
            Environment.ExitCode = report.Summary.OverallScore >= 80 ? 0 : 1;
        }
        catch (Exception ex)
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else if (console != null)
            {
                console.StopSpinner();
                console.WriteError($"Error: {ex.Message}");
            }
            Environment.ExitCode = 1;
        }
    }
    
    private static void DisplayTestResults(CliConsole console, TestReport report)
    {
        var summary = report.Summary;
        
        console.WriteLine();
        console.WriteHeader("📊 Test Results");
        
        // Score with color
        var scoreColor = summary.OverallScore >= 80 ? ConsoleColor.Green :
                        summary.OverallScore >= 60 ? ConsoleColor.Yellow :
                        ConsoleColor.Red;
        
        console.WriteColoredLine($"Overall Score: {summary.OverallScore:F0}%", scoreColor);
        console.WriteLine();
        
        // Pass/Fail breakdown
        console.WriteColoredLine($"  ✓ Passed: {summary.Passed}", ConsoleColor.Green);
        if (summary.Failed > 0)
            console.WriteColoredLine($"  ✗ Failed: {summary.Failed}", ConsoleColor.Red);
        if (summary.Warnings > 0)
            console.WriteColoredLine($"  ⚠ Warnings: {summary.Warnings}", ConsoleColor.Yellow);
        
        console.WritePair("Duration", $"{summary.Duration.TotalSeconds:F1}s");
        console.WriteLine();
        
        // Key findings
        if (summary.KeyFindings.Count > 0)
        {
            console.WriteHeader("🔍 Key Findings");
            foreach (var finding in summary.KeyFindings)
            {
                console.WriteLine($"  • {finding}");
            }
            console.WriteLine();
        }
        
        // Recommendations
        if (summary.Recommendations.Count > 0)
        {
            console.WriteHeader("💡 Recommendations");
            foreach (var rec in summary.Recommendations)
            {
                console.WriteLine($"  • {rec}");
            }
            console.WriteLine();
        }
    }
    
    private static async Task SaveReportAsync(FileInfo output, TestReport report)
    {
        var extension = output.Extension.ToLowerInvariant();
        
        string content = extension switch
        {
            ".html" => report.HtmlReport ?? GenerateHtmlReport(report),
            ".json" => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            ".md" => GenerateMarkdownReport(report),
            _ => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        };
        
        await File.WriteAllTextAsync(output.FullName, content);
    }
    
    private static string GenerateHtmlReport(TestReport report)
    {
        var summary = report.Summary;
        return $@"<!DOCTYPE html>
<html>
<head>
    <title>Nexo Test Report</title>
    <style>
        body {{ font-family: sans-serif; margin: 40px; }}
        .score {{ font-size: 2em; font-weight: bold; }}
        .passed {{ color: green; }}
        .failed {{ color: red; }}
        .warning {{ color: orange; }}
    </style>
</head>
<body>
    <h1>Test Report</h1>
    <div class=""score"">Overall Score: {summary.OverallScore:F0}%</div>
    <p>Passed: {summary.Passed} | Failed: {summary.Failed} | Warnings: {summary.Warnings}</p>
    <p>Duration: {summary.Duration.TotalSeconds:F1}s</p>
    <h2>Key Findings</h2>
    <ul>
        {string.Join("\n        ", summary.KeyFindings.Select(f => $"<li>{f}</li>"))}
    </ul>
    <h2>Recommendations</h2>
    <ul>
        {string.Join("\n        ", summary.Recommendations.Select(r => $"<li>{r}</li>"))}
    </ul>
</body>
</html>";
    }
    
    private static string GenerateMarkdownReport(TestReport report)
    {
        var summary = report.Summary;
        return $@"# Nexo Test Report

## Summary

- **Overall Score**: {summary.OverallScore:F0}%
- **Passed**: {summary.Passed}
- **Failed**: {summary.Failed}
- **Warnings**: {summary.Warnings}
- **Duration**: {summary.Duration.TotalSeconds:F1}s

## Key Findings

{string.Join("\n", summary.KeyFindings.Select(f => $"- {f}"))}

## Recommendations

{string.Join("\n", summary.Recommendations.Select(r => $"- {r}"))}
";
    }
    
    private static TestingDepth ParseTestingDepth(string depth)
    {
        return depth.ToLowerInvariant() switch
        {
            "quick" => TestingDepth.Quick,
            "standard" => TestingDepth.Standard,
            "thorough" => TestingDepth.Thorough,
            "exhaustive" => TestingDepth.Exhaustive,
            _ => TestingDepth.Standard
        };
    }
    
    private static TimeSpan ParseDuration(string duration)
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
    
    private static IServiceProvider BuildServices(string mode)
    {
        var services = new ServiceCollection();
        
        services.AddLogging(b => b.AddConsole());
        
        // Always register IProviderFactory (it handles deterministic mode internally)
        services.AddSingleton<IProviderFactory, ProviderFactory>();
        
        return services.BuildServiceProvider();
    }
    
    private static IExecutionContext CreateExecutionContext(string mode)
    {
        return new ExecutionContext
        {
            AgentId = "demo-test-agent",
            BehaviorId = "demo-test-behavior",
            IsAirGapped = mode == "deterministic",
            AuditMode = false,
            Provider = mode == "deterministic" ? "none" : "openai",
            Variables = new Dictionary<string, object>()
        };
    }
}
