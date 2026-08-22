using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Agent.UseCases.ListAgents;

namespace Ashlar.CLI.Commands;

/// <summary>
/// CLI command for listing available agents.
/// 
/// Provides the `ashlar agent list` command that:
/// - Lists all available agents from the registry
/// - Displays agent metadata (name, description, capabilities)
/// - Outputs in human-readable or JSON format
/// - Useful for discovering available agents
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class ListAgentsCommand
{
    private readonly IMediator _mediator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<ListAgentsCommand> _logger;

    /// <summary>Creates a new ListAgentsCommand instance.</summary>
    public ListAgentsCommand(
        IMediator mediator,
        IConsoleRenderer renderer,
        ILogger<ListAgentsCommand> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(bool json, bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: listing agents");
        }

        try
        {
            var query = new ListAgentsQuery();
            var agents = await _mediator.Send(query);

            _renderer.RenderAgentList(agents, json);

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: listing done");
            }

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

