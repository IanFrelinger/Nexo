using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for observations background agent command.</summary>
public class ObservationsBackgroundAgentCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _logPath;
    private readonly JsonlObservationStore _store;

    public ObservationsBackgroundAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ashlar-obs-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _logPath = Path.Combine(_tempDir, "observations.jsonl");
        _store = new JsonlObservationStore(_logPath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Returns_zero_and_reports_empty_when_log_missing()
    {
        var cmd = NewCmd();
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, source: null, kind: null, sinceHours: null, tail: null, summary: false, formatJson: false));
        rc.Should().Be(0);
        stdout.Should().Contain("No matching observations");
    }

    [Fact]
    public async Task Lists_observations_in_table_filtered_by_kind_and_source()
    {
        /// <summary>Seed.</summary>
        /// <param name="failures"">Failures".</param>
        Seed("runtime-worker-tester", ObservationKind.Test, ObservationSeverity.Error, "3 failures");
        /// <summary>Seed.</summary>
        /// <param name="violations"">Violations".</param>
        Seed("runtime-worker-optimizer", ObservationKind.Analysis, ObservationSeverity.Warn, "5 violations");
        /// <summary>Seed.</summary>
        /// <param name="file"">File".</param>
        Seed("runtime-planner", ObservationKind.AgentAction, ObservationSeverity.Info, "wrote 1 file");

        var cmd = NewCmd();

        var (rc, byKind) = await CaptureAsync(() => Run(cmd, source: null, kind: "Test", sinceHours: null, tail: null, summary: false, formatJson: false));
        rc.Should().Be(0);
        byKind.Should().Contain("runtime-worker-tester");
        byKind.Should().NotContain("runtime-worker-optimizer");

        var (_, bySource) = await CaptureAsync(() => Run(cmd, source: "runtime-planner", kind: null, sinceHours: null, tail: null, summary: false, formatJson: false));
        bySource.Should().Contain("runtime-planner");
        bySource.Should().NotContain("runtime-worker-tester");
    }

    [Fact]
    public async Task Tail_returns_only_most_recent_rows()
    {
        for (var i = 0; i < 10; i++)
            _store.Append(new RuntimeObservation(
                DateTimeOffset.UtcNow.AddSeconds(i),
                "tester", ObservationKind.Test, $"run #{i}", ObservationSeverity.Info));

        var cmd = NewCmd();
        var (_, stdout) = await CaptureAsync(() => Run(cmd, source: null, kind: null, sinceHours: null, tail: 3, summary: false, formatJson: false));

        stdout.Should().Contain("Observations: 3");
        stdout.Should().Contain("run #9");
        stdout.Should().Contain("run #8");
        stdout.Should().Contain("run #7");
        stdout.Should().NotContain("run #0");
    }

    [Fact]
    public async Task Summary_groups_counts_per_source_and_severity()
    {
        /// <summary>Seed.</summary>
        Seed("tester", ObservationKind.Test, ObservationSeverity.Error, "x");
        /// <summary>Seed.</summary>
        Seed("tester", ObservationKind.Test, ObservationSeverity.Info, "y");
        /// <summary>Seed.</summary>
        Seed("tester", ObservationKind.Build, ObservationSeverity.Info, "z");
        /// <summary>Seed.</summary>
        Seed("optimizer", ObservationKind.Analysis, ObservationSeverity.Warn, "w");

        var cmd = NewCmd();
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, source: null, kind: null, sinceHours: null, tail: null, summary: true, formatJson: false));
        rc.Should().Be(0);
        stdout.Should().Contain("tester");
        stdout.Should().Contain("optimizer");
        // Header should be present
        stdout.Should().Contain("Source");
        stdout.Should().Contain("Total");
    }

    [Fact]
    public async Task Json_output_is_well_formed_and_includes_path()
    {
        /// <summary>Seed.</summary>
        Seed("tester", ObservationKind.Test, ObservationSeverity.Info, "ok");

        var cmd = NewCmd();
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, source: null, kind: null, sinceHours: null, tail: null, summary: false, formatJson: true));
        rc.Should().Be(0);
        using var doc = System.Text.Json.JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        root.GetProperty("ok").GetBoolean().Should().BeTrue();
        root.GetProperty("path").GetString().Should().Be(_logPath);
        root.GetProperty("count").GetInt32().Should().Be(1);
        root.GetProperty("observations").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Unknown_kind_returns_error_code_two()
    {
        var cmd = NewCmd();
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, source: null, kind: "Bogus", sinceHours: null, tail: null, summary: false, formatJson: false));
        rc.Should().Be(2);
        // Helpful error goes to stderr; stdout stays empty.
        stdout.Should().BeEmpty();
        var err = _lastErrBuf?.ToString() ?? string.Empty;
        err.Should().Contain("Unknown --kind");
        err.Should().Contain("Allowed:");
    }

    /// <summary>New cmd.</summary>
    private ObservationsBackgroundAgentCommand NewCmd() =>
        new(_store, NullLogger<ObservationsBackgroundAgentCommand>.Instance);

    private void Seed(string source, ObservationKind kind, ObservationSeverity severity, string summary)
    {
        _store.Append(new RuntimeObservation(DateTimeOffset.UtcNow, source, kind, summary, severity));
    }

    private static async Task<(int rc, string stdout)> CaptureAsync(Func<Task<int>> action)
    {
        var rc = await action();
        return (rc, _lastBuf?.ToString() ?? string.Empty);
    }

    [ThreadStatic] private static StringWriter? _lastBuf;
    [ThreadStatic] private static StringWriter? _lastErrBuf;

    [Fact]
    public async Task ZeroOrNegativeSinceHours_isRefusedInsteadOfIgnoringTheWindow()
    {
        var cmd = new ObservationsBackgroundAgentCommand(_store, NullLogger<ObservationsBackgroundAgentCommand>.Instance);
        foreach (var since in new double[] { 0, -1 })
        {
            var (rc, stdout) = await CaptureAsync(() => Run(cmd, source: null, kind: null, sinceHours: since, tail: null, summary: false, formatJson: true));
            rc.Should().Be(1);
            stdout.Should().Contain("Invalid --since-hours");
            stdout.Should().NotContain("\"ok\": true");
        }
    }

    [Fact]
    public async Task ZeroOrNegativeTail_isRefusedInsteadOfReturningAllRows()
    {
        Seed("tester", ObservationKind.Test, ObservationSeverity.Info, "must-not-be-returned");
        var cmd = new ObservationsBackgroundAgentCommand(_store, NullLogger<ObservationsBackgroundAgentCommand>.Instance);
        foreach (var tail in new int?[] { 0, -1 })
        {
            var (rc, stdout) = await CaptureAsync(() => Run(
                cmd, source: null, kind: null, sinceHours: null, tail: tail, summary: false, formatJson: true));
            rc.Should().Be(1);
            stdout.Should().Contain("Invalid --tail");
            stdout.Should().NotContain("must-not-be-returned");
        }
    }

    private Task<int> Run(
        ObservationsBackgroundAgentCommand cmd,
        string? source,
        string? kind,
        double? sinceHours,
        int? tail,
        bool summary,
        bool formatJson)
    {
        _lastBuf = new StringWriter();
        _lastErrBuf = new StringWriter();
        return cmd.ExecuteAsync(source, kind, sinceHours, tail, summary, formatJson, _lastBuf, _lastErrBuf);
    }
}
