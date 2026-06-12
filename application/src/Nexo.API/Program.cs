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
//   • Mesh correlation — UseNexoMeshCorrelation assigns / echoes X-Nexo-Correlation-Id
//     for /api/mesh and brick execute (Phase 3).
//   • MeshSecurityOptions — optional Nexo:Security:Mesh tokens, body size cap,
//     and rate limits for /api/mesh and POST /api/bricks/*/execute (Phase 2);
//     UseNexoMeshSecurity runs before UseNexoApiKeyAuth.
//   • Static files + endpoints — DefaultFiles/StaticFiles serve the SPA;
//     MapNexoEndpoints wires the API; MapFallbackToFile routes unknown paths
//     to index.html for client-side routing.
//
// Environment variables consumed here:
//   NEXO_BACKGROUND_AGENTS_CONFIG — path to agent-definition JSON file
//   (see also NexoSecurityOptions for auth-related env vars)
// ──────────────────────────────────────────────────────────────────────────────

using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediatR;
using Microsoft.OpenApi.Models;
using Nexo.API.Endpoints;
using Nexo.API.Middleware.Ingress;
using Nexo.API.Security;
using Nexo.Core.Application.Middleware.Ports;
using Nexo.Core.Application.Product.Ports;
using Nexo.Infrastructure.Product;
using Nexo.Contracts;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Testing;
using Nexo.Hosting;
using Nexo.Ingress.AwsSns;
using Nexo.Ingress.DynamoDb;
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
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<GrpcTransportOptions>(
    builder.Configuration.GetSection("Nexo:GrpcTransport"));
builder.Services.Configure<NexoSecurityOptions>(
    builder.Configuration.GetSection(NexoSecurityOptions.SectionPath));
builder.Services.Configure<NexoProductOptions>(
    builder.Configuration.GetSection(NexoProductOptions.SectionPath));
builder.Services.Configure<NexoEntitlementsOptions>(
    builder.Configuration.GetSection(NexoEntitlementsOptions.SectionPath));
builder.Services.Configure<NexoPrivateLicenseOptions>(
    builder.Configuration.GetSection(NexoPrivateLicenseOptions.SectionPath));
builder.Services.AddSingleton<IPrivateLicenseValidator, PrivateLicenseValidator>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICopilotSubmissionQuota, CopilotSubmissionQuota>();
builder.Services.AddSingleton<ITenantUsageStore, InMemoryTenantUsageStore>();
builder.Services.AddSingleton<IOrganizationStore, InMemoryOrganizationStore>();
builder.Services.Configure<MeshSecurityOptions>(
    builder.Configuration.GetSection(MeshSecurityOptions.SectionPath));
builder.Services.Configure<SmsIngressDynamoDbOptions>(
    builder.Configuration.GetSection(SmsIngressDynamoDbOptions.SectionPath));
builder.Services.AddOptions<NexoMiddlewareIngressOptions>()
    .Bind(builder.Configuration.GetSection(NexoMiddlewareIngressOptions.SectionPath))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<NexoMiddlewareIngressOptions>, ValidateNexoMiddlewareIngressOptions>();

var smsIngressPreview = builder.Configuration.GetSection(NexoMiddlewareIngressOptions.SectionPath)
    .Get<NexoMiddlewareIngressOptions>() ?? new NexoMiddlewareIngressOptions();
if (string.Equals(smsIngressPreview.SmsIngressApprovalStore, SmsIngressApprovalStoreKind.DynamoDb, StringComparison.OrdinalIgnoreCase))
    builder.Services.AddDynamoDbSmsIngressApprovalStore();
else
    builder.Services.AddSingleton<ISmsIngressApprovalStore, MemorySmsIngressApprovalStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo.API", Version = "v1" }));
builder.Services.AddNexoRuntimeRouting(builder.Configuration);

builder.Services.AddSingleton<INexoIngressAccessor, HttpNexoIngressAccessor>();
builder.Services.AddHttpClient("nexo-sns-signing", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<ISnsSignatureVerifier, SnsRsaSignatureVerifier>();
builder.Services.AddRateLimiter(static o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = static (_, _) => ValueTask.CompletedTask;
    o.AddPolicy<string>("nexo-sms-ingress-posts", static httpContext =>
    {
        var opts = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<NexoMiddlewareIngressOptions>>().CurrentValue;
        if (opts.IngressSmsPostRateLimitPermitLimit <= 0)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                "off",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = int.MaxValue,
                    Window = TimeSpan.FromDays(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                });
        }

        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        var windowSeconds = opts.IngressSmsPostRateLimitWindowSeconds > 0 ? opts.IngressSmsPostRateLimitWindowSeconds : 60;
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = opts.IngressSmsPostRateLimitPermitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
});

// Planner / optimizer / tester background agents need the same runners as `nexo background-agent daemon`.
builder.Services.TryAddSingleton<ICodeAnalysisRunner, CodeAnalysisRunnerAdapter>();
builder.Services.TryAddSingleton<ITestRunRunner, TestRunRunnerAdapter>();
builder.Services.TryAddSingleton<SelfExtendRunnerAdapter>();
builder.Services.TryAddSingleton<ISelfExtendRunner>(sp =>
    sp.GetRequiredService<SelfExtendRunnerAdapter>());

builder.Services.AddNexo(options =>
{
    options.PatternStorePath = builder.Configuration["Nexo:PatternStorePath"];
    options.RegisterBackgroundAgentHostedService =
        builder.Configuration.GetValue("Nexo:RegisterBackgroundAgentHostedService", defaultValue: true);
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RecordSmsYesApprovalCommand).Assembly));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IngressEnvelopeMiddleware>();
app.UseWebSockets();

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
app.UseNexoMeshCorrelation();
app.UseNexoMeshSecurity();
app.UseNexoApiKeyAuth();
app.UsePrivateLicenseGate();
app.UseNexoCopilotScopedAuthorization();

app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI(static c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexo.API v1"));
app.MapNexoEndpoints();
app.MapIngressEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>
/// Exposes the implicit Program entry point for ASP.NET Core integration tests (<c>WebApplicationFactory&lt;Program&gt;</c>).
/// </summary>
public partial class Program
{
}
