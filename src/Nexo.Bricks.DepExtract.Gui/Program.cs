using System.Diagnostics;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProviderFactory, ProviderFactory>();
builder.Services.AddSingleton<CppDependencyExtractorBrick>();
builder.Services.AddSingleton<CppParserAdapterBrick>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", async (IProviderFactory pf) =>
{
    var docker = await CheckDockerAsync();
    var ollama = false;
    try { await pf.EnsureOllamaReachableAsync(requireVisionModel: false); ollama = true; } catch { /* reported as unreachable below */ }
    return Results.Json(new { docker, ollama });
});

app.MapPost("/api/run", async (RunRequest req, CppDependencyExtractorBrick extractor, CppParserAdapterBrick adapter) =>
{
    if (string.IsNullOrWhiteSpace(req.SrcDir) || !Directory.Exists(req.SrcDir))
        return Results.BadRequest(new { error = $"srcDir not found: {req.SrcDir}" });
    if (req.Entries is null || req.Entries.Length == 0)
        return Results.BadRequest(new { error = "at least one entry file is required" });

    var outDir = Path.Combine(Path.GetTempPath(), "dep-extract-gui-" + Guid.NewGuid());
    var ctx = new GuiExecutionContext();

    var extractValues = new Dictionary<string, object>
    {
        ["srcDir"] = req.SrcDir,
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
        extractOut = await extractor.ExecuteAsync(new BrickInput(extractValues), ImplementationType.Deterministic, ctx);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500, title: "extraction failed");
    }

    var lower = extractOut.Get<string[]>("lower");
    var upper = extractOut.Get<string[]>("upper");
    var external = extractOut.Get<string[]>("external");
    var manifestText = extractOut.Get<string>("manifestText");
    var extractDict = extractOut.ToDictionary();
    var duplicatePath = extractDict.TryGetValue("duplicatePath", out var dp) ? (string)dp : null;

    if (req.ManifestOnly || duplicatePath is null)
    {
        return Results.Json(new RunResponse(lower, upper, external, manifestText, duplicatePath,
            null, null, null, null, null, extractOut.Summary ?? ""));
    }

    var adaptValues = new Dictionary<string, object>
    {
        ["parserDir"] = duplicatePath,
        ["entryFiles"] = req.Entries,
        ["outputPath"] = Path.Combine(outDir, "adapted_reader.hpp"),
    };
    if (!string.IsNullOrWhiteSpace(req.Model)) adaptValues["model"] = req.Model!;
    if (req.MaxRetries is { } mr) adaptValues["maxRetries"] = mr;

    BrickOutput adaptOut;
    try
    {
        adaptOut = await adapter.ExecuteAsync(new BrickInput(adaptValues), ImplementationType.Agentic, ctx);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500, title: "adaptation failed",
            extensions: new Dictionary<string, object?> { ["lower"] = lower, ["upper"] = upper, ["manifestText"] = manifestText });
    }

    return Results.Json(new RunResponse(
        lower, upper, external, manifestText, duplicatePath,
        adaptOut.Get<string>("adapterCode"),
        adaptOut.Get<string>("outputPath"),
        adaptOut.Get<double>("sourceApiCoverage"),
        adaptOut.Get<bool>("looksLikeTemplateEcho"),
        adaptOut.Get<string[]>("possibleHallucinations"),
        adaptOut.Summary ?? ""));
});

app.Run("http://localhost:5237");

static async Task<bool> CheckDockerAsync()
{
    try
    {
        var psi = new ProcessStartInfo { FileName = "docker", ArgumentList = { "version" }, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi);
        if (p is null) return false;
        await p.WaitForExitAsync();
        return p.ExitCode == 0;
    }
    catch { return false; }
}

sealed record RunRequest(
    string SrcDir,
    string[] Entries,
    string? ScanDir,
    string[]? IncludeDirs,
    string[]? Defines,
    bool IncludeUpper,
    bool ManifestOnly,
    string? Model,
    int? MaxRetries);

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
    string Summary);

sealed class GuiExecutionContext : IExecutionContext
{
    public string AgentId => "dep-extract-gui";
    public string BehaviorId => "run";
    public bool IsAirGapped => true;
    public bool AuditMode => false;
    public string Provider => "ollama";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}

/// <summary>Exposes the top-level-statement Program to WebApplicationFactory&lt;Program&gt; for integration tests.</summary>
public partial class Program { }
