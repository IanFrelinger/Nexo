using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Telemetry;
using Nexo.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for stats background agent command.</summary>
public class StatsBackgroundAgentCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _logPath;
    private readonly CycleEventStore _store;

    public StatsBackgroundAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-stats-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _logPath = Path.Combine(_tempDir, "cycles.jsonl");
        _store = new CycleEventStore(_logPath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Returns_zero_and_reports_empty_when_log_missing()
    {
        var cmd = new StatsBackgroundAgentCommand(_store, NullLogger<StatsBackgroundAgentCommand>.Instance);
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, agent: null, role: null, sinceHours: null, formatJson: false));
        rc.Should().Be(0);
        stdout.Should().Contain("No matching cycle events");
    }

    [Fact]
    public async Task Aggregates_cycles_per_agent_in_human_table()
    {
        /// <summary>Seed.</summary>
        /// <param name="true">True.</param>
        /// <param name="100">100.</param>
        /// <param name="2">2.</param>
        /// <param name="0">0.</param>
        Seed("runtime-planner", "extender", success: true, durationMs: 100, executed: 2, denied: 0);
        /// <summary>Seed.</summary>
        /// <param name="true">True.</param>
        /// <param name="300">300.</param>
        /// <param name="4">4.</param>
        /// <param name="1">1.</param>
        Seed("runtime-planner", "extender", success: true, durationMs: 300, executed: 4, denied: 1);
        /// <summary>Seed.</summary>
        /// <param name="false">False.</param>
        /// <param name="50">50.</param>
        /// <param name="0">0.</param>
        /// <param name="0">0.</param>
        Seed("runtime-planner", "extender", success: false, durationMs: 50, executed: 0, denied: 0);
        /// <summary>Seed.</summary>
        /// <param name="true">True.</param>
        /// <param name="1000">1000.</param>
        /// <param name="0">0.</param>
        /// <param name="0">0.</param>
        Seed("runtime-worker-tester", "tester", success: true, durationMs: 1000, executed: 0, denied: 0);

        var cmd = new StatsBackgroundAgentCommand(_store, NullLogger<StatsBackgroundAgentCommand>.Instance);
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, agent: null, role: null, sinceHours: null, formatJson: false));
        rc.Should().Be(0);
        stdout.Should().Contain("runtime-planner");
        stdout.Should().Contain("runtime-worker-tester");
        // 3 planner cycles, 2 ok, 1 fail; avg duration (100+300+50)/3 = 150
        stdout.Should().MatchRegex(@"runtime-planner\s+extender\s+3\s+2\s+1\s+150");
        stdout.Should().Contain("Events: 4");
    }

    [Fact]
    public async Task Filters_by_agent_role_and_since_hours()
    {
        /// <summary>Seed.</summary>
        /// <param name="true">True.</param>
        /// <param name="10">10.</param>
        Seed("runtime-planner", "extender", success: true, durationMs: 10);
        /// <summary>Seed.</summary>
        /// <param name="true">True.</param>
        /// <param name="10">10.</param>
        Seed("runtime-worker-optimizer", "optimizer", success: true, durationMs: 10);
        // ancient event must be excluded by --since-hours
        Append(new CycleEvent(
            DateTimeOffset.UtcNow - TimeSpan.FromDays(7),
            "runtime-planner", 99, "extender", 999, 1, 1, 0));

        var cmd = new StatsBackgroundAgentCommand(_store, NullLogger<StatsBackgroundAgentCommand>.Instance);

        var (_, byRole) = await CaptureAsync(() => Run(cmd, agent: null, role: "optimizer", sinceHours: 1, formatJson: false));
        byRole.Should().Contain("runtime-worker-optimizer");
        byRole.Should().NotContain("runtime-planner");

        var (_, byAgent) = await CaptureAsync(() => Run(cmd, agent: "runtime-planner", role: null, sinceHours: 1, formatJson: false));
        byAgent.Should().Contain("runtime-planner");
        byAgent.Should().Contain("Events: 1"); // only the recent planner event, ancient filtered out
    }

    [Fact]
    public async Task Json_output_is_well_formed_and_includes_path()
    {
        /// <summary>Seed.</summary>
        /// <param name="true">True.</param>
        /// <param name="200">200.</param>
        /// <param name="3">3.</param>
        /// <param name="1">1.</param>
        Seed("runtime-planner", "extender", success: true, durationMs: 200, executed: 3, denied: 1);

        var cmd = new StatsBackgroundAgentCommand(_store, NullLogger<StatsBackgroundAgentCommand>.Instance);
        var (rc, stdout) = await CaptureAsync(() => Run(cmd, agent: null, role: null, sinceHours: null, formatJson: true));
        rc.Should().Be(0);
        using var doc = System.Text.Json.JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        root.GetProperty("ok").GetBoolean().Should().BeTrue();
        root.GetProperty("path").GetString().Should().Be(_logPath);
        root.GetProperty("totalEvents").GetInt32().Should().Be(1);
        var agents = root.GetProperty("agents");
        agents.GetArrayLength().Should().Be(1);
        var first = agents[0];
        first.GetProperty("agent").GetString().Should().Be("runtime-planner");
        first.GetProperty("toolsExecuted").GetInt32().Should().Be(3);
        first.GetProperty("toolsDenied").GetInt32().Should().Be(1);
    }

    private void Seed(string agent, string role, bool success, long durationMs, int executed = 1, int denied = 0)
    {
        Append(new CycleEvent(
            ts: DateTimeOffset.UtcNow,
            agent: agent,
            cycle: 1,
            role: role,
            duration_ms: durationMs,
            iterations: 1,
            tools_executed: executed,
            tools_denied: denied,
            success: success,
            stopped_reason: success ? "ok" : "error",
            error: success ? null : "boom"));
    }

    /// <summary>Append.</summary>
    /// <param name="evt">Evt.</param>
    private void Append(CycleEvent evt) => _store.Append(evt);

    private static async Task<(int rc, string stdout)> CaptureAsync(Func<Task<int>> action)
    {
        var rc = await action();
        return (rc, _lastBuf?.ToString() ?? string.Empty);
    }

    [ThreadStatic] private static StringWriter? _lastBuf;
    [ThreadStatic] private static StringWriter? _lastErrBuf;

    private Task<int> Run(StatsBackgroundAgentCommand cmd, string? agent, string? role, double? sinceHours, bool formatJson)
    {
        _lastBuf = new StringWriter();
        _lastErrBuf = new StringWriter();
        return cmd.ExecuteAsync(agent, role, sinceHours, formatJson, _lastBuf, _lastErrBuf);
    }
}
