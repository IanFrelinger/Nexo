using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Transport;

namespace Ashlar.Runtime.Transport;

/// <summary>
/// Remote-side transport dispatcher: routes an invocation to the transport whose registered
/// endpoint prefix matches the target endpoint (e.g. <c>a2a+https://…</c> → A2A), falling back
/// to the default remote transport for everything else (bare gRPC endpoints today).
///
/// This exists because <c>RoutingAgentTransport</c> supports exactly one remote transport and
/// <c>EndpointDescriptor</c> has no protocol field — the scheme prefix convention adds protocol
/// selection without any public-API change to the packable kernel spine. The dispatcher has
/// zero protocol dependencies; concrete transports register via
/// <see cref="AgentTransportSchemeRegistration"/> in DI.
/// </summary>
public sealed class SchemeDispatchingAgentTransport : IAgentTransport
{
    private readonly IReadOnlyList<KeyValuePair<string, IAgentTransport>> _byPrefix;
    private readonly IAgentTransport _fallback;
    private readonly ILogger<SchemeDispatchingAgentTransport> _logger;

    /// <summary>Creates the dispatcher over prefix→transport pairs and the default remote.</summary>
    public SchemeDispatchingAgentTransport(
        IEnumerable<KeyValuePair<string, IAgentTransport>> byPrefix,
        IAgentTransport fallback,
        ILogger<SchemeDispatchingAgentTransport> logger)
    {
        _byPrefix = byPrefix?.ToList() ?? throw new ArgumentNullException(nameof(byPrefix));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var endpoint = request.EffectiveOptions.TargetEndpoint;
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            foreach (var pair in _byPrefix)
            {
                if (endpoint!.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Dispatching invocation for {AgentName} to '{Prefix}' transport (endpoint {TargetEndpoint}).",
                        request.AgentName, pair.Key, endpoint);
                    return pair.Value.SendAsync(request, cancellationToken);
                }
            }
        }

        return _fallback.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        var healthy = true;

        foreach (var pair in _byPrefix)
        {
            var health = await pair.Value.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            healthy &= health.IsHealthy;
            parts.Add($"{pair.Key}: {(health.IsHealthy ? "ok" : health.Message ?? "unhealthy")}");
        }

        var fallbackHealth = await _fallback.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
        healthy &= fallbackHealth.IsHealthy;
        parts.Add($"default: {(fallbackHealth.IsHealthy ? "ok" : fallbackHealth.Message ?? "unhealthy")}");

        var diagnostic = string.Join("; ", parts);
        return new TransportHealth(
            IsHealthy: healthy,
            TransportName: "scheme-dispatch",
            Message: diagnostic,
            TransportType: "scheme-dispatch",
            DiagnosticMessage: diagnostic);
    }
}
