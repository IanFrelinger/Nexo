using FluentAssertions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Step 4: GenerativeProvenance emission, AuditMode policy, IApprovalGate routing.
/// </summary>
public sealed class GenerativeProvenanceTests
{
    [Fact]
    public void EmitProvenance_SetsTypedOutputAndStrategyString()
    {
        var brick = new TestGenerativeBrick();
        var output = new BrickOutput();
        var provenance = GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true);

        brick.Emit(output, provenance);

        output.Get<object>(GenerativeBrick.ProvenanceOutputKey).Should().Be(provenance);
        output.Get<string>("generationStrategy").Should().Be("Templated");
    }

    [Fact]
    public void ApplyAuditPolicy_ForcesHumanReview_ForModelUnderAuditMode()
    {
        var brick = new TestGenerativeBrick();
        var provenance = GenerativeProvenance.Create(GenerationStrategy.Model, requiresHumanReview: false);
        var ctx = new FakeContext(auditMode: true);

        var adjusted = brick.Apply(provenance, ctx);

        adjusted.RequiresHumanReview.Should().BeTrue();
        adjusted.Strategy.Should().Be(GenerationStrategy.Model);
    }

    [Fact]
    public void ApplyAuditPolicy_LeavesTemplatedAlone_UnderAuditMode()
    {
        var brick = new TestGenerativeBrick();
        var provenance = GenerativeProvenance.Create(GenerationStrategy.Templated, requiresHumanReview: false);
        var adjusted = brick.Apply(provenance, new FakeContext(auditMode: true));
        adjusted.RequiresHumanReview.Should().BeFalse();
    }

    [Fact]
    public async Task RouteHumanReview_SkipsGate_WhenNotRequired()
    {
        var called = false;
        var ok = await GenerativeBrick.RouteHumanReviewIfRequiredAsync(
            GenerativeProvenance.Create(GenerationStrategy.Deterministic),
            (_, _, _) =>
            {
                called = true;
                return Task.FromResult(true);
            },
            "action",
            TimeSpan.FromSeconds(1));

        ok.Should().BeTrue();
        called.Should().BeFalse();
    }

    [Fact]
    public async Task RouteHumanReview_Denies_WhenRequiredAndGateNull()
    {
        var ok = await GenerativeBrick.RouteHumanReviewIfRequiredAsync(
            GenerativeProvenance.Create(GenerationStrategy.Model, requiresHumanReview: true),
            requestApproval: null,
            "action",
            TimeSpan.FromSeconds(1));

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task GenerativeHumanReview_RoutesThroughIApprovalGate()
    {
        var gate = new AlwaysApprove();
        var result = await GenerativeHumanReview.RouteAsync(
            GenerativeProvenance.Create(GenerationStrategy.Model, requiresHumanReview: true),
            gate,
            "install artifact",
            TimeSpan.FromSeconds(1));

        result.Should().Be(ApprovalResult.Approved);
    }

    [Fact]
    public async Task GenerativeHumanReview_Denies_WhenGateMissingAndReviewRequired()
    {
        var result = await GenerativeHumanReview.RouteAsync(
            GenerativeProvenance.Create(GenerationStrategy.Model, requiresHumanReview: true),
            gate: null,
            "install artifact",
            TimeSpan.FromSeconds(1));

        result.Should().Be(ApprovalResult.Denied);
    }

    private sealed class TestGenerativeBrick : GenerativeBrick
    {
        public void Emit(BrickOutput output, GenerativeProvenance provenance) =>
            EmitProvenance(output, provenance);

        public GenerativeProvenance Apply(GenerativeProvenance provenance, IExecutionContext context) =>
            ApplyAuditPolicy(provenance, context);

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrickOutput());
    }

    private sealed class FakeContext : IExecutionContext
    {
        public FakeContext(bool auditMode) => AuditMode = auditMode;
        public string AgentId => "test";
        public string BehaviorId => "test";
        public bool IsAirGapped => false;
        public bool AuditMode { get; }
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }

    private sealed class AlwaysApprove : IApprovalGate
    {
        public Task<ApprovalResult> RequestApprovalAsync(
            string actionDescription,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApprovalResult.Approved);
    }
}
