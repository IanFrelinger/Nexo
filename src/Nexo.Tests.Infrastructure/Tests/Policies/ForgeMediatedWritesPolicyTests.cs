using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Policies;

[Trait("Category", "Unit")]
public sealed class ForgeMediatedWritesPolicyTests
{
    private static readonly WorldSnapshot Snap = new(0, new Dictionary<string, object?>());

    private static ToolCall WriteCall(string path)
        => new("repo.fs.write", JsonSerializer.SerializeToElement(new { path, content = "x" }));

    [Theory]
    [InlineData(BackgroundAgentAggressivenessMode.Passive, false)]
    [InlineData(BackgroundAgentAggressivenessMode.SemiActive, false)]
    [InlineData(BackgroundAgentAggressivenessMode.Active, true)]
    [InlineData(BackgroundAgentAggressivenessMode.Ambient, true)]
    public void Src_writes_are_blocked_only_in_low_trust_modes(BackgroundAgentAggressivenessMode mode, bool expectAllowed)
    {
        var policy = new ForgeMediatedWritesPolicy(() => mode);
        var allowed = policy.Approve(WriteCall("src/Foo.cs"), Snap, out var reason);
        allowed.Should().Be(expectAllowed);
        if (!expectAllowed)
            reason.Should().Contain("forge.propose_change");
    }

    [Fact]
    public void Non_source_writes_pass_in_every_mode()
    {
        foreach (var mode in Enum.GetValues<BackgroundAgentAggressivenessMode>())
        {
            var policy = new ForgeMediatedWritesPolicy(() => mode);
            policy.Approve(WriteCall(".nexo/notes.md"), Snap, out _).Should().BeTrue();
            policy.Approve(WriteCall("docs/plan.md"), Snap, out _).Should().BeTrue();
        }
    }

    [Fact]
    public void Non_write_calls_are_always_allowed()
    {
        var policy = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        policy.Approve(new ToolCall("repo.fs.read", JsonSerializer.SerializeToElement(new { path = "src/Foo.cs" })), Snap, out _).Should().BeTrue();
    }
}
