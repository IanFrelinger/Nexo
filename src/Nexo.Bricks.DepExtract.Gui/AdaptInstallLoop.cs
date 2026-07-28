using System.Text;
using Microsoft.Extensions.Logging;
using Nexo.Bricks.DepExtract;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// Closed loop: implement (compile gate via cpp-evtx profile) → install → docker build →
/// optional extract smoke, feeding build/extract failures back into the next implement attempt.
/// </summary>
public sealed class AdaptInstallLoop
{
    private readonly GenerativeArtifactBrick _generator;
    private readonly IInstallExecutor _installer;
    private readonly ILogger<AdaptInstallLoop> _log;

    public AdaptInstallLoop(
        GenerativeArtifactBrick generator,
        IInstallExecutor installer,
        ILogger<AdaptInstallLoop> log)
    {
        _generator = generator;
        _installer = installer;
        _log = log;
    }

    public sealed record Request(
        string PlanId,
        string? PocoContext,
        bool RebuildImage,
        string? ImageTag,
        bool RecreateEvtx,
        string? SmokeExtractFile,
        int MaxBuildRetries,
        bool? PreferScaffold,
        bool? RequireCompile,
        int? MaxCompileRetries,
        string? Model,
        bool ConfirmModelReview = false,
        /// <summary>
        /// When true (golden-file oracle path), a model-authored draft may install without
        /// human ConfirmModelReview — but the caller MUST treat Ok as failure unless
        /// the profile acceptance evaluator passes all checks (and should roll back on fail).
        /// </summary>
        bool OracleAutoInstall = false);

    public sealed record Attempt(
        int Index,
        bool ImplementOk,
        string? AdapterCode,
        bool? CompileOk,
        string? CompileLog,
        bool? InstallOk,
        string? BuildLog,
        string? SmokeDetail,
        string? Error);

    public sealed record Result(
        bool Ok,
        string PlanId,
        string? AdapterCode,
        string? AdaptedReaderPath,
        string? ImageTag,
        string? Summary,
        string? Error,
        IReadOnlyList<Attempt> Attempts);

