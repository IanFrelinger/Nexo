using System.Text.Json;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Common.Models;
using Nexo.Orchestration.Coordination;

namespace Nexo.CLI.Formatting;

/// <summary>
/// Host-specific console renderer for CLI output.
/// </summary>
public interface IConsoleRenderer
{
    void RenderSuccess(string message);
    void RenderError(string message);
    void RenderErrorWithCode(string message, string? errorCode, string? suggestion = null);
    void RenderProgressStart(string message);
    void RenderProgressComplete(string message);
    void RenderProgress(ProgressReport report);
    void RenderAnalysisResult(AnalysisResult result, bool json);
    void RenderValidationResult(ValidationResult result, bool json);
    void RenderAgentResult(AgentExecutionResult result, bool json);
    void RenderAgentList(IReadOnlyList<AgentMetadata> agents, bool json);
    void RenderOrchestrationResult(OrchestrationResult result);
    void RenderJson(object data);
    void RenderTable<T>(IEnumerable<T> items);
}

/// <summary>
/// Console renderer implementation.
/// </summary>
public class ConsoleRenderer : IConsoleRenderer
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ConsoleRenderer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public void RenderSuccess(string message)
    {
        Console.Out.WriteLine(message);
    }

    public void RenderError(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
    }

    public void RenderProgressStart(string message)
    {
        Console.Out.WriteLine($"[progress] {message}");
    }

    public void RenderProgressComplete(string message)
    {
        Console.Out.WriteLine($"[complete] {message}");
    }

    public void RenderProgress(ProgressReport report)
    {
        var stepInfo = report.TotalSteps.HasValue && report.CurrentStep.HasValue
            ? $" ({report.CurrentStep}/{report.TotalSteps})"
            : string.Empty;
        Console.Out.WriteLine($"[{report.Percentage}%]{stepInfo} {report.Message}");
    }

    public void RenderErrorWithCode(string message, string? errorCode, string? suggestion = null)
    {
        Console.Error.WriteLine($"Error [{errorCode}]: {message}");
        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            Console.Error.WriteLine($"Suggestion: {suggestion}");
        }
    }

    public void RenderAgentList(IReadOnlyList<Nexo.Core.Application.Agent.Models.AgentMetadata> agents, bool json)
    {
        if (json)
        {
            var envelope = new CliEnvelope<IReadOnlyList<Nexo.Core.Application.Agent.Models.AgentMetadata>>(
                true, 
                agents, 
                null);
            Console.Out.WriteLine(JsonSerializer.Serialize(envelope, _jsonOptions));
        }
        else
        {
            if (agents.Count == 0)
            {
                Console.Out.WriteLine("No agents found.");
                return;
            }

            Console.Out.WriteLine($"Found {agents.Count} agent(s):");
            foreach (var agent in agents)
            {
                Console.Out.WriteLine($"  - {agent.Name}");
                if (!string.IsNullOrWhiteSpace(agent.Description))
                {
                    Console.Out.WriteLine($"    Description: {agent.Description}");
                }
                if (agent.Capabilities.Count > 0)
                {
                    Console.Out.WriteLine($"    Capabilities: {string.Join(", ", agent.Capabilities)}");
                }
            }
        }
    }

    public void RenderAnalysisResult(AnalysisResult result, bool json)
    {
        if (json)
        {
            var envelope = new CliEnvelope<AnalysisResult>(!result.HasViolations, result, result.HasViolations ? $"Found {result.TotalViolations} violation(s)" : null);
            Console.Out.WriteLine(JsonSerializer.Serialize(envelope, _jsonOptions));
        }
        else
        {
            if (result.HasViolations)
            {
                Console.Error.WriteLine($"Found {result.TotalViolations} violation(s):");
                foreach (var violation in result.Violations)
                {
                    Console.Error.WriteLine($"  - [{violation.Severity}] {violation.Rule}: {violation.Message} ({violation.FilePath}:{violation.LineNumber})");
                }
            }
            else
            {
                Console.Out.WriteLine("No violations found.");
            }
        }
    }

    public void RenderValidationResult(ValidationResult result, bool json)
    {
        if (json)
        {
            var envelope = new CliEnvelope<ValidationResult>(result.Passed, result, result.Passed ? null : "Validation failed");
            Console.Out.WriteLine(JsonSerializer.Serialize(envelope, _jsonOptions));
        }
        else
        {
            if (result.Passed)
            {
                Console.Out.WriteLine($"Validation passed ({result.TestsPassed}/{result.TestsRun} tests)");
            }
            else
            {
                Console.Error.WriteLine($"Validation failed ({result.TestsFailed}/{result.TestsRun} tests failed)");
                if (result.TestResults != null)
                {
                    foreach (var test in result.TestResults.Where(t => !t.Passed))
                    {
                        Console.Error.WriteLine($"  - {test.Name}: {test.Message}");
                    }
                }
            }
        }
    }

    public void RenderAgentResult(AgentExecutionResult result, bool json)
    {
        if (json)
        {
            var envelope = new CliEnvelope<AgentExecutionResult>(result.Success, result, result.Success ? null : result.Message);
            Console.Out.WriteLine(JsonSerializer.Serialize(envelope, _jsonOptions));
        }
        else
        {
            if (result.Success)
            {
                Console.Out.WriteLine($"Agent '{result.AgentName}' executed successfully");
                if (result.Duration.HasValue)
                {
                    Console.Out.WriteLine($"Duration: {result.Duration.Value.TotalMilliseconds}ms");
                }
            }
            else
            {
                Console.Error.WriteLine($"Agent '{result.AgentName}' failed: {result.Message}");
            }
        }
    }

    public void RenderOrchestrationResult(OrchestrationResult result)
    {
        if (result.Success)
        {
            Console.Out.WriteLine($"Orchestration completed successfully");
            Console.Out.WriteLine($"  Agents: {result.Decomposition?.Agents.Count ?? 0}");
            Console.Out.WriteLine($"  Conflicts: {result.Conflicts.Count}");
            Console.Out.WriteLine($"  Escalations: {result.Escalations.Count}");
            if (result.ProgressSummary != null)
            {
                Console.Out.WriteLine($"  Progress: {result.ProgressSummary.Completed}/{result.ProgressSummary.TotalAgents} agents completed ({result.ProgressSummary.ProgressPercentage:P0})");
            }
        }
        else
        {
            Console.Error.WriteLine("Orchestration failed");
            if (result.Conflicts.Count > 0)
            {
                Console.Error.WriteLine($"  Conflicts: {result.Conflicts.Count}");
            }
            if (result.Escalations.Count > 0)
            {
                Console.Error.WriteLine($"  Escalations: {result.Escalations.Count}");
            }
        }
    }

    public void RenderJson(object data)
    {
        Console.Out.WriteLine(JsonSerializer.Serialize(data, _jsonOptions));
    }

    public void RenderTable<T>(IEnumerable<T> items)
    {
        // Simple table rendering - can be enhanced with Spectre.Console later
        foreach (var item in items)
        {
            Console.Out.WriteLine(item?.ToString() ?? string.Empty);
        }
    }

    private record CliEnvelope<T>(
        [property: System.Text.Json.Serialization.JsonPropertyName("ok")] bool Ok,
        [property: System.Text.Json.Serialization.JsonPropertyName("data")] T? Data,
        [property: System.Text.Json.Serialization.JsonPropertyName("error")] string? Error
    );
}

