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
//   • Security — NexoSecurityOptions controls an exposure profile
//     (Public / Lan / Tailnet / Localhost). It does NOT enforce network policy,
//     but any off-loopback profile with AuthorizationMode=None refuses to start
//     unless AllowUnauthenticatedNetworkExposure=true is set explicitly.
//     Optional built-in auth modes (ApiKey, Bearer, Basic, and composite
//     OR-modes) protect mutating endpoints via UseNexoApiKeyAuth middleware.
//   • Remote execution — /api/execution/* (container build/run for
//     RemoteExecutionPlatform) is mapped only when
//     Nexo:Execution:ServeRemoteExecution=true and refuses AuthorizationMode=None.
//   • Mesh correlation — UseNexoMeshCorrelation assigns / echoes X-Nexo-Correlation-Id
//     for /api/mesh and brick execute (Phase 3).
//   • MeshSecurityOptions — optional Nexo:Security:Mesh tokens, body size cap,
//     and rate limits for /api/mesh and POST /api/bricks/*/execute (Phase 2);
//     UseNexoMeshSecurity runs before UseNexoApiKeyAuth.
//   • Static files + endpoints — DefaultFiles/StaticFiles serve the SPA;
//     MapNexoEndpoints wires the API; MapFallbackToFile routes unknown paths
//     to index.html for client-side routing.
//   • Observability — console logging is human-readable by default and switches
//     to JSON lines with Nexo:Logging:Json=true / NEXO_LOG_JSON=1. Metrics stay
//     in-process (MemoryMetricsCollector) unless OTEL_EXPORTER_OTLP_ENDPOINT is
//     set, in which case AddNexoOpenTelemetry exports the "Nexo" meter plus
//     ASP.NET Core / HttpClient traces and metrics over OTLP.
//
// Environment variables consumed here:
//   NEXO_BACKGROUND_AGENTS_CONFIG — path to agent-definition JSON file
//   NEXO_LOG_JSON                 — 1/true = JSON console logging (also Nexo:Logging:Json)
//   OTEL_EXPORTER_OTLP_ENDPOINT   — when set, enables OTLP trace + metric export
//                                   (OTEL_SERVICE_NAME etc. are honoured by the OTel SDK)
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
using Nexo.Abstractions;
using Nexo.API.Protocols;
using Nexo.Hosting;
using Nexo.Hosting.Sdk.Extensions;
using Nexo.Ingress.AwsSns;
using Nexo.Ingress.DynamoDb;
using Nexo.Mcp.Client;
using Nexo.Mcp.Server;
using Nexo.Runtime;
using Nexo.Tools.Dev;
using Nexo.Transport.A2A;
using Nexo.Transport.A2A.Server;
using Nexo.Transport.Grpc;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

// --- Logging: console by default; JSON lines when Nexo:Logging:Json=true or NEXO_LOG_JSON=1 ---
// Read through builder.Configuration (not the kernel's env-only options binding) so the key
// works from appsettings, environment variables and UseSetting alike.
builder.Services.AddLogging(b => b.AddConsole().AddNexoJsonConsoleIfRequested(builder.Configuration));
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<GrpcTransportOptions>(
    builder.Configuration.GetSection("Nexo:GrpcTransport"));
builder.Services.Configure<NexoSecurityOptions>(
    builder.Configuration.GetSection(NexoSecurityOptions.SectionPath));
builder.Services.Configure<NexoExecutionOptions>(
    builder.Configuration.GetSection(NexoExecutionOptions.SectionPath));
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

// --- Agent-protocol adapters (MCP + A2A) ---
// All four directions are feature-flagged off by default (Nexo:Mcp:*, Nexo:A2A:*); registering
// them here only makes the surfaces *available*. AddNexoA2ATransport must run before AddNexo so
// its scheme registration participates in the kernel's remote-transport composition.
builder.Services.AddNexoMcpServer(builder.Configuration).WithHttpTransport();
builder.Services.AddNexoMcpClient(builder.Configuration);
builder.Services.AddNexoA2AServer(builder.Configuration);
builder.Services.AddNexoA2ATransport(builder.Configuration);
builder.Services.AddSingleton<INexoA2AAgentCatalog, AgentRegistryA2ACatalog>();
// Mirror of the A2A card-anonymity decision for the auth middleware (see
// NexoProtocolIngressOptions) — bound from the same section so there is a single source key.
builder.Services.Configure<NexoProtocolIngressOptions>(
    builder.Configuration.GetSection(NexoA2AServerOptions.SectionPath));
