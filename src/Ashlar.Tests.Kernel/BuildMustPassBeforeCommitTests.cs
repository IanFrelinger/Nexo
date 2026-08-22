using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Policies.Dev;
using Xunit;

namespace Ashlar.Tests.Kernel;

public class BuildMustPassBeforeCommitTests
{
    private static ToolCall Commit() => new("repo.git.commit", JsonDocument.Parse("{}").RootElement);
    private static ToolCall Other() => new("repo.fs.write", JsonDocument.Parse("{}").RootElement);

    [Fact]
    public void Allows_non_commit_calls_without_state()
    {
        var p = new BuildMustPassBeforeCommit();
        p.Approve(Other(), new WorldSnapshot(0, new Dictionary<string, object?>()), out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Rejects_commit_when_build_or_tests_missing()
    {
        var p = new BuildMustPassBeforeCommit();
        p.Approve(Commit(), new WorldSnapshot(0, new Dictionary<string, object?>()), out var r1).Should().BeFalse();
        r1.Should().Contain("build/tests not green");

        p.Approve(Commit(), new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = true,
        }), out _).Should().BeFalse();

        p.Approve(Commit(), new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = false,
            ["LastTestsOk"] = true,
        }), out _).Should().BeFalse();
    }

    [Fact]
    public void Allows_commit_when_build_and_tests_pass()
    {
        var p = new BuildMustPassBeforeCommit();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = true,
            ["LastTestsOk"] = true,
        });
        p.Approve(Commit(), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}
