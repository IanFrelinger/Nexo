using FluentAssertions;
using Xunit;

namespace Ashlar.Transport.A2A.Server.Tests;

public sealed class AshlarA2AExposurePolicyTests
{
    internal static AshlarA2AAgentDescriptor Agent(
        string id,
        IReadOnlyList<string>? protocols = null)
        => new(
            Id: id,
            Name: $"{id}-name",
            Description: $"{id} description",
            Version: "1.0.0",
            Domain: "testing",
            Behaviors: new[] { "analyze", "report" },
            CoordinationProtocols: protocols ?? Array.Empty<string>(),
            MaxExecutionTime: null);

    [Fact]
    public void Nothing_is_exposed_by_default()
    {
        var exposed = AshlarA2AExposurePolicy.FilterExposed(
            new[] { Agent("alpha"), Agent("beta") },
            new AshlarA2AServerOptions { Enabled = true });

        exposed.Should().BeEmpty("exposure is deny-by-default");
    }

    [Fact]
    public void Allowlisted_agents_are_exposed()
    {
        var options = new AshlarA2AServerOptions { Enabled = true };
        options.ExposedAgentIds.Add("alpha");

        var exposed = AshlarA2AExposurePolicy.FilterExposed(new[] { Agent("alpha"), Agent("beta") }, options);

        exposed.Should().ContainSingle().Which.Id.Should().Be("alpha");
    }

    [Fact]
    public void Coordination_protocol_exposure_requires_the_explicit_opt_in()
    {
        var candidates = new[] { Agent("alpha", new[] { "a2a" }), Agent("beta") };

        var withoutOptIn = AshlarA2AExposurePolicy.FilterExposed(candidates, new AshlarA2AServerOptions());
        var withOptIn = AshlarA2AExposurePolicy.FilterExposed(
            candidates, new AshlarA2AServerOptions { ExposeByCoordinationProtocol = true });

        withoutOptIn.Should().BeEmpty();
        withOptIn.Should().ContainSingle().Which.Id.Should().Be("alpha");
    }

    [Fact]
    public void Single_exposed_agent_is_the_implicit_primary()
    {
        var primary = AshlarA2AExposurePolicy.ResolvePrimary(new[] { Agent("alpha") }, new AshlarA2AServerOptions());

        primary.Id.Should().Be("alpha");
    }

    [Fact]
    public void Multiple_exposed_agents_require_an_explicit_primary()
    {
        var act = () => AshlarA2AExposurePolicy.ResolvePrimary(
            new[] { Agent("alpha"), Agent("beta") },
            new AshlarA2AServerOptions());

        act.Should().Throw<InvalidOperationException>().WithMessage("*PrimaryAgentId*");
    }

    [Fact]
    public void Unknown_primary_id_fails_loudly()
    {
        var options = new AshlarA2AServerOptions { PrimaryAgentId = "ghost" };

        var act = () => AshlarA2AExposurePolicy.ResolvePrimary(new[] { Agent("alpha") }, options);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ghost*");
    }

    [Fact]
    public void Zero_exposed_agents_fails_rather_than_serving_an_empty_surface()
    {
        var act = () => AshlarA2AExposurePolicy.ResolvePrimary(
            Array.Empty<AshlarA2AAgentDescriptor>(),
            new AshlarA2AServerOptions());

        act.Should().Throw<InvalidOperationException>().WithMessage("*no agents are exposed*");
    }
}
