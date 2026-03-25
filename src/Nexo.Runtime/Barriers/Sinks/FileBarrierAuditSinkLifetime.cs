#if NET8_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexo.Runtime.Barriers.Sinks;

public sealed class FileBarrierAuditSinkLifetime : IHostedService
{
    private readonly FileBarrierAuditSink _sink;
    private readonly ILogger<FileBarrierAuditSinkLifetime> _logger;

    public FileBarrierAuditSinkLifetime(
        FileBarrierAuditSink sink,
        ILogger<FileBarrierAuditSinkLifetime> logger)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _sink.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose FileBarrierAuditSink during shutdown.");
        }
    }
}
#endif
