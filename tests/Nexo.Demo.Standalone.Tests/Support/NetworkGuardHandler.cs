using System.Net.Http;

namespace Nexo.Demo.Tests.Support;

/// <summary>
/// HTTP message handler that blocks network requests in Off/Assist modes
/// </summary>
public sealed class NetworkGuardHandler : DelegatingHandler
{
    public NetworkGuardHandler(HttpMessageHandler? inner = null) : base(inner ?? new HttpClientHandler()) { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Increment the network attempt counter
        DemoHarness.IncrementNetworkAttempts();

        var mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant();
        if (mode is "off" or "assist")
        {
            throw new InvalidOperationException($"Network egress blocked in {mode} mode. Request to {request.RequestUri} was blocked.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