    public async Task<Result> RunAsync(
        Request req,
        Func<string, PlanSession?> resolvePlan,
        Action<PlanSession> savePlan,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.PlanId))
            throw DepExtractException.InvalidInput("planId is required");

        try
        {
            ComposePipeline.EnsureStackIdentity();
        }
        catch (Exception ex)
        {
            throw DepExtractException.InvalidInput(ex.Message);
        }

        var session = resolvePlan(req.PlanId)
            ?? throw DepExtractException.NotFound($"unknown planId: {req.PlanId}");
        if (session.DuplicatePath is null || !Directory.Exists(session.DuplicatePath))
            throw DepExtractException.InvalidInput("plan has no extract duplicate — re-run Propose plan");

        var maxAttempts = Math.Max(1, req.MaxBuildRetries + 1);
        var attempts = new List<Attempt>();
        var failureFeedback = new StringBuilder();
        string? lastCode = null;
        string? lastPath = null;
        string? lastImage = null;
        var lastAttemptUsedModel = false;

        for (var i = 1; i <= maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();
            _log.LogInformation("Adapt-cycle attempt {Attempt}/{Max} for plan {PlanId}", i, maxAttempts, req.PlanId);

            var preferScaffold = req.PreferScaffold ?? session.PreferScaffold;
            var skipOllama = string.Equals(
                Environment.GetEnvironmentVariable("SKIP_OLLAMA"), "1", StringComparison.Ordinal);
            if (i > 1 && failureFeedback.Length > 0)
            {
                var last = attempts.LastOrDefault();
                var smokeOnly = last is { InstallOk: false, Error: { } err }
                    && err.Contains("smoke failed", StringComparison.OrdinalIgnoreCase);
                if (skipOllama || (smokeOnly && !lastAttemptUsedModel))
                {
                    return new Result(false, req.PlanId, lastCode, lastPath, lastImage, null,
                        (smokeOnly && !lastAttemptUsedModel
                            ? "Scaffold succeeded but the golden-file oracle (smoke extract) failed — "
                              + "not retrying via the local model. Fix the parser wire format or sample, then re-onboard:\n"
                            : "Install/build/extract failed and SKIP_OLLAMA=1 blocks model re-adapt:\n")
                        + failureFeedback,
                        attempts);
                }
                preferScaffold = false;
            }

            var requireCompile = req.RequireCompile ?? session.RequireCompile;
            var guidance = session.Draft.OperatorGuidance ?? "";
            if (failureFeedback.Length > 0)
                guidance = (guidance.TrimEnd() + "\n\n" + failureFeedback).Trim();

            var poco = req.PocoContext ?? Environment.GetEnvironmentVariable("POCO_CONTEXT");
            string grounding;
            try
            {
                grounding = CppAdapterPrompt.AssembleGrounding(session.DuplicatePath, session.Entries);
            }
            catch (Exception ex)
            {
                throw DepExtractException.InvalidInput(ex.Message);
            }

            CompileGateRequestContext.RequireSuccess = requireCompile;
            string? code;
            bool? compileOk = null;
            string? compileLog = null;
            CompanionFileDto[]? shimCompanions = null;
            var usedModel = false;
            try
            {
                var entryInclude = session.Entries is { Length: > 0 }
                    ? session.Entries[0].Replace('\\', '/').TrimStart('/')
                    : null;
                var overrides = new Dictionary<string, object>
                {
                    ["operatorGuidance"] = guidance,
                    ["entryFiles"] = session.Entries,
                };
                if (!string.IsNullOrWhiteSpace(poco))
                    overrides["pocoContext"] = poco!;
                if (!string.IsNullOrWhiteSpace(entryInclude))
                    overrides["entryInclude"] = entryInclude!;

                var adaptValues = new Dictionary<string, object>
                {
                    ["target"] = CppEvtxProfileFactory.TargetId,
                    ["grounding"] = grounding,
                    ["outputPath"] = Path.Combine(session.OutDir, "adapted_reader.hpp"),
                    ["preferDeterministic"] = preferScaffold,
                    ["overrides"] = overrides,
                };
                if ((req.MaxCompileRetries ?? session.MaxCompileRetries) is { } mcr)
                    adaptValues["maxRepairAttempts"] = mcr;

                var adaptOut = await _generator.ExecuteAsync(
                    new BrickInput(adaptValues), ImplementationType.Agentic, CycleExecutionContext.Instance, ct)
                    .ConfigureAwait(false);

                var artifact = adaptOut.Get<GeneratedArtifact>("artifact");
                code = artifact?.Content;
                lastCode = code;
                if (string.IsNullOrWhiteSpace(code))
                    throw DepExtractException.EmptyModelResponse();

                var outPath = Path.Combine(session.OutDir, "adapted_reader.hpp");
                Directory.CreateDirectory(session.OutDir);
                await File.WriteAllTextAsync(outPath, code, ct).ConfigureAwait(false);

                var verified = adaptOut.Get<bool>("verified");
                compileOk = verified;
                var prov = adaptOut.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
                if (prov?.Warnings is { Count: > 0 })
                    compileLog = string.Join("\n", prov.Warnings);
                usedModel = prov?.Strategy == GenerationStrategy.Model;
                lastAttemptUsedModel = usedModel;

                // Qt shims for install companions (sandbox shim dir is ephemeral).
                var shimDir = Path.Combine(Path.GetTempPath(), "qt-shims-" + Guid.NewGuid().ToString("n"));
                Directory.CreateDirectory(shimDir);
                try
                {
                    var qsd = QtHeaderShims.EnsureForDirectory(grounding, session.DuplicatePath, shimDir, _log);
                    if (qsd.Count > 0)
                        shimCompanions = qsd.Select(kv => new CompanionFileDto(kv.Key, kv.Value)).ToArray();
                }
                finally
                {
                    try { Directory.Delete(shimDir, recursive: true); } catch { /* ignore */ }
                }

                if (usedModel && !req.ConfirmModelReview && !req.OracleAutoInstall)
                {
                    attempts.Add(new Attempt(i, true, code, compileOk, compileLog, null, null, null,
                        "blocked: model-drafted adapter requires human review before auto-install"));
                    return new Result(false, req.PlanId, code, lastPath, lastImage, null,
                        "This draft was generated by the local model (not the deterministic scaffold). " +
                        "Auto-install via adapt-cycle refuses to proceed without an explicit confirmation — " +
                        "review the adapterCode above (especially inspect_custom, resume_at, and any named-" +
                        "field decode logic), then retry with confirmModelReview:true, or call /api/install " +
                        "directly once you're satisfied with the draft. " +
                        "(Oracle auto-install is only enabled when onboard supplies samples and will " +
                        "still require ALL oracle checks to pass — otherwise the install is rolled back.)",
                        attempts);
                }
            }
            catch (Exception ex)
            {
                attempts.Add(new Attempt(i, false, lastCode, compileOk, compileLog, null, null, null, ex.Message));
                failureFeedback.AppendLine($"Implement attempt {i} failed: {ex.Message}");
                if (i == maxAttempts)
                {
                    return new Result(false, req.PlanId, lastCode, lastPath, lastImage,
                        null, ex.Message, attempts);
                }
                continue;
            }
            finally
            {
                CompileGateRequestContext.RequireSuccess = null;
            }

            if (usedModel)
            {
                var scan = AdapterContentScan.Scan(code!, isModelAuthored: true);
                if (!scan.Ok)
                {
                    var detail = string.Join("; ", scan.Hits.Select(h => $"{h.Rule} @ {h.Excerpt}"));
                    var msg = "Content scan blocked model-authored adapter before install: " + detail;
                    attempts.Add(new Attempt(i, true, code, compileOk, compileLog, false, null, null, msg));
                    return new Result(false, req.PlanId, code, lastPath, lastImage, null, msg, attempts);
                }
                if (scan.OverrideUsed)
                    _log.LogWarning("Content-scan override used: {Reason}; hits={Hits}",
                        scan.OverrideReason, scan.Hits.Count);
            }

            session = session with { Status = "implemented" };
            savePlan(session);

            var model = req.Model ?? session.Model;
            var modelPin = usedModel
                ? await OllamaModelPin.ResolveAsync(model, logger: _log, ct: ct).ConfigureAwait(false)
                : null;
            var installReq = new InstallWorkRequest(
                code,
                null,
                poco,
                req.RebuildImage,
                req.ImageTag,
                shimCompanions,
                req.PlanId,
                req.RecreateEvtx,
                Smoke: !string.IsNullOrWhiteSpace(req.SmokeExtractFile) || req.RebuildImage,
                WaitHealthy: true,
                SmokeExtractFile: req.SmokeExtractFile,
                Strategy: usedModel ? "model" : (preferScaffold ? "scaffold" : "deterministic"),
                ModelTag: modelPin?.Tag,
                ModelDigest: modelPin?.Digest,
                CompileGatePassed: compileOk,
                OracleResult: req.OracleAutoInstall ? "pending" : "n/a",
                ReviewedBy: req.ConfirmModelReview ? "human" : (req.OracleAutoInstall ? "oracle" : "none"),
                IsModelAuthored: usedModel);

            InstallResult install;
            try
            {
                install = await _installer.RunSyncAsync(installReq, resolvePlan, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                attempts.Add(new Attempt(i, true, code, compileOk, compileLog, false, null, null, ex.Message));
                failureFeedback.AppendLine(
                    $"Install/build attempt {i} threw. Fix the adapter and companions so Dockerfile.custom builds:\n{ex.Message}");
                if (i == maxAttempts)
                {
                    return new Result(false, req.PlanId, code, lastPath, lastImage, null, ex.Message, attempts);
                }
                continue;
            }

            lastPath = install.AdaptedReaderPath;
            lastImage = install.ImageTag;
            attempts.Add(new Attempt(
                i, true, code, compileOk, compileLog, install.Ok, install.BuildLog, install.SmokeDetail, install.Error));

            if (install.Ok)
            {
                return new Result(true, req.PlanId, code, install.AdaptedReaderPath, install.ImageTag,
                    install.Summary, null, attempts);
            }

            var reason = install.Error ?? "install failed";
            var blog = install.BuildLog ?? "";
            var smoke = install.SmokeDetail ?? "";
            failureFeedback.AppendLine(
                $"Install/build/extract attempt {i} failed: {reason}\n" +
                (string.IsNullOrWhiteSpace(blog) ? "" : $"Build log (tail):\n{Tail(blog, 3500)}\n") +
                (string.IsNullOrWhiteSpace(smoke) ? "" : $"Smoke/extract detail:\n{smoke}\n") +
                "Revise CustomEventReader (and required proprietary includes) so the custom image builds and extract returns rows. Output a full corrected header only on the next draft.");

            if (i == maxAttempts)
            {
                return new Result(false, req.PlanId, code, install.AdaptedReaderPath, install.ImageTag,
                    null, reason, attempts);
            }
        }

        return new Result(false, req.PlanId, lastCode, lastPath, lastImage, null, "adapt-cycle exhausted", attempts);
    }

    private static string Tail(string s, int max) =>
        s.Length <= max ? s : s[^max..];

    private sealed class CycleExecutionContext : IExecutionContext
    {
        public static readonly CycleExecutionContext Instance = new();
        public string AgentId => "dep-extract-agent";
        public string BehaviorId => "adapt-cycle";
        public bool IsAirGapped => true;
        public bool AuditMode => false;
        public string Provider => "ollama";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
