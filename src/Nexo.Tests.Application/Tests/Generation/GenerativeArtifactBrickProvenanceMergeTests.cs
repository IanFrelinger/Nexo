using FluentAssertions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Proposed additive core change: merge drafter grounding signals into final
/// provenance and re-validate deterministic drafts even when Provenance is set.
/// </summary>
public sealed class GenerativeArtifactBrickProvenanceMergeTests
{
    [Fact]
    public async Task DeterministicPath_MergesUnsupportedReferences_AndRunsValidator()
    {
        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "merge-test",
            Drafter = new StubDrafter(),
            DeterministicDrafter = new StubDetDrafter(),
            Validators = new object[] { new AlwaysOkValidator() },
            Tunables = new GenerationTunables { PreferDeterministic = true, MaxRepairAttempts = 1 },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true
            },
            Knowledge = new DomainKnowledge(),
            Llm = new LLMConfig { Model = "none" }
        });

        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "merge-test");
        input.Set("grounding", "x");

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new FakeContext(),
            CancellationToken.None);

        var prov = output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        prov.UnsupportedReferences.Should().Contain("inventedApi");
        prov.Confidence.Should().Be(0.42);
        prov.Verified.Should().BeTrue();
        AlwaysOkValidator.Calls.Should().BeGreaterThan(0);
    }

    private sealed class StubDetDrafter : IDeterministicDrafter
    {
        public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
        {
            artifact = new GeneratedArtifact
            {
                Content = "SELECT 1;",
                Provenance = GenerativeProvenance.Create(
                    GenerationStrategy.Templated,
                    confidence: 0.42,
                    unsupportedReferences: new[] { "inventedApi" },
                    requiresHumanReview: false)
            };
            return true;
        }
    }

    private sealed class StubDrafter : IArtifactDrafter
    {
        public Task<GeneratedArtifact> DraftAsync(
            GenerationRequest request,
            IReadOnlyList<string> priorFeedback,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GeneratedArtifact { Content = "SELECT 1;" });
    }

    private sealed class AlwaysOkValidator : IPostValidator<GeneratedArtifact>
    {
        public static int Calls;

        public ValueTask<(bool ok, string? reason)> ValidateAsync(
            GeneratedArtifact output,
            CancellationToken ct)
        {
            Calls++;
            return ValueTask.FromResult<(bool, string?)>((true, null));
        }
    }

    private sealed class FakeContext : IExecutionContext
    {
        public string AgentId => "t";
        public string BehaviorId => "t";
        public bool IsAirGapped => false;
        public bool AuditMode => false;
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
