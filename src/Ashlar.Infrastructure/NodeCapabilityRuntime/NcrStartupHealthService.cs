using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Backends;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime;

/// <summary>
/// Runs an NCR/Ollama startup probe and logs degradation diagnostics.
/// </summary>
public sealed class NcrStartupHealthService : IHostedService
{
    private readonly IModelServingBackend _backend;
    private readonly IPlatformPolicy _policy;
    private readonly ILogger<NcrStartupHealthService> _logger;

    /// <summary>Initializes a new ncr startup health service.</summary>
    public NcrStartupHealthService(
        IModelServingBackend backend,
        IPlatformPolicy policy,
        ILogger<NcrStartupHealthService> logger)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Start asynchronously.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_backend is not OllamaModelServingBackend)
        {
            _logger.LogInformation(
                "NCR startup health check skipped: backend {BackendType} is non-Ollama.",
                _backend.BackendType);
            return;
        }

        try
        {
            var available = await _backend.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
            if (available)
            {
                _logger.LogInformation(
                    "NCR startup health check passed: Ollama backend is reachable for platform policy {Platform}.",
                    _policy.Platform);
                return;
            }

            _logger.LogWarning(
                "NCR startup health check degraded: Ollama backend returned unavailable status. " +
                "Agentic execution may escalate until backend is healthy.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "NCR startup health check degraded: failed to reach Ollama backend. " +
                "Verify Ashlar:NodeCapabilityRuntime:Ollama:BaseUrl and Ollama service availability.");
        }
    }

    /// <summary>Stop asynchronously.</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
