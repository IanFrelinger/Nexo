using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Infrastructure.MeshLab;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.MeshLab;

public sealed class MeshLabServiceCollectionExtensionsTests
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
        services.Should().NotContain(d => d.ServiceType == typeof(MeshLabWorkerExecutorBackgroundService));
    }

    [Fact]
    public void AddNexoMeshLabWorkerExecutor_registers_executor_when_enabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:MeshLab:WorkerExecutor:Enabled"] = "true",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoMeshLabWorkerExecutor(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(MeshLabWorkerExecutorClient));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(MeshLabWorkerExecutorBackgroundService));
    }

    [Fact]
    public void MeshLabWorkerExecutorOptions_exposes_section_path_and_defaults()
    {
        var options = new MeshLabWorkerExecutorOptions
        {
            Enabled = true,
            DirectorBaseUrl = "http://director:8080",
            ApiKey = "key",
            TaskNamePrefix = "lab",
            PollIntervalMs = 250,
            ExecuteBrickOnAssignedPeer = false,
            ResultSummary = "done",
        };

        MeshLabWorkerExecutorOptions.SectionPath.Should().Be("Nexo:MeshLab:WorkerExecutor");
        options.DirectorBaseUrl.Should().Be("http://director:8080");
        options.TaskNamePrefix.Should().Be("lab");
        options.PollIntervalMs.Should().Be(250);
        options.ExecuteBrickOnAssignedPeer.Should().BeFalse();
        options.ResultSummary.Should().Be("done");
    }
}
