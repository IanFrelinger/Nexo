using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Objectives;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Objectives;

/// <summary>Tests for objective lifecycle tool.</summary>
public class ObjectiveLifecycleToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ObjectiveStore _store;

    public ObjectiveLifecycleToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-obj-tools-" + Guid.NewGuid().ToString("N"));
        _store = new ObjectiveStore(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Complete_tool_moves_objective_to_done_and_returns_ok()
    {
        _store.Add(new ObjectiveDocument { Id = "task-1", Title = "T1" });
        _store.ClaimNext("planner");

        var tool = new ObjectiveCompleteTool(_store);
        var args = JsonSerializer.SerializeToElement(new { id = "task-1", note = "fixed" });
        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), Snapshot(), default);

        OkPropertyOf(result).Should().BeTrue();
        _store.Find("task-1")!.Status.Should().Be(ObjectiveStatus.Done);
    }

    [Fact]
    public async Task Complete_tool_returns_structured_error_when_not_in_progress()
    {
        _store.Add(new ObjectiveDocument { Id = "task-2", Title = "T2" }); // still pending

        var tool = new ObjectiveCompleteTool(_store);
        var args = JsonSerializer.SerializeToElement(new { id = "task-2", note = (string?)null });
        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), Snapshot(), default);

        OkPropertyOf(result).Should().BeFalse();
        ErrorOf(result).Should().Contain("expected InProgress");
    }

    [Fact]
    public async Task Block_tool_moves_objective_to_blocked_with_reason()
    {
        _store.Add(new ObjectiveDocument { Id = "task-3", Title = "T3" });
        _store.ClaimNext("planner");

        var tool = new ObjectiveBlockTool(_store);
        var args = JsonSerializer.SerializeToElement(new { id = "task-3", reason = "tester offline" });
        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), Snapshot(), default);

        OkPropertyOf(result).Should().BeTrue();
        var blocked = _store.Find("task-3")!;
        blocked.Status.Should().Be(ObjectiveStatus.Blocked);
        blocked.BlockedReason.Should().Be("tester offline");
    }

    [Fact]
    public async Task Block_tool_returns_structured_error_for_unknown_objective()
    {
        var tool = new ObjectiveBlockTool(_store);
        var args = JsonSerializer.SerializeToElement(new { id = "ghost", reason = "missing" });
        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), Snapshot(), default);

        OkPropertyOf(result).Should().BeFalse();
        ErrorOf(result).Should().Contain("not found");
    }

    /// <summary>Snapshot.</summary>
    private static WorldSnapshot Snapshot() => new(0, new Dictionary<string, object?>());

    private static bool OkPropertyOf(ToolResult result)
    {
        var json = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("ok").GetBoolean();
    }

    private static string? ErrorOf(ToolResult result)
    {
        var json = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : null;
    }
}
