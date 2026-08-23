using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Agent.UseCases.RunAgent;
using Ashlar.Core.Domain.Exceptions;

namespace Ashlar.CLI.Commands;

/// <summary>
/// CLI command for running agent actions.
/// 
/// Provides the `ashlar agent` command that:
/// - Executes individual agents by name
/// - Supports optional input files
/// - Displays results in human-readable or JSON format
/// - Shows progress updates (if verbose)
/// - Handles errors and provides appropriate exit codes
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class AgentCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<AgentCommand> _logger;

    /// <summary>Creates a new AgentCommand instance.</summary>
    public AgentCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<AgentCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(string agentName, FileInfo? inputFile, bool json, bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: running agent '{agentName}'");
        }

        try
        {
            var command = new RunAgentCommand(agentName, inputFile);
            var result = await _mediator.Send(command);

            _renderer.RenderAgentResult(result, json);

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: agent '{agentName}' completed");
            }

            return result.Success ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (AgentExecutionException ex)
        {
            _logger.LogError(ex, "Agent execution failed: {AgentName}", ex.AgentName);
            if (!string.IsNullOrEmpty(ex.ErrorCode))
            {
                _renderer.RenderErrorWithCode(ex.Message, ex.ErrorCode, ex.Suggestion);
            }
            else
            {
                _renderer.RenderError(ex.Message);
            }
            return (int)ExitCode.ValidationFailed;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Agent execution timed out");
            _renderer.RenderError($"Timeout: {ex.Message}");
            return (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during agent execution");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

