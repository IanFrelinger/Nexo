using System.Text.Json;
using FluentAssertions;
using Ashlar.Agents.TestKit;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Infrastructure.Certification.HotSwap;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The session execution backend over a scripted fake session: runner provisioning
/// (source upload + offline build), batched job upload, raw-observation parsing, and the
/// fail-closed contract on infrastructure failure. Exec positions are implementation-
/// coupled by design — the flight against a live daemon is the integration proof; these
/// facts pin the orchestration shape and the parsing.
/// </summary>
[Trait("Category", "Certification")]
public sealed class SessionExecutionBackendTests
{
    private static readonly WitnessSpec Witness = new(
        "exec-probe",
        [
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = "x" },
                new Dictionary<string, object> { ["errorCount"] = 0 })
        ]);

    [Fact]
    public void TheRunner_MarksAUnitItCouldNotLoad_WithThePrefixTheGateParses()
    {
        // A unit that fails to LOAD reports every case as thrown, which is shape-identical to a
        // mutant that throws on every case — and a judge reading only Threw scores both as killed.
        // BrickMutationEngine tells them apart by this prefix and refuses the load failure rather
        // than counting it, so if the runner literal drifts, the mutation leg silently goes back to
        // scoring harness failures as kills. Nothing else would notice; this does.
        SessionExecutionBackend.RunnerSource
            .Should().Contain(ExecutionRunnerMarkers.UnitLoadFailurePrefix);
    }

    [Fact]
    public async Task ExecutesTheRunner_AndParsesRawObservations()
    {
        const string reportJson = """
[{"unitId":"candidate","caseIndex":0,"repeat":0,"threw":false,"error":null,"summary":"ok","outputs":{"errorCount":0}},
 {"unitId":"candidate","caseIndex":0,"repeat":1,"threw":false,"error":null,"summary":"ok","outputs":{"errorCount":0}}]
""";
        // Runner provisioning is 8 execs (reset, source ×2, csproj ×2, NuGet.config ×2,
        // build), then per job: units reset, job upload ×2, run. The run exec must answer
        // with the runner's stdout JSON.
        var results = Enumerable.Repeat(FakeSandboxedSessionRunner.Success(), 11)
            .Append(FakeSandboxedSessionRunner.Success(reportJson))
            .ToArray();
        var runner = new FakeSandboxedSessionRunner(results);
        var session = (FakeSandboxedSession)await runner.StartAsync(Spec());
        var backend = new SessionExecutionBackend(session);

        var report = await backend.ExecuteAsync(new CandidateExecutionJob(
            new[] { new CandidateExecutionUnit("candidate", null, "Some.Brick") }, Witness, Repeats: 2));

        backend.Describe().Should().Be($"in-session:{session.SessionId}");
        report.Observations.Should().HaveCount(2);
        report.Observations[0].Outputs!["errorCount"].Should().BeOfType<JsonElement>(
            "outputs stay in transport shape; the gate's judges unwrap them");

        var run = session.ExecCommands[^1];
        run[0].Should().Be("dotnet");
        run[1].Should().EndWith("SessionExecutionRunner.dll");
        session.ExecCommands.Should().Contain(c => c.Count > 2 && c[0] == "dotnet" && c[1] == "build",
            "the runner is built inside the session, offline");
    }

    [Fact]
    public async Task MutantAssemblies_UploadAsUnits_BeforeTheRun()
    {
        const string reportJson = """
[{"unitId":"mut-1","caseIndex":0,"repeat":0,"threw":false,"error":null,"summary":"m","outputs":{}}]
""";
        // Setup (8) + units reset (1) + mutant upload (2) + job (2) + run (1) = 14.
        var results = Enumerable.Repeat(FakeSandboxedSessionRunner.Success(), 13)
            .Append(FakeSandboxedSessionRunner.Success(reportJson))
            .ToArray();
        var runner = new FakeSandboxedSessionRunner(results);
        var session = (FakeSandboxedSession)await runner.StartAsync(Spec());
        var backend = new SessionExecutionBackend(session);

        var report = await backend.ExecuteAsync(new CandidateExecutionJob(
            new[] { new CandidateExecutionUnit("mut-1", new byte[] { 1, 2, 3 }, "Some.Brick") },
            Witness, Repeats: 1));

        report.Observations.Should().ContainSingle().Which.UnitId.Should().Be("mut-1");
        session.ExecCommands.Should().Contain(c =>
            c.Any(part => part.Contains("/ashlar-exec/units/unit-0.dll", StringComparison.Ordinal)),
            "the mutant's PE bytes travel into the session before execution");
    }

    [Fact]
    public async Task RunnerBuildFailure_Throws_FailClosed()
    {
        // Execs 1..7 succeed; the 8th (the runner build) fails.
        var results = Enumerable.Repeat(FakeSandboxedSessionRunner.Success(), 7)
            .Append(FakeSandboxedSessionRunner.Failure("CS0000 nope"))
            .ToArray();
        var runner = new FakeSandboxedSessionRunner(results);
        var session = (FakeSandboxedSession)await runner.StartAsync(Spec());
        var backend = new SessionExecutionBackend(session);

        var act = () => backend.ExecuteAsync(new CandidateExecutionJob(
            new[] { new CandidateExecutionUnit("candidate", null, "Some.Brick") }, Witness, Repeats: 1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("failed to build"),
                "a backend that cannot execute must throw, never fabricate observations");
    }

    [Fact]
    public async Task UnparseableRunnerOutput_Throws_FailClosed()
    {
        var results = Enumerable.Repeat(FakeSandboxedSessionRunner.Success(), 11)
            .Append(FakeSandboxedSessionRunner.Success("MSB1003 not json"))
            .ToArray();
        var runner = new FakeSandboxedSessionRunner(results);
        var session = (FakeSandboxedSession)await runner.StartAsync(Spec());
        var backend = new SessionExecutionBackend(session);

        var act = () => backend.ExecuteAsync(new CandidateExecutionJob(
            new[] { new CandidateExecutionUnit("candidate", null, "Some.Brick") }, Witness, Repeats: 1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("unparseable"));
    }

    private static SandboxSpec Spec() => new(
        "sdk:pinned", Array.Empty<Mount>(), NetworkAccess.None, new[] { "sleep", "infinity" });
}
