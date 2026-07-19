using GameDirector.Agents;
using GameDirector.Bricks;
using GameDirector.Domain;
using GameDirector.Mcp;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Abstractions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using GameDirector.Mcp.Endpoints;
using GameDirector.Mcp.Forge;
using Nexo.API.Security;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Testing;
using Nexo.Client;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Commercial.GameDomain.Mapping;
using Nexo.Commercial.GameDomain.Materials;
using Nexo.API.Endpoints;
using Nexo.Hosting;
using Nexo.Hosting.Sdk.Extensions;
using Nexo.Hosting.Sdk.Options;

var builder = WebApplication.CreateBuilder(args);

var agentsConfigPath = Environment.GetEnvironmentVariable("NEXO_BACKGROUND_AGENTS_CONFIG")
    ?? "apps/game-director/agent_set.game_director.json";
if (!string.IsNullOrWhiteSpace(agentsConfigPath))
{
    var raw = agentsConfigPath.Trim();
    var resolved = Path.IsPathRooted(raw)
        ? raw
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, raw));
    if (File.Exists(resolved))
        builder.Configuration.AddJsonFile(resolved, optional: true, reloadOnChange: true);
}

builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForgeSessionOptions>(builder.Configuration.GetSection(ForgeSessionOptions.SectionPath));
builder.Services.Configure<NexoSecurityOptions>(builder.Configuration.GetSection(NexoSecurityOptions.SectionPath));
builder.Services.Configure<GameDirectorOptions>(builder.Configuration.GetSection(GameDirectorOptions.SectionName));
builder.Services.Configure<MeshSecurityOptions>(builder.Configuration.GetSection(MeshSecurityOptions.SectionPath));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static o => o.SwaggerDoc("v1", new() { Title = "Game Director Studio", Version = "v0.1" }));
builder.Services.AddHttpClient("forge-map");
builder.Services.AddSingleton<IForgeStateService, TenantPartitionedForgeStateService>();
builder.Services.TryAddSingleton<ICodeAnalysisRunner, CodeAnalysisRunnerAdapter>();
builder.Services.TryAddSingleton<ITestRunRunner, TestRunRunnerAdapter>();
builder.Services.TryAddSingleton<SelfExtendRunnerAdapter>();
builder.Services.TryAddSingleton<ISelfExtendRunner>(sp => sp.GetRequiredService<SelfExtendRunnerAdapter>());
builder.Services.TryAddSingleton<UnityWeaponLabGameRunner>();
builder.Services.TryAddSingleton<Nexo.Orchestration.Playtest.Ports.IGameRunner>(
    sp => sp.GetRequiredService<UnityWeaponLabGameRunner>());
builder.Services.TryAddSingleton<
    Nexo.BackgroundAgents.Playtesting.IPlaytestRunRunner,
    PlaytestRunRunnerAdapter>();
builder.Services.AddSingleton<HeuristicMaterialIntelligenceService>();
builder.Services.AddSingleton<IMaterialIntelligenceService>(sp =>
    new ModelAugmentedMaterialIntelligenceService(
        sp.GetRequiredService<Nexo.Abstractions.IModel>(),
        sp.GetRequiredService<HeuristicMaterialIntelligenceService>(),
        sp.GetRequiredService<IOptions<ForgeSessionOptions>>(),
        sp.GetRequiredService<ILogger<ModelAugmentedMaterialIntelligenceService>>()));
builder.Services.AddSingleton<HeuristicVectorMapIntelligenceService>();
builder.Services.AddSingleton<IMapVerificationService, HeuristicMapVerificationService>();
builder.Services.AddSingleton<IVectorMapIntelligenceService>(sp =>
    new ModelAugmentedVectorMapIntelligenceService(
        sp.GetRequiredService<Nexo.Abstractions.IModel>(),
        sp.GetRequiredService<HeuristicVectorMapIntelligenceService>(),
        sp.GetRequiredService<IOptions<ForgeSessionOptions>>(),
        sp.GetRequiredService<ILogger<ModelAugmentedVectorMapIntelligenceService>>()));
builder.Services.AddSingleton<MapPipelineRunner>();

