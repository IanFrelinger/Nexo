using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Bricks.DepExtract;
using Nexo.Bricks.DepExtract.Gui;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

// Avoid Results.Json → PipeWriter.UnflushedBytes failures under ASP.NET 8 TestHost
// with System.Text.Json 9/10 (serialize to string, then Content).
static IResult SafeJson<T>(T value, int statusCode = 200)
{
    var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });
    return Results.Content(json, "application/json", statusCode: statusCode);
}

var plans = new ConcurrentDictionary<string, PlanSession>(StringComparer.Ordinal);
try { PlanSessionStore.LoadInto(plans); } catch { /* best-effort hydrate */ }

// Air-gap: refuse to start if the model endpoint would send proprietary source off-box.
// Override only with NEXO_ALLOW_REMOTE_MODEL=1 (logged). In-cluster compose uses host
// "ollama" via NEXO_OLLAMA_ALLOWLIST (default includes that name).
try
{
    OllamaEndpointPolicy.AssertAirGappedOrThrow();
}
catch (Exception ex)
{
    Console.Error.WriteLine("FATAL: " + ex.Message);
    throw;
}

// First-run bootstrap: compose recreate ops (ports/EVTX_IMAGE switch) require the
// per-project env file. Write it from this container's own environment so a bare
// `docker compose up` works without setup-parser-pipeline.sh / pp having run first.
try
{
    var envFile = PortSettingsStore.ResolveEnvFilePath();
    if (envFile is not null)
    {
        if (!File.Exists(envFile))
        {
            var ports = PortSettingsStore.Load();
            PortSettingsStore.Save(ports.ParserHostPort, ports.AgentHostPort);
        }
        // Always refresh pass-through keys. A retained workspace/env file must not
        // silently keep an old model or mutable release selector after restart.
        foreach (var key in new[]
                 {
                     "OLLAMA_MODEL", "DEPLOY_REGISTRY", "DEPLOY_TAG",
                     "DEPLOY_AGENT_IMAGE", "DEPLOY_TOOL_IMAGE"
                 })
        {
            if (Environment.GetEnvironmentVariable(key) is { Length: > 0 } value)
                PortSettingsStore.UpsertEnvVar(key, value);
        }
    }
}
catch { /* best-effort — agent still runs; recreate endpoints will explain what's missing */ }

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProviderFactory, ProviderFactory>();
builder.Services.AddSingleton<Nexo.Infrastructure.Scaling.IProcessCommandRunner, Nexo.Infrastructure.Scaling.ProcessCommandRunner>();
builder.Services.AddSingleton<Nexo.Core.Application.Execution.Ports.ISandboxedCommandRunner, Nexo.Infrastructure.Execution.Sandbox.DockerSandboxedCommandRunner>();
builder.Services.AddSingleton<Nexo.Core.Application.Execution.Ports.IScratchSpace, Nexo.Infrastructure.Execution.Scratch.FileScratchSpace>();
builder.Services.AddSingleton<Nexo.Core.Application.Execution.Ports.IManagedFileSet, Nexo.Infrastructure.Execution.Scratch.SnapshotManagedFileSet>();
builder.Services.AddSingleton<Nexo.Core.Application.Execution.Ports.ISingleFlightGuard, Nexo.Infrastructure.Execution.Scratch.SingleFlightGuard>();
builder.Services.AddSingleton<Nexo.Bricks.DepExtract.Profile.IPocoDeploymentOps, GuiPocoDeploymentOps>();
builder.Services.AddNexoCppEvtxAgentProfile();
builder.Services.AddSingleton<Nexo.Core.Application.Generation.GenerativeArtifactBrick>();
builder.Services.AddSingleton<CppDependencyExtractorBrick>();
builder.Services.AddSingleton<CppProjectOutlineBrick>();
builder.Services.AddSingleton<InstallExecutor>();
builder.Services.AddSingleton<IInstallExecutor>(sp => sp.GetRequiredService<InstallExecutor>());
builder.Services.AddSingleton<AdaptInstallLoop>();
builder.Services.AddSingleton<OnboardLoop>();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", async (IProviderFactory pf, CancellationToken ct) =>
{
    var docker = await CheckDockerAsync(ct);
    string? ollamaError = null;
    var ollama = false;
    try
    {
        await pf.EnsureOllamaReachableAsync(requireVisionModel: false, ct);
        ollama = true;
    }
    catch (Exception ex)
    {
        ollamaError = ex.Message;
    }

    var workspace = DepExtractPathRules.TryGetWorkspaceRoot();
    var poco = Environment.GetEnvironmentVariable("POCO_CONTEXT");
    var depExtractImage = await CheckDepExtractImageAsync(ct);
    var extractReady = docker.Ok && workspace is not null && depExtractImage.Ok;
    var adaptReady = extractReady && ollama;
    var ports = PortSettingsStore.Load();
    var compileGate = string.Equals(Environment.GetEnvironmentVariable("COMPILE_GATE"), "1", StringComparison.Ordinal);
    var preferScaffoldDefault = string.Equals(Environment.GetEnvironmentVariable("PREFER_SCAFFOLD"), "1", StringComparison.Ordinal);
    var (stackOk, stackDetail) = ComposePipeline.TryCheckStackIdentity();
    return SafeJson(new StatusResponse(
        docker.Ok, ollama, docker.Detail, ollamaError,
        workspace, poco, extractReady, adaptReady, depExtractImage.Detail,
        PortSettingsStore.ParserUiUrl(ports), compileGate, preferScaffoldDefault,
        ports.ParserHostPort, ports.AgentHostPort,
        PortSettingsStore.AgentUiUrl(ports),
        PortSettingsStore.ResolveEnvFilePath(),
        PortSettingsStore.TryGetEnvVar("EVTX_IMAGE") ?? Environment.GetEnvironmentVariable("EVTX_IMAGE"),
        ComposePipeline.ProjectName,
        ComposePipeline.TryFindComposeFile(),
        stackOk,
        stackDetail));
});

