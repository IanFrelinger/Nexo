using System.Text.Json;
using FluentAssertions;
using Nexo.BackgroundAgents.Telemetry;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Telemetry;

public class CycleEventStoreTests : IDisposable
{
    private readonly string _tempDir;

    public CycleEventStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-cycle-events-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Append_creates_parent_directory_and_writes_jsonl_line()
    {
        var path = Path.Combine(_tempDir, "nested", "deep", "cycles.jsonl");
        var store = new CycleEventStore(path);

        store.Append(new CycleEvent(
            ts: new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero),
            agent: "runtime-planner",
            cycle: 7,
            role: "extender",
            duration_ms: 1234,
            iterations: 3,
            tools_executed: 2,
            tools_denied: 1,
            model: "llama3.1:latest",
            provider: "ollama",
            rationale: "noted progress",
            stopped_reason: "no_more_tool_calls",
            success: true,
            error: null));

        File.Exists(path).Should().BeTrue();
        var lines = File.ReadAllLines(path);
        lines.Should().HaveCount(1);

        using var doc = JsonDocument.Parse(lines[0]);
        var root = doc.RootElement;
        root.GetProperty("agent").GetString().Should().Be("runtime-planner");
        root.GetProperty("cycle").GetInt32().Should().Be(7);
        root.GetProperty("role").GetString().Should().Be("extender");
        root.GetProperty("duration_ms").GetInt64().Should().Be(1234);
        root.GetProperty("iterations").GetInt32().Should().Be(3);
        root.GetProperty("tools_executed").GetInt32().Should().Be(2);
        root.GetProperty("tools_denied").GetInt32().Should().Be(1);
        root.GetProperty("model").GetString().Should().Be("llama3.1:latest");
        root.GetProperty("provider").GetString().Should().Be("ollama");
        root.GetProperty("stopped_reason").GetString().Should().Be("no_more_tool_calls");
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.TryGetProperty("error", out _).Should().BeFalse(
            because: "null fields are dropped to keep lines compact");
    }

    [Fact]
    public void Append_then_Read_round_trips_multiple_events_in_order()
    {
        var path = Path.Combine(_tempDir, "cycles.jsonl");
        var store = new CycleEventStore(path);

        for (var i = 0; i < 5; i++)
        {
            store.Append(new CycleEvent(
                ts: DateTimeOffset.UnixEpoch.AddSeconds(i),
                agent: "agent",
                cycle: i,
                role: "extender",
                duration_ms: i * 10,
                iterations: i,
                tools_executed: i,
                tools_denied: 0,
                stopped_reason: "ok"));
        }

        var read = store.Read().ToList();
        read.Should().HaveCount(5);
        read.Select(e => e.cycle).Should().Equal(0, 1, 2, 3, 4);
        read.Select(e => e.duration_ms).Should().Equal(0, 10, 20, 30, 40);
    }

    [Fact]
    public void Read_skips_malformed_lines_without_throwing()
    {
        var path = Path.Combine(_tempDir, "cycles.jsonl");
        File.WriteAllLines(path, new[]
        {
            "{not valid json",
            JsonSerializer.Serialize(new CycleEvent(
                DateTimeOffset.UnixEpoch, "a", 1, "extender", 5, 1, 1, 0)),
            "",
            "also garbage }",
            JsonSerializer.Serialize(new CycleEvent(
                DateTimeOffset.UnixEpoch, "a", 2, "extender", 6, 1, 0, 0))
        });

        var store = new CycleEventStore(path);
        var read = store.Read().ToList();
        read.Select(e => e.cycle).Should().Equal(1, 2);
    }

    [Fact]
    public void Read_returns_empty_when_file_missing()
    {
        var store = new CycleEventStore(Path.Combine(_tempDir, "does-not-exist.jsonl"));
        store.Read().Should().BeEmpty();
    }

    [Fact]
    public async Task Append_is_safe_under_concurrent_writers()
    {
        var path = Path.Combine(_tempDir, "cycles.jsonl");
        var store = new CycleEventStore(path);

        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            store.Append(new CycleEvent(
                DateTimeOffset.UnixEpoch.AddSeconds(i),
                "a", i, "extender", 1, 1, 0, 0));
        })).ToArray();

        await Task.WhenAll(tasks);

        var read = store.Read().ToList();
        read.Should().HaveCount(50);
        read.Select(e => e.cycle).OrderBy(x => x).Should().Equal(Enumerable.Range(0, 50));
    }
}
