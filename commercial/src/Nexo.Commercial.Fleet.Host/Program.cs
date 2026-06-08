using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Nexo.API.Endpoints;
using Nexo.API.Middleware.Ingress;
using Nexo.API.Security;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Testing;
using Nexo.Commercial.Fleet.Api;
using Nexo.Contracts;
using Nexo.Core.Application.Middleware.Ports;
using Nexo.Hosting;
using Nexo.Ingress.AwsSns;
using Nexo.Ingress.DynamoDb;
using Nexo.Runtime;
using Nexo.Transport.Grpc;

var builder = WebApplication.CreateBuilder(args);

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

var disableObservationPipeline =
    builder.Configuration.GetValue("Nexo:DisableObservationPipeline", defaultValue: false);

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
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICopilotSubmissionQuota, CopilotSubmissionQuota>();
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
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo Commercial Fleet Host", Version = "v1" }));
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
    options.DisableObservationPipeline = disableObservationPipeline;
    options.DisableOpenFleetDirector = true;
});

builder.Services.AddNexoCommercialFleetDirector(
    builder.Configuration,
    includeKnowledgeReplication: !disableObservationPipeline);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RecordSmsYesApprovalCommand).Assembly));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IngressEnvelopeMiddleware>();
app.UseWebSockets();
app.UseNexoMeshCorrelation();
app.UseNexoMeshSecurity();
app.UseNexoApiKeyAuth();
app.UseNexoCopilotScopedAuthorization();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI(static c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexo Commercial Fleet Host v1"));

app.MapNexoEndpoints();
app.MapNexoCommercialFleetEndpoints();
app.MapIngressEndpoints();

app.Run();

/// <summary>Entry point type for integration tests and hosting.</summary>
public partial class FleetHostProgram;
