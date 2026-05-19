using FluentAssertions;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Infrastructure.Fleet.MeshLab;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class MeshLabWorkerExecutorClientTests
{
    [Fact]
    public void PickCandidate_returns_assigned_task_matching_prefix()
    {
        var tasks = new[]
        {
            new MeshLabTaskSnapshot("a", "mesh-lab-verify-task", "Assigned", "http://peer-b:8080", "tok1"),
            new MeshLabTaskSnapshot("b", "mesh-lab-worker-exec-task", "Assigned", "http://peer-b:8080", "tok2"),
        };

        var picked = MeshLabWorkerExecutorClient.PickCandidate(tasks, "mesh-lab-worker-exec");
        picked.Should().NotBeNull();
        picked!.TaskId.Should().Be("b");
    }

    [Theory]
    [InlineData("Assigned", MeshTaskStatus.Assigned, true)]
    [InlineData("1", MeshTaskStatus.Assigned, true)]
    [InlineData("Running", MeshTaskStatus.Assigned, false)]
    public void IsStatus_matches_string_or_numeric(string status, MeshTaskStatus expected, bool match)
    {
        MeshLabWorkerExecutorClient.IsStatus(status, expected).Should().Be(match);
    }
}
