using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Core.Application.Agent.UseCases.RunAgent;

/// <summary>
/// Handler for running agent actions.
/// </summary>
public class RunAgentHandler : IRequestHandler<RunAgentCommand, AgentExecutionResult>
{
    private readonly IAgentExecutor _agentExecutor;
    private readonly ILogger<RunAgentHandler> _logger;

    public RunAgentHandler(
        IAgentExecutor agentExecutor,
        ILogger<RunAgentHandler> logger)
    {
        _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentExecutionResult> Handle(
        RunAgentCommand request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting agent execution: {AgentName}",
            request.AgentName);

        try
        {
            var result = await _agentExecutor.ExecuteAsync(
                request.AgentName,
                request.InputFile,
                cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Agent execution completed: {AgentName}, Success: {Success}, Duration: {Duration}ms",
                request.AgentName,
                result.Success,
                duration.TotalMilliseconds);

            return result with { Duration = duration };
        }
        catch (AgentExecutionException)
        {
            // Re-throw domain exceptions
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(
                ex,
                "Agent execution timed out: {AgentName}",
                request.AgentName);

            throw new AgentExecutionException(
                request.AgentName,
                $"Agent execution timed out: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during agent execution: {AgentName}",
                request.AgentName);

            throw new AgentExecutionException(
                request.AgentName,
                $"Agent execution failed: {ex.Message}",
                ex);
        }
    }
}

