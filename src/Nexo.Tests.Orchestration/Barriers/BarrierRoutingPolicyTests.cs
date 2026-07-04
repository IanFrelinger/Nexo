using FluentAssertions;
using Nexo.Abstractions.Routing;
using Nexo.Orchestration.Routing;
using Xunit;

namespace Nexo.Tests.Orchestration.Barriers;

/// <summary>Tests for barrier routing policy.</summary>
public sealed class BarrierRoutingPolicyTests
{
    [Fact]
    public void Apply_EndpointAcceptsAll_AlwaysPassesThrough()
    {
        var policy = new BarrierRoutingPolicy();
        var endpoints = new[]
        {
            Endpoint("https://all", [])
        };

        var result = policy.Apply(endpoints, Context("internal"));

        result.Should().ContainSingle(x => x.Endpoint == "https://all");
    }

    [Fact]
    public void Apply_EndpointAcceptsSpecificLevel_PassesMatchingRejectsNonMatching()
    {
        var policy = new BarrierRoutingPolicy();
        var endpoint = Endpoint("https://internal", ["internal"]);

        var allowed = policy.Apply([endpoint], Context("internal"));
        var denied = policy.Apply([endpoint], Context("confidential"));

        allowed.Should().ContainSingle();
        denied.Should().BeEmpty();
    }

    [Fact]
    public void Apply_NoMatchingEndpoint_ReturnsEmptyList()
    {
        var policy = new BarrierRoutingPolicy();
        var endpoints = new[]
        {
            Endpoint("https://x", ["public"])
        };

        var result = policy.Apply(endpoints, Context("restricted"));

        result.Should().BeEmpty();
    }

    private static EndpointRoutingContext Context(string barrier)
    {
        return new EndpointRoutingContext(
            AgentName: "agent-1",
            CorrelationId: "corr-1",
            RequiredCapabilities: [],
            BarrierLevel: barrier,
            PreferredRegion: null,
            AgentMetadata: new Dictionary<string, object?>());
    }

    private static EndpointDescriptor Endpoint(string endpoint, IReadOnlyList<string> acceptedLevels)
    {
        return new EndpointDescriptor(
            Endpoint: endpoint,
            Name: endpoint,
            SupportedCapabilities: ["CodeGeneration"],
            AcceptedBarrierLevels: acceptedLevels,
            Region: null,
            Priority: 1,
            IsHealthy: true);
    }
}