builder.Services.AddNexoSdk(sdk => sdk
    .RegisterBrick<BalanceAnalysisBrick>()
    .RegisterBrick<MapFlowBrick>()
    .RegisterAgent<BalanceWatcherAgent>()
    .RegisterAgent<MapValidatorAgent>()
    .RegisterAgentCard(BalanceWatcherAgent.Card)
    .RegisterAgentCard(MapValidatorAgent.Card));

builder.Services.AddNexo(options =>
{
    options.PatternStorePath = builder.Configuration["Nexo:PatternStorePath"];
    options.RegisterBackgroundAgentHostedService =
        builder.Configuration.GetValue("Nexo:RegisterBackgroundAgentHostedService", false);
    options.DeploymentProfile = NexoDeploymentProfile.Server;
});

builder.Services.RemoveAll<IModel>();
builder.Services.AddSingleton<IModel, GameDirectorOfflineModel>();
builder.Services.AddSingleton<BalanceAnalysisBrick>();
builder.Services.AddSingleton<MapFlowBrick>();
builder.Services.AddSingleton<ContentGenerationBrick>();
builder.Services.RemoveAll<BrickRegistry>();
builder.Services.RemoveAll<IBrickRegistry>();
builder.Services.AddSingleton<BrickRegistry>(sp => new BrickRegistry([
    sp.GetRequiredService<BalanceAnalysisBrick>(),
    sp.GetRequiredService<MapFlowBrick>(),
    sp.GetRequiredService<ContentGenerationBrick>()
]));
builder.Services.AddSingleton<IBrickRegistry>(sp => sp.GetRequiredService<BrickRegistry>());

builder.Services.AddSingleton<IForgeSessionReader>(sp =>
{
    var forge = sp.GetRequiredService<IForgeStateService>();
    return new ForgeStateSessionReader(
        () => forge.Session.SessionId,
        () => forge.Session.AestheticPacks.FirstOrDefault()?.Id ?? forge.Session.Name);
});

builder.Services.AddHostedService<TrustPackActivatorHostedService>();
builder.Services.AddSingleton<IActivityFeedPublisher, AuditLogActivityFeedPublisher>();
builder.Services.AddHostedService<BalanceWatcherHostedService>();
builder.Services.AddHostedService<MapValidatorHostedService>();
builder.Services.AddGameDirectorMcp();

var apiKey = builder.Configuration["Nexo:Security:ApiKey"];
builder.Services.AddNexoClient(o =>
{
    o.BaseUrl = builder.Configuration["GameDirector:ApiBaseUrl"] ?? "http://127.0.0.1:8080";
    o.ApiKey = apiKey;
    o.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Nexo.Core.Application.Agent.UseCases.RunAgent.RunAgentCommand).Assembly));

Console.WriteLine("Game Director: building host...");
var app = builder.Build();
Console.WriteLine("Game Director: listening on {0}", builder.Configuration["ASPNETCORE_URLS"] ?? "http://127.0.0.1:8080");

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseNexoMeshCorrelation();
app.UseNexoMeshSecurity();
app.UseNexoApiKeyAuth();
app.UseNexoCopilotScopedAuthorization();
app.UseMiddleware<ForgeAuthenticationMiddleware>();
app.UseMiddleware<ForgeTenantMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Game Director Studio v0.1"));
app.MapNexoEndpoints();
app.MapForgeEndpoints();
app.MapMcpEndpoint("/mcp");
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Entry point type for integration tests and hosting.</summary>
public partial class GameDirectorProgram;

internal sealed class TrustPackActivatorHostedService : IHostedService
{
    private readonly ITrustPolicyPackRegistry? _registry;
    private readonly GameDirectorOptions _options;
    private readonly ILogger<TrustPackActivatorHostedService> _logger;

    public TrustPackActivatorHostedService(
        IOptions<GameDirectorOptions> options,
        ILogger<TrustPackActivatorHostedService> logger,
        ITrustPolicyPackRegistry? registry = null)
    {
        _options = options.Value;
        _logger = logger;
        _registry = registry;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_registry is null)
        {
            _logger.LogWarning("Trust policy pack registry not available");
            return;
        }

        try
        {
            var active = await _registry.ActivateAsync(_options.TrustPackId, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Activated trust pack {PackId}@{Version}", active.Id, active.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate trust pack {PackId}", _options.TrustPackId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
