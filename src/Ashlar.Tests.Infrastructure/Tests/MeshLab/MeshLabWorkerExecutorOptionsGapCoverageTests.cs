using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Infrastructure.MeshLab;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.MeshLab;

/// <summary>Tests for mesh lab worker executor options gap coverage.</summary>
public sealed class MeshLabWorkerExecutorOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_mesh_lab_worker_configuration()
    {
        var options = new MeshLabWorkerExecutorOptions();

        MeshLabWorkerExecutorOptions.SectionPath.Should().Be("Ashlar:MeshLab:WorkerExecutor");
        options.DirectorBaseUrl.Should().Be("http://127.0.0.1:18081");
        options.TaskNamePrefix.Should().Be("mesh-lab-worker-exec");
        options.PollIntervalMs.Should().Be(500);
        options.ExecuteBrickOnAssignedPeer.Should().BeTrue();
        options.ResultSummary.Should().Be("mesh-lab-worker-executor-complete");
    }
}
