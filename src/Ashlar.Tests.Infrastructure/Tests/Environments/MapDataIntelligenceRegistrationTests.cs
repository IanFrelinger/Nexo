using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Environments.Ports;
using Ashlar.Infrastructure.Environments;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Environments;

/// <summary>Tests for map data intelligence registration.</summary>
public sealed class MapDataIntelligenceRegistrationTests
{
    [Fact]
    public void ReplaceMapIntelligenceWithModelBackedDefaults_resolves_concrete_types()
    {
        var model = new Mock<IModel>();
        var services = new ServiceCollection();
        services.AddSingleton<IModel>(model.Object);
        services.AddMapDataProviderRouting();
        services.ReplaceMapIntelligenceWithModelBackedDefaults();
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IVectorMapIntelligenceService>().Should().BeOfType<ModelBackedVectorMapIntelligenceService>();
        sp.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<ModelBackedMaterialIntelligenceService>();
        sp.GetRequiredService<IMapVerificationService>().Should().BeOfType<OsmSharpMapVerificationService>();
    }
}