// Read-only repo tools available for MCP allowlisting (exposure still requires
// Nexo:Mcp:Server:ExposedToolIds entries); mutating tools are deliberately not pre-registered.
builder.Services.AddSingleton<ITool, RepoFsReadTool>();
builder.Services.AddSingleton<ITool, RepoFsListTool>();

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
    o.AddPolicy<string>("nexo-mcp", static httpContext =>
    {
        var opts = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<NexoMiddlewareIngressOptions>>().CurrentValue;
        return BuildPerIpFixedWindowPartition(httpContext, opts.McpRateLimitPermitLimit, opts.McpRateLimitWindowSeconds);
    });
    o.AddPolicy<string>("nexo-a2a", static httpContext =>
    {
        var opts = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<NexoMiddlewareIngressOptions>>().CurrentValue;
        return BuildPerIpFixedWindowPartition(httpContext, opts.A2ARateLimitPermitLimit, opts.A2ARateLimitWindowSeconds);
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

// --- Observability: OTLP export (traces + metrics), enabled only by the standard OTel endpoint var ---
// Must run after AddNexo: AddNexoOpenTelemetry replaces the in-process MemoryMetricsCollector
// (the default when the endpoint is unset) with the OpenTelemetry-backed collector, whose "Nexo"
// meter carries the ncr.* / nexo.* keys as attributes on nexo.operation.duration / .count.
// The exporter batches in the background, so an unreachable endpoint never fails startup.
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    var otelServiceName = builder.Configuration["OTEL_SERVICE_NAME"];
    builder.Services.AddNexoOpenTelemetry(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            serviceName: string.IsNullOrWhiteSpace(otelServiceName) ? "Nexo.API" : otelServiceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter());
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IngressEnvelopeMiddleware>();
app.UseWebSockets();

// --- Security: exposure profile (fails closed off-loopback) + optional built-in auth ---
{
    var sec = app.Services.GetRequiredService<IOptions<NexoSecurityOptions>>().Value;
    var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Nexo.Security");
    var hasConfiguredAuthMode = Enum.TryParse<NexoAuthorizationMode>(sec.AuthorizationMode, true, out var authMode)
        && authMode != NexoAuthorizationMode.None;
    // The legacy flag counts as "auth configured": with no key it now fails closed (401) instead of open.
    var hasAnyBuiltInAuth = hasConfiguredAuthMode || sec.RequireApiKeyForMutatingEndpoints;

    if (Enum.TryParse<NexoExposureProfile>(sec.ExposureProfile, true, out var prof))
    {
        var offLoopback = prof is NexoExposureProfile.Lan or NexoExposureProfile.Tailnet or NexoExposureProfile.Public;
        if (offLoopback && !hasAnyBuiltInAuth)
        {
            // Off-loopback with AuthorizationMode=None means every mutating route under /api (and the
            // opt-in execution surface) would answer unauthenticated callers on the network. Refuse to
            // start unless the operator states that something in front of Nexo.API authenticates.
            if (!sec.AllowUnauthenticatedNetworkExposure)
            {
                throw new InvalidOperationException(
                    $"Nexo:Security:ExposureProfile is '{prof}' but no built-in auth is configured (Nexo:Security:AuthorizationMode=None). " +
                    "Refusing to start: mutating routes under /api would be reachable from the network without credentials. " +
                    "Set Nexo:Security:AuthorizationMode (ApiKey, BearerToken, Basic, ...) plus the matching credential, " +
                    "or set Nexo:Security:AllowUnauthenticatedNetworkExposure=true only when an authenticating proxy or network ACL fronts this host " +
                    "(see SECURITY.md, 'Default posture and in-scope surfaces').");
            }

            log.LogWarning(
                "ExposureProfile is {Profile} with no built-in auth and AllowUnauthenticatedNetworkExposure=true: mutating routes under /api are unauthenticated. Ensure an authenticating proxy or network ACL fronts Nexo.API.",
                prof);
        }

        if (prof == NexoExposureProfile.Public)
        {
            log.LogWarning(
                "ExposureProfile is Public: use TLS and authentication in front of Nexo.API; the profile does not enforce network policy.");
        }
        else if (prof is NexoExposureProfile.Lan or NexoExposureProfile.Tailnet)
        {
            log.LogInformation("ExposureProfile is {Profile}: review docs for firewall / ACL guidance.", prof);
        }
    }

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
            "Nexo API key auth is required for mutating endpoints, but no API key is configured. Mutating endpoints will reject every request (401) until Nexo:Security:ApiKey is set.");
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

// --- Swagger (OpenAPI document + UI): on in Development, otherwise opt-in via Nexo:Api:EnableSwagger ---
// The document enumerates every mapped route and schema; keep it off the network by default and let
// operators turn it on explicitly (Nexo__Api__EnableSwagger=true) when they front the host with auth.
{
    var enableSwagger = app.Configuration.GetValue<bool?>("Nexo:Api:EnableSwagger") ?? app.Environment.IsDevelopment();
    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(static c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexo.API v1"));
        if (!app.Environment.IsDevelopment())
        {
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Nexo.Security")
                .LogInformation("Swagger UI is enabled outside Development (Nexo:Api:EnableSwagger=true): /swagger exposes the full route catalogue.");
        }
    }
}

app.MapNexoEndpoints();
app.MapIngressEndpoints();

// --- Agent-protocol endpoints (map nothing while disabled; see IngressCatalog rows) ---
// Both live under /api so NexoApiKeyAuthMiddleware protects them (all verbs — see
// ShouldProtect's protocol-path handling); the root agent card is the /.well-known exception
// handled explicitly there. NO AllowAnonymous anywhere on these surfaces.
app.MapNexoMcpEndpoint()?.RequireRateLimiting("nexo-mcp");
var a2aEndpoints = app.MapNexoA2AEndpoints();
if (a2aEndpoints is not null)
{
    foreach (var rpcEndpoint in a2aEndpoints.RpcEndpoints)
    {
        rpcEndpoint.RequireRateLimiting("nexo-a2a");
    }

    foreach (var cardEndpoint in a2aEndpoints.CardEndpoints)
    {
        cardEndpoint.RequireRateLimiting("nexo-a2a");
    }
}

app.MapFallbackToFile("index.html");

app.Run();

static RateLimitPartition<string> BuildPerIpFixedWindowPartition(
    HttpContext httpContext, int permitLimit, int windowSeconds)
{
    if (permitLimit <= 0)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            "off",
            static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = int.MaxValue,
                Window = TimeSpan.FromDays(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    }

    var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    var window = windowSeconds > 0 ? windowSeconds : 60;
    return RateLimitPartition.GetFixedWindowLimiter(
        key,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(window),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
}

/// <summary>
/// Exposes the implicit Program entry point for ASP.NET Core integration tests (<c>WebApplicationFactory&lt;Program&gt;</c>).
/// </summary>
public partial class Program
{
}
