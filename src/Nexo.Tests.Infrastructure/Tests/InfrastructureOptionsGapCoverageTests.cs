using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Infrastructure.MeshLab;
using Nexo.Infrastructure.Metrics;
using Nexo.Infrastructure.ModelArtifacts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests;

public class InfrastructureOptionsGapCoverageTests
{
    [Fact]
    public void MeshLabWorkerExecutorOptions_exposes_defaults()
    {
        MeshLabWorkerExecutorOptions.SectionPath.Should().Be("Nexo:MeshLab:WorkerExecutor");
        new MeshLabWorkerExecutorOptions
        {
            Enabled = true,
            DirectorBaseUrl = "http://director:8080",
            ApiKey = "key",
            TaskNamePrefix = "lab",
            PollIntervalMs = 250,
            ExecuteBrickOnAssignedPeer = false,
            ResultSummary = "done",
        }.PollIntervalMs.Should().Be(250);
    }

    [Fact]
    public void DockerOllamaModelArtifactCatalogOptions_exposes_defaults()
    {
        DockerOllamaModelArtifactCatalogOptions.SectionName
            .Should().Be("Nexo:ModelArtifactCatalog:DockerOllama");
        new DockerOllamaModelArtifactCatalogOptions
        {
            Enabled = false,
            OllamaImagePrefix = "custom/ollama",
            OllamaContainerPort = 8080,
        }.OllamaContainerPort.Should().Be(8080);
    }

    [Fact]
    public async Task OpenTelemetryMetricsCollector_records_metrics()
    {
        var collector = new OpenTelemetryMetricsCollector(NullLogger<OpenTelemetryMetricsCollector>.Instance);
        OpenTelemetryMetricsCollector.MeterName.Should().Be("Nexo");

        collector.RecordExecutionTime("test-op", TimeSpan.FromMilliseconds(12));
        collector.IncrementCounter("test-counter", 2);

        var snapshot = await collector.GetSnapshotAsync();
        snapshot.CollectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        snapshot.ExecutionTimes.Should().BeEmpty();
    }

    [Fact]
    public void OpenTelemetryMetricsCollector_implements_metrics_port()
    {
        typeof(OpenTelemetryMetricsCollector).Should().Implement<IMetricsCollector>();
    }
}
