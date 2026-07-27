using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Nexo.Bricks.DepExtract;

namespace Nexo.Bricks.DepExtract.Gui;

public sealed class InstallJob
{
    public required string Id { get; init; }
    public string Status { get; set; } = "queued"; // queued|running|ok|error
    public List<string> Log { get; } = new();
    public InstallResult? Result { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}

public sealed record InstallResult(
    bool Ok,
    string? Error,
    string AdaptedReaderPath,
    string? ImageTag,
    string? BuildLog,
    string ParserUiUrl,
    string Summary,
    string[] CompanionPaths,
    string? RecreateLog,
    bool SmokeRan,
    bool? SmokeOk,
    string? SmokeDetail,
    IReadOnlyList<string> ProgressLog);

/// <summary>Seam so AdaptInstallLoop's retry orchestration can be unit-tested without real Docker/Ollama.</summary>
public interface IInstallExecutor
{
    Task<InstallResult> RunSyncAsync(
        InstallWorkRequest req,
        Func<string, PlanSession?>? resolvePlan,
        CancellationToken ct);
}

/// <summary>
/// Install drafted adapter into Poco (<c>common/adapted_reader.hpp</c>), optional custom image
/// build (companion compile gate), recreate <c>evtx</c>, optional smoke.
/// </summary>
public sealed class InstallExecutor : IInstallExecutor
{
    private readonly ConcurrentDictionary<string, InstallJob> _jobs = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private int _active;

    public InstallJob? Get(string id) => _jobs.TryGetValue(id, out var j) ? j : null;

    public InstallJob Start(
        InstallWorkRequest req,
        Func<string, PlanSession?>? resolvePlan,
        CancellationToken ct)
    {
        var job = new InstallJob { Id = Guid.NewGuid().ToString("N")[..12] };
        _jobs[job.Id] = job;
        _ = Task.Run(() => RunJobAsync(job, req, resolvePlan, ct), CancellationToken.None);
        return job;
    }

    public async Task<InstallResult> RunSyncAsync(
        InstallWorkRequest req,
        Func<string, PlanSession?>? resolvePlan,
        CancellationToken ct)
    {
        var job = new InstallJob { Id = "sync-" + Guid.NewGuid().ToString("N")[..8] };
        await RunJobAsync(job, req, resolvePlan, ct).ConfigureAwait(false);
        return job.Result ?? Fail(job.Error ?? "install failed", job.Log);
    }

    /// <summary>
    /// Restore <c>common/adapted_reader.hpp</c> + companions from <c>.nexo-prev</c> snapshots,
    /// retag <paramref name="imageTag"/>-prev → <paramref name="imageTag"/>, and recreate evtx.
    /// Used when post-install oracle checks fail after a successful build/recreate.
    /// </summary>
    public static async Task RollbackPreviousAsync(
        string? pocoContext,
        string? imageTag,
        Action<string>? onLog,
        CancellationToken ct)
    {
        var poco = string.IsNullOrWhiteSpace(pocoContext)
            ? Environment.GetEnvironmentVariable("POCO_CONTEXT")
            : pocoContext;
        if (string.IsNullOrWhiteSpace(poco))
            throw new InvalidOperationException("pocoContext is required for rollback");
        poco = DepExtractPathRules.RequireExistingDirectory(poco, "pocoContext");
        DepExtractPathRules.EnsurePocoContextAllowed(poco);

        void Log(string line) => onLog?.Invoke(line);

        var dest = Path.Combine(poco, "common", "adapted_reader.hpp");
        var destPrev = dest + ".nexo-prev";
        var destTmp = dest + ".nexo-tmp";
        var companionManifest = Path.Combine(poco, "common", ".nexo-adapter-companions");
        var companionManifestPrev = companionManifest + ".nexo-prev";
        var tag = string.IsNullOrWhiteSpace(imageTag) ? "evtx:custom" : imageTag!;
        await RollbackFullAsync(
            poco, dest, destPrev, destTmp, companionManifest, companionManifestPrev,
            tag, tag + "-prev", Log, ct).ConfigureAwait(false);
    }

