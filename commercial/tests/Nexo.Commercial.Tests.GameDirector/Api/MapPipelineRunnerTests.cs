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

    private sealed class EmptyGeoJsonHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string json = """{"type":"FeatureCollection","features":[]}""";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/geo+json")
            });
        }
    }

    private sealed class Png1x1Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(png)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
                }
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
    public async Task RunAsync_FetchTerrain_IncludesPngParse_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.MaxFetchResponseBytes = 4096;
            o.EnableTerrainPayloadParsing = true;
            o.AllowMapFetchWhenAllowedHostsEmpty = false;
            o.AllowedMapFetchHosts = ["example.com"];
        });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new Png1x1Handler());
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
            ["fetch_terrain"],
            []);

        var result = await runner.RunAsync(plan, new MapPipelineRunRequest(
            DryRun: false,
            TimeoutMs: 5000,
            TerrainDataUrl: "https://example.com/t.png"));

        result.Success.Should().BeTrue();
        var terr = result.Stages.Should().Contain(s => s.Stage == "fetch_terrain" && s.Status == "ok").Subject;
        terr.Detail.Should().Contain("format=png");
        terr.Detail.Should().Contain("parse=png:");
        terr.Detail.Should().Contain("1x1");
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

    [Fact]
    public async Task RunAsync_FetchVector_Error_When_MapVerificationFailsPipeline_And_VerificationWarning()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.MaxFetchResponseBytes = 4096;
            o.MapVerificationFailsPipeline = true;
            o.EnableMapVerification = true;
            o.EnableVectorPayloadParsing = true;
            o.EnableVectorIntelligence = false;
            o.AllowMapFetchWhenAllowedHostsEmpty = false;
            o.AllowedMapFetchHosts = ["example.com"];
        });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new EmptyGeoJsonHandler());
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
            VectorDataUrl: "https://example.com/empty.geojson"));

        result.Success.Should().BeFalse();
        var vec = result.Stages.Should().Contain(s => s.Stage == "fetch_vector").Subject;
        vec.Status.Should().Be("error");
        vec.Detail.Should().Contain("verify=");
    }

    [Fact]
    public async Task RunAsync_FetchVector_Ok_When_MapVerificationFailsPipeline_False_EmptyGeoJson()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.MaxFetchResponseBytes = 4096;
            o.MapVerificationFailsPipeline = false;
            o.EnableMapVerification = true;
            o.EnableVectorPayloadParsing = true;
            o.EnableVectorIntelligence = false;
            o.AllowMapFetchWhenAllowedHostsEmpty = false;
            o.AllowedMapFetchHosts = ["example.com"];
        });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new EmptyGeoJsonHandler());
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
            VectorDataUrl: "https://example.com/empty.geojson"));

        result.Success.Should().BeTrue();
        result.Stages.Single(s => s.Stage == "fetch_vector").Status.Should().Be("ok");
    }
}
