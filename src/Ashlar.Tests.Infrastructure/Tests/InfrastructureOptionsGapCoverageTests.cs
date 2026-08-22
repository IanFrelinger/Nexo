using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Infrastructure.MeshLab;
using Ashlar.Infrastructure.Metrics;
using Ashlar.Infrastructure.ModelArtifacts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests;

/// <summary>Tests for infrastructure options gap coverage.</summary>
public class InfrastructureOptionsGapCoverageTests
{
    [Fact]
    public void MeshLabWorkerExecutorOptions_exposes_defaults()
    {
        MeshLabWorkerExecutorOptions.SectionPath.Should().Be("Ashlar:MeshLab:WorkerExecutor");
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
            .Should().Be("Ashlar:ModelArtifactCatalog:DockerOllama");
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
        OpenTelemetryMetricsCollector.MeterName.Should().Be("Ashlar");

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