    /// <summary>Rewrite the greppable provenance oracle/reviewed_by lines on an installed adapter.</summary>
    public static async Task UpdateInstalledProvenanceAsync(
        string? pocoContext,
        string oracleResult,
        string reviewedBy,
        CancellationToken ct)
    {
        var poco = string.IsNullOrWhiteSpace(pocoContext)
            ? Environment.GetEnvironmentVariable("POCO_CONTEXT")
            : pocoContext;
        if (string.IsNullOrWhiteSpace(poco)) return;
        var dest = Path.Combine(poco, "common", "adapted_reader.hpp");
        if (!File.Exists(dest)) return;
        var body = await File.ReadAllTextAsync(dest, ct).ConfigureAwait(false);
        var updated = AdapterProvenance.UpdateOracle(body, oracleResult, reviewedBy);
        if (!ReferenceEquals(updated, body) && updated != body)
            await File.WriteAllTextAsync(dest, updated, ct).ConfigureAwait(false);
    }

    private async Task RunJobAsync(
        InstallJob job,
        InstallWorkRequest req,
        Func<string, PlanSession?>? resolvePlan,
        CancellationToken ct)
    {
        lock (_gate)
        {
            if (_active > 0)
            {
                job.Status = "error";
                job.Error = "Another install is already running.";
                job.FinishedAt = DateTimeOffset.UtcNow;
                job.Result = Fail(job.Error, job.Log);
                return;
            }
            _active++;
        }

        job.Status = "running";
        void Log(string line)
        {
            lock (job.Log) job.Log.Add(line);
        }

        try
        {
            var result = await ExecuteAsync(req, resolvePlan, Log, ct).ConfigureAwait(false);
            // Attach full progress log from job
            result = result with { ProgressLog = Snapshot(job.Log) };
            job.Result = result;
            job.Status = result.Ok ? "ok" : "error";
            job.Error = result.Error;
        }
        catch (Exception ex)
        {
            Log("error: " + ex.Message);
            job.Status = "error";
            job.Error = ex.Message;
            job.Result = Fail(ex.Message, job.Log);
        }
        finally
        {
            job.FinishedAt = DateTimeOffset.UtcNow;
            lock (_gate) _active = Math.Max(0, _active - 1);
        }
    }

    private static IReadOnlyList<string> Snapshot(List<string> log)
    {
        lock (log) return log.ToList();
    }

    private static InstallResult Fail(string error, List<string> log) =>
        new(false, error, "", null, null, PortSettingsStore.ParserUiUrl(), error,
            Array.Empty<string>(), null, false, null, null, Snapshot(log));

