using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Policies.Dev;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Policies;

/// <summary>Tests for forge mediated writes policy gap coverage.</summary>
[Trait("Category", "Unit")]
public sealed class ForgeMediatedWritesPolicyGapCoverageTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Constructor_throws_for_null_mode_store()
    {
        var act = () => new ForgeMediatedWritesPolicy((IAggressivenessModeStore)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("modeStore");
    }

    [Fact]
    public void Constructor_throws_for_null_mode_override()
    {
        var act = () => new ForgeMediatedWritesPolicy((Func<BackgroundAgentAggressivenessMode>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("modeOverride");
    }

    [Fact]
    public void Approve_blocks_tests_prefix_in_passive_mode()
    {
        var policy = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        var call = CreateToolCall("repo.fs.write", "tests/unit/FooTests.cs");

        policy.Approve(call, EmptySnapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("forge.propose_change");
    }

    [Fact]
    public void Approve_normalizes_backslash_paths_before_mediation_check()
    {
        var policy = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        var call = CreateToolCall("repo.fs.search_replace", @"src\Bar.cs");

        policy.Approve(call, EmptySnapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("src/Bar.cs");
    }

    [Fact]
    public void Approve_reads_mode_from_store_on_each_call()
    {
        var store = new Mock<IAggressivenessModeStore>();
        store.SetupSequence(s => s.GetMode())
            .Returns(BackgroundAgentAggressivenessMode.Active)
            .Returns(BackgroundAgentAggressivenessMode.SemiActive);

        var policy = new ForgeMediatedWritesPolicy(store.Object);
        var call = CreateToolCall("repo.fs.write", "src/Foo.cs");

        policy.Approve(call, EmptySnapshot, out _).Should().BeTrue();
        policy.Approve(call, EmptySnapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("SemiActive");
    }

    private static ToolCall CreateToolCall(string toolId, string path)
    {
        var json = JsonSerializer.SerializeToElement(new { path });
        /// <summary>Tool call.</summary>
        return new ToolCall(toolId, json);
    }
}
