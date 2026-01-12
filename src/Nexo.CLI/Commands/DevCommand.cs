using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev;
using Nexo.Agents.AutonomousDev.Configuration;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Agents.UniversalTester;
using Nexo.CLI.Output;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for running the Autonomous Development Agent.
/// </summary>
public class DevCommand : Command
{
    public DevCommand() : base("dev", "Run Autonomous Development Agent to build features autonomously")
    {
        var projectOption = new Option<DirectoryInfo>(
            "--project",
            "Path to the project to modify")
        { IsRequired = true };
        
        var taskOption = new Option<string>(
            "--task",
            "What to build (natural language)")
        { IsRequired = true };
        
        var specOption = new Option<FileInfo?>(
            "--spec",
            "Detailed specification file (optional)");
        
        var acceptanceOption = new Option<string?>(
            "--acceptance",
            "Acceptance criteria (optional)");
        
        var iterationsOption = new Option<int>(
            "--max-iterations",
            () => 10,
            "Maximum iteration attempts");
        
        var autonomyOption = new Option<string>(
            "--autonomy",
            () => "supervised",
            "Autonomy level: supervised, semi-autonomous, fully-autonomous");
        
        var personaOption = new Option<string>(
            "--test-persona",
            () => "average",
            "Mock user persona for testing: novice, average, power-user, adversarial, accessibility, impatient");
        
        var outputOption = new Option<FileInfo?>(
            "--output",
            "Session output file path");
        
        var dryRunOption = new Option<bool>(
            "--dry-run",
            () => false,
            "Plan only, don't make changes");
        
        var verboseOption = new Option<bool>(
            "--verbose",
            () => false,
            "Show detailed progress");
        
        var jsonOption = new Option<bool>(
            "--format-json",
            () => false,
            "Emit machine-readable JSON");
        
        AddOption(projectOption);
        AddOption(taskOption);
        AddOption(specOption);
        AddOption(acceptanceOption);
        AddOption(iterationsOption);
        AddOption(autonomyOption);
        AddOption(personaOption);
        AddOption(outputOption);
        AddOption(dryRunOption);
        AddOption(verboseOption);
        AddOption(jsonOption);
        
        this.SetHandler(ExecuteAsync,
            projectOption, taskOption, specOption, acceptanceOption,
            iterationsOption, autonomyOption, personaOption, outputOption, dryRunOption, verboseOption, jsonOption);
    }
    