app.MapGet("/api/pipeline", () =>
{
    var ports = PortSettingsStore.Load();
    var envFile = PortSettingsStore.ResolveEnvFilePath();
    var compose = ComposePipeline.TryFindComposeFile();
    string? pipelineMd = null;
    var ws = DepExtractPathRules.TryGetWorkspaceRoot();
    if (ws is not null)
    {
        var md = Path.Combine(ws, "PIPELINE.md");
        if (File.Exists(md)) pipelineMd = md;
    }
    return SafeJson(new PipelineResponse(
        ComposePipeline.ProjectName,
        compose,
        envFile,
        pipelineMd,
        ports.ParserHostPort,
        ports.AgentHostPort,
        PortSettingsStore.ParserUiUrl(ports),
        PortSettingsStore.AgentUiUrl(ports),
        PortSettingsStore.TryGetEnvVar("EVTX_IMAGE") ?? Environment.GetEnvironmentVariable("EVTX_IMAGE"),
        ComposePipeline.AllowedServices.OrderBy(s => s).ToArray()));
});

app.MapGet("/api/ports", () =>
{
    var ports = PortSettingsStore.Load();
    return SafeJson(new PortsResponse(
        ports.ParserHostPort,
        ports.AgentHostPort,
        PortSettingsStore.ParserUiUrl(ports),
        PortSettingsStore.AgentUiUrl(ports),
        PortSettingsStore.ResolveEnvFilePath(),
        PortSettingsStore.RestartHint(ports)));
});

app.MapPut("/api/ports", (PortsRequest req) =>
{
    try
    {
        var current = PortSettingsStore.Load();
        var parser = req.ParserHostPort ?? current.ParserHostPort;
        var agent = req.AgentHostPort ?? current.AgentHostPort;
        if (parser is < 1 or > 65535 || agent is < 1 or > 65535)
            return ApiError(400, "invalid_input", "Ports must be integers 1–65535");
        if (parser == agent)
            return ApiError(400, "invalid_input", "Parser and agent host ports must differ");

        var saved = PortSettingsStore.Save(parser, agent);
        return SafeJson(new PortsResponse(
            saved.ParserHostPort,
            saved.AgentHostPort,
            PortSettingsStore.ParserUiUrl(saved),
            PortSettingsStore.AgentUiUrl(saved),
            PortSettingsStore.ResolveEnvFilePath(),
            PortSettingsStore.RestartHint(saved)));
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return ApiError(400, "invalid_input", ex.Message);
    }
    catch (Exception ex)
    {
        return ApiError(500, "ports_save_failed", ex.Message);
    }
});

/// <summary>Probe Docker + TCP and return a free parser/agent host-port pair.</summary>
app.MapGet("/api/ports/suggest", async (int? preferParser, int? preferAgent, bool? save, CancellationToken ct) =>
{
    try
    {
        var suggestion = await HostPortAllocator.SuggestAsync(
            preferParser ?? PortSettingsStore.DefaultParserHostPort,
            preferAgent ?? PortSettingsStore.DefaultAgentHostPort,
            ct);

        PortSettings? saved = null;
        if (save == true)
            saved = PortSettingsStore.Save(suggestion.ParserHostPort, suggestion.AgentHostPort);

        var ports = saved ?? new PortSettings(suggestion.ParserHostPort, suggestion.AgentHostPort);
        return SafeJson(new PortsSuggestResponse(
            ports.ParserHostPort,
            ports.AgentHostPort,
            PortSettingsStore.ParserUiUrl(ports),
            PortSettingsStore.AgentUiUrl(ports),
            PortSettingsStore.ResolveEnvFilePath(),
            saved is null ? suggestion.Summary : PortSettingsStore.RestartHint(ports),
            suggestion.PreferredParserHostPort,
            suggestion.PreferredAgentHostPort,
            suggestion.ChangedFromPreferred,
            suggestion.Summary,
            suggestion.BusySample.Take(24).ToArray(),
            saved is not null));
    }
    catch (Exception ex)
    {
        return ApiError(500, "ports_suggest_failed", ex.Message);
    }
});

/// <summary>Auto-pick free ports and persist (same as GET suggest?save=true).</summary>
app.MapPost("/api/ports/auto", async (PortsAutoRequest? req, CancellationToken ct) =>
{
    try
    {
        var current = PortSettingsStore.Load();
        var suggestion = await HostPortAllocator.SuggestAsync(
            req?.PreferParserHostPort ?? current.ParserHostPort,
            req?.PreferAgentHostPort ?? current.AgentHostPort,
            ct);
        var saved = PortSettingsStore.Save(suggestion.ParserHostPort, suggestion.AgentHostPort);
        return SafeJson(new PortsSuggestResponse(
            saved.ParserHostPort,
            saved.AgentHostPort,
            PortSettingsStore.ParserUiUrl(saved),
            PortSettingsStore.AgentUiUrl(saved),
            PortSettingsStore.ResolveEnvFilePath(),
            PortSettingsStore.RestartHint(saved),
            suggestion.PreferredParserHostPort,
            suggestion.PreferredAgentHostPort,
            suggestion.ChangedFromPreferred,
            suggestion.Summary,
            suggestion.BusySample.Take(24).ToArray(),
            true));
    }
    catch (Exception ex)
    {
        return ApiError(500, "ports_auto_failed", ex.Message);
    }
});

/// <summary>Force-recreate compose services with the current parser-pipeline.env ports.</summary>
app.MapPost("/api/ports/recreate", async (PortsRecreateRequest? req, CancellationToken ct) =>
{
    try
    {
        var previousAgent = PortSettingsStore.Load().AgentHostPort;
        var withAgent = req?.WithExtractAgent ?? true;
        IReadOnlyList<string>? services = null;
        if (req?.Services is { Length: > 0 })
            services = req.Services;
        else if (req?.EvtxOnly == true)
            services = new[] { "evtx" };

        var dryRun = req?.DryRun == true;
        var waitHealthy = req?.WaitHealthy is not false;
        var result = await ComposePipeline.RecreateAsync(
            services,
            withAgent && req?.EvtxOnly != true,
            ct,
            dryRun: dryRun,
            waitHealthy: waitHealthy && !dryRun,
            previousAgentPort: previousAgent);
        if (!result.Ok)
            return ApiError(502, "compose_recreate_failed", result.Log);

        var log = result.Log;
        if (!string.IsNullOrWhiteSpace(result.HealthLog))
            log = (log + "\n" + result.HealthLog).Trim();

        return SafeJson(new PortsRecreateResponse(
            true,
            log,
            result.Project,
            result.ComposeFile,
            result.EnvFile,
            result.ParserUiUrl ?? PortSettingsStore.ParserUiUrl(),
            result.AgentUiUrl ?? PortSettingsStore.AgentUiUrl(),
            result.Services,
            result.Command,
            dryRun,
            waitHealthy && !dryRun,
            result.ReloadAgentSuggested,
            result.HealthLog));
    }
    catch (Exception ex)
    {
        return ApiError(500, "compose_recreate_failed", ex.Message);
    }
});

