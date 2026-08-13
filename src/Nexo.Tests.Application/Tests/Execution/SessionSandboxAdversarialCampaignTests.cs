using FluentAssertions;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Application.Tests.Execution;

/// <summary>
/// Adversarial validation campaign, session half (extension spec §12): scripted
/// adversaries attack the sandbox lifecycle, confinement, network modes, attestation, and
/// the certificate's environment inputs. Acceptance criterion: no attack yields host
/// execution, silent degradation, an unreaped leak, or a certifiable environment record —
/// the failure is always loud and closed.
/// </summary>
public sealed class SessionSandboxAdversarialCampaignTests
{
    private static SandboxSpec KeepaliveSpec(
        NetworkAccess network = NetworkAccess.None,
        ResourceLimits? limits = null,
        IReadOnlyList<Mount>? mounts = null) => new(
        Image: "proposer:latest",
        Mounts: mounts ?? Array.Empty<Mount>(),
        Network: network,
        Command: new[] { "sleep", "infinity" },
        Limits: limits);

    [Fact]
    public async Task Attack_work_after_teardown_is_refused_never_rehomed_to_the_host()
    {
        // Adversary: hold the session reference past disposal and keep submitting work,
        // betting on a silent re-create or host fallback. Both backends refuse loudly.
        var docker = new RoutedProcessRunner(_ => new ProcessCommandResult(0, "", ""));
        var real = await new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch))
            .StartAsync(KeepaliveSpec());
        await real.DisposeAsync();
        var fake = await new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success())
            .StartAsync(KeepaliveSpec());
        await fake.DisposeAsync();

        foreach (var session in new ISandboxedSession[] { real, fake })
        {
            var act = () => session.ExecAsync(new[] { "curl", "evil.example" });
            await act.Should().ThrowAsync<InvalidOperationException>(
                "a torn-down session must never quietly serve work again");
        }
    }

    [Fact]
    public void Attack_requesting_unrealizable_containment_gets_refusal_not_degradation()
    {
        // Adversary: request HostServicesOnly hoping the v1 backend quietly falls back to
        // bridge networking with open egress while the spec claims containment.
        var spec = KeepaliveSpec(NetworkAccess.HostServicesOnly);

        var oneShot = () => DockerSandboxedCommandRunner.BuildDockerArguments(spec);
        var session = () => DockerSandboxedSessionRunner.BuildStartArguments(spec, "nexo-session-x", 0);

        oneShot.Should().Throw<NotSupportedException>();
        session.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Attack_smuggling_a_writable_surface_through_overlapping_confinement_is_refused()
    {
        // Adversary: declare "src/" read-write twice at different depths so that mount
        // ordering decides effective permissions — then rely on the wider one winning.
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { "src/Generated/", "src/" } };

        var act = () => confinement.ToMounts("/repo");

        act.Should().Throw<InvalidOperationException>().WithMessage("*overlap*");
    }

    [Fact]
    public async Task Attack_stripping_the_deadline_label_makes_the_reaper_MORE_aggressive()
    {
        // Adversary: remove/garble the deadline label so the sweep can never prove the
        // session expired. Fail-closed: an unattributable session container is a leak by
        // definition and dies on the next sweep.
        var removed = new List<string>();
        var docker = new RoutedProcessRunner(args =>
        {
            if (args[0] == "ps")
                return new ProcessCommandResult(0, "nexo-session-stripped\t\nnexo-session-garbled\tsoon\n", "");
            removed.Add(args[2]);
            return new ProcessCommandResult(0, "", "");
        });
        var reaper = new DockerSandboxSessionReaper(docker, new FrozenClock(DateTimeOffset.UnixEpoch));

        var reaped = await reaper.SweepAsync();

        reaped.Should().BeEquivalentTo("nexo-session-stripped", "nexo-session-garbled");
        removed.Should().HaveCount(2);
    }

    [Fact]
    public async Task Attack_sabotaging_the_provenance_sink_cannot_block_teardown()
    {
        // Adversary: register a sink that throws, hoping teardown aborts and the
        // container survives with its mounts attached.
        var rmCalls = 0;
        var docker = new RoutedProcessRunner(args =>
        {
            if (args[0] == "rm")
                rmCalls++;
            return new ProcessCommandResult(0, "", "");
        });
        var runner = new DockerSandboxedSessionRunner(
            docker, new FrozenClock(DateTimeOffset.UnixEpoch), provenance: new SabotageSink());

        var session = await runner.StartAsync(KeepaliveSpec());
        await session.DisposeAsync();

        rmCalls.Should().Be(1, "teardown must complete regardless of sink behavior");
    }

    [Fact]
    public async Task Attack_weakened_environment_cannot_be_attested_into_a_certificate()
    {
        // Adversary: give the harness an engine that silently ignores the requested
        // memory cap, then try to carry that environment into certificate inputs.
        var runner = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success());
        var spec = KeepaliveSpec(limits: new ResourceLimits(Memory: "2g"));
        var session = (FakeSandboxedSession)await runner.StartAsync(spec);
        var honest = await session.AttestAsync();
        session.AttestationOverride = honest with { EffectiveMemoryBytes = 0 };

        var weakened = await session.AttestAsync();

        var refuse = weakened.ThrowIfBelowRequested;
        refuse.Should().Throw<InvalidOperationException>("refuse-below-floor is the harness's gate");

        // And even if a compromised harness skipped the refusal, an attestation with no
        // resolved image identity can never become certificate inputs.
        var unresolved = weakened with { ImageDigest = null };
        var inputs = () => SessionEnvironmentInputs.From(spec, unresolved);
        inputs.Should().Throw<InvalidOperationException>().WithMessage("*image digest*");
    }

    [Fact]
    public async Task Attack_crashing_mid_work_still_tears_the_session_down()
    {
        // Adversary: make the work inside the harness scope throw, betting the session
        // outlives the crash. `await using` is the teardown contract on exception paths.
        var runner = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Failure("boom"));

        var act = async () =>
        {
            await using var session = await runner.StartAsync(KeepaliveSpec());
            await session.ExecAsync(new[] { "dotnet", "test" });
            throw new InvalidOperationException("harness crash mid-work");
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
        runner.ActiveSessions.Should().Be(0, "no attack may leave a live session behind");
    }

    // --- helpers -------------------------------------------------------------------------

    private sealed class SabotageSink : ISessionProvenanceSink
    {
        public void Record(SessionProvenanceEvent provenanceEvent) =>
            throw new InvalidOperationException("sink sabotage");
    }

    private sealed class FrozenClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FrozenClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class RoutedProcessRunner : IProcessCommandRunner
    {
        private readonly Func<IReadOnlyList<string>, ProcessCommandResult> _handler;

        public RoutedProcessRunner(Func<IReadOnlyList<string>, ProcessCommandResult> handler) =>
            _handler = handler;

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_handler(arguments));
    }
}
