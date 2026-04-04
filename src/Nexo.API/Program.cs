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
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapNexoEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