app.MapGet("/api/workspace", (string? path, int? depth) =>
{
    var root = DepExtractPathRules.TryGetWorkspaceRoot();
    if (root is null)
        return ApiError(400, "invalid_input", "WORKSPACE_ROOT is not configured");

    string target;
    try
    {
        if (string.IsNullOrWhiteSpace(path) || path is "." or "/")
            target = root;
        else if (Path.IsPathRooted(path))
            target = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(path, "path");
        else
            target = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(Path.Combine(root, path), "path");
    }
    catch (Exception ex)
    {
        return MapException(ex, "invalid workspace path");
    }

    if (!Directory.Exists(target))
        return ApiError(404, "not_found", $"path not found: {path}");

    var maxDepth = Math.Clamp(depth ?? 2, 0, 4);
    var entries = ListWorkspace(target, root, maxDepth, 0);
    return SafeJson(new WorkspaceResponse(root, Path.GetRelativePath(root, target).Replace('\\', '/'), entries));
});

/// <summary>Extract deps + return a diagrammed adapt plan for operator approve / feedback.</summary>
app.MapPost("/api/plan", async (PlanRequest req, CppDependencyExtractorBrick extractor, ILoggerFactory logFactory, CancellationToken ct) =>
{
    var log = logFactory.CreateLogger("DepExtract.Plan");
    var prepared = PrepareRun(req.SrcDir, req.Entries);
    if (prepared.Error is { } prepErr) return prepErr;

    var extractValues = BuildExtractValues(prepared.SrcDir!, prepared.OutDir!, req.Entries!, req.ScanDir, req.IncludeDirs, req.Defines, req.IncludeUpper, manifestOnly: false);
    BrickOutput extractOut;
    try
    {
        extractOut = await extractor.ExecuteAsync(new BrickInput(extractValues), ImplementationType.Deterministic, new GuiExecutionContext(), ct);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "plan extraction failed for {SrcDir}", prepared.SrcDir);
        return MapException(ex, "extraction failed");
    }

    var lower = extractOut.Get<string[]>("lower");
    var upper = extractOut.Get<string[]>("upper");
    var external = extractOut.Get<string[]>("external");
    var manifestText = extractOut.Get<string>("manifestText");
    var extractDict = extractOut.ToDictionary();
    var duplicatePath = extractDict.TryGetValue("duplicatePath", out var dp) ? (string)dp : null;
    var bundle = AdaptPlanBuilder.TryLoadSourceBundle(prepared.SrcDir!, req.Entries!);
    var draft = AdaptPlanBuilder.Build(
        prepared.SrcDir!, req.Entries!, lower, upper, external, bundle,
        includeUpper: req.IncludeUpper);
    var preferScaffold = req.PreferScaffold
        ?? string.Equals(Environment.GetEnvironmentVariable("PREFER_SCAFFOLD"), "1", StringComparison.Ordinal);
    var requireCompile = req.RequireCompile
        ?? string.Equals(Environment.GetEnvironmentVariable("COMPILE_GATE"), "1", StringComparison.Ordinal);

    var planId = Guid.NewGuid().ToString("n")[..12];
    var session = new PlanSession(
        planId, prepared.SrcDir!, req.Entries!, req.ScanDir, req.IncludeDirs, req.Defines, req.IncludeUpper,
        preferScaffold, requireCompile, req.Model, req.MaxRetries,
        prepared.OutDir!, duplicatePath, lower, upper, external, manifestText,
        draft, new List<string>(), "proposed", DateTimeOffset.UtcNow,
        req.MaxCompileRetries);
    plans[planId] = session;
    PlanSessionStore.Save(session);
    log.LogInformation("Created adapt plan {PlanId} for {SrcDir} strategy={Strategy}", planId, prepared.SrcDir, draft.Strategy);
    return SafeJson(ToPlanResponse(session));
});

/// <summary>Revise a proposed plan from operator feedback (re-renders diagram + narrative).</summary>
app.MapPost("/api/plan/revise", (RevisePlanRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.PlanId))
        return ApiError(400, "invalid_input", "planId is required");
    if (!TryGetPlan(plans, req.PlanId, out var session) || session is null)
        return ApiError(404, "not_found", $"unknown planId: {req.PlanId}");
    if (session.Status is "implemented")
        return ApiError(400, "invalid_input", "plan already implemented — propose a new plan");

    var extras = new List<AdaptPlanBuilder.PlanConstraint>();
    foreach (var m in req.Must ?? [])
        if (!string.IsNullOrWhiteSpace(m)) extras.Add(new AdaptPlanBuilder.PlanConstraint("must", m.Trim()));
    foreach (var m in req.MustNot ?? [])
        if (!string.IsNullOrWhiteSpace(m)) extras.Add(new AdaptPlanBuilder.PlanConstraint("must_not", m.Trim()));

    var feedback = (req.Feedback ?? "").Trim();
    if (feedback.Length == 0 && extras.Count == 0)
        return ApiError(400, "invalid_input", "feedback or must/mustNot constraints are required");

    if (feedback.Length > 0)
        session.Feedback.Add(feedback);
    foreach (var c in extras)
        session.Feedback.Add($"{c.Kind}: {c.Text}");

    var bundle = AdaptPlanBuilder.TryLoadSourceBundle(session.SrcDir, session.Entries);
    var draft = AdaptPlanBuilder.Revise(
        session.Draft, feedback, session.SrcDir, session.Entries,
        session.Lower, session.Upper, session.External, bundle, session.Feedback,
        includeUpper: session.IncludeUpper, extraConstraints: extras);
    var revised = session with { Draft = draft, Status = "revised" };
    plans[req.PlanId] = revised;
    PlanSessionStore.Save(revised);
    return SafeJson(ToPlanResponse(revised));
});

