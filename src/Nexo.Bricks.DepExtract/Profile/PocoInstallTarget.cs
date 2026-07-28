using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>Options for <see cref="PocoInstallTarget"/>.</summary>
public sealed class PocoInstallTargetOptions
{
    /// <summary>Poco/evtx root (or POCO_CONTEXT).</summary>
    public string? PocoContext { get; init; }

    /// <summary>Image tag to build/retag (default evtx:custom).</summary>
    public string ImageTag { get; init; } = "evtx:custom";

    /// <summary>When true, docker build Dockerfile.custom after file apply.</summary>
    public bool RebuildImage { get; init; } = true;

    /// <summary>When true (and RebuildImage), recreate the evtx service.</summary>
    public bool RecreateEvtx { get; init; } = true;

    /// <summary>Wait for healthy after recreate.</summary>
    public bool WaitHealthy { get; init; } = true;

    /// <summary>Optional extract file for smoke; null skips extract (health-only when Smoke=true).</summary>
    public string? SmokeExtractFile { get; init; }

    /// <summary>Run smoke after recreate.</summary>
    public bool Smoke { get; init; }

    /// <summary>Service name to recreate (must be in <see cref="AllowedServices"/>).</summary>
    public string RecreateService { get; init; } = "evtx";

    /// <summary>Single-flight key (default poco-install).</summary>
    public string FlightKey { get; init; } = "poco-install";

    /// <summary>Treat artifact as model-authored for content scan.</summary>
    public bool IsModelAuthored { get; init; }

    /// <summary>Provenance strategy label.</summary>
    public string Strategy { get; init; } = "scaffold";

    /// <summary>Optional model tag for provenance header.</summary>
    public string? ModelTag { get; init; }

    /// <summary>Optional model digest for provenance header.</summary>
    public string? ModelDigest { get; init; }

    /// <summary>Compile-gate result for provenance header.</summary>
    public bool? CompileGatePassed { get; init; }

    /// <summary>Oracle result for provenance header.</summary>
    public string? OracleResult { get; init; }

    /// <summary>Reviewed-by for provenance header.</summary>
    public string? ReviewedBy { get; init; }
}

