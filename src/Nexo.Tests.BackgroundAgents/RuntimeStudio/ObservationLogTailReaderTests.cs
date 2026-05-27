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

    [Fact]
    public void ReadTail_missing_or_blank_path_returns_empty()
    {
        ObservationLogTailReader.ReadTail("").Should().Be((0, null));
        ObservationLogTailReader.ReadTail("   ").Should().Be((0, null));
        ObservationLogTailReader.ReadTail(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl")).Should().Be((0, null));
    }

    [Fact]
    public void ReadTail_empty_file_returns_zero()
    {
        File.WriteAllText(_path, string.Empty);
        ObservationLogTailReader.ReadTail(_path).Should().Be((0, null));
    }

    [Fact]
    public void ReadTail_parses_ts_from_malformed_json_line()
    {
        var ts = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        File.WriteAllText(_path, """{"ts":"2024-06-01T12:00:00Z","source":"a","kind":"NotARealKind","summary":"x"}""" + "\n");

        var (_, last) = ObservationLogTailReader.ReadTail(_path);

        last.Should().Be(ts);
    }

    [Fact]
    public void ReadTail_respects_max_tail_bytes()
    {
        var t1 = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2024, 1, 2, 11, 0, 0, TimeSpan.Zero);
        var oldLine = new string('x', 2048) + JsonSerializer.Serialize(new RuntimeObservation(t1, "old", ObservationKind.Build, "x"));
        var newLine = JsonSerializer.Serialize(new RuntimeObservation(t2, "new", ObservationKind.Test, "y"));
        File.WriteAllText(_path, oldLine + "\n" + newLine + "\n");

        var (_, last) = ObservationLogTailReader.ReadTail(_path, maxTailBytes: 512);

        last.Should().Be(t2);
    }

    [Fact]
    public void ReadTail_swallows_io_errors()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-obs-dir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            ObservationLogTailReader.ReadTail(dir).Should().Be((0, null));
        }
        finally
        {
            Directory.Delete(dir);
        }
    }
}
