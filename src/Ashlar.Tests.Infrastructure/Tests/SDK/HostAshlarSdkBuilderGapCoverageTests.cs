using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Hosting.Sdk;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SDK;
/// <summary>Tests for host ashlar sdk builder gap coverage.</summary>
public sealed class HostAshlarSdkBuilderGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_null_options()
    {
        var act = () => new HostAshlarSdkBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterBrick_adds_type_to_options()
    {
        var options = new AshlarSdkOptions();
        var builder = new HostAshlarSdkBuilder(options);

        builder.RegisterBrick<SdkGapTestBrick>().Should().BeSameAs(builder);
        options.BrickTypes.Should().ContainSingle().Which.Should().Be(typeof(SdkGapTestBrick));
    }

    [Fact]
    public void RegisterAgent_adds_agent_type_when_it_implements_IAgent()
    {
        var options = new AshlarSdkOptions();
        var builder = new HostAshlarSdkBuilder(options);

        builder.RegisterAgent<SdkGapTestAgent>().Should().BeSameAs(builder);
        options.AgentTypes.Should().ContainSingle().Which.Should().Be(typeof(SdkGapTestAgent));
    }

    [Fact]
    public void RegisterAgent_rejects_types_that_do_not_implement_IAgent()
    {
        var builder = new HostAshlarSdkBuilder(new AshlarSdkOptions());

        var act = () => builder.RegisterAgent<string>();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("T");
    }

    [Fact]
    public void RegisterAgentCard_adds_card_to_options()
    {
        var options = new AshlarSdkOptions();
        var builder = new HostAshlarSdkBuilder(options);
        var card = new AgentCard
        {
            Id = "gap-agent",
            Name = "Gap Agent",
            Domain = "test",
            Description = "coverage",
        };

        builder.RegisterAgentCard(card).Should().BeSameAs(builder);
        options.AgentCards.Should().ContainSingle().Which.Should().BeSameAs(card);
    }

    [Fact]
    public void RegisterAgentCard_rejects_null_card()
    {
        var builder = new HostAshlarSdkBuilder(new AshlarSdkOptions());
        var act = () => builder.RegisterAgentCard(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Obsolete_AshlarSdkBuilder_ctor_forwards_to_host_builder()
    {
#pragma warning disable CS0618
        var options = new AshlarSdkOptions();
        var builder = new AshlarSdkBuilder(options);
#pragma warning restore CS0618

        builder.RegisterBrick<SdkGapTestBrick>();
        options.BrickTypes.Should().Contain(typeof(SdkGapTestBrick));
    }

    /// <summary>Tests for sdk gap test brick.</summary>
    private sealed class SdkGapTestBrick : DomainBrick
    {
        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }

    /// <summary>Tests for sdk gap test agent.</summary>
    private sealed class SdkGapTestAgent : IAgent
    {
        public string Name => "sdk-gap-agent";

        public Task<AgentActions> ThinkAsync(
            AgentObservation obs,
            IToolbox tools,
            IAgentMemory mem,
            CancellationToken ct)
            => Task.FromResult(AgentActions.None);
    }
}