/// <summary>
/// Profile <see cref="IDeploymentTarget"/>: ports InstallExecutor onto
/// <see cref="IManagedFileSet"/> + <see cref="ISingleFlightGuard"/> + injectable
/// <see cref="IPocoDeploymentOps"/>. Failed build/recreate rolls back the
/// managed file set and prior image tag. The post-smoke ship/rollback verdict is
/// delegated to the profile's <see cref="IAcceptanceEvaluator"/>.
/// </summary>
public sealed class PocoInstallTarget : IAcceptanceGatedDeploymentTarget
{
    /// <summary>Services the agent may recreate (parity with ComposePipeline).</summary>
    public static readonly HashSet<string> AllowedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "evtx",
        "dep-extract",
        "dep-extract-agent",
        "ollama",
        "ollama-pull",
    };

    private readonly IManagedFileSet _files;
    private readonly ISingleFlightGuard _flight;
    private readonly IPocoDeploymentOps _ops;
    private readonly PocoInstallTargetOptions _options;

    /// <summary>Creates the Poco/evtx deployment target.</summary>
    public PocoInstallTarget(
        IManagedFileSet files,
        ISingleFlightGuard flight,
        IPocoDeploymentOps ops,
        PocoInstallTargetOptions options)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _flight = flight ?? throw new ArgumentNullException(nameof(flight));
        _ops = ops ?? throw new ArgumentNullException(nameof(ops));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<DeploymentApplyResult> ApplyAsync(
        GeneratedArtifact artifact,
        CancellationToken cancellationToken = default) =>
        ApplyAsync(artifact, DefaultAcceptanceEvaluator.Instance, cancellationToken);

    /// <inheritdoc />
    public Task<DeploymentApplyResult> ApplyAsync(
        GeneratedArtifact artifact,
        IAcceptanceEvaluator acceptance,
        CancellationToken cancellationToken = default)
    {
        if (artifact is null) throw new ArgumentNullException(nameof(artifact));
        if (acceptance is null) throw new ArgumentNullException(nameof(acceptance));

        return _flight.RunExclusiveAsync(
            _options.FlightKey,
            ct => ApplyCoreAsync(artifact, acceptance, ct),
            cancellationToken);
    }

    private async Task<DeploymentApplyResult> ApplyCoreAsync(
        GeneratedArtifact artifact,
        IAcceptanceEvaluator acceptance,
        CancellationToken ct)
    {
        _ops.EnsureStackIdentity();

        if (_options.RecreateEvtx && _options.RebuildImage)
        {
            try { EnsureAllowedService(_options.RecreateService); }
            catch (InvalidOperationException ex)
            {
                return Fail(ex.Message);
            }
        }

        var poco = ResolvePocoContext();
        var commonRoot = Path.Combine(poco, "common");
        Directory.CreateDirectory(commonRoot);

        var code = artifact.Content ?? "";
        var scan = AdapterContentScan.Scan(code, _options.IsModelAuthored);
        if (!scan.Ok)
        {
            var detail = string.Join("; ", scan.Hits.Select(h => $"{h.Rule} @ {h.Excerpt}"));
            return Fail("Adapter content scan blocked install: " + detail);
        }

        var strategy = string.IsNullOrWhiteSpace(_options.Strategy) ? "scaffold" : _options.Strategy;
        var reviewedBy = _options.ReviewedBy
            ?? (string.Equals(strategy, "model", StringComparison.OrdinalIgnoreCase) ? "none" : "none");
        code = AdapterProvenance.Prepend(code, new AdapterProvenance.Info(
            strategy,
            _options.ModelTag,
            _options.ModelDigest,
            _options.CompileGatePassed,
            _options.OracleResult ?? "n/a",
            reviewedBy));

        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["adapted_reader.hpp"] = code
        };

        var companionRels = new List<string>();
        foreach (var (relRaw, content) in artifact.Files ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(relRaw)) continue;
            var rel = relRaw.Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(rel) || rel.Contains("..", StringComparison.Ordinal))
                continue;
            if (rel.Equals("adapted_reader.hpp", StringComparison.OrdinalIgnoreCase))
                continue;
            files[rel] = content ?? "";
            companionRels.Add(rel);
        }

        files[".nexo-adapter-companions"] = string.Join(
            Environment.NewLine,
            companionRels.Select(r => r.Replace('\\', '/')));

        // Snapshot + remove prior companions that are not in the new set so
        // rollback can restore them (ManagedFileSet only tracks written paths).
        var removedCompanions = await SnapshotAndRemoveStaleCompanionsAsync(
            commonRoot, companionRels, ct).ConfigureAwait(false);

        ManagedFileApplyResult apply;
        try
        {
            apply = await _files.ApplyAsync(commonRoot, files, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await RestoreRemovedCompanionsAsync(removedCompanions, ct).ConfigureAwait(false);
            return Fail("managed file apply failed: " + ex.Message);
        }

        var imageTag = string.IsNullOrWhiteSpace(_options.ImageTag) ? "evtx:custom" : _options.ImageTag;
        var imagePrev = imageTag + "-prev";
        var buildCommitted = false;

        async Task RollbackFilesAsync(CancellationToken rct)
        {
            await apply.Rollback(rct).ConfigureAwait(false);
            await RestoreRemovedCompanionsAsync(removedCompanions, rct).ConfigureAwait(false);
        }

        // Full rollback (files + prior image tag + recreate) — only after a
        // successful build replaced the live tag (parity with InstallExecutor).
        async Task RollbackFullAsync(CancellationToken rct)
        {
            await RollbackFilesAsync(rct).ConfigureAwait(false);
            if (!buildCommitted) return;
            await _ops.RetagImageAsync(imagePrev, imageTag, rct).ConfigureAwait(false);
            _ops.UpsertEnvVar("EVTX_IMAGE", imageTag);
            try
            {
                await _ops.RecreateServiceAsync(
                    _options.RecreateService,
                    waitHealthy: true,
                    rct).ConfigureAwait(false);
            }
            catch
            {
                // best-effort
            }
        }

        if (_options.RebuildImage)
        {
            await _ops.RetagImageAsync(imageTag, imagePrev, ct).ConfigureAwait(false);

            var (buildOk, buildLog) = await _ops.BuildCustomImageAsync(poco, imageTag, ct)
                .ConfigureAwait(false);
            if (!buildOk)
            {
                await RollbackFilesAsync(ct).ConfigureAwait(false);
                return Fail("docker build failed (companion compile gate): " + Trim(buildLog, 400), imageTag);
            }

            buildCommitted = true;
            _ops.UpsertEnvVar("EVTX_IMAGE", imageTag);
        }

        if (_options.RecreateEvtx && _options.RebuildImage)
        {
            EnsureAllowedService(_options.RecreateService);
            try
            {
                var (ok, log) = await _ops.RecreateServiceAsync(
                    _options.RecreateService,
                    _options.WaitHealthy,
                    ct).ConfigureAwait(false);
                if (!ok || LooksUnhealthy(log))
                {
                    await RollbackFullAsync(ct).ConfigureAwait(false);
                    return Fail("evtx recreate/health failed; rolled back: " + Trim(log, 400), imageTag);
                }
            }
            catch (Exception ex)
            {
                await RollbackFullAsync(ct).ConfigureAwait(false);
                return Fail("evtx recreate failed; rolled back: " + ex.Message, imageTag);
            }
        }

        // Capture smoke output; the verdict on it is not ours to make.
        var smoke = DeploymentSmokeResult.NotRun;
        if (_options.Smoke || !string.IsNullOrWhiteSpace(_options.SmokeExtractFile))
        {
            var (smokeOk, detail, outputs) = await _ops
                .SmokeWithOutputsAsync(_options.SmokeExtractFile, ct)
                .ConfigureAwait(false);
            smoke = smokeOk
                ? DeploymentSmokeResult.Pass(detail, outputs)
                : DeploymentSmokeResult.Fail(detail, outputs);
        }

        // Acceptance runs AFTER smoke and BEFORE the deployment is committed.
        var verdict = acceptance.Evaluate(new AcceptanceContext(artifact, ProvenanceOf(artifact), smoke));
        if (verdict.Decision == AcceptanceDecision.Rollback)
        {
            if (_options.RebuildImage)
                await RollbackFullAsync(ct).ConfigureAwait(false);
            else
                await RollbackFilesAsync(ct).ConfigureAwait(false);

            var signals = verdict.Signals.Count == 0
                ? ""
                : " [" + string.Join(", ", verdict.Signals) + "]";
            return Fail("acceptance rejected the deployment: " + verdict.Reason + signals, imageTag);
        }

        var dest = Path.Combine(commonRoot, "adapted_reader.hpp");
        return new DeploymentApplyResult
        {
            Succeeded = true,
            Message = companionRels.Count == 0
                ? "Adapter installed."
                : $"Adapter installed with {companionRels.Count} companion source(s).",
            Destination = dest
        };
    }

    // The engine attaches provenance before deploying. Direct callers (host loops,
    // tests) may hand over a bare artifact; treat that as a verified scaffold so the
    // acceptance verdict rests on the smoke evidence rather than a missing header.
    private static GenerativeProvenance ProvenanceOf(GeneratedArtifact artifact) =>
        artifact.Provenance
        ?? GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true);

    private string ResolvePocoContext()
    {
        var poco = string.IsNullOrWhiteSpace(_options.PocoContext)
            ? Environment.GetEnvironmentVariable("POCO_CONTEXT")
            : _options.PocoContext;
        if (string.IsNullOrWhiteSpace(poco))
            throw new InvalidOperationException("pocoContext is required (or set POCO_CONTEXT)");
        poco = DepExtractPathRules.RequireExistingDirectory(poco, "pocoContext");
        DepExtractPathRules.EnsurePocoContextAllowed(poco);
        if (!File.Exists(Path.Combine(poco, "poco", "main.cpp")))
            throw new InvalidOperationException(
                "pocoContext does not look like the Poco/evtx tree (missing poco/main.cpp)");
        return Path.GetFullPath(poco);
    }

    private static void EnsureAllowedService(string service)
    {
        if (!AllowedServices.Contains(service))
        {
            throw new InvalidOperationException(
                $"service '{service}' is not allowed. Allowed: "
                + string.Join(", ", AllowedServices.OrderBy(x => x)));
        }
    }

    private static bool LooksUnhealthy(string? log) =>
        !string.IsNullOrWhiteSpace(log)
        && (log.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || log.Contains("unhealthy", StringComparison.OrdinalIgnoreCase));

    private static DeploymentApplyResult Fail(string message, string? destination = null) =>
        new()
        {
            Succeeded = false,
            Message = message,
            Destination = destination
        };

    private static string Trim(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "…");

    private static async Task<Dictionary<string, string>> SnapshotAndRemoveStaleCompanionsAsync(
        string commonRoot,
        IReadOnlyList<string> newRels,
        CancellationToken ct)
    {
        var removed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var manifest = Path.Combine(commonRoot, ".nexo-adapter-companions");
        if (!File.Exists(manifest))
            return removed;

        var keep = new HashSet<string>(
            newRels.Select(r => r.Replace('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        foreach (var previous in await File.ReadAllLinesAsync(manifest, ct).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(previous)) continue;
            var rel = previous.Replace('\\', '/').Trim();
            if (keep.Contains(rel)) continue;
            var oldPath = Path.GetFullPath(Path.Combine(commonRoot, rel.Replace('/', Path.DirectorySeparatorChar)));
            var rootPrefix = Path.GetFullPath(commonRoot).TrimEnd(Path.DirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;
            if (!oldPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!File.Exists(oldPath)) continue;
            removed[oldPath] = await File.ReadAllTextAsync(oldPath, ct).ConfigureAwait(false);
            File.Delete(oldPath);
        }

        return removed;
    }

    private static async Task RestoreRemovedCompanionsAsync(
        IReadOnlyDictionary<string, string> removed,
        CancellationToken ct)
    {
        foreach (var (path, content) in removed)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
