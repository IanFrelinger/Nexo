using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Policies;

[Trait("Category", "Unit")]
public sealed class BuildMustPassBeforeCommitGapCoverageTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());
    private static readonly ToolCall CommitCall = new("repo.git.commit", JsonSerializer.SerializeToElement(new { }));
    private static readonly ToolCall WriteCall = new("repo.fs.write", JsonSerializer.SerializeToElement(new { path = "src/x.cs" }));

    [Fact]
    public void Approve_allows_non_commit_tools_without_build_state()
    {
        var policy = new BuildMustPassBeforeCommit();

        policy.Approve(WriteCall, EmptySnapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_rejects_commit_when_only_tests_are_green()
    {
        var policy = new BuildMustPassBeforeCommit();
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastTestsOk"] = true,
        });

        policy.Approve(CommitCall, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("build/tests not green");
    }

    [Fact]
    public void Approve_rejects_commit_when_build_flag_is_not_true()
    {
        var policy = new BuildMustPassBeforeCommit();
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = "true",
            ["LastTestsOk"] = true,
        });

        policy.Approve(CommitCall, snapshot, out _).Should().BeFalse();
    }
}