/// <summary>Operator approved the plan — run adaptation (scaffold/model) against the stored extract.</summary>
app.MapPost("/api/plan/implement", async (ImplementPlanRequest req, Nexo.Core.Application.Generation.GenerativeArtifactBrick generator, ILoggerFactory logFactory, CancellationToken ct) =>
{
    var log = logFactory.CreateLogger("DepExtract.Plan");
    if (string.IsNullOrWhiteSpace(req.PlanId))
        return ApiError(400, "invalid_input", "planId is required");
    if (!TryGetPlan(plans, req.PlanId, out var session) || session is null)
        return ApiError(404, "not_found", $"unknown planId: {req.PlanId}");
    if (session.DuplicatePath is null || !Directory.Exists(session.DuplicatePath))
        return ApiError(400, "invalid_input", "plan has no extract duplicate — re-run Propose plan");

    var preferScaffold = req.PreferScaffold ?? session.PreferScaffold;
    var requireCompile = req.RequireCompile ?? session.RequireCompile;
    var guidance = session.Draft.OperatorGuidance ?? "";
    if (!string.IsNullOrWhiteSpace(req.ExtraGuidance))
        guidance = string.IsNullOrWhiteSpace(guidance)
            ? req.ExtraGuidance!.Trim()
            : guidance.TrimEnd() + "\n\n" + req.ExtraGuidance.Trim();
    var poco = Environment.GetEnvironmentVariable("POCO_CONTEXT");

    CppEvtxAdaptRunner.AdaptResult adapt;
    try
    {
        adapt = await CppEvtxAdaptRunner.RunAsync(
            generator,
            session.DuplicatePath,
            session.Entries,
            Path.Combine(session.OutDir, "adapted_reader.hpp"),
            preferScaffold,
            requireCompile,
            guidance,
            poco,
            req.MaxCompileRetries ?? session.MaxCompileRetries,
            new GuiExecutionContext(),
            ct);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "plan implement failed for {PlanId}", req.PlanId);
        return MapException(ex, "adaptation failed", new Dictionary<string, object?>
        {
            ["planId"] = session.PlanId,
            ["lower"] = session.Lower,
            ["upper"] = session.Upper,
            ["external"] = session.External,
            ["manifestText"] = session.ManifestText,
            ["duplicatePath"] = session.DuplicatePath,
        });
    }

    var done = session with { Status = "implemented" };
    plans[req.PlanId] = done;
    PlanSessionStore.Save(done);

    return SafeJson(new RunResponse(
        session.Lower, session.Upper, session.External, session.ManifestText, session.DuplicatePath,
        adapt.AdapterCode,
        adapt.OutputPath,
        adapt.SourceApiCoverage,
        adapt.LooksLikeTemplateEcho,
        adapt.PossibleHallucinations,
        adapt.Summary,
        adapt.CompileOk,
        adapt.CompileLog,
        session.PlanId));
});

app.MapGet("/api/plan/{planId}", (string planId) =>
{
    if (!TryGetPlan(plans, planId, out var session) || session is null)
        return ApiError(404, "not_found", $"unknown planId: {planId}");
    return SafeJson(ToPlanResponse(session));
});

app.MapGet("/api/plan/{planId}/export.md", (string planId) =>
{
    if (!TryGetPlan(plans, planId, out var session) || session is null)
        return ApiError(404, "not_found", $"unknown planId: {planId}");
    var md = AdaptPlanBuilder.ToMarkdown(session.Draft, session.PlanId, session.Status, session.Feedback);
    var exportPath = Path.Combine(PlanSessionStore.ResolvePlansDirectory(), session.PlanId + ".md");
    File.WriteAllText(exportPath, md);
    return Results.Text(md, "text/markdown", System.Text.Encoding.UTF8);
});

