using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Transport;
using Nexo.Orchestration.Agents;

namespace Nexo.Orchestration.Transport;

/// <summary>
/// Default in-process transport implementation.
/// Delegates invocations to the existing lifecycle manager to preserve behavior.
/// </summary>
public sealed class InProcessAgentTransport : IAgentTransport
{
    private readonly LifecycleManager _lifecycleManager;
    private readonly ILogger<InProcessAgentTransport> _logger;

    public InProcessAgentTransport(
        LifecycleManager lifecycleManager,
        ILogger<InProcessAgentTransport> logger)
    {
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> SendAsync(
        AgentInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            var output = await _lifecycleManager.ExecuteAgentAsync(
                request.AgentName,
                request.DependencyOutputs,
                cancellationToken);

            return new AgentResult(
                Success: true,
                Output: output,
                Duration: DateTimeOffset.UtcNow - startedAt);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("not registered", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                ex,
                "In-process transport could not resolve agent {AgentName} (CorrelationId: {CorrelationId})",
                request.AgentName,
                request.CorrelationId);

            return new AgentResult(
                Success: false,
                ErrorMessage: ex.Message,
                Duration: DateTimeOffset.UtcNow - startedAt,
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["errorCode"] = "AGENT_NOT_FOUND"
                });
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(
                ex,
                "In-process transport timed out for agent {AgentName} (CorrelationId: {CorrelationId})",
                request.AgentName,
                request.CorrelationId);

            return new AgentResult(
                Success: false,
                ErrorMessage: ex.Message,
                Duration: DateTimeOffset.UtcNow - startedAt,
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["errorCode"] = "TIMEOUT"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "In-process transport failed executing agent {AgentName} (CorrelationId: {CorrelationId})",
                request.AgentName,
                request.CorrelationId);

            return new AgentResult(
                Success: false,
                ErrorMessage: ex.Message,
                Duration: DateTimeOffset.UtcNow - startedAt,
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["errorCode"] = "TRANSPORT_ERROR"
                });
        }
    }

    public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TransportHealth(
            IsHealthy: true,
            TransportName: nameof(InProcessAgentTransport),
            Message: "In-process transport is available"));
    }
}