    private static async Task<InstallResult> ExecuteAsync(
        InstallWorkRequest req,
        Func<string, PlanSession?>? resolvePlan,
        Action<string> log,
        CancellationToken ct)
    {
        var poco = string.IsNullOrWhiteSpace(req.PocoContext)
            ? Environment.GetEnvironmentVariable("POCO_CONTEXT")
            : req.PocoContext;
        if (string.IsNullOrWhiteSpace(poco))
            throw new InvalidOperationException("pocoContext is required (or set POCO_CONTEXT)");
        poco = DepExtractPathRules.RequireExistingDirectory(poco, "pocoContext");
        DepExtractPathRules.EnsurePocoContextAllowed(poco);
        if (!File.Exists(Path.Combine(poco, "poco", "main.cpp")))
            throw new InvalidOperationException("pocoContext does not look like the Poco/evtx tree (missing poco/main.cpp)");

        string code;
        if (!string.IsNullOrWhiteSpace(req.AdapterCode))
        {
            code = req.AdapterCode!;
        }
        else if (!string.IsNullOrWhiteSpace(req.AdapterPath))
        {
            var path = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(req.AdapterPath!, "adapterPath");
            if (!File.Exists(path))
                throw new FileNotFoundException($"adapterPath not found: {path}");
            code = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException("adapterCode or adapterPath is required");
        }

        // Content scan (model-authored only)
        var scan = AdapterContentScan.Scan(code, req.IsModelAuthored);
        if (!scan.Ok)
        {
            var detail = string.Join("; ", scan.Hits.Select(h => $"{h.Rule} @ {h.Excerpt}"));
            throw new InvalidOperationException(
                "Adapter content scan blocked install: " + detail +
                (scan.OverrideReason is not null ? " (" + scan.OverrideReason + ")" : "") +
                ". Set NEXO_ALLOW_UNSAFE_ADAPTER=1 and NEXO_UNSAFE_ADAPTER_REASON=… to override.");
        }
        if (scan.OverrideUsed)
            log($"WARN: content-scan override used — reason={scan.OverrideReason}; hits={scan.Hits.Count}");

        var strategy = req.Strategy
            ?? (req.IsModelAuthored ? "model" : "scaffold");
        var reviewedBy = req.ReviewedBy
            ?? (string.Equals(strategy, "model", StringComparison.OrdinalIgnoreCase) ? "none" : "none");
        code = AdapterProvenance.Prepend(code, new AdapterProvenance.Info(
            strategy,
            req.ModelTag,
            req.ModelDigest,
            req.CompileGatePassed,
            req.OracleResult ?? "n/a",
            reviewedBy));

        var dest = Path.Combine(poco, "common", "adapted_reader.hpp");
        var destTmp = dest + ".nexo-tmp";
        var destPrev = dest + ".nexo-prev";
        var companionManifest = Path.Combine(poco, "common", ".nexo-adapter-companions");
        var companionManifestPrev = companionManifest + ".nexo-prev";
        var imageTag = string.IsNullOrWhiteSpace(req.ImageTag) ? "evtx:custom" : req.ImageTag!;
        var imagePrevTag = imageTag + "-prev";

        // Snapshot previous good adapter + companion manifest for rollback.
        if (File.Exists(dest))
        {
            File.Copy(dest, destPrev, overwrite: true);
            log($"snapshot previous adapter → {destPrev}");
        }
        if (File.Exists(companionManifest))
            File.Copy(companionManifest, companionManifestPrev, overwrite: true);

        if (File.Exists(companionManifest))
        {
            foreach (var previous in await File.ReadAllLinesAsync(companionManifest, ct).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(previous)) continue;
                var oldPath = Path.GetFullPath(Path.Combine(poco, "common", previous));
                var commonRoot = Path.GetFullPath(Path.Combine(poco, "common")) + Path.DirectorySeparatorChar;
                if (oldPath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
                    File.Delete(oldPath);
            }
        }

        // Write to temp; promote only after successful build (transactional).
        await File.WriteAllTextAsync(destTmp, code, ct).ConfigureAwait(false);
        log($"staged adapter → {destTmp} ({code.Length} bytes)");
        // Build context reads common/adapted_reader.hpp — swap into place for the build,
        // keeping .nexo-prev for rollback.
        File.Copy(destTmp, dest, overwrite: true);
        log($"wrote adapter → {dest} (pending build confirmation)");

        var companions = new List<string>();
        // Relative paths under common/ (structure-preserving). Also track leaf names
        // so explicit flat CompanionFiles still de-dupe against auto-copies.
        var writtenRels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var writtenLeaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (req.CompanionFiles is { Length: > 0 })
        {
            foreach (var c in req.CompanionFiles)
            {
                if (c is null || string.IsNullOrWhiteSpace(c.Name) || c.Content is null) continue;
                var rel = NormalizeCompanionRel(c.Name);
                var leaf = Path.GetFileName(rel);
                if (string.IsNullOrWhiteSpace(leaf))
                    throw new InvalidOperationException($"invalid companion file name: {c.Name}");
                var companionDest = Path.Combine(poco, "common", rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(companionDest)!);
                await File.WriteAllTextAsync(companionDest, c.Content, ct).ConfigureAwait(false);
                companions.Add(companionDest);
                writtenRels.Add(rel);
                writtenLeaves.Add(leaf);
                log($"wrote companion → {companionDest}");
            }
        }

        PlanSession? planSession = null;
        if (!string.IsNullOrWhiteSpace(req.PlanId) && resolvePlan is not null)
            planSession = resolvePlan(req.PlanId!);

        if (planSession?.DuplicatePath is { } dupRoot && Directory.Exists(dupRoot))
        {
            // Relative paths inside the extract duplicate (preserves ../ include chains).
            var autoRels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddFromHint(string hint)
            {
                var norm = hint.Replace('\\', '/').TrimStart('/');
                if (string.IsNullOrWhiteSpace(norm) || norm.Contains("..", StringComparison.Ordinal)) return;
                var direct = Path.Combine(dupRoot, norm.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(direct))
                {
                    autoRels.Add(norm);
                    return;
                }
                var leaf = Path.GetFileName(norm);
                if (!IsCompanionLeaf(leaf)) return;
                var found = FindUnder(dupRoot, leaf);
                if (found is null) return;
                autoRels.Add(RelUnder(dupRoot, found));
            }

            foreach (var e in planSession.Entries ?? Array.Empty<string>())
                AddFromHint(e);
            foreach (var l in planSession.Lower ?? Array.Empty<string>())
                AddFromHint(l);
            foreach (Match m in Regex.Matches(code, "#\\s*include\\s*\"([^\"]+)\""))
                AddFromHint(m.Groups[1].Value);

            foreach (var hintRel in autoRels.ToArray())
            {
                var rel = hintRel;
                if (writtenRels.Contains(rel)) continue;
                var leaf = Path.GetFileName(rel);
                if (!IsCompanionLeaf(leaf)) continue;
                var src = Path.Combine(dupRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(src))
                {
                    src = FindUnder(dupRoot, leaf);
                    if (src is null) continue;
                    rel = RelUnder(dupRoot, src);
                    if (writtenRels.Contains(rel)) continue;
                }
                var companionDest = Path.Combine(poco, "common", rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(companionDest)!);
                File.Copy(src, companionDest, overwrite: true);
                companions.Add(companionDest);
                writtenRels.Add(rel);
                writtenLeaves.Add(Path.GetFileName(rel));
                log($"auto-copied companion {src} → {companionDest}");
            }

            // Out-of-line definitions: each header companion's same-basename source
            // sibling (Foo.hpp -> Foo.cpp, put in the duplicate by dep-extract) must
            // ship too, or the image build fails at link with undefined references.
            // Prefer the sibling next to the header (same directory) so nested trees stay intact.
            string[] siblingExts = [".cpp", ".cc", ".cxx", ".c"];
            foreach (var headerRel in writtenRels.ToArray())
            {
                var ext = Path.GetExtension(headerRel);
                if (ext is not (".hpp" or ".h" or ".hh" or ".hxx")) continue;
                var dir = Path.GetDirectoryName(headerRel.Replace('/', Path.DirectorySeparatorChar)) ?? "";
                var stem = Path.GetFileNameWithoutExtension(headerRel);
                foreach (var se in siblingExts)
                {
                    var siblingRel = string.IsNullOrEmpty(dir)
                        ? stem + se
                        : (dir.Replace('\\', '/') + "/" + stem + se);
                    if (writtenRels.Contains(siblingRel)) continue;
                    var src = Path.Combine(dupRoot, siblingRel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(src))
                    {
                        src = FindUnder(dupRoot, stem + se);
                        if (src is null) continue;
                        siblingRel = RelUnder(dupRoot, src);
                        if (writtenRels.Contains(siblingRel)) continue;
                    }
                    var siblingDest = Path.Combine(poco, "common", siblingRel.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(siblingDest)!);
                    File.Copy(src, siblingDest, overwrite: true);
                    companions.Add(siblingDest);
                    writtenRels.Add(siblingRel);
                    writtenLeaves.Add(Path.GetFileName(siblingRel));
                    log($"auto-copied source sibling {src} → {siblingDest}");
                }
            }
        }

        await File.WriteAllLinesAsync(
            companionManifest,
            companions.Select(p => Path.GetRelativePath(Path.Combine(poco, "common"), p)),
            ct).ConfigureAwait(false);

        string? buildLog = null;
        string? recreateLog = null;
        if (req.RebuildImage)
        {
            // Preserve prior image as evtx:custom-prev (best-effort) before replacing the tag.
            await DockerRetagAsync(imageTag, imagePrevTag, log, ct).ConfigureAwait(false);

            log($"compile gate: docker build --network=none -f Dockerfile.custom -t {imageTag}");
            var (ok, blog) = await DockerBuildAsync(poco, imageTag, ct).ConfigureAwait(false);
            buildLog = blog;
            foreach (var line in blog.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(30))
                log(line);
            if (!ok)
            {
                await RollbackAdapterFilesAsync(dest, destPrev, destTmp, companionManifest, companionManifestPrev, log, ct)
                    .ConfigureAwait(false);
                return new InstallResult(false, "docker build failed (companion compile gate)", dest, imageTag, buildLog,
                    PortSettingsStore.ParserUiUrl(), "Custom image build failed; adapter rolled back.", companions.ToArray(), null,
                    false, null, null, Array.Empty<string>());
            }
            // Build succeeded — commit temp staging.
            try { File.Delete(destTmp); } catch { /* ignore */ }
            PortSettingsStore.UpsertEnvVar("EVTX_IMAGE", imageTag);
            log($"EVTX_IMAGE={imageTag}");
        }
        else
        {
            try { File.Delete(destTmp); } catch { /* ignore */ }
        }

        var recreate = req.RecreateEvtx ?? req.RebuildImage;
        if (recreate && req.RebuildImage)
        {
            log("recreate: evtx");
            try
            {
                var result = await ComposePipeline.RecreateAsync(
                    new[] { "evtx" },
                    withExtractAgent: false,
                    ct,
                    dryRun: false,
                    waitHealthy: req.WaitHealthy is not false,
                    previousAgentPort: null,
                    onLog: log).ConfigureAwait(false);
                recreateLog = string.Join("\n", new[] { result.Log, result.HealthLog }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!result.Ok || (req.WaitHealthy is not false && result.HealthLog is not null
                        && (result.HealthLog.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                            || result.HealthLog.Contains("unhealthy", StringComparison.OrdinalIgnoreCase))))
                {
                    log("evtx recreate/health failed — rolling back to previous image + adapter");
                    await RollbackFullAsync(
                        poco, dest, destPrev, destTmp, companionManifest, companionManifestPrev,
                        imageTag, imagePrevTag, log, ct).ConfigureAwait(false);
                    return new InstallResult(false, "evtx recreate/health failed; rolled back", dest, imageTag, buildLog,
                        PortSettingsStore.ParserUiUrl(), "Rolled back after unhealthy recreate.", companions.ToArray(), recreateLog,
                        false, null, null, Array.Empty<string>());
                }
                log("evtx recreate ok");
            }
            catch (Exception ex)
            {
                recreateLog = ex.Message;
                log("evtx recreate error — rolling back: " + ex.Message);
                await RollbackFullAsync(
                    poco, dest, destPrev, destTmp, companionManifest, companionManifestPrev,
                    imageTag, imagePrevTag, log, ct).ConfigureAwait(false);
                return new InstallResult(false, "evtx recreate failed; rolled back: " + ex.Message, dest, imageTag, buildLog,
                    PortSettingsStore.ParserUiUrl(), "Rolled back after recreate error.", companions.ToArray(), recreateLog,
                    false, null, null, Array.Empty<string>());
            }
        }

        bool smokeRan = false;
        bool? smokeOk = null;
        string? smokeDetail = null;
        if (req.Smoke == true || !string.IsNullOrWhiteSpace(req.SmokeExtractFile))
        {
            smokeRan = true;
            log(string.IsNullOrWhiteSpace(req.SmokeExtractFile)
                ? "smoke: GET /health"
                : $"smoke: health + extract {req.SmokeExtractFile}");
            var (ok, detail) = await SmokeEvtxAsync(req.SmokeExtractFile, ct).ConfigureAwait(false);
            smokeOk = ok;
            smokeDetail = detail;
            log(ok ? "smoke ok: " + detail : "smoke failed: " + detail);
            if (!ok)
            {
                if (req.RebuildImage)
                {
                    log("smoke failed — rolling back adapter + image");
                    await RollbackFullAsync(
                        poco, dest, destPrev, destTmp, companionManifest, companionManifestPrev,
                        imageTag, imagePrevTag, log, ct).ConfigureAwait(false);
                }
                return new InstallResult(false, "Install completed but smoke failed: " + detail, dest, imageTag, buildLog,
                    PortSettingsStore.ParserUiUrl(), "Smoke failed; rolled back when possible.", companions.ToArray(), recreateLog,
                    true, false, detail, Array.Empty<string>());
            }
        }

        var parserUiUrl = PortSettingsStore.ParserUiUrl();
        var summary = companions.Count == 0
            ? "Adapter installed. Open the Poco parser GUI to run extractions."
            : $"Adapter installed with {companions.Count} companion source(s). Open the Poco parser GUI to run extractions.";
        string? resultImage = req.RebuildImage ? imageTag : null;
        if (!string.IsNullOrWhiteSpace(resultImage))
            summary += $" EVTX_IMAGE={resultImage}.";

        return new InstallResult(true, null, dest, resultImage, buildLog, parserUiUrl, summary,
            companions.ToArray(), recreateLog, smokeRan, smokeOk, smokeDetail, Array.Empty<string>());
    }