app.MapPost("/api/run", async (RunRequest req, CppDependencyExtractorBrick extractor, Nexo.Core.Application.Generation.GenerativeArtifactBrick generator, ILoggerFactory logFactory, CancellationToken ct) =>
{
    var log = logFactory.CreateLogger("DepExtract.Agent");
    if (string.IsNullOrWhiteSpace(req.SrcDir))
        return ApiError(400, "invalid_input", "srcDir is required");
    if (!Directory.Exists(req.SrcDir))
        return ApiError(404, "not_found", $"srcDir not found: {req.SrcDir}");
    if (req.Entries is null || req.Entries.Length == 0)
        return ApiError(400, "invalid_input", "at least one entry file is required");

    string srcDir;
    try
    {
        srcDir = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(req.SrcDir, "srcDir");
    }
    catch (Exception ex)
    {
        return MapException(ex, "invalid srcDir");
    }

    var tmpBase = Environment.GetEnvironmentVariable("TMPDIR");
    if (string.IsNullOrWhiteSpace(tmpBase))
    {
        tmpBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } ws
            ? Path.Combine(ws, ".dep-extract-out")
            : Path.GetTempPath();
    }
    Directory.CreateDirectory(tmpBase);
    var outDir = Path.Combine(tmpBase, "dep-extract-gui-" + Guid.NewGuid());
    try
    {
        DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(outDir, "outDir");
    }
    catch (Exception ex)
    {
        return MapException(ex, "invalid outDir");
    }

    var ctx = new GuiExecutionContext();
    var extractValues = new Dictionary<string, object>
    {
        ["srcDir"] = srcDir,
        ["outDir"] = outDir,
        ["entries"] = req.Entries,
    };
    if (!string.IsNullOrWhiteSpace(req.ScanDir)) extractValues["scanDir"] = req.ScanDir!;
    if (req.IncludeDirs is { Length: > 0 }) extractValues["includeDirs"] = req.IncludeDirs;
    if (req.Defines is { Length: > 0 }) extractValues["defines"] = req.Defines;
    if (req.IncludeUpper) extractValues["includeUpper"] = true;
    if (req.ManifestOnly) extractValues["manifestOnly"] = true;

    BrickOutput extractOut;
    try
    {
        extractOut = await extractor.ExecuteAsync(new BrickInput(extractValues), ImplementationType.Deterministic, ctx, ct);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "extraction failed for {SrcDir}", srcDir);
        return MapException(ex, "extraction failed");
    }

    var lower = extractOut.Get<string[]>("lower");
    var upper = extractOut.Get<string[]>("upper");
    var external = extractOut.Get<string[]>("external");
    var manifestText = extractOut.Get<string>("manifestText");
    var extractDict = extractOut.ToDictionary();
    var duplicatePath = extractDict.TryGetValue("duplicatePath", out var dp) ? (string)dp : null;

    if (req.ManifestOnly || req.ExtractOnly || duplicatePath is null)
    {
        return SafeJson(new RunResponse(lower, upper, external, manifestText, duplicatePath,
            null, null, null, null, null, extractOut.Summary ?? "", null, null, null));
    }

    var requireCompile = req.RequireCompile
        ?? string.Equals(Environment.GetEnvironmentVariable("COMPILE_GATE"), "1", StringComparison.Ordinal);
    var preferScaffold = req.PreferScaffold
        ?? string.Equals(Environment.GetEnvironmentVariable("PREFER_SCAFFOLD"), "1", StringComparison.Ordinal);
    var poco = Environment.GetEnvironmentVariable("POCO_CONTEXT");
    CppEvtxAdaptRunner.AdaptResult adapt;
    try
    {
        adapt = await CppEvtxAdaptRunner.RunAsync(
            generator,
            duplicatePath,
            req.Entries,
            Path.Combine(outDir, "adapted_reader.hpp"),
            preferScaffold,
            requireCompile,
            operatorGuidance: null,
            poco,
            req.MaxCompileRetries,
            ctx,
            ct);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "adaptation failed for {SrcDir}", srcDir);
        return MapException(ex, "adaptation failed", new Dictionary<string, object?>
        {
            ["lower"] = lower,
            ["upper"] = upper,
            ["external"] = external,
            ["manifestText"] = manifestText,
            ["duplicatePath"] = duplicatePath,
        });
    }

    return SafeJson(new RunResponse(
        lower, upper, external, manifestText, duplicatePath,
        adapt.AdapterCode,
        adapt.OutputPath,
        adapt.SourceApiCoverage,
        adapt.LooksLikeTemplateEcho,
        adapt.PossibleHallucinations,
        adapt.Summary,
        adapt.CompileOk,
        adapt.CompileLog,
        null));
});

/// <summary>Install a drafted adapter into Poco and optionally rebuild the custom evtx image.</summary>
app.MapPost("/api/install", async (InstallRequest req, InstallExecutor installer, CancellationToken ct) =>
{
    try
    {
        CompanionFileDto[]? companions = null;
        if (req.CompanionFiles is { Length: > 0 })
        {
            companions = req.CompanionFiles
                .Where(c => c is not null)
                .Select(c => new CompanionFileDto(c.Name, c.Content))
                .ToArray();
        }

        var work = new InstallWorkRequest(
            req.AdapterCode,
            req.AdapterPath,
            req.PocoContext,
            req.RebuildImage,
            req.ImageTag,
            companions,
            req.PlanId,
            req.RecreateEvtx,
            req.Smoke,
            req.WaitHealthy,
            req.SmokeExtractFile);

        PlanSession? Resolve(string id) => TryGetPlan(plans, id, out var s) ? s : null;

        if (req.Async == true)
        {
            var job = installer.Start(work, Resolve, ct);
            return Results.Accepted($"/api/install/{job.Id}", new InstallJobAccepted(job.Id, job.Status));
        }

        var result = await installer.RunSyncAsync(work, Resolve, ct);
        if (!result.Ok)
        {
            var code = (result.Error ?? "").Contains("compile gate", StringComparison.OrdinalIgnoreCase)
                       || (result.Error ?? "").Contains("docker build failed", StringComparison.OrdinalIgnoreCase)
                ? "build_failed"
                : "install_failed";
            return ApiError(502, code, result.Error ?? "install failed", new Dictionary<string, object?>
            {
                ["progressLog"] = result.ProgressLog,
                ["buildLog"] = result.BuildLog,
                ["recreateLog"] = result.RecreateLog,
            });
        }

        return SafeJson(new InstallResponse(
            result.AdaptedReaderPath,
            result.ImageTag,
            result.BuildLog,
            result.ParserUiUrl,
            result.Summary,
            result.CompanionPaths,
            result.RecreateLog,
            null,
            result.SmokeRan,
            result.SmokeOk,
            result.SmokeDetail,
            result.ProgressLog.ToArray()));
    }
    catch (Exception ex)
    {
        return MapException(ex, "install failed");
    }
});

/// <summary>
/// One-call onboarding: outline → entry inference → extract → Qt triage →
/// draft/gate/install → golden-file oracle. Samples turn the oracle on, which
/// substitutes for human review of model drafts.
/// </summary>
app.MapPost("/api/onboard", async (OnboardApiRequest req, OnboardLoop onboard, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(req.SrcDir))
            return ApiError(400, "invalid_input", "srcDir is required");

        PlanSession? Resolve(string id) => TryGetPlan(plans, id, out var s) ? s : null;
        void Save(PlanSession s)
        {
            plans[s.PlanId] = s;
            PlanSessionStore.Save(s);
        }

        var result = await onboard.RunAsync(
            new OnboardLoop.Request(
                req.SrcDir, req.Entries, req.Samples, req.Model,
                req.OutlineFiles ?? 0, req.MinEvents ?? 0,
                req.MaxBuildRetries ?? 2, req.MaxCompileRetries ?? 3,
                req.PreferScaffold ?? true),
            Resolve, Save, ct).ConfigureAwait(false);
        return SafeJson(result, statusCode: result.Ok ? 200 : 422);
    }
    catch (Exception ex)
    {
        return MapException(ex, "onboard failed");
    }
});

