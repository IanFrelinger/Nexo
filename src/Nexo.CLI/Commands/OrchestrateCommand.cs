using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Orchestration.Coordination;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for orchestrating agent execution.
/// 
/// Provides the `nexo orchestrate` command that:
/// - Takes a user request as input
/// - Delegates to Orchestrator for full workflow execution
/// - Displays progress and results in human-readable or JSON format
/// - Handles errors and provides appropriate exit codes
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class OrchestrateCommand
{
    private readonly Orchestrator _orchestrator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<OrchestrateCommand> _logger;

    public OrchestrateCommand(
        Orchestrator orchestrator,
        IConsoleRenderer renderer,
        ILogger<OrchestrateCommand> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the orchestrate command.
    /// </summary>
    /// <param name="request">User request to orchestrate</param>
    /// <param name="json">Whether to output JSON format</param>
    /// <param name="verbose">Whether to show verbose progress output</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    public async Task<int> ExecuteAsync(string request, bool json, bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: orchestrating request: {request}");
        }

        try
        {
            var result = await _orchestrator.OrchestrateAsync(request);

            if (json)
            {
                var jsonData = new
                {
                    ok = result.Success,
                    correlationId,
                    data = new
                    {
                        success = result.Success,
                        agentCount = result.Decomposition?.Agents.Count ?? 0,
                        conflicts = result.Conflicts.Count,
                        escalations = result.Escalations.Count,
                        progress = result.ProgressSummary != null ? new
                        {
                            completed = result.ProgressSummary.Completed,
                            total = result.ProgressSummary.TotalAgents,
                            percentage = result.ProgressSummary.ProgressPercentage
                        } : null,
                        output = result.IntegratedOutput?.IntegratedResults
                    }
                };
                _renderer.RenderJson(jsonData);
            }
            else
            {
                _renderer.RenderOrchestrationResult(result);
            }

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: orchestration completed");
            }

            return result.Success ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestration failed");
            if (!json)
            {
                _renderer.RenderError($"Orchestration failed: {ex.Message}");
            }
            else
            {
                _renderer.RenderJson(new
                {
                    ok = false,
                    correlationId,
                    error = ex.Message
                });
            }
            return (int)ExitCode.UnexpectedError;
        }
    }
}

