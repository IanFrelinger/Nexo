using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;

namespace Nexo.Infrastructure.NodeCapabilityRuntime;

/// <summary>
/// Runs an NCR/Ollama startup probe and logs degradation diagnostics.
/// </summary>
public sealed class NcrStartupHealthService : IHostedService
{
    private readonly IModelServingBackend _backend;
    private readonly IPlatformPolicy _policy;
    private readonly ILogger<NcrStartupHealthService> _logger;

    public NcrStartupHealthService(
        IModelServingBackend backend,
        IPlatformPolicy policy,
        ILogger<NcrStartupHealthService> logger)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
                "Verify Nexo:NodeCapabilityRuntime:Ollama:BaseUrl and Ollama service availability.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