/// <summary>
/// Privacy-preserving project outline: local-model responsibility prose over a
/// deterministic dependency inventory, mechanically redacted for sharing.
/// </summary>
app.MapPost("/api/outline", async (OutlineRequest req, CppProjectOutlineBrick outliner, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(req.SrcDir))
            return ApiError(400, "invalid_input", "srcDir is required");
        var values = new Dictionary<string, object> { ["srcDir"] = req.SrcDir };
        if (!string.IsNullOrWhiteSpace(req.Model)) values["model"] = req.Model!;
        if (req.MaxLlmFiles is { } mlf) values["maxLlmFiles"] = mlf;

        var result = await outliner.ExecuteAsync(
            new BrickInput(values), ImplementationType.Agentic, new GuiExecutionContext(), ct);
        var dict = result.ToDictionary();
        return SafeJson(new OutlineResponse(
            true,
            dict.TryGetValue("outlinePath", out var p) ? p as string : null,
            dict.TryGetValue("outlineMarkdown", out var m) ? m as string : null,
            dict.TryGetValue("filesScanned", out var fs) ? Convert.ToInt32(fs) : 0,
            dict.TryGetValue("classesFound", out var cf) ? Convert.ToInt32(cf) : 0,
            dict.TryGetValue("llmFiles", out var lf) ? Convert.ToInt32(lf) : 0,
            dict.TryGetValue("redactionCount", out var rc) ? Convert.ToInt32(rc) : 0));
    }
    catch (Exception ex)
    {
        return MapException(ex, "outline failed");
    }
});

/// <summary>
/// Implement → install → docker build → optional extract smoke, with bounded retries on build/extract failure.
/// </summary>
app.MapPost("/api/adapt-cycle", async (AdaptCycleRequest req, AdaptInstallLoop loop, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(req.PlanId))
            return ApiError(400, "invalid_input", "planId is required");

        PlanSession? Resolve(string id) => TryGetPlan(plans, id, out var s) ? s : null;
        void Save(PlanSession s)
        {
            plans[s.PlanId] = s;
            PlanSessionStore.Save(s);
        }

        var result = await loop.RunAsync(
            new AdaptInstallLoop.Request(
                req.PlanId,
                req.PocoContext,
                req.RebuildImage,
                req.ImageTag,
                req.RecreateEvtx,
                req.SmokeExtractFile,
                req.MaxBuildRetries,
                req.PreferScaffold,
                req.RequireCompile,
                req.MaxCompileRetries,
                req.Model,
                req.ConfirmModelReview),
            Resolve,
            Save,
            ct).ConfigureAwait(false);

        var dto = new AdaptCycleResponse(
            result.Ok,
            result.PlanId,
            result.AdapterCode,
            result.AdaptedReaderPath,
            result.ImageTag,
            result.Summary,
            result.Error,
            result.Attempts.Select(a => new AdaptCycleAttemptDto(
                a.Index, a.ImplementOk, a.AdapterCode, a.CompileOk, a.CompileLog,
                a.InstallOk, a.BuildLog, a.SmokeDetail, a.Error)).ToArray());

        if (!result.Ok)
            return ApiError(502, "adapt_cycle_failed", result.Error ?? "adapt-cycle failed", new Dictionary<string, object?>
            {
                ["planId"] = result.PlanId,
                ["attempts"] = dto.Attempts,
                ["adapterCode"] = result.AdapterCode,
                ["buildLog"] = result.Attempts.LastOrDefault()?.BuildLog,
            });

        return SafeJson(dto);
    }
    catch (Exception ex)
    {
        return MapException(ex, "adapt-cycle failed");
    }
});

app.MapGet("/api/install/{jobId}", (string jobId, InstallExecutor installer) =>
{
    var job = installer.Get(jobId);
    if (job is null)
        return ApiError(404, "not_found", $"install job not found: {jobId}");
    IReadOnlyList<string> log;
    lock (job.Log) log = job.Log.ToList();
    return SafeJson(new InstallJobStatus(
        job.Id,
        job.Status,
        job.Error,
        log.ToArray(),
        job.Result is null
            ? null
            : new InstallResponse(
                job.Result.AdaptedReaderPath,
                job.Result.ImageTag,
                job.Result.BuildLog,
                job.Result.ParserUiUrl,
                job.Result.Summary,
                job.Result.CompanionPaths,
                job.Result.RecreateLog,
                job.Id,
                job.Result.SmokeRan,
                job.Result.SmokeOk,
                job.Result.SmokeDetail,
                job.Result.ProgressLog.ToArray()),
        job.StartedAt,
        job.FinishedAt));
});

app.Run(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:5237");

static PreparedRun PrepareRun(string? srcDirReq, string[]? entries)
{
    if (string.IsNullOrWhiteSpace(srcDirReq))
        return new PreparedRun(ApiError(400, "invalid_input", "srcDir is required"), null, null);
    if (!Directory.Exists(srcDirReq))
        return new PreparedRun(ApiError(404, "not_found", $"srcDir not found: {srcDirReq}"), null, null);
    if (entries is null || entries.Length == 0)
        return new PreparedRun(ApiError(400, "invalid_input", "at least one entry file is required"), null, null);

    string srcDir;
    try { srcDir = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(srcDirReq, "srcDir"); }
    catch (Exception ex) { return new PreparedRun(MapException(ex, "invalid srcDir"), null, null); }

    var tmpBase = Environment.GetEnvironmentVariable("TMPDIR");
    if (string.IsNullOrWhiteSpace(tmpBase))
    {
        tmpBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } ws
            ? Path.Combine(ws, ".dep-extract-out")
            : Path.GetTempPath();
    }
    Directory.CreateDirectory(tmpBase);
    var outDir = Path.Combine(tmpBase, "dep-extract-gui-" + Guid.NewGuid());
    try { DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(outDir, "outDir"); }
    catch (Exception ex) { return new PreparedRun(MapException(ex, "invalid outDir"), null, null); }

    return new PreparedRun(null, srcDir, outDir);
}

