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

    [Fact]
    public async Task RunAsync_FetchesVectorBytes_WhenUrlProvided()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ForgeSessionOptions>(o =>
        {
            o.MaxFetchResponseBytes = 1024;
            o.EnableVectorIntelligence = false;
            o.AllowMapFetchWhenAllowedHostsEmpty = false;
            o.AllowedMapFetchHosts = ["example.com"];
        });
        services.AddHttpClient("forge-map")
            .ConfigurePrimaryHttpMessageHandler(() => new OkHandler());
        services.AddSingleton<HeuristicVectorMapIntelligenceService>();
        services.AddSingleton<IVectorMapIntelligenceService>(sp => sp.GetRequiredService<HeuristicVectorMapIntelligenceService>());
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
        result.Stages.Should().Contain(s => s.Stage == "fetch_vector" && s.Status == "ok");
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
