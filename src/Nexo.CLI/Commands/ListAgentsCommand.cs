using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Agent.UseCases.ListAgents;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for listing available agents.
/// </summary>
public class ListAgentsCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<ListAgentsCommand> _logger;

    public ListAgentsCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<ListAgentsCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(bool json)
    {
        try
        {
            var query = new ListAgentsQuery();
            var agents = await _mediator.Send(query);

            _renderer.RenderAgentList(agents, json);

            return (int)ExitCode.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing agents");
            _renderer.RenderError(ex.Message);
            return (int)ExitCode.UnexpectedError;
        }
    }
}

