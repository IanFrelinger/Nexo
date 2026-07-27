using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// One-call, fully local onboarding of a foreign C++ parser:
///   outline (optional) → entry inference → extraction (+siblings) →
///   Qt triage via stub-compile probe → draft/gate/install →
///   golden-file oracle against operator-supplied sample files.
///
/// The oracle substitutes for human review only when <see cref="OracleEvaluator"/>
/// passes every check (min events, multi-sample agreement, field sanity). Without
/// samples, model drafts stop for human review. Failed post-install oracle checks
/// trigger <see cref="InstallExecutor.RollbackPreviousAsync"/>. Everything runs
/// against the local model and local Docker — the air-gap is never crossed.
/// </summary>
public sealed class OnboardLoop
{
    private readonly CppDependencyExtractorBrick _extractor;
    private readonly CppProjectOutlineBrick _outliner;
    private readonly AdaptInstallLoop _adaptLoop;
    private readonly ILogger<OnboardLoop> _log;

    public OnboardLoop(
        CppDependencyExtractorBrick extractor,
        CppProjectOutlineBrick outliner,
        AdaptInstallLoop adaptLoop,
        ILogger<OnboardLoop> log)
    {
        _extractor = extractor;
        _outliner = outliner;
        _adaptLoop = adaptLoop;
        _log = log;
    }

    public sealed record Request(
        string SrcDir,
        string[]? Entries,
        string[]? Samples,          // workspace paths to native-format sample files (golden oracle)
        string? Model,
        int OutlineFiles = 0,       // 0 = skip the outline pass
        int MinEvents = 0, // 0 → OracleEvaluator.DefaultMinEvents (>= 5)
        int MaxBuildRetries = 2,
        int? MaxCompileRetries = 3,
        bool PreferScaffold = true);

    public sealed record Result(
        bool Ok,
        string Stage,               // where the flow ended: outline|infer|extract|triage|adapt|oracle|done
        string? PlanId,
        string[]? Entries,
        string[]? EntryAlternatives,
        string? OutlinePath,
        string Triage,              // no-qt | qt-peripheral | qt-load-bearing | moc-blocked
        string? TriageDetail,
        string[]? QtShims,
        bool OracleMode,
        int? OracleRows,
        string? ImageTag,
        string? Summary,
        string? Error,
        IReadOnlyList<AdaptInstallLoop.Attempt>? Attempts);

    public async Task<Result> RunAsync(
        Request req,
        Func<string, PlanSession?> resolvePlan,
        Action<PlanSession> savePlan,
        CancellationToken ct)
    {
        var srcDir = DepExtractPathRules.RequireExistingDirectory(req.SrcDir, "srcDir");
        DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(srcDir, "srcDir");
        var model = string.IsNullOrWhiteSpace(req.Model) ? null : req.Model;

        // ---- 1. Outline (optional, informational) ----------------------------
        string? outlinePath = null;
        if (req.OutlineFiles > 0)
        {
            try
            {
                var ov = new Dictionary<string, object>
                {
                    ["srcDir"] = srcDir,
                    ["maxLlmFiles"] = req.OutlineFiles,
                };
                if (model is not null) ov["model"] = model;
                var oOut = await _outliner.ExecuteAsync(
                    new BrickInput(ov), ImplementationType.Agentic, new GuiExecutionContext(), ct).ConfigureAwait(false);
                outlinePath = oOut.ToDictionary().TryGetValue("outlinePath", out var op) ? op as string : null;
            }
            catch (Exception ex)
            {
                _log.LogWarning("Onboard outline pass failed (continuing): {Message}", ex.Message);
            }
        }

        // ---- 2. Entry inference ----------------------------------------------
        string[] entries;
        string[] alternatives = [];
        if (req.Entries is { Length: > 0 })
        {
            entries = req.Entries;
        }
        else
        {
            var candidates = InferEntries(srcDir);
            if (candidates.Count == 0)
            {
                return Fail("infer", outlinePath,
                    "No parser entry candidate found: no header declares a pull-style API " +
                    "(pull/advance/next/nextEvent/readNext/read). Pass entries explicitly.");
            }
            entries = [candidates[0]];
            alternatives = candidates.Skip(1).Take(5).ToArray();
            _log.LogInformation("Onboard inferred entry {Entry} ({Alt} alternative(s))", entries[0], alternatives.Length);
        }

        // ---- 3. Extraction -----------------------------------------------------
        var ws = DepExtractPathRules.TryGetWorkspaceRoot();
        var tmpBase = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(tmpBase))
            tmpBase = ws is not null ? Path.Combine(ws, ".dep-extract-out") : Path.GetTempPath();
        var outDir = Path.Combine(tmpBase, "onboard-" + Guid.NewGuid().ToString("n")[..12]);
        Directory.CreateDirectory(outDir);

