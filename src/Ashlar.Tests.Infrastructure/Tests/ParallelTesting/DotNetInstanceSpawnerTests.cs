using FluentAssertions;
using Ashlar.Core.Application.ParallelTesting.Models;
using Ashlar.Infrastructure.ParallelTesting;
using Ashlar.Tests.Infrastructure;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.ParallelTesting;

/// <summary>Fail-closed coverage for the parallel-test instance spawner.</summary>
public sealed class DotNetInstanceSpawnerTests
{
    [Fact]
    public async Task SpawnAsync_WithNonExistentPath_MarksInstanceFailed()
    {
        var spawner = new DotNetInstanceSpawner();
        var instances = await spawner.SpawnAsync(
            1,
            [new ParameterSet { Overrides = new Dictionary<string, string> { ["filter"] = "Any" } }],
            "/nonexistent/path/to/project.csproj");

        instances.Should().ContainSingle();
        instances[0].Passed.Should().BeFalse();
        instances[0].Output.Should().Contain("not found");
    }

    [Fact]
    public async Task SpawnAsync_WithEmptyFilter_MarksInstanceFailed()
    {
        var repoRoot = TestPaths.FindRepoRoot();
        var csproj = Path.Combine(repoRoot, "src/Ashlar.Tests.Contracts/Ashlar.Tests.Contracts.csproj");
        File.Exists(csproj).Should().BeTrue();

        var spawner = new DotNetInstanceSpawner();
        var instances = await spawner.SpawnAsync(
            1,
            [new ParameterSet
            {
                Overrides = new Dictionary<string, string>
                {
                    ["filter"] = "FullyQualifiedName~DoesNotExistParallelTests",
                },
            }],
            csproj);

        instances.Should().ContainSingle();
        instances[0].Passed.Should().BeFalse();
        instances[0].Output.Should().Contain("No tests matched the filter");
    }

    [Theory]
    [InlineData("No test is available in Foo.dll.", false)]
    [InlineData("No test matches the given testcase filter", false)]
    [InlineData("Passed: 0, Failed: 0, Skipped: 0", false)]
    [InlineData("Passed! - Failed: 0, Passed: 1, Skipped: 0", true)]
    [InlineData("unparseable output", false)]
    public void HasExecutedTests_fails_closed_on_empty_runs(string output, bool expected)
    {
        DotNetInstanceSpawner.HasExecutedTests(output).Should().Be(expected);
    }
}