static Dictionary<string, object> BuildExtractValues(
    string srcDir, string outDir, string[] entries, string? scanDir, string[]? includeDirs, string[]? defines,
    bool includeUpper, bool manifestOnly)
{
    var extractValues = new Dictionary<string, object>
    {
        ["srcDir"] = srcDir,
        ["outDir"] = outDir,
        ["entries"] = entries,
    };
    if (!string.IsNullOrWhiteSpace(scanDir)) extractValues["scanDir"] = scanDir!;
    if (includeDirs is { Length: > 0 }) extractValues["includeDirs"] = includeDirs;
    if (defines is { Length: > 0 }) extractValues["defines"] = defines;
    if (includeUpper) extractValues["includeUpper"] = true;
    if (manifestOnly) extractValues["manifestOnly"] = true;
    return extractValues;
}

static bool TryGetPlan(ConcurrentDictionary<string, PlanSession> plans, string planId, out PlanSession? session)
{
    if (plans.TryGetValue(planId, out session))
        return true;
    session = PlanSessionStore.TryLoad(planId);
    if (session is null) return false;
    plans[planId] = session;
    return true;
}

static PlanResponse ToPlanResponse(PlanSession s) => new(
    s.PlanId,
    s.Status,
    s.Draft.Title,
    s.Draft.StructureSummary,
    s.Draft.AdaptationSummary,
    s.Draft.Steps.ToArray(),
    s.Draft.Strategy,
    s.Draft.DiagramMermaid,
    s.Draft.Diagram,
    s.Draft.Classes.Select(c => new PlanClassDto(c.Name, c.Methods)).ToArray(),
    s.Draft.Risks.Select(r => new PlanRiskDto(r.Code, r.Severity, r.Message)).ToArray(),
    s.Draft.Constraints.Select(c => new PlanConstraintDto(c.Kind, c.Text)).ToArray(),
    s.Lower, s.Upper, s.External, s.ManifestText, s.DuplicatePath,
    s.Feedback.ToArray(),
    s.CreatedAt,
    "Review risks and constraints. Approve to implement, or revise with Must / Must not feedback.",
    $"/api/plan/{s.PlanId}/export.md");

static IResult ApiError(int status, string code, string detail, IDictionary<string, object?>? extras = null) =>
    SafeJson(new ErrorResponse(code, detail, extras), statusCode: status);

static IResult MapException(Exception ex, string title, IDictionary<string, object?>? extras = null)
{
    if (ex is DepExtractException de)
        return ApiError(de.SuggestedStatusCode, de.Code, de.Message, extras);

    return ex switch
    {
        ArgumentException or ArgumentNullException => ApiError(400, "invalid_input", ex.Message, extras),
        DirectoryNotFoundException or FileNotFoundException => ApiError(404, "not_found", ex.Message, extras),
        OperationCanceledException => ApiError(499, "cancelled", "Request was cancelled.", extras),
        _ => ApiError(500, "internal_error", $"{title}: {ex.Message}", extras)
    };
}

static async Task<DockerStatus> CheckDockerAsync(CancellationToken ct)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList = { "version", "--format", "{{.Server.Version}}" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p is null) return new DockerStatus(false, "failed to start docker process");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await p.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return new DockerStatus(false, "docker version timed out");
        }
        var stdout = await p.StandardOutput.ReadToEndAsync(ct);
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        if (p.ExitCode != 0)
            return new DockerStatus(false, string.IsNullOrWhiteSpace(stderr) ? $"exit {p.ExitCode}" : stderr.Trim());
        return new DockerStatus(true, string.IsNullOrWhiteSpace(stdout) ? "ok" : stdout.Trim());
    }
    catch (Exception ex)
    {
        return new DockerStatus(false, ex.Message);
    }
}

static async Task<DockerStatus> CheckDepExtractImageAsync(CancellationToken ct)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList = { "image", "inspect", "dep-extract:latest", "--format", "{{.Id}}" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p is null) return new DockerStatus(false, "failed to start docker");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try { await p.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return new DockerStatus(false, "image inspect timed out");
        }
        if (p.ExitCode != 0)
            return new DockerStatus(false, "dep-extract:latest image missing — build tools/dep_extract");
        var id = (await p.StandardOutput.ReadToEndAsync(ct)).Trim();
        return new DockerStatus(true, string.IsNullOrWhiteSpace(id) ? "present" : id);
    }
    catch (Exception ex)
    {
        return new DockerStatus(false, ex.Message);
    }
}

static List<WorkspaceEntry> ListWorkspace(string dir, string root, int maxDepth, int depth)
{
    var list = new List<WorkspaceEntry>();
    IEnumerable<string> dirs;
    IEnumerable<string> files;
    try
    {
        dirs = Directory.EnumerateDirectories(dir).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        files = Directory.EnumerateFiles(dir).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
    }
    catch
    {
        return list;
    }

    foreach (var d in dirs.Take(200))
    {
        var name = Path.GetFileName(d);
        if (name is "bin" or "obj" or ".git" or "node_modules") continue;
        var rel = Path.GetRelativePath(root, d).Replace('\\', '/');
        list.Add(new WorkspaceEntry(rel, name, true, null));
        if (depth < maxDepth)
            list.AddRange(ListWorkspace(d, root, maxDepth, depth + 1));
    }
    foreach (var f in files.Take(400))
    {
        var name = Path.GetFileName(f);
        var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
        list.Add(new WorkspaceEntry(rel, name, false, new FileInfo(f).Length));
    }
    return list;
}

sealed record DockerStatus(bool Ok, string Detail);
sealed record StatusResponse(
    bool Docker, bool Ollama, string? DockerDetail, string? OllamaError,
    string? WorkspaceRoot, string? PocoContext, bool ExtractReady, bool AdaptReady,
    string? DepExtractImageDetail,
    string? ParserUiUrl, bool CompileGateDefault, bool PreferScaffoldDefault,
    int ParserHostPort = PortSettingsStore.DefaultParserHostPort,
    int AgentHostPort = PortSettingsStore.DefaultAgentHostPort,
    string? AgentUiUrl = null,
    string? PortsEnvFile = null,
    string? EvtxImage = null,
    string? ComposeProject = null,
    string? ComposeFile = null,
    bool StackIdentityOk = false,
    string? StackIdentityDetail = null);