    private async Task ExecuteAsync(
        DirectoryInfo project,
        string task,
        FileInfo? spec,
        string? acceptance,
        int maxIterations,
        string autonomy,
        string testPersona,
        FileInfo? output,
        bool dryRun,
        bool verbose,
        bool json)
    {
        var console = json ? null : new CliConsole(verbose);
        
        if (!json && console != null)
        {
            console.WriteHeader("🔄 Nexo Autonomous Development Agent");
            console.WritePair("Project", project.FullName);
            console.WritePair("Task", task);
            console.WritePair("Autonomy", autonomy);
            console.WritePair("Max Iterations", maxIterations.ToString());
            console.WritePair("Test Persona", testPersona);
            if (dryRun) console.WriteColoredLine("DRY RUN MODE", ConsoleColor.Yellow);
            console.WriteLine();
        }
        
        try
        {
            // Load spec file if provided
            string? detailedSpec = null;
            if (spec != null && spec.Exists)
            {
                detailedSpec = await File.ReadAllTextAsync(spec.FullName);
                if (!json && console != null)
                {
                    console.WritePair("Spec loaded", spec.Name);
                }
            }
            
            // Build configuration
            var config = new DevTaskConfig
            {
                Task = task,
                ProjectPath = project.FullName,
                DetailedSpec = detailedSpec,
                AcceptanceCriteria = acceptance,
                MaxIterations = maxIterations,
                Autonomy = ParseAutonomy(autonomy),
                TestPersona = ParsePersona(testPersona)
            };
            
            // Create services and agent
            var services = BuildServices();
            var logger = services.GetRequiredService<ILogger<DevCommand>>();
            var providerFactory = services.GetRequiredService<IProviderFactory>();
            var tester = new UniversalTesterAgent(providerFactory, logger);
            var agent = new AutonomousDevAgent(providerFactory, tester, logger);
            
            // Create execution context
            var context = CreateExecutionContext();
            
            if (dryRun)
            {
                // Just show the plan
                if (!json && console != null)
                {
                    console.WriteHeader("📋 Development Plan (Dry Run)");
                    console.WriteColored("No changes made (dry run mode)", ConsoleColor.Yellow);
                }
                return;
            }
            
            // Set up progress reporting
            if (!json && console != null)
            {
                console.WriteLine();
                console.WriteHeader("Starting Development Loop");
            }
            
            // Run the agent
            var session = await agent.ExecuteAsync(config, context, CancellationToken.None);
            
            // Display final results
            if (json)
            {
                var jsonOutput = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonOutput);
                
                if (output != null)
                {
                    await File.WriteAllTextAsync(output.FullName, jsonOutput);
                }
            }
            else if (console != null)
            {
                DisplayDevResults(console, session);
                
                if (output != null)
                {
                    var jsonOutput = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(output.FullName, jsonOutput);
                    console.WriteSuccess($"Session saved to {output.FullName}");
                }
            }
            
            Environment.ExitCode = session.Status == Models.SessionStatus.Completed ? 0 : 1;
        }
        catch (Exception ex)
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else if (console != null)
            {
                console.WriteError($"Error: {ex.Message}");
            }
            Environment.ExitCode = 1;
        }
    }
    
    private static void DisplayDevResults(CliConsole console, DevelopmentSession session)
    {
        console.WriteLine();
        
        var statusColor = session.Status switch
        {
            Models.SessionStatus.Completed => ConsoleColor.Green,
            Models.SessionStatus.Partial => ConsoleColor.Yellow,
            Models.SessionStatus.Failed => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
        
        var statusText = session.Status switch
        {
            Models.SessionStatus.Completed => "✅ Development Complete!",
            Models.SessionStatus.Partial => "⚠️ Partial Success",
            Models.SessionStatus.Failed => "❌ Development Failed",
            _ => "⏸️ Development Stopped"
        };
        
        console.WriteColoredLine(statusText, statusColor);
        
        console.WriteLine();
        console.WritePair("Iterations", session.Iterations.Count.ToString());
        
        if (session.Iterations.Count > 0)
        {
            var lastIteration = session.Iterations.Last();
            console.WritePair("Final Score", $"{lastIteration.Feedback.AcceptanceScore:F0}%");
        }
        
        var duration = session.EndTime - session.StartTime;
        console.WritePair("Duration", $"{duration.TotalMinutes:F1} minutes");
        
        // Show iteration summary table
        if (session.Iterations.Count > 0)
        {
            console.WriteLine();
            console.WriteHeader("Iteration Summary");
            
            var headers = new[] { "Iteration", "Score", "Issues", "Status" };
            var rows = session.Iterations.Select(iter =>
            {
                var score = iter.Feedback.AcceptanceScore;
                var scoreColor = score >= 90 ? "✓" : score >= 70 ? "~" : "✗";
                return new[]
                {
                    iter.Number.ToString(),
                    $"{scoreColor} {score:F0}%",
                    iter.Feedback.Issues.Count.ToString(),
                    iter.Feedback.OverallSuccess ? "✓" : "✗"
                };
            }).ToArray();
            
            console.WriteTable(headers, rows);
        }
        
        // Show files changed
        var filesChanged = session.Iterations
            .SelectMany(i => i.Artifacts)
            .Select(a => a.TargetPath)
            .Distinct()
            .ToList();
        
        if (filesChanged.Count > 0)
        {
            console.WriteLine();
            console.WriteHeader("Modified Files");
            foreach (var file in filesChanged)
            {
                console.WriteLine($"  • {file}");
            }
        }
    }
    
    private static AutonomyLevel ParseAutonomy(string autonomy)
    {
        return autonomy.ToLowerInvariant() switch
        {
            "supervised" => AutonomyLevel.Supervised,
            "semi-autonomous" or "semiautonomous" => AutonomyLevel.SemiAutonomous,
            "fully-autonomous" or "fullyautonomous" => AutonomyLevel.FullyAutonomous,
            _ => AutonomyLevel.Supervised
        };
    }
    
    private static MockUserPersona ParsePersona(string persona)
    {
        return persona.ToLowerInvariant() switch
        {
            "novice" => MockUserPersona.Novice,
            "average" => MockUserPersona.Average,
            "power-user" or "poweruser" => MockUserPersona.PowerUser,
            "adversarial" => MockUserPersona.Adversarial,
            "accessibility" => MockUserPersona.Accessibility,
            "impatient" => MockUserPersona.Impatient,
            _ => MockUserPersona.Average
        };
    }
    
    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton<IProviderFactory, ProviderFactory>();
        
        return services.BuildServiceProvider();
    }
    
    private static IExecutionContext CreateExecutionContext()
    {
        return new ExecutionContext
        {
            AgentId = "demo-dev-agent",
            BehaviorId = "demo-dev-behavior",
            IsAirGapped = false,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
    }
}
