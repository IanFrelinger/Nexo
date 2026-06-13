#if NET8_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Routing;
using Nexo.Transport.Grpc;

namespace Nexo.Runtime.Routing;

/// <summary>
/// Periodic endpoint health monitor that updates registry health state.
/// </summary>
internal sealed class EndpointHealthMonitor : BackgroundService
{
    private readonly IEndpointRegistry _endpointRegistry;
    private readonly GrpcAgentTransport _grpcTransport;
    private readonly RoutingOptions _routingOptions;
    private readonly ILogger<EndpointHealthMonitor> _logger;
    private readonly Dictionary<string, int> _consecutiveFailures = new(StringComparer.OrdinalIgnoreCase);

    public EndpointHealthMonitor(
        IEndpointRegistry endpointRegistry,
        GrpcAgentTransport grpcTransport,
        IOptions<RoutingOptions> routingOptions,
        ILogger<EndpointHealthMonitor> logger)
    {
        _endpointRegistry = endpointRegistry ?? throw new ArgumentNullException(nameof(endpointRegistry));
        _grpcTransport = grpcTransport ?? throw new ArgumentNullException(nameof(grpcTransport));
        _routingOptions = routingOptions?.Value ?? new RoutingOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(1, _routingOptions.HealthCheckIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        await ProbeAllAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProbeAllAsync(stoppingToken);
        }
    }

    private async Task ProbeAllAsync(CancellationToken cancellationToken)
    {
        foreach (var descriptor in _endpointRegistry.GetAll())
        {
            if (string.IsNullOrWhiteSpace(descriptor.Endpoint))
            {
                continue;
            }

            try
            {
                var health = await _grpcTransport.CheckEndpointHealthAsync(
                    descriptor.Endpoint,
                    cancellationToken);
                _endpointRegistry.UpdateHealth(descriptor.Endpoint, health.IsHealthy);

                if (!health.IsHealthy)
                {
                    var failures = IncrementFailureCount(descriptor.Endpoint);
                    if (failures >= Math.Max(1, _routingOptions.DegradedLogFailureThreshold))
                    {
                        _logger.LogWarning(
                            "Endpoint {EndpointName} ({Endpoint}) is degraded after {FailureCount} failed probes: {Diagnostic}",
                            descriptor.Name,
                            descriptor.Endpoint,
                            failures,
                            health.DiagnosticMessage ?? health.Message ?? "No diagnostic message");
                    }
                }
                else
                {
                    ResetFailureCount(descriptor.Endpoint);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _endpointRegistry.UpdateHealth(descriptor.Endpoint, false);
                var failures = IncrementFailureCount(descriptor.Endpoint);
                if (failures >= Math.Max(1, _routingOptions.DegradedLogFailureThreshold))
                {
                    _logger.LogWarning(
                        ex,
                        "Endpoint health probe failed for {EndpointName} ({Endpoint}) after {FailureCount} failed probes",
                        descriptor.Name,
                        descriptor.Endpoint,
                        failures);
                }
            }
        }
    }

    private int IncrementFailureCount(string endpoint)
    {
        lock (_consecutiveFailures)
        {
            _consecutiveFailures.TryGetValue(endpoint, out var current);
            var next = current + 1;
            _consecutiveFailures[endpoint] = next;
            return next;
        }
    }

    private void ResetFailureCount(string endpoint)
    {
        lock (_consecutiveFailures)
        {
            _consecutiveFailures.Remove(endpoint);
        }
    }
}
#endif
