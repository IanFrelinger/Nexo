using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>How a mesh auto-pull node is configured: the shared folder to pull trusted
/// <c>.ashpkg</c> from, the project whose gate/policy decides them, and the poll interval.</summary>
public sealed record MeshAutoPullSettings(string PullDir, string ProjectDir, int IntervalSeconds);

/// <summary>The outcome of one pull pass, aggregated across every package scanned.</summary>
public sealed record MeshPullSummary(
    int Scanned, int Admitted, int Held, int Rejected, int Refused, int AlreadyImported, int Errors)
{
    /// <summary>Nothing to pull — the dir was absent or empty.</summary>
    public static MeshPullSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);
}

/// <summary>
/// A5 cross-machine sharing (consumer side): a hosted service that, on a timer, pulls TRUSTED signed
/// <c>.ashpkg</c> packages a peer published into a shared folder and submits each through the SAME
/// receiver-sovereign import path a manual <c>ashlar pkg import</c> uses — so how a package arrived
/// never changes how it is admitted. The Phase-3 trust root (a package sealed by an untrusted key is
/// refused before anything parks), the local policy's admission decision (a <c>proposing</c> consumer
/// HOLDS imported code for review — the safe cross-machine default), and the append-once dedupe all
/// come for free from <see cref="PackageImport.SubmitAsync"/>; this only supplies the timer + directory
/// scan the roadmap called the one missing piece.
///
/// <para>Opt-in and fail-closed: it is registered only when a pull dir is configured
/// (<c>ASHLAR_MESH_PULL_DIR</c>), an absent dir is a no-op (not an error), and a per-tick failure is
/// logged and retried next interval — it never crashes the daemon. It builds on signed <c>.ashpkg</c>
/// ONLY; the unsigned <c>.nxpkg</c> sneakernet path is deliberately untouched.</para>
/// </summary>
public sealed class MeshAutoPullService : BackgroundService
{
    private readonly MeshAutoPullSettings _settings;
    private readonly ILogger<MeshAutoPullService> _logger;

    /// <summary>Creates the mesh auto-pull service.</summary>
    public MeshAutoPullService(MeshAutoPullSettings settings, ILogger<MeshAutoPullService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.IntervalSeconds <= 0 || string.IsNullOrWhiteSpace(_settings.PullDir))
        {
            _logger.LogInformation("Mesh auto-pull disabled (no pull dir or non-positive interval).");
            return;
        }

        _logger.LogInformation(
            "Mesh auto-pull armed: {Dir} every {Interval}s → project {Project}. Only signers this node trusts are admitted.",
            _settings.PullDir, _settings.IntervalSeconds, _settings.ProjectDir);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.IntervalSeconds));
        try
        {
            // Pull once at startup, then on each tick — a node that just came up should not wait a
            // whole interval to pick up what a peer already published.
            do
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        try
        {
            var s = await PullOnceAsync(_settings.PullDir, _settings.ProjectDir, ct).ConfigureAwait(false);
            if (s.Scanned > 0)
            {
                _logger.LogInformation(
                    "Mesh auto-pull: scanned {Scanned} — {Admitted} admitted, {Held} held (awaiting review), "
                    + "{Rejected} rejected, {Refused} refused (untrusted signer), {Already} already decided, {Errors} error(s).",
                    s.Scanned, s.Admitted, s.Held, s.Rejected, s.Refused, s.AlreadyImported, s.Errors);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A pull pass must never take the daemon down — say so and retry next interval.
            _logger.LogWarning(ex, "Mesh auto-pull tick failed — retrying next interval.");
        }
    }

    /// <summary>
    /// One pull pass: enumerate <c>*.ashpkg</c> in <paramref name="pullDir"/> (skipping dotfiles and
    /// macOS AppleDouble sidecars) and submit each through <see cref="PackageImport.SubmitAsync"/>,
    /// aggregating the outcomes. A per-file failure counts as an error and does not stop the pass.
    /// Static and side-effect-scoped so it is directly testable without the timer.
    /// </summary>
    public static async Task<MeshPullSummary> PullOnceAsync(string pullDir, string projectDir, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pullDir) || !Directory.Exists(pullDir))
        {
            return MeshPullSummary.Empty;
        }

        var files = Directory.EnumerateFiles(pullDir, "*.ashpkg")
            .Where(f => !Path.GetFileName(f).StartsWith('.'))   // dotfiles + AppleDouble (._x.ashpkg)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        int admitted = 0, held = 0, rejected = 0, refused = 0, already = 0, errors = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
                var result = await PackageImport.SubmitAsync(projectDir, json).ConfigureAwait(false);
                switch (result.Outcome)
                {
                    case PackageAdmission.Admitted: admitted++; break;
                    case PackageAdmission.Held: held++; break;
                    case PackageAdmission.Rejected: rejected++; break;
                    case PackageAdmission.Refused: refused++; break;
                    case PackageAdmission.AlreadyImported: already++; break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                errors++;
            }
        }

        return new MeshPullSummary(files.Count, admitted, held, rejected, refused, already, errors);
    }
}
