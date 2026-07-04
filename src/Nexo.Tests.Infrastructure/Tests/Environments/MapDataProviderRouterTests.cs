using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using Nexo.Infrastructure.Environments;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Environments;

/// <summary>Tests for map data provider router.</summary>
public sealed class MapDataProviderRouterTests
{
    /// <summary>Tests for http vector.</summary>
    private sealed class HttpVector : IVectorMapDataProvider
    {
        public string Kind => MapDataSourceKinds.Http;
        public Task<VectorMapDataResult> FetchAsync(
            MapDataGeographicBounds bounds,
            MapDataSourceBinding binding,
            MapDataRequestContext context,
            string formatHint,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new VectorMapDataResult(ReadOnlyMemory<byte>.Empty, "application/octet-stream"));
    }

    /// <summary>Tests for overpass vector.</summary>
    private sealed class OverpassVector : IVectorMapDataProvider
    {
        public string Kind => MapDataSourceKinds.Overpass;
        public Task<VectorMapDataResult> FetchAsync(
            MapDataGeographicBounds bounds,
            MapDataSourceBinding binding,
            MapDataRequestContext context,
            string formatHint,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new VectorMapDataResult(ReadOnlyMemory<byte>.Empty, "application/octet-stream"));
    }

    [Fact]
    public void Router_resolves_by_binding_kind()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IVectorMapDataProvider, HttpVector>();
        services.AddSingleton<IVectorMapDataProvider, OverpassVector>();
        services.AddMapDataProviderRouting();
        var sp = services.BuildServiceProvider();

        var router = sp.GetRequiredService<IMapDataProviderRouter>();
        router.ResolveVector(new MapDataSourceBinding(Kind: MapDataSourceKinds.Overpass))
            .Should().BeOfType<OverpassVector>();
        router.ResolveVector(new MapDataSourceBinding(Kind: MapDataSourceKinds.Http))
            .Should().BeOfType<HttpVector>();
    }

    [Fact]
    public void Duplicate_kind_throws_at_router_construction()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IVectorMapDataProvider, HttpVector>();
        services.AddSingleton<IVectorMapDataProvider, HttpVector>();
        services.AddMapDataProviderRouting();

        var act = () => services.BuildServiceProvider().GetRequiredService<IMapDataProviderRouter>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate*");
    }
}
