using Nexo.Agents.AutonomousDev;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI implementation of approval callback for supervised mode.
/// </summary>
public class DevCommandApprovalCallback : IApprovalCallback
{
    private readonly CliConsole _console;
    
    public DevCommandApprovalCallback(CliConsole console)
    {
        _console = console;
    }
    
    public Task<bool> ApproveOpenQuestionsAsync(Specification specification, CancellationToken ct = default)
    {
        _console.WriteLine();
        _console.WriteHeader("❓ Open Questions Require Resolution");
        _console.WriteLine();
        
        foreach (var question in specification.OpenQuestions)
        {
            _console.WriteColoredLine($"  • {question}", ConsoleColor.Yellow);
        }
        
        _console.WriteLine();
        _console.WriteColored("These questions need answers before proceeding.", ConsoleColor.Yellow);
        _console.WriteLine();
        _console.WriteLine("Please provide answers or press Enter to skip (agent will proceed with assumptions).");
        
        // Use CliConsole prompt method
        return Task.FromResult(_console.PromptYesNo("Proceed with assumptions?", defaultValue: true));
    }
    
    public Task<bool> ApprovePlanAsync(DevelopmentPlan plan, CancellationToken ct = default)
    {
        _console.WriteLine();
        _console.WriteHeader("📋 Development Plan - Approval Required");
        _console.WriteLine();
        
        _console.WritePair("Tasks", plan.Tasks.Count.ToString());
        _console.WritePair("Estimated Duration", $"{plan.EstimatedDuration.TotalMinutes:F1} minutes");
        _console.WriteLine();
        
        _console.WriteHeader("Tasks:");
        foreach (var task in plan.Tasks)
        {
            _console.WriteLine($"  [{task.Type}] {task.Title}");
            if (!string.IsNullOrEmpty(task.Description))
            {
                _console.WriteColoredLine($"    {task.Description}", ConsoleColor.Gray);
            }
        }
        
        _console.WriteLine();
        _console.WriteColored("Do you want to proceed with this plan? (y/n): ", ConsoleColor.Cyan);
        
        // Use CliConsole prompt method
        return Task.FromResult(_console.PromptYesNo("Proceed with this plan?", defaultValue: true));
    }
    
    public Task<bool> ApproveChangesAsync(IReadOnlyList<GeneratedArtifact> artifacts, CancellationToken ct = default)
    {
        _console.WriteLine();
        _console.WriteHeader("📝 Generated Changes - Approval Required");
        _console.WriteLine();
        
        _console.WritePair("Files to Modify", artifacts.Count.ToString());
        _console.WriteLine();
        
        foreach (var artifact in artifacts)
        {
            var changeType = artifact.IsFullFile ? "Create/Replace" : "Modify";
            _console.WriteLine($"  [{changeType}] {artifact.TargetPath}");
            if (!string.IsNullOrEmpty(artifact.Description))
            {
                _console.WriteColoredLine($"    {artifact.Description}", ConsoleColor.Gray);
            }
        }
        
        _console.WriteLine();
        _console.WriteColored("Do you want to apply these changes? (y/n): ", ConsoleColor.Cyan);
        
        // Use CliConsole prompt method
        return Task.FromResult(_console.PromptYesNo("Apply these changes?", defaultValue: true));
    }
    
    public Task<string?> GetClarificationAsync(string question, CancellationToken ct = default)
    {
        _console.WriteLine();
        _console.WriteHeader("❓ Clarification Needed");
        _console.WriteLine();
        _console.WriteColoredLine(question, ConsoleColor.Yellow);
        _console.WriteLine();
        _console.WriteColored("Please provide clarification (or press Enter to skip): ", ConsoleColor.Cyan);
        
        // Use CliConsole prompt method
        var response = _console.Prompt("Clarification: ");
        return Task.FromResult(string.IsNullOrWhiteSpace(response) ? null : response);
    }
}
