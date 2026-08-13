using FluentAssertions;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Docker backend for session sandboxes (extension spec Part B): a session is started
/// detached with name + deadline labels, work goes through <c>docker exec</c>, teardown is
/// guaranteed on every disposal path and idempotent, use-after-teardown is loud, and the
/// reaper sweeps leaked containers by deadline — fail-closed for sessions whose deadline
/// is unreadable. (The backend-neutral port and TestKit fake contracts are pinned in
/// <c>Nexo.Tests.Application</c>; this suite exercises the Infrastructure backend.)
/// </summary>
public sealed class DockerSandboxSessionTests
{
    private static SandboxSpec KeepaliveSpec(TimeSpan? timeout = null) => new(
        Image: "proposer:latest",
        Mounts: new[] { new Mount("/host/work", "/work", ReadOnly: false) },
        Network: NetworkAccess.None,
        Command: new[] { "sleep", "infinity" },
        Limits: new ResourceLimits(Memory: "2g", Timeout: timeout));

    // --- start ---------------------------------------------------------------------------

    [Fact]
    public void BuildStartArguments_RunsDetached_WithNameAndSessionAndDeadlineLabels()
    {
        var args = DockerSandboxedSessionRunner.BuildStartArguments(
            KeepaliveSpec(), "nexo-session-abc", deadlineUnixSeconds: 1234567890);

        args.Should().ContainInOrder("run", "-d", "--rm", "--name", "nexo-session-abc");
        args.Should().ContainInOrder("--label", DockerSandboxedSessionRunner.SessionLabel);
        args.Should().ContainInOrder("--label", $"{DockerSandboxedSessionRunner.DeadlineLabelKey}=1234567890");
        args.Should().Contain("--network=none");
        args.Should().Contain("/host/work:/work", "the session workspace mount is read-write");
        args.Should().ContainInOrder("proposer:latest", "sleep", "infinity");
    }

    [Fact]
    public void BuildStartArguments_Throws_WithoutKeepaliveCommand()
    {
        var spec = KeepaliveSpec() with { Command = Array.Empty<string>() };

        var act = () => DockerSandboxedSessionRunner.BuildStartArguments(spec, "s", 0);

        act.Should().Throw<ArgumentException>().WithMessage("*keepalive*");
    }

    [Fact]
    public async Task StartAsync_StampsTheDeadline_FromTheSpecTimeout()
    {
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        IReadOnlyList<string>? startArgs = null;
        var docker = new FakeProcessRunner((_, args) =>
        {
            startArgs ??= args.ToArray();
            return Task.FromResult(new ProcessCommandResult(0, "container-id", ""));
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(now));

        await using var session = await runner.StartAsync(KeepaliveSpec(TimeSpan.FromMinutes(10)));

        var expected = now.AddMinutes(10).ToUnixTimeSeconds();
        startArgs.Should().Contain($"{DockerSandboxedSessionRunner.DeadlineLabelKey}={expected}");
        session.SessionId.Should().StartWith(DockerSandboxedSessionRunner.NamePrefix);
    }

    [Fact]
    public async Task StartAsync_FailsClosed_WhenTheContainerCannotStart()
    {
        var docker = new FakeProcessRunner((_, _) =>
            Task.FromResult(new ProcessCommandResult(125, "", "no such image")));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));

