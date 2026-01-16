using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev;
using Nexo.Agents.AutonomousDev.Configuration;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Agents.UniversalTester;
using Nexo.CLI.Output;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.IO;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for running the Autonomous Development Agent.
/// </summary>
public class AutonomousDevCommand : Command
{
    public AutonomousDevCommand() : base("dev", "Run Autonomous Development Agent to build features autonomously")
    {
        var projectOption = new Option<DirectoryInfo>(
            "--project",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            "Path to the project to modify (defaults to current directory)");
        
        var taskOption = new Option<string>(
            "--task",
            () => "Create/update NEXO_AGENT_NOTES.md with a short demo note (safe, non-breaking change).",
            "What to build (natural language; defaults to a safe demo task)");
        
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

        var resumeOption = new Option<FileInfo?>(
            "--resume",
            "Resume from a previously saved session JSON file");

        var testTargetOption = new Option<string?>(
            "--test-target",
            () => "cli://dotnet --version",
            "Target for the Universal Tester (defaults to an offline CLI target)");

        var providerOption = new Option<string>(
            "--provider",
            () => "offline",
            "LLM provider: offline, mock-json, mock, openai, azure, ollama (offline providers work air-gapped)");

        var yesOption = new Option<bool>(
            "--yes",
            () => false,
            "Auto-approve all prompts (non-interactive)");

        var assumeDefaultsOption = new Option<bool>(
            "--assume-defaults",
            () => true,
            "When prompted for open questions/clarifications, proceed with defaults/assumptions");
        
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
        AddOption(resumeOption);
        AddOption(testTargetOption);
        AddOption(providerOption);
        AddOption(yesOption);
        AddOption(assumeDefaultsOption);
        AddOption(dryRunOption);
        AddOption(verboseOption);
        AddOption(jsonOption);
        
        this.SetHandler(async (InvocationContext ctx) =>
        {
            var project = ctx.ParseResult.GetValueForOption(projectOption)!;
            var task = ctx.ParseResult.GetValueForOption(taskOption)!;
            var spec = ctx.ParseResult.GetValueForOption(specOption);
            var acceptance = ctx.ParseResult.GetValueForOption(acceptanceOption);
            var maxIterations = ctx.ParseResult.GetValueForOption(iterationsOption);
            var autonomy = ctx.ParseResult.GetValueForOption(autonomyOption) ?? "supervised";
            var testPersona = ctx.ParseResult.GetValueForOption(personaOption) ?? "average";
            var output = ctx.ParseResult.GetValueForOption(outputOption);
            var resume = ctx.ParseResult.GetValueForOption(resumeOption);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOption);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
            var json = ctx.ParseResult.GetValueForOption(jsonOption);
            var testTarget = ctx.ParseResult.GetValueForOption(testTargetOption);
            var provider = ctx.ParseResult.GetValueForOption(providerOption) ?? "offline";
            var yes = ctx.ParseResult.GetValueForOption(yesOption);
            var assumeDefaults = ctx.ParseResult.GetValueForOption(assumeDefaultsOption);

            await ExecuteAsync(project, task, spec, acceptance, maxIterations, autonomy, testPersona, output, resume, testTarget, provider, yes, assumeDefaults, dryRun, verbose, json);
        });
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
        FileInfo? resume,
        string? testTarget,
        string provider,
        bool yes,
        bool assumeDefaults,
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
            console.WritePair("Test Target", testTarget ?? "(auto)");
            console.WritePair("Provider", provider);
            if (dryRun) console.WriteColoredLine("DRY RUN MODE", ConsoleColor.Yellow);
            console.WriteLine();
        }
        
        try
        {
            DevelopmentSession? resumeSession = null;
            if (resume != null && resume.Exists)
            {
                var resumeJson = await TextFile.ReadAllTextAsync(resume.FullName);
                resumeSession = JsonSerializer.Deserialize<DevelopmentSession>(resumeJson);
                if (!json && console != null && resumeSession != null)
                {
                    console.WritePair("Resuming session", resume.FullName);
                    console.WritePair("Resumed iterations", resumeSession.Iterations.Count.ToString());
                }
            }

            // Load spec file if provided
            string? detailedSpec = null;
            if (spec != null && spec.Exists)
            {
                detailedSpec = await TextFile.ReadAllTextAsync(spec.FullName);
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
                TestPersona = ParsePersona(testPersona),
                TestTarget = testTarget
            };
            
            // Create console first (needed for approval callback)
            if (console == null && !json)
            {
                console = new CliConsole(verbose);
            }
            
            // Create services and agent
            var services = BuildServices();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var testerLogger = loggerFactory.CreateLogger<UniversalTesterAgent>();
            var agentLogger = loggerFactory.CreateLogger<AutonomousDevAgent>();
            var providerFactory = services.GetRequiredService<Nexo.Infrastructure.Execution.IProviderFactory>();
            var tester = new UniversalTesterAgent(providerFactory, testerLogger);

            // Session persistence: if output provided, snapshot session as it progresses.
            // If resuming and output not specified, default to writing back to the resume file.
            var effectiveOutput = output ?? resume;
            ISessionStore? sessionStore = effectiveOutput != null
                ? new DevSessionFileStore(effectiveOutput)
                : null;
            
            // Create approval callback for supervised mode
            IApprovalCallback? approvalCallback = null;
            if (config.Autonomy == AutonomyLevel.Supervised && console != null)
            {
                approvalCallback = new DevCommandApprovalCallback(console, yes, assumeDefaults);
            }
            
            var agent = new AutonomousDevAgent(providerFactory, tester, agentLogger, approvalCallback, sessionStore);
            
            // Create execution context
            var context = CreateExecutionContext(provider);
            
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
            var session = resumeSession != null
                ? await agent.ExecuteAsync(config, context, resumeSession, CancellationToken.None)
                : await agent.ExecuteAsync(config, context, CancellationToken.None);
            
            // Display final results
            if (json)
            {
                var jsonOutput = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonOutput);
                
                if (output != null)
                {
                    await TextFile.WriteAllTextAsync(output.FullName, jsonOutput);
                }
            }
            else if (console != null)
            {
                DisplayDevResults(console, session);
                
                if (output != null)
                {
                    var jsonOutput = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                    await TextFile.WriteAllTextAsync(output.FullName, jsonOutput);
                    console.WriteSuccess($"Session saved to {output.FullName}");
                }
            }
            
            Environment.ExitCode = session.Status == SessionStatus.Completed ? 0 : 1;
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
            SessionStatus.Completed => ConsoleColor.Green,
            SessionStatus.Partial => ConsoleColor.Yellow,
            SessionStatus.Failed => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
        
        var statusText = session.Status switch
        {
            SessionStatus.Completed => "✅ Development Complete!",
            SessionStatus.Partial => "⚠️ Partial Success",
            SessionStatus.Failed => "❌ Development Failed",
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
        services.AddSingleton<Nexo.Infrastructure.Execution.IProviderFactory, Nexo.Infrastructure.Execution.ProviderFactory>();
        
        return services.BuildServiceProvider();
    }
    
    private static IExecutionContext CreateExecutionContext(string provider)
    {
        return new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "demo-dev-agent",
            BehaviorId = "demo-dev-behavior",
            IsAirGapped = false,
            AuditMode = false,
            Provider = string.IsNullOrWhiteSpace(provider) ? "offline" : provider,
            Variables = new Dictionary<string, object>()
        };
    }
}
