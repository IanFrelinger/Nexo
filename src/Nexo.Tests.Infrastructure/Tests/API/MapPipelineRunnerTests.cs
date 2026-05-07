using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.API.Forge;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Mapping;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class MapPipelineRunnerTests
{
    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3, 4])
            });
        }
    }

    private sealed class JsonOkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[0,0]}}]}""";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/geo+json")
            });
        }
    }

    [Fact]
    public async Task RunAsync_FetchesVectorBytes_WhenUrlProvided()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.MaxFetchResponseBytes = 1024;
            o.EnableVectorIntelligence = false;
            o.EnableVectorPayloadParsing = true;
            o.AllowMapFetchWhenAllowedHostsEmpty = false;
            o.AllowedMapFetchHosts = ["example.com"];
        });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new OkHandler());
        services.AddSingleton<HeuristicVectorMapIntelligenceService>();
        services.AddSingleton<IVectorMapIntelligenceService>(sp => sp.GetRequiredService<HeuristicVectorMapIntelligenceService>());
        services.AddSingleton<IMapVerificationService, HeuristicMapVerificationService>();
        services.AddSingleton<IForgeStateService>(new InMemoryForgeStateService());
        services.AddSingleton<MapPipelineRunner>();

        await using var sp = services.BuildServiceProvider();
        var runner = sp.GetRequiredService<MapPipelineRunner>();

        var plan = new MapAdaptationPlan(
            MapRenderingProfiles.VoxelGrid,
            "voxel",
            ["fetch_vector", "emit_host_manifest"],
            []);

        var req = new MapPipelineRunRequest(
            DryRun: false,
            TimeoutMs: 5000,
            VectorDataUrl: "https://example.com/data.bin");

        var result = await runner.RunAsync(plan, req);
        result.Success.Should().BeTrue();
        var vec = result.Stages.Should().Contain(s => s.Stage == "fetch_vector" && s.Status == "ok").Subject;
        vec.Detail.Should().Contain("parse=skipped");
        vec.Detail.Should().Contain("verify=");
    }

    [Fact]
    public async Task RunAsync_IncludesGeoJsonParseSummary_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.MaxFetchResponseBytes = 4096;
            o.EnableVectorIntelligence = false;
            o.EnableVectorPayloadParsing = true;
            o.AllowMapFetchWhenAllowedHostsEmpty = false;
            o.AllowedMapFetchHosts = ["example.com"];
        });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new JsonOkHandler());
        services.AddSingleton<HeuristicVectorMapIntelligenceService>();
        services.AddSingleton<IVectorMapIntelligenceService>(sp => sp.GetRequiredService<HeuristicVectorMapIntelligenceService>());
        services.AddSingleton<IMapVerificationService, HeuristicMapVerificationService>();
        services.AddSingleton<IForgeStateService>(new InMemoryForgeStateService());
        services.AddSingleton<MapPipelineRunner>();
        await using var sp = services.BuildServiceProvider();
        var runner = sp.GetRequiredService<MapPipelineRunner>();

        var plan = new MapAdaptationPlan(
            MapRenderingProfiles.VoxelGrid,
            "voxel",
            ["fetch_vector"],
            []);

        var result = await runner.RunAsync(plan, new MapPipelineRunRequest(
            DryRun: false,
            TimeoutMs: 5000,
            VectorDataUrl: "https://example.com/tiles.geojson"));

        result.Stages.Single().Detail.Should().Contain("parse=geojson");
        result.Stages.Single().Detail.Should().Contain("verify=");
    }

    [Fact]
    public async Task RunAsync_SkipsFetch_WhenNoUrl()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o => { o.AllowMapFetchWhenAllowedHostsEmpty = true; });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new OkHandler());
        services.AddSingleton<HeuristicVectorMapIntelligenceService>();
        services.AddSingleton<IVectorMapIntelligenceService>(sp => sp.GetRequiredService<HeuristicVectorMapIntelligenceService>());
        services.AddSingleton<IMapVerificationService, HeuristicMapVerificationService>();
        services.AddSingleton<IForgeStateService>(new InMemoryForgeStateService());
        services.AddSingleton<MapPipelineRunner>();
        await using var sp = services.BuildServiceProvider();
        var runner = sp.GetRequiredService<MapPipelineRunner>();

        var plan = new MapAdaptationPlan(
            MapRenderingProfiles.VoxelGrid,
            "voxel",
            ["fetch_vector"],
            []);

        var result = await runner.RunAsync(plan, new MapPipelineRunRequest(DryRun: false, TimeoutMs: 5000));
        result.Success.Should().BeTrue();
        result.Stages.Single().Status.Should().Be("skipped");
    }
}
