using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.HostRunners;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.HostRunners;

/// <summary>
/// The extraTools fold-in seam lets dynamically discovered tools (e.g. MCP proxies surfaced via
/// IToolSource) reach planner toolboxes without the factory knowing where they came from.
/// </summary>
public sealed class RepoFsToolboxFactoryExtraToolsTests
{
    private sealed class StubDelta : IActionDelta
    {
        public int TickFrom => 0;
        public int TickTo => 1;
        public IReadOnlyList<byte>? Signature { get; set; }
        public IReadOnlyList<string> Log { get; } = new[] { "stub" };
    }

    private sealed class StubTool : ITool
    {
        public string Id => "mcp:test:stub";

        public ToolSchema Schema => new(Id, "stub extra tool", """{"type":"object"}""");

        public Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
            => Task.FromResult(new ToolResult(new StubDelta(), "stub-ran"));
    }

    [Fact]
    public void CreateMinimal_folds_extra_tools_into_the_toolbox()
    {
        var (tools, _) = RepoFsToolboxFactory.CreateMinimal(extraTools: new ITool[] { new StubTool() });

        tools.Schemas().Should().Contain(s => s.Id == "mcp:test:stub");
    }

    [Fact]
    public void CreateWithBuildTest_folds_extra_tools_into_the_toolbox()
    {
        var (tools, _, _) = RepoFsToolboxFactory.CreateWithBuildTest(extraTools: new ITool[] { new StubTool() });

        tools.Schemas().Should().Contain(s => s.Id == "mcp:test:stub");
    }

    [Fact]
    public async Task Folded_extra_tools_are_invocable_through_the_toolbox()
    {
        var (tools, _) = RepoFsToolboxFactory.CreateMinimal(extraTools: new ITool[] { new StubTool() });

        using var args = JsonDocument.Parse("{}");
        var result = await tools.InvokeAsync(
            new ToolCall("mcp:test:stub", args.RootElement),
            WorldSnapshot.ForRepo("X:/repo"),
            CancellationToken.None);

        result.Payload.Should().Be("stub-ran");
    }

    [Fact]
    public void Null_extra_tools_change_nothing()
    {
        var (withNull, _) = RepoFsToolboxFactory.CreateMinimal(extraTools: null);
        var (without, _) = RepoFsToolboxFactory.CreateMinimal();

        withNull.Schemas().Select(s => s.Id).Should().BeEquivalentTo(without.Schemas().Select(s => s.Id));
    }
}
