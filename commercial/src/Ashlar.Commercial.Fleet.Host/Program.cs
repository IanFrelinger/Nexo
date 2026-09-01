using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Ashlar.API.Endpoints;
using Ashlar.API.Middleware.Ingress;
using Ashlar.API.Security;
using Ashlar.BackgroundAgents.Extending;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.BackgroundAgents.Optimization;
using Ashlar.BackgroundAgents.Testing;
using Ashlar.Commercial.Fleet.Api;
using Ashlar.Contracts;
using Ashlar.Core.Application.Middleware.Ports;
using Ashlar.Hosting;
using Ashlar.Ingress.AwsSns;
using Ashlar.Ingress.DynamoDb;
using Ashlar.Runtime;
using Ashlar.Transport.Grpc;

var builder = WebApplication.CreateBuilder(args);

var agentsConfigPath = Environment.GetEnvironmentVariable("ASHLAR_BACKGROUND_AGENTS_CONFIG");
if (!string.IsNullOrWhiteSpace(agentsConfigPath))
{
    var raw = agentsConfigPath.Trim();
    var resolved = Path.IsPathRooted(raw)
        ? raw
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, raw));
    if (!File.Exists(resolved))
    {
        throw new InvalidOperationException(
            $"ASHLAR_BACKGROUND_AGENTS_CONFIG file not found: {resolved}");
    }

    builder.Configuration.AddJsonFile(resolved, optional: false, reloadOnChange: true);
}

var disableObservationPipeline =
    builder.Configuration.GetValue("Ashlar:DisableObservationPipeline", defaultValue: false);

builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<GrpcTransportOptions>(
    builder.Configuration.GetSection("Ashlar:GrpcTransport"));
builder.Services.Configure<AshlarSecurityOptions>(
    builder.Configuration.GetSection(AshlarSecurityOptions.SectionPath));
builder.Services.Configure<AshlarProductOptions>(
    builder.Configuration.GetSection(AshlarProductOptions.SectionPath));
builder.Services.Configure<AshlarEntitlementsOptions>(
    builder.Configuration.GetSection(AshlarEntitlementsOptions.SectionPath));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICopilotSubmissionQuota, CopilotSubmissionQuota>();
builder.Services.Configure<MeshSecurityOptions>(
    builder.Configuration.GetSection(MeshSecurityOptions.SectionPath));
builder.Services.Configure<SmsIngressDynamoDbOptions>(
    builder.Configuration.GetSection(SmsIngressDynamoDbOptions.SectionPath));
builder.Services.AddOptions<AshlarMiddlewareIngressOptions>()
    .Bind(builder.Configuration.GetSection(AshlarMiddlewareIngressOptions.SectionPath))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AshlarMiddlewareIngressOptions>, ValidateAshlarMiddlewareIngressOptions>();

var smsIngressPreview = builder.Configuration.GetSection(AshlarMiddlewareIngressOptions.SectionPath)
    .Get<AshlarMiddlewareIngressOptions>() ?? new AshlarMiddlewareIngressOptions();
if (string.Equals(smsIngressPreview.SmsIngressApprovalStore, SmsIngressApprovalStoreKind.DynamoDb, StringComparison.OrdinalIgnoreCase))
    builder.Services.AddDynamoDbSmsIngressApprovalStore();
else
    builder.Services.AddSingleton<ISmsIngressApprovalStore, MemorySmsIngressApprovalStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ashlar Commercial Fleet Host", Version = "v1" }));
builder.Services.AddAshlarRuntimeRouting(builder.Configuration);

builder.Services.AddSingleton<IAshlarIngressAccessor, HttpAshlarIngressAccessor>();
builder.Services.AddHttpClient("ashlar-sns-signing", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<ISnsSignatureVerifier, SnsRsaSignatureVerifier>();
builder.Services.AddRateLimiter(static o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = static (_, _) => ValueTask.CompletedTask;
    o.AddPolicy<string>("ashlar-sms-ingress-posts", static httpContext =>
    {
        var opts = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<AshlarMiddlewareIngressOptions>>().CurrentValue;
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

builder.Services.AddAshlar(options =>
{
    options.PatternStorePath = builder.Configuration["Ashlar:PatternStorePath"];
    options.RegisterBackgroundAgentHostedService =
        builder.Configuration.GetValue("Ashlar:RegisterBackgroundAgentHostedService", defaultValue: true);
    options.DisableObservationPipeline = disableObservationPipeline;
});

builder.Services.AddAshlarCommercialFleetDirector(
    builder.Configuration,
    includeKnowledgeReplication: !disableObservationPipeline);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RecordSmsYesApprovalCommand).Assembly));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IngressEnvelopeMiddleware>();
app.UseWebSockets();
app.UseAshlarMeshCorrelation();
app.UseAshlarMeshSecurity();
app.UseAshlarApiKeyAuth();
app.UseAshlarCopilotScopedAuthorization();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI(static c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ashlar Commercial Fleet Host v1"));

app.MapAshlarEndpoints();
app.MapAshlarCommercialFleetEndpoints();
app.MapIngressEndpoints();

app.Run();

/// <summary>Entry point type for integration tests and hosting.</summary>
public partial class FleetHostProgram;
