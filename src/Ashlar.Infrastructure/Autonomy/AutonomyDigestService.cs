using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Infrastructure.Certification.HotSwap;

namespace Ashlar.Infrastructure.Autonomy;

/// <summary>
/// Periodically renders the human digest (autonomy spec §7) from the retained swap
/// provenance into the host's log — the "what did the loop do / what is it holding"
/// summary a human reads on cadence, not a control surface. Single-op controls (pause,
/// rollback, demotion) stay where they are: on the authority services, invoked
/// deliberately, never from a rendering job.
///
/// <para>Renders from <see cref="RecordingBrickSwapProvenanceSink"/> — the retaining sink
/// <see cref="AutonomyServiceCollectionExtensions.AddAshlarAutonomyDigestJob"/> installs.
/// Render failures are logged and the cadence continues: a digest that dies on one bad
/// render is a digest nobody reads.</para>
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class AutonomyDigestService : BackgroundService
{
    private readonly RecordingBrickSwapProvenanceSink _sink;
    private readonly IOptions<AshlarAutonomyOptions> _options;
    private readonly ILogger<AutonomyDigestService> _logger;

    /// <summary>Creates the digest job over the retaining sink.</summary>
    public AutonomyDigestService(
        RecordingBrickSwapProvenanceSink sink,
        IOptions<AshlarAutonomyOptions> options,
        ILogger<AutonomyDigestService> logger)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Renders one digest from the sink's current retained events.</summary>
    internal string RenderOnce()
    {
        var events = _sink.Snapshot();
        var digest = AutonomyDigest.Render(events);
        return _sink.DroppedEvents > 0
            ? digest + $"\n(ring saturated: showing the most recent {events.Count} event(s); "
                + $"{_sink.DroppedEvents} older event(s) dropped)"
            : digest;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = _options.Value.DigestIntervalSeconds;
        if (seconds <= 0)
        {
            _logger.LogInformation("Autonomy digest job disabled (DigestIntervalSeconds={Seconds})", seconds);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    _logger.LogInformation("{Digest}", RenderOnce());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Autonomy digest render failed; the cadence continues");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Host shutdown — the quiet exit.
        }
    }
}
