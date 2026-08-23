#if NET8_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ashlar.Runtime.Barriers.Sinks;

/// <summary>Hosted service that flushes and disposes <see cref="FileBarrierAuditSink"/> on shutdown.</summary>
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

    /// <summary>No-op startup hook.</summary>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Flushes and disposes the file audit sink on shutdown.</summary>
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
