// ──────────────────────────────────────────────────────────────────────────────
// Nexo.API — Self-hosted API + SPA portal host
//
// This is the HTTP entry-point for Nexo. It serves:
//   1. A REST API (mapped via MapNexoEndpoints) for orchestration, analysis,
//      configuration, and background-agent management.
//   2. A static SPA portal (wwwroot/index.html) via UseStaticFiles +
//      MapFallbackToFile, so the UI is co-located with the API.
//
// Startup sequence:
//   • Optional agent config merge — if NEXO_BACKGROUND_AGENTS_CONFIG is set,
//     the JSON file is merged into the host configuration so that background-
//     agent definitions (schedules, models, policies) can live in a separate
//     file managed independently of appsettings.json.
//   • DI registration — AddNexo (shared with the CLI host) registers all
//     kernel services. The key difference from the CLI: the API sets
//     RegisterBackgroundAgentHostedService = true, which causes background
//     agents to run as IHostedService instances inside this process rather
//     than in a standalone daemon.
//   • Security — NexoSecurityOptions controls an advisory exposure profile
//     (Public / Lan / Tailnet / Local) that emits log warnings but does NOT
//     enforce network policy. Optional built-in auth modes (ApiKey, Bearer,
//     Basic, and composite OR-modes) protect mutating endpoints via
//     UseNexoApiKeyAuth middleware.
//   • Static files + endpoints — DefaultFiles/StaticFiles serve the SPA;
//     MapNexoEndpoints wires the API; MapFallbackToFile routes unknown paths
//     to index.html for client-side routing.
//
// Environment variables consumed here:
//   NEXO_BACKGROUND_AGENTS_CONFIG — path to agent-definition JSON file
//   (see also NexoSecurityOptions for auth-related env vars)
// ──────────────────────────────────────────────────────────────────────────────

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.API.Endpoints;
using Nexo.API.Security;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Testing;
using Nexo.Hosting;
using Nexo.Runtime;
using Nexo.Transport.Grpc;

var builder = WebApplication.CreateBuilder(args);

// --- Optional agent config merge ---
var agentsConfigPath = Environment.GetEnvironmentVariable("NEXO_BACKGROUND_AGENTS_CONFIG");
if (!string.IsNullOrWhiteSpace(agentsConfigPath))
{
    var raw = agentsConfigPath.Trim();
    var resolved = Path.IsPathRooted(raw)
        ? raw
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, raw));
    if (!File.Exists(resolved))
    {
        throw new InvalidOperationException(
            $"NEXO_BACKGROUND_AGENTS_CONFIG file not found: {resolved}");
    }

    builder.Configuration.AddJsonFile(resolved, optional: false, reloadOnChange: true);
}

builder.Services.AddLogging(b => b.AddConsole());
builder.Services.Configure<GrpcTransportOptions>(
    builder.Configuration.GetSection("Nexo:GrpcTransport"));
builder.Services.Configure<NexoSecurityOptions>(
    builder.Configuration.GetSection(NexoSecurityOptions.SectionPath));
builder.Services.AddNexoRuntimeRouting(builder.Configuration);

// Planner / optimizer / tester background agents need the same runners as `nexo background-agent daemon`.
builder.Services.TryAddSingleton<ICodeAnalysisRunner, CodeAnalysisRunnerAdapter>();
builder.Services.TryAddSingleton<ITestRunRunner, TestRunRunnerAdapter>();
builder.Services.TryAddSingleton<SelfExtendRunnerAdapter>();
builder.Services.TryAddSingleton<ISelfExtendRunner>(sp =>
    sp.GetRequiredService<SelfExtendRunnerAdapter>());

builder.Services.AddNexo(options =>
{
    options.PatternStorePath = builder.Configuration["Nexo:PatternStorePath"];
    options.RegisterBackgroundAgentHostedService = true;
});

var app = builder.Build();

