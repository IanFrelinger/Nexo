using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Transport;

namespace Ashlar.Runtime.Transport;

/// <summary>
/// Delegates invocation to in-process or remote transport based on request options.
/// </summary>
public sealed class RoutingAgentTransport : IAgentTransport
{
    private readonly IAgentTransport _inProcess;
    private readonly IAgentTransport _remote;
    private readonly ILogger<RoutingAgentTransport> _logger;

    public RoutingAgentTransport(
        IAgentTransport inProcess,
        IAgentTransport remote,
        ILogger<RoutingAgentTransport> logger)
    {
        _inProcess = inProcess ?? throw new ArgumentNullException(nameof(inProcess));
        _remote = remote ?? throw new ArgumentNullException(nameof(remote));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Routes an agent invocation to in-process or remote transport based on target endpoint.</summary>
    public Task<AgentResult> SendAsync(
        AgentInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.EffectiveOptions.TargetEndpoint))
        {
            _logger.LogDebug("Routing invocation for {AgentName} to in-process transport.", request.AgentName);
            return _inProcess.SendAsync(request, cancellationToken);
        }

        _logger.LogDebug(
            "Routing invocation for {AgentName} to remote transport endpoint {TargetEndpoint}.",
            request.AgentName,
            request.EffectiveOptions.TargetEndpoint);
        return _remote.SendAsync(request, cancellationToken);
    }

    /// <summary>Returns combined health from in-process and remote transports.</summary>
    public async Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var inProcessHealth = await _inProcess.CheckHealthAsync(cancellationToken);
        var remoteHealth = await _remote.CheckHealthAsync(cancellationToken);

        var healthy = inProcessHealth.IsHealthy && remoteHealth.IsHealthy;
        var diagnostic = $"in-process: {inProcessHealth.DiagnosticMessage ?? inProcessHealth.Message ?? "ok"}; " +
                         $"remote: {remoteHealth.DiagnosticMessage ?? remoteHealth.Message ?? "ok"}";

        return new TransportHealth(
            IsHealthy: healthy,
            TransportName: "routing",
            Message: diagnostic,
            TransportType: "routing",
            DiagnosticMessage: diagnostic);
    }
}
