using System.Runtime.Loader;
using FluentAssertions;
using Nexo.Certification.Contracts;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Certification.HotSwap;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Hot-swap host tests: verify-at-load refusals, fail-closed (no partial) swaps,
/// generation succession with drain + collection, and provenance events per outcome.
/// </summary>
[Trait("Category", "Certification")]
public sealed class CertifiedBrickHotSwapHostTests
{
    private const string HmacKey = "hot-swap-host-test-hmac";
    private const string ProbeBrickId = "hot-swap-probe";
    private const string OtherBrickId = "hot-swap-other";

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
        Description = "Marker brick for hot-swap host tests.";
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var delayMs = input.Get<int>("delayMs", 0);
        if (delayMs > 0)
            await Task.Delay(delayMs, cancellationToken);
        var output = new BrickOutput { Summary = "{MARKER}" };
        output.Set("marker", "{MARKER}");
        return output;
    }
}
""";

    private const string OtherBrickSource = """
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.HotSwapProbe;

public sealed class HotSwapOtherBrick : DomainBrick
{
    public HotSwapOtherBrick()
    {
        Id = "hot-swap-other";
        Name = "Hot Swap Other";
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput { Summary = "other" };
        output.Set("marker", "other");
        return Task.FromResult(output);
    }
}
""";

    [Fact]
    public async Task Swap_ValidCertifiedBrick_ServesInvocations()
    {
        var sink = new RecordingSink();
        using var host = CreateHost(sink);
        var source = ProbeSource("v1");

        var result = await host.SwapAsync(new[] { ProbeRequest(source) });

        result.Swapped.Should().BeTrue(Describe(result));
        result.GenerationId.Should().Be(1);
        host.CurrentGenerationId.Should().Be(1);
        host.CurrentBrickIds.Should().Contain(ProbeBrickId);

        var output = await Execute(host, ProbeBrickId);
        output.Get<string>("marker").Should().Be("v1");

        var outcomes = sink.Snapshot().Select(e => e.Outcome).ToList();
        outcomes.Should().Contain(BrickSwapProvenanceOutcomes.BrickLoaded);
        outcomes.Should().Contain(BrickSwapProvenanceOutcomes.SwapCommitted);
        sink.Snapshot().Should().Contain(e =>
            e.Outcome == BrickSwapProvenanceOutcomes.BrickLoaded &&
            e.BrickId == ProbeBrickId &&
            !string.IsNullOrWhiteSpace(e.ContentHash));
    }

    [Fact]
    public async Task Swap_TamperedSource_RefusesWholeSwap_PreviousGenerationKeepsServing()
    {
        var sink = new RecordingSink();
        using var host = CreateHost(sink);
        await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) });

        // One valid brick and one whose bytes changed after certification: the whole
        // swap must be refused — the valid brick must not load either (no partial swap).
        var tamperedSource = ProbeSource("v2");
        var tamperedRequest = ProbeRequest(tamperedSource) with
        {
            SourceCode = tamperedSource + "// tampered after certification"
        };
        var result = await host.SwapAsync(new[]
        {
            OtherRequest(),
            tamperedRequest
        });

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle(r =>
            r.BrickId == ProbeBrickId &&
            r.Stage == BrickSwapRefusalStage.Verification &&
            r.FailureCode == "content-hash-mismatch");

        host.CurrentGenerationId.Should().Be(1, "a refused swap must leave generation 1 serving");
        (await Execute(host, ProbeBrickId)).Get<string>("marker").Should().Be("v1");
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Execute(host, OtherBrickId));

        sink.Snapshot().Should().Contain(e =>
            e.Outcome == BrickSwapProvenanceOutcomes.SwapRefused);
        sink.Snapshot().Should().Contain(e =>
            e.Outcome == BrickSwapProvenanceOutcomes.BrickRefused &&
            e.BrickId == ProbeBrickId &&
            e.FailureCode == "content-hash-mismatch");
    }

    [Fact]
    public async Task Swap_UnadmittedRecord_IsRefused()
    {
        using var host = CreateHost(new RecordingSink());
        var source = ProbeSource("v1");
        var request = ProbeRequest(source) with
        {
            Record = CertifyRecord(ProbeBrickId, source) with { Admitted = false, Status = "FAIL" }
        };

        var result = await host.SwapAsync(new[] { request });

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle(r => r.FailureCode == "record-not-admitted");
        host.CurrentGenerationId.Should().BeNull();
    }

    [Fact]
    public async Task Swap_RecordMintedForDifferentBrick_IsRefused()
    {
        using var host = CreateHost(new RecordingSink());
        var source = ProbeSource("v1");
        var request = ProbeRequest(source) with
        {
            Record = CertifyRecord("some-other-brick", source)
        };

        var result = await host.SwapAsync(new[] { request });

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle(r =>
            r.Stage == BrickSwapRefusalStage.Request &&
            r.FailureCode == "record-brick-mismatch");
    }

    [Fact]
    public async Task Swap_CertifiedButNonCompilingSource_IsRefused_AndContextIsTornDown()
    {
        var sink = new RecordingSink();
        using var host = CreateHost(sink);
        const string garbage = "this is certified but it is not C#";
        var request = new CertifiedBrickLoadRequest
        {
            BrickId = ProbeBrickId,
            SourceCode = garbage,
            Record = CertifyRecord(ProbeBrickId, garbage)
        };

        var result = await host.SwapAsync(new[] { request });

        result.Swapped.Should().BeFalse();
        result.Refusals.Should().ContainSingle(r =>
            r.Stage == BrickSwapRefusalStage.Compilation &&
            r.FailureCode == "compile-failed");
        host.CurrentGenerationId.Should().BeNull();

        // Earlier tests' disposed hosts may leave contexts pending collection (Dispose
        // deliberately skips forced GC), so sweep before asserting emptiness.
        for (var i = 0; i < 10 && HasGenerationContext(); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        AssemblyLoadContext.All.Should().NotContain(
            c => c.Name != null && c.Name.StartsWith("BrickGeneration_", StringComparison.Ordinal),
            "a refused swap must not leave a half-built generation context behind");
    }

    [Fact]
    public async Task SecondSwap_ReplacesGeneration_AndOldContextIsCollected()
    {
        var sink = new RecordingSink();
        using var host = CreateHost(sink);

        var first = await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) });
        (await Execute(host, ProbeBrickId)).Get<string>("marker").Should().Be("v1");

        var second = await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v2")) });

        second.Swapped.Should().BeTrue(Describe(second));
        second.GenerationId.Should().Be(2);
        (await Execute(host, ProbeBrickId)).Get<string>("marker").Should().Be("v2");

        second.PreviousGenerationContextName.Should().Be(first.GenerationContextName);
        second.PreviousGenerationCollected.Should().BeTrue(
            "the drained generation-1 context must be collected once its bricks are dropped");
        AssemblyLoadContext.All.Should().NotContain(
            c => c.Name == first.GenerationContextName,
            "the retired context must be gone from AssemblyLoadContext.All (leak attribution by name)");

        var gen1Events = sink.Snapshot().Where(e => e.Generation == 1).Select(e => e.Outcome).ToList();
        gen1Events.Should().Contain(BrickSwapProvenanceOutcomes.GenerationRetired);
        gen1Events.Should().Contain(BrickSwapProvenanceOutcomes.GenerationCollected);
    }

    [Fact]
    public async Task InFlightInvocation_Drains_BeforeOldGenerationUnloads()
    {
        var sink = new RecordingSink();
        using var host = CreateHost(sink);
        await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) });

        var input = new BrickInput(new Dictionary<string, object> { ["delayMs"] = 1200 });
        var inFlight = host.ExecuteAsync(
            ProbeBrickId, input, ImplementationType.Deterministic, new TestExecutionContext());
        await Task.Delay(150);

        var swap = await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v2")) });

        var inFlightOutput = await inFlight;
        inFlightOutput.Get<string>("marker").Should().Be("v1",
            "the invocation that started on generation 1 must complete on generation 1");
        swap.Swapped.Should().BeTrue(Describe(swap));
        swap.PreviousGenerationCollected.Should().BeTrue(
            "the swap must drain the in-flight invocation before unloading generation 1");
        (await Execute(host, ProbeBrickId)).Get<string>("marker").Should().Be("v2");
    }

    [Fact]
    public async Task ConcurrentSwaps_Serialize_AndBothCommit()
    {
        using var host = CreateHost(new RecordingSink());
        await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) });

        var swapB = host.SwapAsync(new[] { ProbeRequest(ProbeSource("markerB")) });
        var swapC = host.SwapAsync(new[] { ProbeRequest(ProbeSource("markerC")) });
        var results = await Task.WhenAll(swapB, swapC);

        results.Should().OnlyContain(r => r.Swapped);
        results.Select(r => r.GenerationId).Should().BeEquivalentTo(new int?[] { 2, 3 });
        host.CurrentGenerationId.Should().Be(3);

        var winner = results.Single(r => r.GenerationId == 3);
        var expectedMarker = ReferenceEquals(winner, results[0]) ? "markerB" : "markerC";
        (await Execute(host, ProbeBrickId)).Get<string>("marker").Should().Be(expectedMarker);
    }

    [Fact]
    public async Task Execute_UnknownBrick_Throws()
    {
        using var host = CreateHost(new RecordingSink());
        await host.SwapAsync(new[] { ProbeRequest(ProbeSource("v1")) });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Execute(host, "no-such-brick"));
    }

    [Fact]
    public async Task Execute_BeforeFirstSwap_Throws()
    {
        using var host = CreateHost(new RecordingSink());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(host, ProbeBrickId));
    }

    // ---------- helpers ----------

    private static bool HasGenerationContext() =>
        AssemblyLoadContext.All.Any(c =>
            c.Name != null && c.Name.StartsWith("BrickGeneration_", StringComparison.Ordinal));

    private static CertifiedBrickHotSwapHost CreateHost(RecordingSink sink) =>
        new(sink, logger: null, hmacKey: HmacKey, drainTimeout: TimeSpan.FromSeconds(10));

    private static string ProbeSource(string marker) =>
        ProbeBrickTemplate.Replace("{MARKER}", marker);

    private static CertifiedBrickLoadRequest ProbeRequest(string source) => new()
    {
        BrickId = ProbeBrickId,
        SourceCode = source,
        Record = CertifyRecord(ProbeBrickId, source)
    };

    private static CertifiedBrickLoadRequest OtherRequest() => new()
    {
        BrickId = OtherBrickId,
        SourceCode = OtherBrickSource,
        Record = CertifyRecord(OtherBrickId, OtherBrickSource)
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
            Gate = "hot-swap-test-harness"
        };
        return record with { Signature = CertificationRecordSigning.Sign(record, HmacKey) };
    }

    private static Task<BrickOutput> Execute(CertifiedBrickHotSwapHost host, string brickId) =>
        host.ExecuteAsync(
            brickId,
            new BrickInput(new Dictionary<string, object>()),
            ImplementationType.Deterministic,
            new TestExecutionContext());

    private static string Describe(CertifiedBrickSwapResult result) =>
        result.Swapped
            ? "swapped"
            : string.Join("; ", result.Refusals.Select(r => $"{r.BrickId} {r.Stage} {r.FailureCode}: {r.Reason}"));

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId => "hot-swap-test";
        public string BehaviorId => "hot-swap-test";
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
