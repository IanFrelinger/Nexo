using System.Text.Json;
using FluentAssertions;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.BackgroundAgents.RuntimeStudio;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.RuntimeStudio;

/// <summary>Tests for runtime studio metrics collector.</summary>
public sealed class RuntimeStudioMetricsCollectorTests : IDisposable
{
    private readonly string _root;

    public RuntimeStudioMetricsCollectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ashlar-rs-coll-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Collect_fixed_clock_oldest_pending_hours()
    {
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var obj = new ObjectiveStore(Path.Combine(_root, "objectives"));
        obj.Add(new ObjectiveDocument
        {
            Id = "p1",
            Title = "Wait",
            Body = "b",
            CreatedAt = now.AddHours(-5),
            UpdatedAt = now.AddHours(-5)
        });

        var forge = new ChangeProposalStore(Path.Combine(_root, "forge"));
        var obsPath = Path.Combine(_root, "obs.jsonl");
        var ts = new DateTimeOffset(2024, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var line = JsonSerializer.Serialize(new RuntimeObservation(ts, "unit", ObservationKind.Build, "ok"));
        File.WriteAllText(obsPath, line + "\n");

        var disk = RuntimeStudioMetricsCollector.Collect(obj, forge, obsPath, nowUtc: now);

        disk.ObjectiveSla.PendingCount.Should().Be(1);
        disk.ObjectiveSla.OldestPendingAgeHours.Should().BeApproximately(5.0, 0.01);
        disk.ObservationsFileBytes.Should().BeGreaterThan(0);
        disk.ObservationsTailLineCount.Should().Be(1);
        disk.ObservationsLastTimestamp.Should().Be(ts);
    }
}
