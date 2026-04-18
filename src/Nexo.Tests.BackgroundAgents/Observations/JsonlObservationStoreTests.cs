using System.Text.Json;
using FluentAssertions;
using Nexo.BackgroundAgents.Observations;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Observations;

public class JsonlObservationStoreTests : IDisposable
{
    private readonly string _tempDir;

    public JsonlObservationStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-observations-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Append_creates_parent_directory_and_writes_jsonl_line()
    {
        var path = Path.Combine(_tempDir, "nested", "deep", "observations.jsonl");
        var store = new JsonlObservationStore(path);

        store.Append(new RuntimeObservation(
            ts: new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero),
            source: "runtime-worker-tester",
            kind: ObservationKind.Test,
            summary: "PathAllowlist: 5/5 passed",
            severity: ObservationSeverity.Info,
            facts: new Dictionary<string, string>
            {
                ["passed"] = "5",
                ["failed"] = "0",
                ["filter"] = "PathAllowlist"
            },
            agentCycle: 7));

        File.Exists(path).Should().BeTrue();
        var lines = File.ReadAllLines(path);
        lines.Should().HaveCount(1);

        using var doc = JsonDocument.Parse(lines[0]);
        var root = doc.RootElement;
        root.GetProperty("source").GetString().Should().Be("runtime-worker-tester");
        root.GetProperty("kind").GetString().Should().Be("Test");
        root.GetProperty("summary").GetString().Should().Be("PathAllowlist: 5/5 passed");
        root.GetProperty("severity").GetString().Should().Be("Info");
        root.GetProperty("agent_cycle").GetInt32().Should().Be(7);
        root.GetProperty("facts").GetProperty("passed").GetString().Should().Be("5");
    }

    [Fact]
    public void ReadSince_returns_observations_in_append_order()
    {
        var store = new JsonlObservationStore(Path.Combine(_tempDir, "observations.jsonl"));
        for (var i = 0; i < 5; i++)
        {
            store.Append(new RuntimeObservation(
                DateTimeOffset.UnixEpoch.AddSeconds(i),
                "src",
                ObservationKind.Test,
                $"summary {i}"));
        }

        var read = store.ReadSince().ToList();
        read.Should().HaveCount(5);
        read.Select(o => o.summary).Should().Equal("summary 0", "summary 1", "summary 2", "summary 3", "summary 4");
    }

    [Fact]
    public void ReadSince_filters_by_timestamp_and_kind()
    {
        var store = new JsonlObservationStore(Path.Combine(_tempDir, "observations.jsonl"));
        var t0 = DateTimeOffset.UtcNow - TimeSpan.FromHours(2);
        store.Append(new RuntimeObservation(t0, "a", ObservationKind.Build, "old build"));
        store.Append(new RuntimeObservation(t0.AddMinutes(60), "a", ObservationKind.Test, "old test"));
        store.Append(new RuntimeObservation(t0.AddMinutes(110), "a", ObservationKind.Build, "recent build"));
        store.Append(new RuntimeObservation(t0.AddMinutes(115), "a", ObservationKind.Test, "recent test"));

        var recent = store.ReadSince(since: t0.AddMinutes(100)).ToList();
        recent.Should().HaveCount(2);

        var recentBuilds = store.ReadSince(since: t0.AddMinutes(100), kind: ObservationKind.Build).ToList();
        recentBuilds.Should().ContainSingle().Which.summary.Should().Be("recent build");

        var allTests = store.ReadSince(kind: ObservationKind.Test).ToList();
        allTests.Should().HaveCount(2);
    }

    [Fact]
    public void ReadSince_respects_limit()
    {
        var store = new JsonlObservationStore(Path.Combine(_tempDir, "observations.jsonl"));
        for (var i = 0; i < 10; i++)
            store.Append(new RuntimeObservation(DateTimeOffset.UnixEpoch.AddSeconds(i), "a", ObservationKind.Build, $"#{i}"));

        store.ReadSince(limit: 3).Should().HaveCount(3);
    }

    [Fact]
    public void ReadSince_skips_malformed_lines_without_throwing()
    {
        var path = Path.Combine(_tempDir, "observations.jsonl");
        File.WriteAllLines(path, new[]
        {
            "{not valid",
            JsonSerializer.Serialize(new RuntimeObservation(DateTimeOffset.UnixEpoch, "a", ObservationKind.Test, "ok 1")),
            "",
            "garbage }",
            JsonSerializer.Serialize(new RuntimeObservation(DateTimeOffset.UnixEpoch, "a", ObservationKind.Test, "ok 2"))
        });

        var store = new JsonlObservationStore(path);
        store.ReadSince().Select(o => o.summary).Should().Equal("ok 1", "ok 2");
    }

    [Fact]
    public void ReadSince_returns_empty_when_file_missing()
    {
        new JsonlObservationStore(Path.Combine(_tempDir, "missing.jsonl")).ReadSince().Should().BeEmpty();
    }

    [Fact]
    public async Task Append_is_safe_under_concurrent_writers()
    {
        var path = Path.Combine(_tempDir, "observations.jsonl");
        var store = new JsonlObservationStore(path);

        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            store.Append(new RuntimeObservation(DateTimeOffset.UnixEpoch.AddSeconds(i), "a", ObservationKind.Test, $"#{i}"));
        })).ToArray();

        await Task.WhenAll(tasks);

        var read = store.ReadSince().ToList();
        read.Should().HaveCount(50);
    }
}


