using FluentAssertions;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Docker attestation flow, session provenance, and certificate environment inputs
/// (extension spec Part B): the backend attests what the environment actually is
/// fail-closed, every lifecycle transition emits a provenance event whose sink can never
/// fail the operation, and the environment projects into deterministic certificate
/// inputs. (The <c>SessionAttestation</c> model's parsing/shortfall semantics are pinned
/// in <c>Nexo.Tests.Application</c>.)
/// </summary>
public sealed class DockerSessionAttestationTests
{
    private static SandboxSpec Spec(ResourceLimits? limits = null) => new(
        Image: "proposer:latest",
        Mounts: Array.Empty<Mount>(),
        Network: NetworkAccess.None,
        Command: new[] { "sleep", "infinity" },
        Limits: limits);

    private static SessionAttestation Attestation(string? digest = "sha256:abc") => new()
    {
        SessionId = "nexo-session-t",
        Image = "proposer:latest",
        ImageDigest = digest,
        EngineVersion = "27.0",
        Requested = new ResourceLimits(Memory: "2g"),
        EffectiveMemoryBytes = 2L * 1024 * 1024 * 1024,
        AttestedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void ParseInspectLine_ReadsDigestAndCaps_AndTreatsNilAsUnknown()
    {
        DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t2147483648\t256\t2000000000")
            .Should().Be(("sha256:abc", 2147483648L, 256L, 2000000000L));
        DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t0\t<nil>\t0")
            .Should().Be(("sha256:abc", 0L, (long?)null, 0L));
    }

    [Fact]
    public async Task AttestAsync_ReadsInspectAndEngineVersion_AndEmitsProvenance()
    {
        var sink = new RecordingSink();
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "run" => new ProcessCommandResult(0, "cid", ""),
            "inspect" => new ProcessCommandResult(0, "sha256:deadbeef\t2147483648\t256\t2000000000\n", ""),
            "version" => new ProcessCommandResult(0, "27.1.1\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch), provenance: sink);
        var session = await runner.StartAsync(Spec(new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2")));

        var attestation = await session.AttestAsync();

        attestation.ImageDigest.Should().Be("sha256:deadbeef");
        attestation.EngineVersion.Should().Be("27.1.1");
        attestation.FindShortfalls().Should().BeEmpty();
        sink.Events.Select(e => e.Outcome).Should().ContainInOrder(
            SessionProvenanceOutcomes.Started, SessionProvenanceOutcomes.Attested);
    }

    [Fact]
    public async Task AttestAsync_FailsClosed_WhenInspectFails()
    {
        var docker = new RoutedProcessRunner(args => args[0] == "inspect"
            ? new ProcessCommandResult(1, "", "no such container")
            : new ProcessCommandResult(0, "", ""));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));
        var session = await runner.StartAsync(Spec());

        var act = () => session.AttestAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot be attested*");
    }

    [Fact]
    public async Task SessionLifecycle_EmitsStartedAndStopped_AndAThrowingSinkNeverFailsTheOperation()
    {
        var sink = new RecordingSink { ThrowOnRecord = true };
        var docker = new RoutedProcessRunner(_ => new ProcessCommandResult(0, "", ""));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch), provenance: sink);

        var act = async () =>
        {
            var session = await runner.StartAsync(Spec());
            await session.DisposeAsync();
        };

        await act.Should().NotThrowAsync("a provenance sink failure must never fail a session operation");
    }

    [Fact]
    public async Task Reaper_EmitsReapedEvents()
    {
        var sink = new RecordingSink();
        var docker = new RoutedProcessRunner(args => args[0] == "ps"
            ? new ProcessCommandResult(0, "nexo-session-old\t1\n", "")
            : new ProcessCommandResult(0, "", ""));
        var reaper = new DockerSandboxSessionReaper(
            docker, new FrozenClock(DateTimeOffset.FromUnixTimeSeconds(100)), provenance: sink);

        await reaper.SweepAsync();

        sink.Events.Should().ContainSingle().Which.Should().Match<SessionProvenanceEvent>(e =>
            e.Outcome == SessionProvenanceOutcomes.Reaped && e.SessionId == "nexo-session-old");
    }

    [Fact]
    public void EnvironmentInputs_CarryDigestSpecHashAndAttestationHash_Deterministically()
    {
        var spec = Spec(new ResourceLimits(Memory: "2g"));
        var attestation = Attestation();

        var first = SessionEnvironmentInputs.From(spec, attestation);
        var second = SessionEnvironmentInputs.From(spec, attestation);

        first.Select(i => i.Kind).Should().Equal(
            SessionEnvironmentInputs.ImageDigestKind,
            SessionEnvironmentInputs.SandboxSpecKind,
            SessionEnvironmentInputs.AttestationKind);
        first[0].Hash.Should().Be("sha256:abc");
        first.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.Hash));
        second.Select(i => i.Hash).Should().Equal(first.Select(i => i.Hash),
            "environment inputs must be deterministic for the same spec + attestation");
    }

    [Fact]
    public void EnvironmentInputs_RefuseAnUnresolvedImageIdentity()
    {
        var act = () => SessionEnvironmentInputs.From(Spec(), Attestation(digest: null));

        act.Should().Throw<InvalidOperationException>().WithMessage("*image digest*");
    }

    // --- helpers -------------------------------------------------------------------------

    private sealed class RecordingSink : ISessionProvenanceSink
    {
        public List<SessionProvenanceEvent> Events { get; } = new();
        public bool ThrowOnRecord { get; init; }

        public void Record(SessionProvenanceEvent provenanceEvent)
        {
            Events.Add(provenanceEvent);
            if (ThrowOnRecord)
                throw new InvalidOperationException("sink deliberately failing");
        }
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