    private static async Task DockerRetagAsync(string fromTag, string toTag, Action<string> log, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("image");
            psi.ArgumentList.Add("inspect");
            psi.ArgumentList.Add(fromTag);
            using var inspect = Process.Start(psi);
            if (inspect is null) return;
            await inspect.WaitForExitAsync(ct).ConfigureAwait(false);
            if (inspect.ExitCode != 0) return;

            var tagPsi = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            tagPsi.ArgumentList.Add("tag");
            tagPsi.ArgumentList.Add(fromTag);
            tagPsi.ArgumentList.Add(toTag);
            using var tag = Process.Start(tagPsi);
            if (tag is null) return;
            await tag.WaitForExitAsync(ct).ConfigureAwait(false);
            if (tag.ExitCode == 0)
                log($"retagged {fromTag} → {toTag}");
        }
        catch (Exception ex)
        {
            log("warn: could not snapshot previous image: " + ex.Message);
        }
    }

    private static async Task RollbackAdapterFilesAsync(
        string dest, string destPrev, string destTmp,
        string companionManifest, string companionManifestPrev,
        Action<string> log, CancellationToken ct)
    {
        try
        {
            if (File.Exists(destTmp)) File.Delete(destTmp);
            if (File.Exists(destPrev))
            {
                File.Copy(destPrev, dest, overwrite: true);
                log($"rollback adapter ← {destPrev}");
            }
            if (File.Exists(companionManifestPrev))
            {
                File.Copy(companionManifestPrev, companionManifest, overwrite: true);
                log("rollback companion manifest");
            }
        }
        catch (Exception ex)
        {
            log("warn: adapter file rollback failed: " + ex.Message);
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task RollbackFullAsync(
        string poco,
        string dest, string destPrev, string destTmp,
        string companionManifest, string companionManifestPrev,
        string imageTag, string imagePrevTag,
        Action<string> log, CancellationToken ct)
    {
        await RollbackAdapterFilesAsync(dest, destPrev, destTmp, companionManifest, companionManifestPrev, log, ct)
            .ConfigureAwait(false);
        await DockerRetagAsync(imagePrevTag, imageTag, log, ct).ConfigureAwait(false);
        PortSettingsStore.UpsertEnvVar("EVTX_IMAGE", imageTag);
        try
        {
            var result = await ComposePipeline.RecreateAsync(
                new[] { "evtx" },
                withExtractAgent: false,
                ct,
                dryRun: false,
                waitHealthy: true,
                previousAgentPort: null,
                onLog: log).ConfigureAwait(false);
            log(result.Ok ? "rollback recreate ok" : "rollback recreate failed: " + result.Log);
        }
        catch (Exception ex)
        {
            log("warn: rollback recreate failed: " + ex.Message);
        }
    }

    private static async Task<(bool Ok, string Detail)> SmokeEvtxAsync(string? extractFile, CancellationToken ct)
    {
        var ports = PortSettingsStore.Load();
        var baseUrl = PortSettingsStore.ParserReachableBaseUrl(ports);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var health = await http.GetAsync($"{baseUrl}/health", ct).ConfigureAwait(false);
            if (!health.IsSuccessStatusCode)
                return (false, $"health HTTP {(int)health.StatusCode} via {baseUrl}");

            if (string.IsNullOrWhiteSpace(extractFile))
                return (true, $"health ok via {baseUrl} (host :{ports.ParserHostPort})");

            var enc = Uri.EscapeDataString(extractFile.Trim());
            var insp = await http.GetAsync($"{baseUrl}/inspect?file={enc}", ct).ConfigureAwait(false);
            var inspBody = await insp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!insp.IsSuccessStatusCode)
                return (false, $"inspect HTTP {(int)insp.StatusCode}: {Trim(inspBody, 400)}");

            using var extractReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/extract?file={enc}&max_events=25");
            extractReq.Content = new ByteArrayContent(Array.Empty<byte>());
            var extract = await http.SendAsync(extractReq, ct).ConfigureAwait(false);
            var extractBody = await extract.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!extract.IsSuccessStatusCode)
                return (false, $"extract HTTP {(int)extract.StatusCode}: {Trim(extractBody, 400)}");

            var jobId = TryReadJsonString(extractBody, "id") ?? TryReadJsonString(extractBody, "jobId");
            if (string.IsNullOrWhiteSpace(jobId))
                return (false, "extract response missing job id: " + Trim(extractBody, 300));

            var deadline = DateTime.UtcNow.AddSeconds(90);
            string? state = null;
            string statusBody = "";
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                var st = await http.GetAsync($"{baseUrl}/status?id={Uri.EscapeDataString(jobId)}", ct).ConfigureAwait(false);
                statusBody = await st.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                state = TryReadJsonString(statusBody, "status") ?? TryReadJsonString(statusBody, "state");
                if (string.Equals(state, "done", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "ok", StringComparison.OrdinalIgnoreCase))
                    break;
                if (string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
                    return (false, $"extract job {jobId} failed: {Trim(statusBody, 500)}");
                await Task.Delay(250, ct).ConfigureAwait(false);
            }

            if (!string.Equals(state, "done", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(state, "ok", StringComparison.OrdinalIgnoreCase))
                return (false, $"extract job {jobId} timeout (last={state}): {Trim(statusBody, 300)}");

            var result = await http.GetAsync($"{baseUrl}/result?id={Uri.EscapeDataString(jobId)}", ct).ConfigureAwait(false);
            var csv = await result.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccessStatusCode)
                return (false, $"result HTTP {(int)result.StatusCode}: {Trim(csv, 300)}");
            var dataRows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Skip(1).Count();
            if (dataRows <= 0)
                return (false, $"extract produced no data rows for {extractFile} (job={jobId})");

            return (true,
                $"health+extract ok via {baseUrl} (host :{ports.ParserHostPort}) " +
                $"file={extractFile} rows={dataRows} job={jobId}");
        }
        catch (Exception ex)
        {
            return (false, $"{ex.Message} (via {baseUrl})");
        }
    }

    private static string Trim(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private static string? TryReadJsonString(string json, string prop)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(prop, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.String)
                return el.GetString();
            // nested { "job": { "id": ... } } — ignore for now
        }
        catch { /* ignore */ }
        return null;
    }

    private static async Task<(bool Ok, string Log)> DockerBuildAsync(string poco, string imageTag, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = poco,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        // BuildKit honors --network=none so the image build cannot pull/exfil during compile.
        psi.Environment["DOCKER_BUILDKIT"] = "1";
        psi.ArgumentList.Add("build");
        DockerHardening.AddBuildIsolationArgs(psi);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("Dockerfile.custom");
        psi.ArgumentList.Add("-t");
        psi.ArgumentList.Add(imageTag);
        psi.ArgumentList.Add(".");
        try
        {
            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException("failed to start docker build");
            var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = p.StandardError.ReadToEndAsync(ct);
            var exit = await DockerHardening.WaitForExitWithTimeoutAsync(
                p, DockerHardening.BuildTimeoutSeconds, ct).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return (exit == 0, (stdout + "\n" + stderr).Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static bool IsCompanionLeaf(string leaf)
    {
        var ext = Path.GetExtension(leaf);
        return ext is ".hpp" or ".h" or ".hh" or ".hxx" or ".cpp" or ".cc" or ".cxx" or ".c";
    }

    private static string NormalizeCompanionRel(string name)
    {
        var rel = name.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(rel) || rel.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException($"invalid companion file name: {name}");
        return rel;
    }

    private static string RelUnder(string root, string fullPath) =>
        Path.GetRelativePath(root, fullPath).Replace('\\', '/');

    private static string? FindUnder(string root, string leaf)
    {
        try
        {
            return Directory.EnumerateFiles(root, leaf, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Internal work request (maps from API InstallRequest).</summary>
public sealed record InstallWorkRequest(
    string? AdapterCode,
    string? AdapterPath,
    string? PocoContext,
    bool RebuildImage,
    string? ImageTag,
    CompanionFileDto[]? CompanionFiles,
    string? PlanId,
    bool? RecreateEvtx,
    bool? Smoke,
    bool? WaitHealthy,
    string? SmokeExtractFile = null,
    string? Strategy = null,
    string? ModelTag = null,
    string? ModelDigest = null,
    bool? CompileGatePassed = null,
    string? OracleResult = null,
    string? ReviewedBy = null,
    bool IsModelAuthored = false);

public sealed record CompanionFileDto(string Name, string Content);
