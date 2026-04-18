using System.Text.Json;
using FluentAssertions;
using Nexo.BackgroundAgents.Observations;
using Nexo.BackgroundAgents.RuntimeStudio;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RuntimeStudio;

public sealed class ObservationLogTailReaderTests : IDisposable
{
    private readonly string _path;

    public ObservationLogTailReaderTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "nexo-obs-tail-" + Guid.NewGuid().ToString("N") + ".jsonl");
    }

    public void Dispose()
    {
        try { File.Delete(_path); } catch { /* best effort */ }
    }

    [Fact]
    public void ReadTail_returns_newest_ts()
    {
        var t1 = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2024, 1, 2, 11, 0, 0, TimeSpan.Zero);
        var line1 = JsonSerializer.Serialize(new RuntimeObservation(t1, "a", ObservationKind.Build, "x"));
        var line2 = JsonSerializer.Serialize(new RuntimeObservation(t2, "b", ObservationKind.Test, "y"));
        File.WriteAllText(_path, line1 + "\n" + line2 + "\n");

        var (count, last) = ObservationLogTailReader.ReadTail(_path);
        count.Should().Be(2);
        last.Should().Be(t2);
    }
}
