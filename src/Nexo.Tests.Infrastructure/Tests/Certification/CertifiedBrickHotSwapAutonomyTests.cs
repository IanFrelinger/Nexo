using FluentAssertions;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Certification.HotSwap;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The swap host's autonomy surfaces (autonomous self-extension spec R3.2 third leg,
/// R4.2, R5.1, R5.3): tier-mismatched autonomous admissions are refused independently of
/// the certifier, the recursion ceiling is re-enforced, retained generations reactivate
/// from their emitted images with no compiler run, and a revoked hash never loads again —
/// rollback included.
/// </summary>
[Trait("Category", "Certification")]
public sealed class CertifiedBrickHotSwapAutonomyTests
{
    private const string HmacKey = "hot-swap-autonomy-test-hmac";
    private const string ProbeBrickId = "hot-swap-probe";

    private const string ProbeBrickTemplate = """
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.HotSwapProbe;

public sealed class HotSwapProbeBrick : DomainBrick
{
    public HotSwapProbeBrick()
    {
        Id = "hot-swap-probe";
        Name = "Hot Swap Probe";
        Description = "Marker brick for hot-swap autonomy tests.";
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput { Summary = "{MARKER}" };
        output.Set("marker", "{MARKER}");
        return Task.FromResult(output);
    }
}
""";

    [Fact]
    public async Task AutonomousTier1_IsRefused_AtTheSwapHost_IndependentlyOfTheCertifier()
    {
        using var host = CreateHost();
        var request = ProbeRequest(ProbeSource("v1")) with
        {
            Autonomous = new AutonomousAdmission { Tier = ObjectiveTier.Tier1HumanAdmission }
        };

        var result = await host.SwapAsync(new[] { request });

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle().Which.FailureCode
            .Should().Be("tier-requires-human-admission");
        host.CurrentGenerationId.Should().BeNull("nothing may load on a refusal");
    }

    [Fact]
    public async Task AutonomousTier0_Swaps_AndHumanDrivenFlowsAreUntouched()
    {
        using var host = CreateHost();

        var autonomous = ProbeRequest(ProbeSource("v1")) with
        {
            Autonomous = new AutonomousAdmission
            {
                Tier = ObjectiveTier.Tier0Autonomous,
                Lineage = GenerationLineage.Child(GenerationLineage.HumanAuthored, "sig-parent"),
            }
        };
        (await host.SwapAsync(new[] { autonomous })).Swapped.Should().BeTrue();

        // Null Autonomous = a human is driving; the historical flow keeps working.
        (await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v2")) })).Swapped.Should().BeTrue();
    }

    [Fact]
    public async Task TheSwapHost_ReenforcesTheRecursionCeiling_Independently()
    {
        using var host = CreateHost();
        var request = ProbeRequest(ProbeSource("v1")) with
        {
            Autonomous = new AutonomousAdmission
            {
                Tier = ObjectiveTier.Tier0Autonomous,
                Lineage = new GenerationLineage
                {
                    Depth = 3,
                    ParentCertificateSignatures = ["a", "b", "c"],
                }
            }
        };

        var result = await host.SwapAsync(new[] { request });

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("recursion-refused");
    }

    [Fact]
    public async Task Rollback_ReactivatesTheRetainedGeneration_FromItsEmittedImages()
    {
        var sink = new RecordingSink();
        using var host = CreateHost(sink);
        (await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) })).Swapped.Should().BeTrue();
        (await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v2")) })).Swapped.Should().BeTrue();
        (await Execute(host)).Get<string>("marker").Should().Be("v2");

        var rollback = await host.RollbackToAsync(1);

        rollback.Swapped.Should().BeTrue(Describe(rollback));
        (await Execute(host)).Get<string>("marker").Should().Be("v1",
            "rollback reactivates generation 1's behavior");
        sink.Snapshot().Should().Contain(e =>
            e.Outcome == BrickSwapProvenanceOutcomes.RollbackCommitted &&
            e.Reason!.Contains("generation 1"));
    }

    [Fact]
    public async Task Rollback_ToAGenerationOutsideTheWindow_IsRefusedLoudly()
    {
        using var host = CreateHost();
        (await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) })).Swapped.Should().BeTrue();

        var result = await host.RollbackToAsync(99);

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("no-retained-generation");
    }

    [Fact]
    public async Task RevokedHash_IsRefusedPermanently_EvenViaRollback()
    {
        var revocations = new InMemoryCertificateRevocationList();
        using var host = CreateHost(revocations: revocations);
        var v1 = ProbeSource("v1");
        (await host.SwapAsync(new[] { ProbeRequest(v1) })).Swapped.Should().BeTrue();
        (await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v2")) })).Swapped.Should().BeTrue();

        // Quarantine v1 (as the watch window will after a regression, R5.3).
        revocations.Revoke(BrickContentHasher.ComputeSha256(v1), "regressed post-swap");

        // Bit-identical resubmission: refused.
        var resubmit = await host.SwapAsync(new[] { ProbeRequest(v1) });
        resubmit.Swapped.Should().BeFalse();
        resubmit.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("revoked-hash");

        // Rollback to the generation containing the revoked hash: also refused —
        // quarantine outranks retention.
        var rollback = await host.RollbackToAsync(1);
        rollback.Swapped.Should().BeFalse();
        rollback.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("revoked-hash");

        // The unrevoked v2 generation keeps serving untouched.
        (await Execute(host)).Get<string>("marker").Should().Be("v2");
    }

    // --- helpers -------------------------------------------------------------------------

    private static CertifiedBrickHotSwapHost CreateHost(
        RecordingSink? sink = null,
        ICertificateRevocationList? revocations = null) =>
        new(sink, logger: null, hmacKey: HmacKey, drainTimeout: TimeSpan.FromSeconds(10),
            revocations: revocations, retentionWindow: 2);

    private static string ProbeSource(string marker) =>
        ProbeBrickTemplate.Replace("{MARKER}", marker);

    private static CertifiedBrickLoadRequest ProbeRequest(string source) => new()
    {
        BrickId = ProbeBrickId,
        SourceCode = source,
        Record = CertifyRecord(ProbeBrickId, source)
    };

    private static CertificationRecordData CertifyRecord(string brickId, string source)
    {
        var record = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = brickId,
            ContentHash = BrickContentHasher.ComputeSha256(source),
            Gate = "hot-swap-autonomy-test-harness"
        };
        return record with { Signature = CertificationRecordSigning.Sign(record, HmacKey) };
    }

    private static Task<BrickOutput> Execute(CertifiedBrickHotSwapHost host) =>
        host.ExecuteAsync(
            ProbeBrickId,
            new BrickInput(new Dictionary<string, object>()),
            ImplementationType.Deterministic,
            new TestExecutionContext());

    private static string Describe(CertifiedBrickSwapResult result) =>
        result.Swapped
            ? "swapped"
            : string.Join("; ", result.Refusals.Select(r => $"{r.BrickId} {r.Stage} {r.FailureCode}: {r.Reason}"));

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId => "hot-swap-autonomy-test";
        public string BehaviorId => "hot-swap-autonomy-test";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "deterministic";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }

    private sealed class RecordingSink : ICertifiedBrickSwapProvenanceSink
    {
        private readonly object _gate = new();
        private readonly List<BrickSwapProvenanceEvent> _events = new();

        public void Record(BrickSwapProvenanceEvent provenanceEvent)
        {
            lock (_gate)
            {
                _events.Add(provenanceEvent);
            }
        }

        public IReadOnlyList<BrickSwapProvenanceEvent> Snapshot()
        {
            lock (_gate)
            {
                return _events.ToList();
            }
        }
    }
}