        var act = () => runner.StartAsync(KeepaliveSpec());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no such image*");
    }

    // --- exec + teardown -----------------------------------------------------------------

    [Fact]
    public async Task Exec_RoutesThroughDockerExec_AndTeardownIsIdempotentRmForce()
    {
        var calls = new List<IReadOnlyList<string>>();
        var docker = new FakeProcessRunner((_, args) =>
        {
            calls.Add(args.ToArray());
            return Task.FromResult(new ProcessCommandResult(0, "ok", ""));
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));

        var session = await runner.StartAsync(KeepaliveSpec());
        await session.ExecAsync(new[] { "dotnet", "build" });
        await session.StopAsync();
        await session.DisposeAsync();

        calls[1].Should().ContainInOrder("exec", session.SessionId, "dotnet", "build");
        calls.Count(a => a.Take(2).SequenceEqual(new[] { "rm", "-f" }))
            .Should().Be(1, "teardown is idempotent — stop plus dispose must not double-remove");
        calls[2].Should().ContainInOrder("rm", "-f", session.SessionId);
    }

    [Fact]
    public async Task ExecAfterTeardown_IsLoud_NeverSilentlyRecreated()
    {
        var docker = new FakeProcessRunner((_, _) => Task.FromResult(new ProcessCommandResult(0, "", "")));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));
        var session = await runner.StartAsync(KeepaliveSpec());
        await session.DisposeAsync();

        var act = () => session.ExecAsync(new[] { "dotnet", "test" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*stopped*");
    }

    [Fact]
    public async Task TeardownFailure_NeverThrows_TheReaperIsTheBackstop()
    {
        var docker = new FakeProcessRunner((_, args) =>
            Task.FromResult(args[0] == "rm"
                ? new ProcessCommandResult(1, "", "removal already in progress")
                : new ProcessCommandResult(0, "", "")));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));
        var session = await runner.StartAsync(KeepaliveSpec());

        var act = () => session.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync("a failed teardown must not mask the work's own result");
    }

    // --- reaper --------------------------------------------------------------------------

    [Fact]
    public void ParseSessionLine_ReadsNameAndDeadline_AndTreatsGarbageDeadlineAsMissing()
    {
        DockerSandboxSessionReaper.ParseSessionLine("nexo-session-a\t1000")
            .Should().Be(("nexo-session-a", (long?)1000L));
        DockerSandboxSessionReaper.ParseSessionLine("nexo-session-b\tnot-a-number")
            .Should().Be(("nexo-session-b", (long?)null));
        DockerSandboxSessionReaper.ParseSessionLine("   ").Should().BeNull();
    }

    [Theory]
    [InlineData(999, true)]   // deadline in the past
    [InlineData(1000, true)]  // deadline exactly now
    [InlineData(1001, false)] // still alive
    public void ShouldReap_ComparesDeadlineToNow(long deadline, bool expected)
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1000);

        DockerSandboxSessionReaper.ShouldReap(deadline, now).Should().Be(expected);
    }

    [Fact]
    public void ShouldReap_TreatsMissingDeadlineAsExpired()
    {
        DockerSandboxSessionReaper.ShouldReap(null, DateTimeOffset.UnixEpoch)
            .Should().BeTrue("a session container without a readable deadline is a leak by definition");
    }

    [Fact]
    public async Task Sweep_RemovesExpiredAndUnlabeledSessions_LeavesLiveOnesAlone()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(2000);
        var removed = new List<string>();
        var docker = new FakeProcessRunner((_, args) =>
        {
            if (args[0] == "ps")
            {
                return Task.FromResult(new ProcessCommandResult(
                    0,
                    "nexo-session-expired\t1000\nnexo-session-live\t3000\nnexo-session-unlabeled\t\n",
                    ""));
            }

            removed.Add(args[2]);
            return Task.FromResult(new ProcessCommandResult(0, "", ""));
        });
        var reaper = new DockerSandboxSessionReaper(docker, new FrozenClock(now));

        var reaped = await reaper.SweepAsync();

        reaped.Should().BeEquivalentTo("nexo-session-expired", "nexo-session-unlabeled");
        removed.Should().BeEquivalentTo("nexo-session-expired", "nexo-session-unlabeled");
    }

    [Fact]
    public async Task Sweep_ReapsNothing_WhenTheListingFails()
    {
        var rmCalls = 0;
        var docker = new FakeProcessRunner((_, args) =>
        {
            if (args[0] == "ps")
                return Task.FromResult(new ProcessCommandResult(1, "", "daemon unreachable"));
            rmCalls++;
            return Task.FromResult(new ProcessCommandResult(0, "", ""));
        });
        var reaper = new DockerSandboxSessionReaper(docker, new FrozenClock(DateTimeOffset.UnixEpoch));

        var reaped = await reaper.SweepAsync();

        reaped.Should().BeEmpty();
        rmCalls.Should().Be(0, "an unreadable listing must not trigger blind removals — the next sweep retries");
    }

    // --- helpers -------------------------------------------------------------------------

    private sealed class FrozenClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FrozenClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class FakeProcessRunner : IProcessCommandRunner
    {
        private readonly Func<string, IReadOnlyList<string>, Task<ProcessCommandResult>> _handler;

        public FakeProcessRunner(Func<string, IReadOnlyList<string>, Task<ProcessCommandResult>> handler) =>
            _handler = handler;

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            fileName.Should().Be("docker");
            return _handler(fileName, arguments);
        }
    }
}