// --- Security: advisory exposure profile + optional built-in auth ---
{
    var sec = app.Services.GetRequiredService<IOptions<NexoSecurityOptions>>().Value;
    var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Nexo.Security");
    if (Enum.TryParse<NexoExposureProfile>(sec.ExposureProfile, true, out var prof))
    {
        if (prof == NexoExposureProfile.Public)
        {
            log.LogWarning(
                "ExposureProfile is Public: use TLS and authentication in front of Nexo.API; this setting is advisory only.");
        }
        else if (prof is NexoExposureProfile.Lan or NexoExposureProfile.Tailnet)
        {
            log.LogInformation("ExposureProfile is {Profile}: review docs for firewall / ACL guidance.", prof);
        }
    }

    var hasConfiguredAuthMode = Enum.TryParse<NexoAuthorizationMode>(sec.AuthorizationMode, true, out var authMode)
        && authMode != NexoAuthorizationMode.None;
    if (hasConfiguredAuthMode)
    {
        if (!Enum.TryParse<NexoAuthorizationScope>(sec.AuthorizationScope, true, out var authScope))
        {
            authScope = NexoAuthorizationScope.MutatingApi;
            log.LogWarning(
                "Nexo built-in auth scope '{Scope}' is invalid; defaulting to {DefaultScope}.",
                sec.AuthorizationScope,
                authScope);
        }

        log.LogInformation("Nexo built-in auth mode {Mode} is enabled with scope {Scope}.", authMode, authScope);
        if (sec.RequireApiKeyForMutatingEndpoints)
        {
            log.LogInformation(
                "Legacy RequireApiKeyForMutatingEndpoints is also set. Built-in auth mode {Mode} takes precedence.",
                authMode);
        }

        var missingConfig = authMode switch
        {
            NexoAuthorizationMode.ApiKey => string.IsNullOrWhiteSpace(sec.ApiKey),
            NexoAuthorizationMode.BearerToken => string.IsNullOrWhiteSpace(sec.BearerToken),
            NexoAuthorizationMode.Basic => string.IsNullOrWhiteSpace(sec.BasicAuthUsername) || string.IsNullOrWhiteSpace(sec.BasicAuthPassword),
            NexoAuthorizationMode.ApiKeyOrBearerToken => string.IsNullOrWhiteSpace(sec.ApiKey) && string.IsNullOrWhiteSpace(sec.BearerToken),
            NexoAuthorizationMode.ApiKeyOrBasic => string.IsNullOrWhiteSpace(sec.ApiKey)
                                                   && (string.IsNullOrWhiteSpace(sec.BasicAuthUsername) || string.IsNullOrWhiteSpace(sec.BasicAuthPassword)),
            NexoAuthorizationMode.BearerTokenOrBasic => string.IsNullOrWhiteSpace(sec.BearerToken)
                                                        && (string.IsNullOrWhiteSpace(sec.BasicAuthUsername) || string.IsNullOrWhiteSpace(sec.BasicAuthPassword)),
            NexoAuthorizationMode.Any => string.IsNullOrWhiteSpace(sec.ApiKey)
                                         && string.IsNullOrWhiteSpace(sec.BearerToken)
                                         && (string.IsNullOrWhiteSpace(sec.BasicAuthUsername) || string.IsNullOrWhiteSpace(sec.BasicAuthPassword)),
            _ => false
        };

        if (missingConfig)
        {
            log.LogWarning(
                "Nexo built-in auth mode {Mode} is enabled but required credentials are not fully configured. Protected routes will reject requests until configuration is complete.",
                authMode);
        }
    }
    else if (sec.RequireApiKeyForMutatingEndpoints && string.IsNullOrWhiteSpace(sec.ApiKey))
    {
        log.LogWarning(
            "Nexo API key auth is required for mutating endpoints, but no API key is configured. Mutating endpoints are effectively unauthenticated.");
    }
}

// --- Middleware pipeline: SPA static files → auth → API endpoints ---
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseNexoApiKeyAuth();

app.MapNexoEndpoints();
app.MapForgeEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
