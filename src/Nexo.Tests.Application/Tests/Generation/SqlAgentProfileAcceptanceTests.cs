using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Authoring;
using Nexo.Bricks.SqlProfile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Acceptance: SQL profile plugin drives the same GenerativeArtifactBrick
/// with zero edits to the fixed engine.
/// </summary>
public sealed class SqlAgentProfileAcceptanceTests
{
    [Fact]
    public async Task SqlProfile_DeterministicPath_ProducesSelectViaSameBrick()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoGenerativeAgents();
        services.AddNexoSqlAgentProfile();
        await using var sp = services.BuildServiceProvider();

        // Materialize registry (loads profile sources).
        var registry = sp.GetRequiredService<IAgentProfileRegistry>();
        registry.Resolve(SqlAgentProfileFactory.TargetId).Should().NotBeNull();

        var brick = sp.GetRequiredService<GenerativeArtifactBrick>();
        var input = new BrickInput();
        input.Set("target", SqlAgentProfileFactory.TargetId);
        input.Set("grounding", "events");

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new FakeContext(),
            CancellationToken.None);

        var artifact = output.Get<GeneratedArtifact>("artifact");
        artifact.Content.Should().Be("SELECT * FROM events;");
        output.Get<bool>("verified").Should().BeTrue();
        output.Get<string>("generationStrategy").Should().Be("Deterministic");
    }

    [Fact]
    public async Task SqlProfile_RepairLoop_RecoversFromEmptyDraft()
    {
        var registry = new AgentProfileRegistry();
        // Prefer model path so empty grounding exercises repair.
        var profile = new SqlAgentProfileFactory().Create(new ServiceCollection().BuildServiceProvider());
        registry.Register(new AgentProfile
        {
            TargetId = profile.TargetId,
            Drafter = profile.Drafter,
            DeterministicDrafter = profile.DeterministicDrafter,
            Validators = profile.Validators,
            Tunables = new GenerationTunables { PreferDeterministic = false, MaxRepairAttempts = 3 },
            Capabilities = profile.Capabilities,
            Knowledge = profile.Knowledge,
            Llm = profile.Llm
        });

        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "sql");
        input.Set("grounding", "");

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Agentic,
            new FakeContext(),
            CancellationToken.None);

        var artifact = output.Get<GeneratedArtifact>("artifact");
        artifact.Content.Should().Be("SELECT 1;");
        output.Get<bool>("verified").Should().BeTrue();
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