sealed record PipelineResponse(
    string ComposeProject,
    string? ComposeFile,
    string? EnvFile,
    string? PipelineMd,
    int ParserHostPort,
    int AgentHostPort,
    string ParserUiUrl,
    string AgentUiUrl,
    string? EvtxImage,
    string[] AllowedServices);

sealed record PortsRequest(int? ParserHostPort, int? AgentHostPort);

sealed record PortsAutoRequest(int? PreferParserHostPort, int? PreferAgentHostPort);

sealed record PortsRecreateRequest(
    bool? WithExtractAgent,
    bool? EvtxOnly,
    string[]? Services,
    bool? DryRun = null,
    bool? WaitHealthy = null);

sealed record PortsRecreateResponse(
    bool Ok,
    string Log,
    string Project,
    string ComposeFile,
    string EnvFile,
    string ParserUiUrl,
    string AgentUiUrl,
    string[]? Services = null,
    string? Command = null,
    bool DryRun = false,
    bool WaitHealthy = false,
    bool ReloadAgentSuggested = false,
    string? HealthLog = null);

sealed record PortsResponse(
    int ParserHostPort,
    int AgentHostPort,
    string ParserUiUrl,
    string AgentUiUrl,
    string? EnvFile,
    string RestartHint);

sealed record PortsSuggestResponse(
    int ParserHostPort,
    int AgentHostPort,
    string ParserUiUrl,
    string AgentUiUrl,
    string? EnvFile,
    string RestartHint,
    int PreferredParserHostPort,
    int PreferredAgentHostPort,
    bool ChangedFromPreferred,
    string Summary,
    int[] BusySample,
    bool Saved);
sealed record ErrorResponse(string Error, string Detail, IDictionary<string, object?>? Extras = null);
sealed record WorkspaceEntry(string Path, string Name, bool IsDir, long? SizeBytes);
sealed record WorkspaceResponse(string WorkspaceRoot, string RelativePath, IReadOnlyList<WorkspaceEntry> Entries);

sealed record RunRequest(
    string SrcDir,
    string[] Entries,
    string? ScanDir,
    string[]? IncludeDirs,
    string[]? Defines,
    bool IncludeUpper,
    bool ManifestOnly,
    bool ExtractOnly,
    string? Model,
    int? MaxRetries,
    bool? RequireCompile,
    bool? PreferScaffold,
    int? MaxCompileRetries = null);

sealed record RunResponse(
    string[] Lower,
    string[] Upper,
    string[] External,
    string ManifestText,
    string? DuplicatePath,
    string? AdapterCode,
    string? AdapterOutputPath,
    double? SourceApiCoverage,
    bool? LooksLikeTemplateEcho,
    string[]? PossibleHallucinations,
    string Summary,
    bool? CompileOk,
    string? CompileLog,
    string? PlanId = null);

sealed record PreparedRun(IResult? Error, string? SrcDir, string? OutDir);

sealed record CompanionFile(string Name, string Content);

sealed record InstallRequest(
    string? AdapterCode,
    string? AdapterPath,
    string? PocoContext,
    bool RebuildImage,
    string? ImageTag,
    CompanionFile[]? CompanionFiles = null,
    string? PlanId = null,
    bool? RecreateEvtx = null,
    bool? Smoke = null,
    bool? WaitHealthy = null,
    bool? Async = null,
    string? SmokeExtractFile = null);

sealed record OnboardApiRequest(
    string SrcDir,
    string[]? Entries = null,
    string[]? Samples = null,
    string? Model = null,
    int? OutlineFiles = null,
    int? MinEvents = null,
    int? MaxBuildRetries = null,
    int? MaxCompileRetries = null,
    bool? PreferScaffold = null);

sealed record OutlineRequest(
    string SrcDir,
    string? Model = null,
    int? MaxLlmFiles = null);

sealed record OutlineResponse(
    bool Ok,
    string? OutlinePath,
    string? OutlineMarkdown,
    int FilesScanned,
    int ClassesFound,
    int LlmFiles,
    int RedactionCount);

sealed record AdaptCycleRequest(
    string PlanId,
    string? PocoContext = null,
    bool RebuildImage = true,
    string? ImageTag = null,
    bool RecreateEvtx = true,
    string? SmokeExtractFile = null,
    int MaxBuildRetries = 2,
    bool? PreferScaffold = null,
    bool? RequireCompile = null,
    int? MaxCompileRetries = null,
    string? Model = null,
    bool ConfirmModelReview = false);

sealed record AdaptCycleAttemptDto(
    int Index,
    bool ImplementOk,
    string? AdapterCode,
    bool? CompileOk,
    string? CompileLog,
    bool? InstallOk,
    string? BuildLog,
    string? SmokeDetail,
    string? Error);

sealed record AdaptCycleResponse(
    bool Ok,
    string PlanId,
    string? AdapterCode,
    string? AdaptedReaderPath,
    string? ImageTag,
    string? Summary,
    string? Error,
    AdaptCycleAttemptDto[] Attempts);

sealed record InstallResponse(
    string AdaptedReaderPath,
    string? ImageTag,
    string? BuildLog,
    string ParserUiUrl,
    string Summary,
    string[]? CompanionPaths = null,
    string? RecreateLog = null,
    string? JobId = null,
    bool SmokeRan = false,
    bool? SmokeOk = null,
    string? SmokeDetail = null,
    string[]? ProgressLog = null);

sealed record InstallJobAccepted(string JobId, string Status);

sealed record InstallJobStatus(
    string JobId,
    string Status,
    string? Error,
    string[] Log,
    InstallResponse? Result,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt);

sealed class GuiExecutionContext : IExecutionContext
{
    public string AgentId => "dep-extract-agent";
    public string BehaviorId => "run";
    public bool IsAirGapped => true;
    public bool AuditMode => false;
    public string Provider => "ollama";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}

/// <summary>Exposes the top-level-statement Program to WebApplicationFactory&lt;Program&gt; for integration tests.</summary>
public partial class Program { }
