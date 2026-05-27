using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Fleet.Ports;
using Nexo.Infrastructure.Fleet;
using Nexo.Infrastructure.Fleet.MeshLab;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class FleetServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddNexoFleetDirector_registers_in_memory_registries_by_default()
    {
        var services = new ServiceCollection();
        services.AddNexoFleetDirector();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IFleetNodeRegistry>().Should().BeOfType<InMemoryFleetNodeRegistry>();
        provider.GetRequiredService<IMeshTaskRegistry>().Should().BeOfType<InMemoryMeshTaskRegistry>();
        provider.GetRequiredService<IMeshTaskPlacementService>().Should().NotBeNull();
    }

    [Fact]
    public void AddNexoFleetDirector_registers_lite_db_registries_when_configured()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "nexo-fleet-di-" + Guid.NewGuid().ToString("N") + ".db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Mesh:Persistence:Provider"] = "LiteDb",
                ["Nexo:Mesh:Persistence:DatabasePath"] = dbPath,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddNexoFleetDirector(configuration);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IFleetNodeRegistry>().Should().BeOfType<LiteDbFleetNodeRegistry>();
        provider.GetRequiredService<IMeshTaskRegistry>().Should().BeOfType<LiteDbMeshTaskRegistry>();

        TryDelete(dbPath);
    }

    [Fact]
    public void AddNexoFleetDirector_throws_for_unknown_persistence_provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Mesh:Persistence:Provider"] = "Postgres",
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddNexoFleetDirector(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown mesh persistence provider*");
    }

    [Fact]
    public void AddNexoMeshLabWorkerExecutor_skips_registration_when_disabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:MeshLab:WorkerExecutor:Enabled"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddNexoMeshLabWorkerExecutor(configuration);

        services.Should().NotContain(d => d.ServiceType == typeof(MeshLabWorkerExecutorClient));
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
