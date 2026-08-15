using FluentAssertions;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Execution.Ports;
using Xunit;

namespace Nexo.Tests.Application.Tests.Execution;

/// <summary>
/// The <see cref="SessionAttestation"/> model (extension spec Part B): backend encoding
/// parsers, the fail-closed shortfall verdicts, the refuse-below-floor gate, and the
/// TestKit fake's faithful-by-default attestation. The Docker attestation flow,
/// provenance lifecycle, and certificate environment inputs are pinned in
/// <c>Nexo.Tests.Infrastructure</c> (<c>DockerSessionAttestationTests</c>).
/// </summary>
public sealed class SessionAttestationTests
{
    private static SessionAttestation Attestation(
        ResourceLimits requested,
        long? memory = null,
        long? pids = null,
        long? nano = null,
        string? digest = "sha256:abc") => new()
    {
        SessionId = "nexo-session-t",
        Image = "proposer:latest",
        ImageDigest = digest,
        EngineVersion = "27.0",
        Requested = requested,
        EffectiveMemoryBytes = memory,
        EffectivePidsLimit = pids,
        EffectiveNanoCpus = nano,
        AttestedAt = DateTimeOffset.UnixEpoch,
    };

    // --- parsing -------------------------------------------------------------------------

    [Theory]
    [InlineData("2g", 2L * 1024 * 1024 * 1024)]
    [InlineData("512m", 512L * 1024 * 1024)]
    [InlineData("64k", 64L * 1024)]
    [InlineData("1024", 1024L)]
    [InlineData("junk", null)]
    [InlineData("", null)]
    [InlineData("-5m", null)]
    public void ParseMemoryToBytes_HandlesBackendEncodings(string value, long? expected) =>
        SessionAttestation.ParseMemoryToBytes(value).Should().Be(expected);

    [Theory]
    [InlineData("2", 2_000_000_000L)]
    [InlineData("0.5", 500_000_000L)]
    [InlineData("x", null)]
    public void ParseCpusToNano_HandlesBackendEncodings(string value, long? expected) =>
        SessionAttestation.ParseCpusToNano(value).Should().Be(expected);

    // --- shortfall verdicts --------------------------------------------------------------

    [Fact]
    public void FaithfulEnvironment_HasNoShortfalls()
    {
        var attestation = Attestation(
            new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2"),
            memory: 2L * 1024 * 1024 * 1024, pids: 256, nano: 2_000_000_000);

        attestation.FindShortfalls().Should().BeEmpty();
        var act = attestation.ThrowIfBelowRequested;
        act.Should().NotThrow();
    }

    [Fact]
    public void WeakerOrUnverifiedLimits_AreShortfalls_AndStricterOnesAreNot()
    {
        var requested = new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2");

        // Weaker in every dimension: bigger memory cap, unlimited pids, more cpu.
        Attestation(requested, memory: 4L * 1024 * 1024 * 1024, pids: 0, nano: 4_000_000_000)
            .FindShortfalls().Should().HaveCount(3);
        // Unknown values are unverified, which is fail-closed a shortfall.
        Attestation(requested, memory: null, pids: null, nano: null)
            .FindShortfalls().Should().HaveCount(3);
        // Stricter than requested is never a shortfall.
        Attestation(requested, memory: 1L * 1024 * 1024 * 1024, pids: 128, nano: 1_000_000_000)
            .FindShortfalls().Should().BeEmpty();
    }

    [Fact]
    public void UnparsableRequestedLimit_IsAShortfall_NotATrustedGuess()
    {
        Attestation(new ResourceLimits(Memory: "lots"), memory: 123456789)
            .FindShortfalls().Should().ContainSingle().Which.Should().Contain("unverifiable");
    }

    [Fact]
    public void NothingRequested_MeansNothingToVerify()
    {
        Attestation(new ResourceLimits()).FindShortfalls().Should().BeEmpty();
    }

    [Fact]
    public void ThrowIfBelowRequested_NamesEveryShortfall()
    {
        var attestation = Attestation(new ResourceLimits(Memory: "2g", Pids: 10), memory: null, pids: null);

        var act = attestation.ThrowIfBelowRequested;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*refusing*memory*")
            .WithMessage("*pids*");
    }

    // --- TestKit fake --------------------------------------------------------------------

    [Fact]
    public async Task FakeSession_AttestsFaithfullyByDefault_AndHonorsOverrides()
    {
        var runner = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success());
        var session = (FakeSandboxedSession)await runner.StartAsync(new SandboxSpec(
            Image: "proposer:latest",
            Mounts: Array.Empty<Mount>(),
            Network: NetworkAccess.None,
            Command: new[] { "sleep", "infinity" },
            Limits: new ResourceLimits(Memory: "2g", Pids: 64, Cpus: "1")));

        var faithful = await session.AttestAsync();
        faithful.FindShortfalls().Should().BeEmpty("the default fake environment honors the request");

        session.AttestationOverride = faithful with { EffectiveMemoryBytes = null };
        var weakened = await session.AttestAsync();
        weakened.FindShortfalls().Should().ContainSingle();
    }
}
