using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Telemetry;
using Ashlar.CLI.Commands.BackgroundAgent;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// A4 overnight report: joins cycle activity (cycles.jsonl) with admission-gate outcomes
/// (&lt;project&gt;/.ashlar/gates) over a window. Pins the activity roll-up, the held/rejected outcome
/// counts, and graceful degradation when there are no gate records.
/// </summary>
public sealed class ReportBackgroundAgentCommandTests : IDisposable
{
    private readonly string _dir;
    private readonly string _project;
    private readonly CycleEventStore _cycles;

    public ReportBackgroundAgentCommandTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-report-tests-" + Guid.NewGuid().ToString("N"));
        _project = Path.Combine(_dir, "project");
        Directory.CreateDirectory(_project);
        _cycles = new CycleEventStore(Path.Combine(_dir, "cycles.jsonl"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private ReportBackgroundAgentCommand NewCmd() =>
        new(_cycles, NullLogger<ReportBackgroundAgentCommand>.Instance);

    private static async Task<string> RunAsync(ReportBackgroundAgentCommand cmd, double? since, string? project)
    {
        var sb = new StringWriter();
        var rc = await cmd.ExecuteAsync(since, project, formatJson: false, sb, TextWriter.Null);
        rc.Should().Be(0);
        return sb.ToString();
    }

    private void SeedCycle(string agent, bool success) =>
        _cycles.Append(new CycleEvent(
            ts: DateTimeOffset.UtcNow, agent: agent, cycle: 1, role: "extender",
            duration_ms: 100, iterations: 2, tools_executed: 3, tools_denied: 1,
            model: "qwen", provider: "ollama", rationale: null, stopped_reason: "empty",
            success: success, error: success ? null : "boom"));

    private async Task SeedGateAsync(string id, string gatesRequired, params (string name, bool passed)[] courses)
    {
        var yaml = $"""
            apiVersion: ashlar/v1
            kind: Policy
            sandbox:
              root: .
              writable: []
            selfExtend:
              mode: proposing
              budget:
                extensions: 3
                window: 24h
              mayAdd: [brick]
              gatesRequired: {gatesRequired}
            never:
              - modify_gate
              - widen_sandbox
              - access_signing_keys
              - truncate_ledger
              - grant_capability
            """;
        PolicyLoader.TryLoad(yaml, out var policy, out var reason).Should().BeTrue(reason);
        var proposal = new ExtensionProposal
        {
            Id = id,
            Kind = "brick",
            Summary = "test " + id,
            ProposedBy = "night-agent",
            ProposedAt = DateTimeOffset.UtcNow,
            Courses = courses.Select(c => new CourseResult { Name = c.name, Passed = c.passed, Detail = "test" }).ToList(),
        };
        await new GateStore(Path.Combine(_project, ".ashlar")).ProposeAsync(policy!, proposal, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Rolls_up_cycle_activity_in_the_window()
    {
        SeedCycle("night-agent", success: true);
        SeedCycle("night-agent", success: false);

        var output = await RunAsync(NewCmd(), since: 48, _project);

        output.Should().Contain("night-agent");
        output.Should().Contain("2 cycle(s)");
        output.Should().Contain("1 ok / 1 fail");
    }

    [Fact]
    public async Task Summarises_gate_outcomes_held_and_rejected()
    {
        // Held: the one required course passed. Rejected: a required course ('tests') never ran.
        await SeedGateAsync("ext-held01", "[sandbox]", ("sandbox", true));
        await SeedGateAsync("ext-rej01", "[sandbox, tests]", ("sandbox", true));

        var output = await RunAsync(NewCmd(), since: 48, _project);

        output.Should().Contain("1 held");
        output.Should().Contain("1 rejected");
        output.Should().Contain("ext-rej01");
    }

    [Fact]
    public async Task NoGateRecords_degradesGracefully()
    {
        SeedCycle("night-agent", success: true);

        var output = await RunAsync(NewCmd(), since: 48, _project);

        output.Should().Contain("no gate records");
    }

    [Fact]
    public async Task Window_excludesOlderCycles()
    {
        _cycles.Append(new CycleEvent(
            ts: DateTimeOffset.UtcNow - TimeSpan.FromHours(48), agent: "old-agent", cycle: 1, role: "extender",
            duration_ms: 10, iterations: 1, tools_executed: 0, tools_denied: 0, success: true));

        var output = await RunAsync(NewCmd(), since: 1, _project);

        output.Should().Contain("No cycles in the window");
        output.Should().NotContain("old-agent");
    }

    [Fact]
    public async Task ZeroOrNegativeSinceHours_isRefusedWithoutRemappingTo24()
    {
        var cmd = NewCmd();
        foreach (var since in new double[] { 0, -1 })
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var rc = await cmd.ExecuteAsync(since, _project, formatJson: true, stdout, stderr);
            rc.Should().Be(1);
            var output = stdout.ToString() + stderr.ToString();
            output.Should().Contain("Invalid --since-hours");
            output.Should().NotContain("\"windowHours\": 24");
        }
    }
}
