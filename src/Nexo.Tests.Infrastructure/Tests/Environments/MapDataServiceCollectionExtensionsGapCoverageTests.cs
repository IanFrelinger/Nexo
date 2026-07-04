using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using Nexo.Infrastructure.Environments;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Environments;

/// <summary>Tests for map data service collection extensions gap coverage.</summary>
public sealed class MapDataServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddMapDataProviderRouting_registers_no_op_defaults()
    {
        var services = new ServiceCollection();
        services.AddMapDataProviderRouting();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMapDataProviderRouter>().Should().NotBeNull();
        provider.GetRequiredService<IVectorMapIntelligenceService>().Should().BeOfType<NoOpVectorMapIntelligenceService>();
        provider.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<NoOpMaterialIntelligenceService>();
        provider.GetRequiredService<IMapVerificationService>().Should().BeOfType<NoOpMapVerificationService>();
    }

    [Fact]
    public void ReplaceMapIntelligenceWithModelBackedDefaults_swaps_to_model_and_osm_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IModel>());
        services.AddMapDataProviderRouting();
        services.ReplaceMapIntelligenceWithModelBackedDefaults();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IVectorMapIntelligenceService>().Should().BeOfType<ModelBackedVectorMapIntelligenceService>();
        provider.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<ModelBackedMaterialIntelligenceService>();
        provider.GetRequiredService<IMapVerificationService>().Should().BeOfType<OsmSharpMapVerificationService>();
    }

    [Fact]
    public void ReplaceMapVerificationWithOsmSharp_swaps_only_verification_service()
    {
        var services = new ServiceCollection();
        services.AddMapDataProviderRouting();
        services.ReplaceMapVerificationWithOsmSharp();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<NoOpMaterialIntelligenceService>();
        provider.GetRequiredService<IMapVerificationService>().Should().BeOfType<OsmSharpMapVerificationService>();
    }
}
