using FluentAssertions;
using Ashlar.Infrastructure.MeshLab;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.MeshLab;

/// <summary>Tests for mesh lab task status.</summary>
public sealed class MeshLabTaskStatusTests
{
    [Theory]
    [InlineData(MeshLabTaskStatus.Pending, 0)]
    [InlineData(MeshLabTaskStatus.Assigned, 1)]
    [InlineData(MeshLabTaskStatus.Running, 2)]
    [InlineData(MeshLabTaskStatus.Succeeded, 3)]
    [InlineData(MeshLabTaskStatus.Failed, 4)]
    public void Enum_values_match_director_http_status_codes(MeshLabTaskStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
        MeshLabWorkerExecutorClient.IsStatus(expected.ToString(), status).Should().BeTrue();
    }
}
