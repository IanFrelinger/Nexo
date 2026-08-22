using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for fleet service collection extensions gap coverage.</summary>
public sealed class FleetServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddAshlarFleetDirector_registers_in_memory_registries_by_default()
    {
        var services = new ServiceCollection();
        services.AddAshlarFleetDirector();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IFleetNodeRegistry>().Should().BeOfType<InMemoryFleetNodeRegistry>();
        provider.GetRequiredService<IMeshTaskRegistry>().Should().BeOfType<InMemoryMeshTaskRegistry>();
        provider.GetRequiredService<IMeshTaskPlacementService>().Should().NotBeNull();
    }

    [Fact]
    public void AddAshlarFleetDirector_registers_lite_db_registries_when_configured()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "ashlar-fleet-di-" + Guid.NewGuid().ToString("N") + ".db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ashlar:Mesh:Persistence:Provider"] = "LiteDb",
                ["Ashlar:Mesh:Persistence:DatabasePath"] = dbPath,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddAshlarFleetDirector(configuration);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IFleetNodeRegistry>().Should().BeOfType<LiteDbFleetNodeRegistry>();
        provider.GetRequiredService<IMeshTaskRegistry>().Should().BeOfType<LiteDbMeshTaskRegistry>();

        /// <summary>Attempts to delete; returns false on failure.</summary>
        TryDelete(dbPath);
    }

    [Fact]
    public void AddAshlarFleetDirector_throws_for_unknown_persistence_provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ashlar:Mesh:Persistence:Provider"] = "Postgres",
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddAshlarFleetDirector(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown mesh persistence provider*");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
