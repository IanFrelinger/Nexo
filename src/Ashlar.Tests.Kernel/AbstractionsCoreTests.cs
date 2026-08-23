using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;
using Ashlar.Abstractions.Database;
using Ashlar.Abstractions.Transport;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>Tests for abstractions core.</summary>
public class AbstractionsCoreTests
{
    [Fact]
    public void ActionDelta_Merge_empty_returns_empty_delta()
    {
        var merged = ActionDelta.Merge(Array.Empty<IActionDelta>());
        merged.TickFrom.Should().Be(0);
        merged.TickTo.Should().Be(0);
        merged.Log.Should().BeEmpty();
    }

    [Fact]
    public void ActionDelta_Merge_combines_ticks_and_logs()
    {
        var d1 = new ActionDelta(1, 3, new[] { "a" });
        var d2 = new ActionDelta(2, 5, new[] { "b", "c" });
        var merged = ActionDelta.Merge(new IActionDelta[] { d1, d2 });
        merged.TickFrom.Should().Be(1);
        merged.TickTo.Should().Be(5);
        merged.Log.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void ActionDelta_signature_round_trips()
    {
        var delta = new ActionDelta(0, 1, Array.Empty<string>()) { Signature = new byte[] { 1, 2 } };
        delta.Signature.Should().Equal(new byte[] { 1, 2 });
    }

    [Fact]
    public void AgentActions_None_is_empty()
    {
        AgentActions.None.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void WorldSnapshot_ForRepo_sets_repo_and_output_roots()
    {
        var snap = WorldSnapshot.ForRepo("/repo");
        snap.Data["RepoRoot"].Should().Be("/repo");
        snap.Data["OutputRoot"].Should().Be(Path.Combine("/repo", "out"));

        var custom = WorldSnapshot.ForRepo("/repo", "/out", tick: 7);
        custom.Tick.Should().Be(7);
        custom.Data["OutputRoot"].Should().Be("/out");
    }

    [Fact]
    public void ToolCall_ParseArgs_deserializes_arguments()
    {
        var json = JsonDocument.Parse("""{"n":42}""").RootElement;
        var call = new ToolCall("t", json);
        var args = call.ParseArgs<Dictionary<string, int>>();
        args["n"].Should().Be(42);
    }

    [Fact]
    public void CapabilityAttribute_exposes_capabilities()
    {
        var attr = new CapabilityAttribute("a", "b");
        attr.Capabilities.Should().Equal("a", "b");
    }
}
