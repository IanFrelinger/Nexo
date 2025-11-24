using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for running agent actions.
/// </summary>
public class AgentCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<AgentCommand> _logger;

    public AgentCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<AgentCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string agentName, FileInfo? inputFile, bool json)
    {
        try
        {
            var command = new RunAgentCommand(agentName, inputFile);
            var result = await _mediator.Send(command);

            _renderer.RenderAgentResult(result, json);

            return result.Success ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (AgentExecutionException ex)
        {
            _logger.LogError(ex, "Agent execution failed: {AgentName}", ex.AgentName);
            _renderer.RenderError(ex.Message);
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

