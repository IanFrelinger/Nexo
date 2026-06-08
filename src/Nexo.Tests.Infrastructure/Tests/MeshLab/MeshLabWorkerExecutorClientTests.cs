using FluentAssertions;
using Nexo.Infrastructure.MeshLab;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.MeshLab;

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
    [InlineData("Assigned", MeshLabTaskStatus.Assigned, true)]
    [InlineData("1", MeshLabTaskStatus.Assigned, true)]
    [InlineData("Running", MeshLabTaskStatus.Assigned, false)]
    public void IsStatus_matches_string_or_numeric(string status, MeshLabTaskStatus expected, bool match)
    {
        MeshLabWorkerExecutorClient.IsStatus(status, expected).Should().Be(match);
    }
}
