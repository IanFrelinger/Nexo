using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Infrastructure.MeshLab;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.MeshLab;

public sealed class MeshLabServiceCollectionExtensionsGapCoverageTests
{
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
}
