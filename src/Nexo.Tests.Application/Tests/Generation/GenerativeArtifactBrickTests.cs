using FluentAssertions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Step 5: GenerativeArtifactBrick drives profiles via registry — no core edits to add targets.
/// </summary>
public sealed class GenerativeArtifactBrickTests
{
    [Fact]
    public async Task ExecuteAsync_UsesDeterministicDrafter_WhenAvailable()
    {
        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "demo",
            Drafter = new ThrowingDrafter(),
            DeterministicDrafter = new FixedDeterministic("SELECT 1"),
            Tunables = new GenerationTunables { PreferDeterministic = true },
            Capabilities = new AgentProfileCapabilities { SupportsDeterministic = true },
            Validators = Array.Empty<object>()
        });

        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "demo");
        input.Set("grounding", "unused");

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new FakeContext(),
            CancellationToken.None);

        var artifact = output.Get<GeneratedArtifact>("artifact");
        artifact.Content.Should().Be("SELECT 1");
        // The deterministic drafter reports Deterministic, not Templated: a
        // template-expanded draft and a code-generated one are different
        // provenance claims and must not be conflated.
        output.Get<string>("generationStrategy").Should().Be("Deterministic");
    }

    [Fact]
    public async Task ExecuteAsync_RunsRepairLoop_WithFakeValidator()
    {
        var registry = new AgentProfileRegistry();
        var drafter = new CountingDrafter();
        registry.Register(new AgentProfile
        {
            TargetId = "repair-demo",
            Drafter = drafter,
            Tunables = new GenerationTunables { PreferDeterministic = false, MaxRepairAttempts = 3 },
            Capabilities = new AgentProfileCapabilities(),
            Validators = new object[] { new PassOnSecondValidator(drafter) }
        });

        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "repair-demo");

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Agentic,
            new FakeContext(),
            CancellationToken.None);

        drafter.Calls.Should().Be(2);
        output.Get<bool>("verified").Should().BeTrue();
        var provenance = output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        provenance.Strategy.Should().Be(GenerationStrategy.Model);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenTargetUnknown()
    {
        var brick = new GenerativeArtifactBrick(new AgentProfileRegistry());
        var input = new BrickInput();
        input.Set("target", "missing");

        var act = () => brick.ExecuteAsync(input, ImplementationType.Agentic, new FakeContext(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class FixedDeterministic : IDeterministicDrafter
    {
        private readonly string _content;
        public FixedDeterministic(string content) => _content = content;
        public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
        {
            artifact = new GeneratedArtifact { Content = _content };
            return true;
        }
    }

    private sealed class ThrowingDrafter : IArtifactDrafter
    {
        public Task<GeneratedArtifact> DraftAsync(
            GenerationRequest request,
            IReadOnlyList<string> priorFeedback,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("model path should not run");
    }

    private sealed class CountingDrafter : IArtifactDrafter
    {
        public int Calls { get; private set; }
        public Task<GeneratedArtifact> DraftAsync(
            GenerationRequest request,
            IReadOnlyList<string> priorFeedback,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new GeneratedArtifact { Content = $"draft-{Calls}" });
        }
    }

    private sealed class PassOnSecondValidator : IPostValidator<GeneratedArtifact>
    {
        private readonly CountingDrafter _drafter;
        public PassOnSecondValidator(CountingDrafter drafter) => _drafter = drafter;
        public ValueTask<(bool ok, string? reason)> ValidateAsync(GeneratedArtifact output, CancellationToken ct) =>
            _drafter.Calls >= 2
                ? ValueTask.FromResult<(bool, string?)>((true, null))
                : ValueTask.FromResult<(bool, string?)>((false, "need another draft"));
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
