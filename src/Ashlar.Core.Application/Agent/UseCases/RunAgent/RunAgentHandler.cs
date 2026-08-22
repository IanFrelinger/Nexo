using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Agent.Ports;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Domain.Exceptions;

namespace Ashlar.Core.Application.Agent.UseCases.RunAgent;

/// <summary>
/// MediatR handler for running agent actions.
/// 
/// Responsibilities:
/// - Executes agents via IAgentExecutor
/// - Records execution metrics (duration, success/failure counts)
/// - Handles timeouts and exceptions
/// - Logs execution details
/// 
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class RunAgentHandler : IRequestHandler<RunAgentCommand, AgentExecutionResult>
{
    private readonly IAgentExecutor _agentExecutor;
    private readonly ILogger<RunAgentHandler> _logger;
    private readonly IMetricsCollector? _metricsCollector;

    /// <summary>Creates a handler that executes agents via <see cref="IAgentExecutor"/>.</summary>
    /// <param name="agentExecutor">Service that runs agent actions.</param>
    /// <param name="logger">Logger for execution progress and errors.</param>
    /// <param name="metricsCollector">Optional metrics collector for execution telemetry.</param>
    public RunAgentHandler(
        IAgentExecutor agentExecutor,
        ILogger<RunAgentHandler> logger,
        IMetricsCollector? metricsCollector = null)
    {
        _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Handles the RunAgentCommand by executing the specified agent.
    /// </summary>
    /// <param name="request">Command containing agent name and optional input file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent execution result with success status, output, and duration</returns>
    /// <exception cref="AgentExecutionException">Thrown when agent execution fails</exception>
    /// <exception cref="TimeoutException">Thrown when agent execution times out</exception>
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
            _metricsCollector?.RecordExecutionTime($"Agent.{request.AgentName}", duration);
            _metricsCollector?.IncrementCounter("Agent.Executed");
            if (result.Success)
            {
                _metricsCollector?.IncrementCounter("Agent.Success");
            }
            else
            {
                _metricsCollector?.IncrementCounter("Agent.Failed");
            }

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
            _metricsCollector?.IncrementCounter("Agent.Timeouts");
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
            _metricsCollector?.IncrementCounter("Agent.Errors");
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