        var extractValues = new Dictionary<string, object>
        {
            ["srcDir"] = srcDir,
            ["outDir"] = outDir,
            ["entries"] = entries,
        };
        BrickOutput extractOut;
        try
        {
            extractOut = await _extractor.ExecuteAsync(
                new BrickInput(extractValues), ImplementationType.Deterministic, new GuiExecutionContext(), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Fail("extract", outlinePath, $"extraction failed: {ex.Message}", entries, alternatives);
        }

        var lower = extractOut.Get<string[]>("lower") ?? [];
        var upper = extractOut.Get<string[]>("upper") ?? [];
        var external = extractOut.Get<string[]>("external") ?? [];
        var manifestText = extractOut.Get<string>("manifestText") ?? "";
        var duplicatePath = extractOut.ToDictionary().TryGetValue("duplicatePath", out var dp) ? dp as string : null;
        if (duplicatePath is null || !Directory.Exists(duplicatePath))
            return Fail("extract", outlinePath, "extraction produced no duplicate directory", entries, alternatives);

        // ---- 4. Qt triage via stub-compile probe -------------------------------
        var entryLeaf = Path.GetFileName(entries[0].Replace('\\', '/'));
        var entryText = SafeRead(Path.Combine(srcDir, entries[0]));
        var shims = QtHeaderShims.EnsureForDirectory(entryText ?? "", duplicatePath, logger: _log);
        var poco = Environment.GetEnvironmentVariable("POCO_CONTEXT");

        var triage = shims.Count > 0 ? "qt-peripheral" : "no-qt";
        string? triageDetail = null;
        if (!string.IsNullOrWhiteSpace(poco))
        {
            var probe = BuildProbeAdapter(entryLeaf);
            var probeResult = await AdapterCompileGate.TryCompileAsync(
                probe, poco, duplicatePath, requireSuccess: true, logger: _log, cancellationToken: ct).ConfigureAwait(false);
            if (probeResult.Ok is not true)
            {
                var log = probeResult.Log ?? "";
                triageDetail = string.Join("\n", log.Split('\n').Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase)).Take(5));
                if (Regex.IsMatch(log, @"Q_OBJECT|QObject|\bmoc\b", RegexOptions.IgnoreCase))
                    triage = "moc-blocked";
                else if (Regex.IsMatch(log, @"\bQ[A-Z]\w+"))
                    triage = "qt-load-bearing";
                else
                    triage = "compile-issue";
            }
        }

        _log.LogInformation("Onboard triage for {Entry}: {Triage} (shims: {Shims})",
            entryLeaf, triage, string.Join(", ", shims.Keys));

        if (triage == "moc-blocked")
        {
            return new Result(false, "triage", null, entries, alternatives, outlinePath, triage,
                triageDetail, shims.Keys.ToArray(), req.Samples is { Length: > 0 }, null, null,
                "This parser is a Q_OBJECT (moc-generated code) — it cannot be transplanted into the " +
                "Qt-less app image. Its decode logic must be re-expressed; the outline names the class " +
                "responsibilities to guide that.",
                null, null);
        }

        // ---- 5. Plan session ----------------------------------------------------
        var bundle = AdaptPlanBuilder.TryLoadSourceBundle(srcDir, entries);
        var draft = AdaptPlanBuilder.Build(srcDir, entries, lower, upper, external, bundle, includeUpper: false);
        var planId = Guid.NewGuid().ToString("n")[..12];
        var session = new PlanSession(
            planId, srcDir, entries, null, null, null, false,
            req.PreferScaffold, true, model, null,
            outDir, duplicatePath, lower, upper, external, manifestText,
            draft, new List<string>(), "proposed", DateTimeOffset.UtcNow,
            req.MaxCompileRetries);
        savePlan(session);

        // ---- 6. Oracle staging ---------------------------------------------------
        var oracleMode = req.Samples is { Length: > 0 };
        var minEvents = req.MinEvents > 0 ? req.MinEvents : OracleEvaluator.DefaultMinEvents;
        var stagedSamples = new List<(string Original, string Staged)>();
        string? smokeFile = null;
        if (oracleMode)
        {
            foreach (var sample in req.Samples!)
            {
                var staged = await StageSampleAsync(sample, ct).ConfigureAwait(false);
                if (staged is null)
                    return Fail("oracle", outlinePath, $"could not stage sample into the parser app volume: {sample}", entries, alternatives, planId, triage, shims);
                stagedSamples.Add((sample, staged));
            }
            smokeFile = stagedSamples[0].Staged;
        }

        // ---- 7. Draft → gate → install → smoke(oracle) ----------------------------
        // Model drafts still require ConfirmModelReview for /api/adapt-cycle.
        // Onboard oracle path may auto-install model output ONLY via OracleAutoInstall;
        // Ok stays false unless OracleEvaluator passes every check (multi-sample + fields).
        var loopReq = new AdaptInstallLoop.Request(
            planId,
            poco,
            RebuildImage: true,
            ImageTag: null,
            RecreateEvtx: true,
            SmokeExtractFile: smokeFile,
            MaxBuildRetries: req.MaxBuildRetries,
            PreferScaffold: req.PreferScaffold,
            RequireCompile: true,
            MaxCompileRetries: req.MaxCompileRetries,
            Model: model,
            ConfirmModelReview: false,
            OracleAutoInstall: oracleMode);

        var run = await _adaptLoop.RunAsync(loopReq, resolvePlan, savePlan, ct).ConfigureAwait(false);

        int? rows = null;
        OracleEvaluator.Verdict? verdict = null;
        var oracleOk = !oracleMode;
        if (oracleMode)
        {
            var sampleInputs = new List<(string Sample, string? SmokeDetail, string? Csv)>();
            foreach (var (orig, staged) in stagedSamples)
            {
                var (ok, smokeDetail, body) = await SmokeSampleAsync(staged, ct).ConfigureAwait(false);
                if (!ok)
                    _log.LogWarning("Oracle sample {Sample} smoke failed: {Detail}", orig, smokeDetail);
                sampleInputs.Add((orig, smokeDetail, body));
            }

            verdict = OracleEvaluator.Evaluate(sampleInputs, minEvents);
            rows = verdict.Samples.Count > 0 ? verdict.Samples.Min(s => s.Rows) : null;
            oracleOk = run.Ok && verdict.AllPassed;
            if (run.Ok && verdict.AllPassed)
            {
                await InstallExecutor.UpdateInstalledProvenanceAsync(poco, "pass", "oracle", ct)
                    .ConfigureAwait(false);
            }
            else if (run.Ok && !verdict.AllPassed)
            {
                _log.LogWarning("Oracle checks failed after install — rolling back: {Summary}", verdict.Summary);
                try
                {
                    await InstallExecutor.UpdateInstalledProvenanceAsync(poco, "fail", "oracle", ct)
                        .ConfigureAwait(false);
                    await InstallExecutor.RollbackPreviousAsync(
                        poco, run.ImageTag, line => _log.LogInformation("[rollback] {Line}", line), ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Oracle rollback failed after failed checks");
                }
            }
        }

        var stage = run.Ok ? (oracleOk ? "done" : "oracle") : "adapt";
        var summary = run.Ok
            ? oracleOk
                ? $"Onboarded via {triage} path: {run.Summary} {verdict?.Summary ?? $"Oracle: rows>={minEvents}"}"
                : $"Installed but oracle UNVERIFIED — {verdict?.Summary ?? $"rows={rows} < min={minEvents}"}"
            : run.Summary;

        return new Result(
            run.Ok && oracleOk, stage, planId, entries, alternatives, outlinePath, triage, triageDetail,
            shims.Keys.ToArray(), oracleMode, rows, run.ImageTag, summary, run.Error, run.Attempts);
    }

    static async Task<(bool Ok, string Detail, string? Csv)> SmokeSampleAsync(string extractFile, CancellationToken ct)
    {
        var ports = PortSettingsStore.Load();
        var baseUrl = PortSettingsStore.ParserReachableBaseUrl(ports);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var enc = Uri.EscapeDataString(extractFile.Trim());
            using var extractReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/extract?file={enc}&max_events=200");
            extractReq.Content = new ByteArrayContent(Array.Empty<byte>());
            var extract = await http.SendAsync(extractReq, ct).ConfigureAwait(false);
            var extractBody = await extract.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!extract.IsSuccessStatusCode)
                return (false, $"extract HTTP {(int)extract.StatusCode}", null);

            string? jobId = null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(extractBody);
                if (doc.RootElement.TryGetProperty("id", out var idEl))
                    jobId = idEl.GetString();
            }
            catch { /* ignore */ }
            if (string.IsNullOrWhiteSpace(jobId))
                return (false, "extract missing job id", null);

            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                var st = await http.GetAsync($"{baseUrl}/status?id={Uri.EscapeDataString(jobId)}", ct).ConfigureAwait(false);
                var statusBody = await st.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                string? state = null;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(statusBody);
                    if (doc.RootElement.TryGetProperty("status", out var s)) state = s.GetString();
                }
                catch { /* ignore */ }
                if (string.Equals(state, "done", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "ok", StringComparison.OrdinalIgnoreCase))
                    break;
                if (string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
                    return (false, "extract job failed: " + statusBody, null);
                await Task.Delay(250, ct).ConfigureAwait(false);
            }

            var result = await http.GetAsync($"{baseUrl}/result?id={Uri.EscapeDataString(jobId)}", ct).ConfigureAwait(false);
            var csv = await result.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccessStatusCode)
                return (false, $"result HTTP {(int)result.StatusCode}", null);
            var dataRows = OracleEvaluator.CountDataRows(csv);
            return (dataRows > 0, $"health+extract ok rows={dataRows} job={jobId}", csv);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    // ------------------------------------------------------------------------

    static Result Fail(string stage, string? outlinePath, string error,
        string[]? entries = null, string[]? alternatives = null, string? planId = null,
        string triage = "unknown", Dictionary<string, string>? shims = null) =>
        new(false, stage, planId, entries, alternatives, outlinePath, triage, null,
            shims?.Keys.ToArray(), false, null, null, null, error, null);

    static string? SafeRead(string path)
    {
        try { return File.ReadAllText(path); } catch { return null; }
    }

    /// <summary>Headers declaring a pull-style API, ranked: path-ctor first, then smaller files.</summary>
    static List<string> InferEntries(string srcDir)
    {
        string[] skip = [".git", "build", "bin", "obj", "test", "tests", "example", "examples"];
        var pullRx = new Regex(@"\b(pull|advance|nextEvent|readNext|next|read)\s*\(", RegexOptions.Compiled);
        var ctorRx = new Regex(@"\(\s*const\s+(?:std::)?(?:filesystem::path|string)\s*&", RegexOptions.Compiled);
        var classRx = new Regex(@"(?m)^\s*(?:class|struct)\s+[A-Za-z_]\w*[^;{]*\{", RegexOptions.Compiled);

        var ranked = new List<(string Rel, int Score, long Size)>();
        foreach (var file in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(srcDir, file).Replace('\\', '/');
            if (skip.Any(d => rel.Split('/').Contains(d, StringComparer.OrdinalIgnoreCase))) continue;
            var ext = Path.GetExtension(file);
            if (ext is not (".hpp" or ".h" or ".hh" or ".hxx")) continue;
            var info = new FileInfo(file);
            if (info.Length > 512_000) continue;
            string text;
            try { text = File.ReadAllText(file); } catch { continue; }
            if (!classRx.IsMatch(text) || !pullRx.IsMatch(text)) continue;
            var score = (ctorRx.IsMatch(text) ? 2 : 0) + 1;
            ranked.Add((rel, score, info.Length));
        }
        return ranked.OrderByDescending(r => r.Score).ThenBy(r => r.Size).Select(r => r.Rel).ToList();
    }

    /// <summary>
    /// Minimal adapter that only proves the extracted entry header COMPILES in the
    /// Qt-less toolchain (with shims present). Decodes nothing.
    /// </summary>
    static string BuildProbeAdapter(string entryLeaf) =>
        $$"""
        #pragma once
        #include "processing.hpp"
        #include "{{entryLeaf}}"
        #include <memory>
        namespace fileproc {
        class CustomEventReader : public IEventReader {
        public:
            explicit CustomEventReader(const std::filesystem::path&) {}
            void set_requested_fields(const std::vector<std::string>&) override {}
            bool decodes_named_fields() const override { return false; }
            bool next(Event&, size_t) override { return false; }
            bool can_resume() const override { return false; }
            uint64_t position() const override { return 0; }
            void resume_at(uint64_t) override {}
        };
        inline FileInfo inspect_custom(const std::filesystem::path&) { return FileInfo{}; }
        inline void use_custom_reader() {}
        } // namespace fileproc
        """;

    /// <summary>Copies a workspace sample file into the parser app's /data volume; returns the staged leaf name.</summary>
    async Task<string?> StageSampleAsync(string samplePath, CancellationToken ct)
    {
        try
        {
            var full = Path.GetFullPath(samplePath);
            DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(full, "sample");
            if (!File.Exists(full)) { _log.LogWarning("sample missing: {Path}", full); return null; }

            var project = PortSettingsStore.ResolveComposeProject();
            var id = await RunDockerAsync(
                ["ps", "--filter", $"label=com.docker.compose.project={project}",
                 "--filter", "label=com.docker.compose.service=evtx", "--format", "{{.ID}}"], ct).ConfigureAwait(false);
            id = id?.Trim().Split('\n').FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(id)) { _log.LogWarning("no running evtx container found for staging"); return null; }

            var leaf = Path.GetFileName(full);
            var cp = await RunDockerAsync(["cp", full, $"{id}:/data/{leaf}"], ct).ConfigureAwait(false);
            _log.LogInformation("staged oracle sample {Leaf} into evtx:/data ({Container})", leaf, id);
            return cp is null ? null : leaf;
        }
        catch (Exception ex)
        {
            _log.LogWarning("sample staging failed: {Message}", ex.Message);
            return null;
        }
    }

    static async Task<string?> RunDockerAsync(string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        if (p is null) return null;
        var stdout = await p.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await p.WaitForExitAsync(ct).ConfigureAwait(false);
        return p.ExitCode == 0 ? stdout : null;
    }
}
