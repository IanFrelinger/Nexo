using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for objectives background agent command.</summary>
public class ObjectivesBackgroundAgentCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ObjectiveStore _store;

    public ObjectivesBackgroundAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ashlar-obj-cli-tests-" + Guid.NewGuid().ToString("N"));
        _store = new ObjectiveStore(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    /// <summary>New cmd.</summary>
    /// <param name="null">Null.</param>
    private ObjectivesBackgroundAgentCommand NewCmd(IObservationStore? observations = null) =>
        new(_store, NullLogger<ObjectivesBackgroundAgentCommand>.Instance, observations);

    [Fact]
    public async Task Add_then_list_returns_the_new_objective_in_pending()
    {
        var cmd = NewCmd();
        var (rcAdd, stdoutAdd) = await CaptureAsync(() => cmd.AddAsync(
            id: "task-add",
            title: "Add CLI test",
            priority: 50,
            tags: new[] { "cli", "tests" },
            body: "Body text",
            bodyFile: null,
            formatJson: false,
            stdout: TestStdout!,
            stderr: TestStderr!));
        rcAdd.Should().Be(0);
        stdoutAdd.Should().Contain("Added objective 'task-add'");

        var (rcList, stdoutList) = await CaptureAsync(() => cmd.ListAsync(
            status: "Pending", tag: null, formatJson: false,
            stdout: TestStdout!, stderr: TestStderr!));
        rcList.Should().Be(0);
        stdoutList.Should().Contain("task-add");
        stdoutList.Should().Contain("Add CLI test");
    }

    [Fact]
    public async Task List_with_invalid_status_returns_2_and_lists_allowed()
    {
        var cmd = NewCmd();
        var (rc, _, stderr) = await CaptureWithStderrAsync(() => cmd.ListAsync(
            status: "nope", tag: null, formatJson: false,
            stdout: TestStdout!, stderr: TestStderr!));
        rc.Should().Be(2);
        stderr.Should().Contain("Pending");
        stderr.Should().Contain("Blocked");
    }

    [Fact]
    public async Task Show_returns_1_when_missing_and_emits_helpful_message()
    {
        var cmd = NewCmd();
        var (rc, _, stderr) = await CaptureWithStderrAsync(() => cmd.ShowAsync(
            id: "ghost", formatJson: false,
            stdout: TestStdout!, stderr: TestStderr!));
        rc.Should().Be(1);
        stderr.Should().Contain("ghost");
        stderr.Should().Contain("not found");
    }

    [Fact]
    public async Task Complete_after_claim_marks_done_and_show_reports_status()
    {
        _store.Add(new ObjectiveDocument { Id = "task-complete", Title = "Complete me" });
        _store.ClaimNext("planner");

        var cmd = NewCmd();
        var (rcComplete, _) = await CaptureAsync(() => cmd.CompleteAsync(
            id: "task-complete", note: "ok",
            formatJson: false, stdout: TestStdout!, stderr: TestStderr!));
        rcComplete.Should().Be(0);

        var (rcShow, stdoutShow) = await CaptureAsync(() => cmd.ShowAsync(
            id: "task-complete", formatJson: false,
            stdout: TestStdout!, stderr: TestStderr!));
        rcShow.Should().Be(0);
        stdoutShow.Should().Contain("Status:     Done");
    }

    [Fact]
    public async Task Block_then_unblock_round_trips_status()
    {
        _store.Add(new ObjectiveDocument { Id = "task-block", Title = "Block me" });

        var cmd = NewCmd();
        var (rcBlock, _) = await CaptureAsync(() => cmd.BlockAsync(
            id: "task-block", reason: "infra down",
            formatJson: false, stdout: TestStdout!, stderr: TestStderr!));
        rcBlock.Should().Be(0);
        _store.Find("task-block")!.Status.Should().Be(ObjectiveStatus.Blocked);

        var (rcUnblock, _) = await CaptureAsync(() => cmd.UnblockAsync(
            id: "task-block", formatJson: false,
            stdout: TestStdout!, stderr: TestStderr!));
        rcUnblock.Should().Be(0);
        _store.Find("task-block")!.Status.Should().Be(ObjectiveStatus.Pending);
    }

    [Fact]
    public async Task Stats_json_reports_counts_by_status_and_tag()
    {
        _store.Add(new ObjectiveDocument { Id = "a", Title = "A", Tags = new[] { "alpha" } });
        _store.Add(new ObjectiveDocument { Id = "b", Title = "B", Tags = new[] { "alpha", "beta" } });
        _store.Add(new ObjectiveDocument { Id = "c", Title = "C", Tags = new[] { "beta" } });
        _store.ClaimNext("planner");

        var cmd = NewCmd();
        var (rc, stdout) = await CaptureAsync(() => cmd.StatsAsync(
            formatJson: true, stdout: TestStdout!, stderr: TestStderr!));
        rc.Should().Be(0);

        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(3);
        doc.RootElement.GetProperty("byStatus").GetProperty("Pending").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("byStatus").GetProperty("InProgress").GetInt32().Should().Be(1);
        var tags = doc.RootElement.GetProperty("byTag");
        tags.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Report_joins_observations_to_objectives_by_objective_id_fact()
    {
        _store.Add(new ObjectiveDocument { Id = "rep-1", Title = "Reportable" });
        _store.ClaimNext("planner");

        var obsPath = Path.Combine(_tempDir, "observations.jsonl");
        var obsStore = new JsonlObservationStore(obsPath);
        obsStore.Append(new RuntimeObservation(
            ts: DateTimeOffset.UtcNow,
            source: "planner",
            kind: ObservationKind.Build,
            summary: "Build succeeded",
            severity: ObservationSeverity.Info,
            facts: new Dictionary<string, string> { ["objective_id"] = "rep-1" }));
        obsStore.Append(new RuntimeObservation(
            ts: DateTimeOffset.UtcNow,
            source: "planner",
            kind: ObservationKind.Test,
            summary: "Tests failed",
            severity: ObservationSeverity.Error,
            facts: new Dictionary<string, string> { ["objective_id"] = "rep-1" }));
        // Untagged observation — must NOT count toward rep-1.
        obsStore.Append(new RuntimeObservation(
            ts: DateTimeOffset.UtcNow,
            source: "tester",
            kind: ObservationKind.Test,
            summary: "Tests succeeded"));

        var cmd = NewCmd(obsStore);
        var (rc, stdout) = await CaptureAsync(() => cmd.ReportAsync(
            id: "rep-1", status: null, sinceHours: null,
            formatJson: true, stdout: TestStdout!, stderr: TestStderr!));
        rc.Should().Be(0);

        using var doc = JsonDocument.Parse(stdout);
        var rows = doc.RootElement.GetProperty("rows");
        rows.GetArrayLength().Should().Be(1);
        var row = rows[0];
        row.GetProperty("Id").GetString().Should().Be("rep-1");
        row.GetProperty("Observations").GetInt32().Should().Be(2);
        row.GetProperty("Build").GetInt32().Should().Be(1);
        row.GetProperty("Test").GetInt32().Should().Be(1);
        row.GetProperty("Errors").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Report_zeroOrNegativeSinceHours_isRefused()
    {
        var cmd = NewCmd();
        foreach (var since in new double[] { 0, -1 })
        {
            var (rc, stdout) = await CaptureAsync(() => cmd.ReportAsync(
                id: null, status: null, sinceHours: since,
                formatJson: true, stdout: TestStdout!, stderr: TestStderr!));
            rc.Should().Be(1);
            stdout.Should().Contain("Invalid --since-hours");
            stdout.Should().NotContain("\"ok\": true");
        }
    }

    // Test helpers — write through caller-supplied streams so xUnit's parallel
    // execution doesn't clobber Console.Out across fixtures.
    private StringWriter? TestStdout;
    private StringWriter? TestStderr;

    private async Task<(int rc, string stdout)> CaptureAsync(Func<Task<int>> action)
    {
        TestStdout = new StringWriter();
        TestStderr = new StringWriter();
        var rc = await action();
        return (rc, TestStdout.ToString());
    }

    private async Task<(int rc, string stdout, string stderr)> CaptureWithStderrAsync(Func<Task<int>> action)
    {
        TestStdout = new StringWriter();
        TestStderr = new StringWriter();
        var rc = await action();
        return (rc, TestStdout.ToString(), TestStderr.ToString());
    }
}
