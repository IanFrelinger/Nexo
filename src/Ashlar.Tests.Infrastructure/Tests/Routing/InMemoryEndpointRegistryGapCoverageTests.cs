using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Routing;
using Ashlar.Runtime.Routing;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Routing;

/// <summary>Tests for in memory endpoint registry gap coverage.</summary>
public sealed class InMemoryEndpointRegistryGapCoverageTests
{
    [Fact]
    public void Constructor_seeds_endpoints_from_options_and_skips_blank_urls()
    {
        var registry = new InMemoryEndpointRegistry(
            Options.Create(new RoutingOptions
            {
                Endpoints =
                [
                    new EndpointDescriptorConfig
                    {
                        Endpoint = "http://127.0.0.1:5001",
                        Name = "primary",
                        Priority = 10,
                    },
                    new EndpointDescriptorConfig { Endpoint = "  ", Name = "blank" },
                ],
            }),
            NullLogger<InMemoryEndpointRegistry>.Instance);

        registry.GetAll().Should().ContainSingle(d => d.Name == "primary" && d.IsHealthy);
    }

    [Fact]
    public void Register_rejects_null_descriptor()
    {
        var registry = CreateRegistry();
        var act = () => registry.Register(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateHealth_updates_known_endpoint_and_warns_on_unknown()
    {
        var registry = CreateRegistry();
        registry.Register(new EndpointDescriptor(
            Endpoint: "http://127.0.0.1:5002",
            Name: "worker",
            SupportedCapabilities: Array.Empty<string>(),
            AcceptedBarrierLevels: Array.Empty<string>(),
            Region: null,
            Priority: 50,
            IsHealthy: true));

        registry.UpdateHealth("http://127.0.0.1:5002", false);
        registry.GetAll().Should().ContainSingle(d => d.IsHealthy == false);

        registry.UpdateHealth("http://missing", true);
        registry.UpdateHealth("", true);
    }

    [Fact]
    public void GetAll_orders_by_priority_then_name()
    {
        var registry = CreateRegistry();
        registry.Register(new EndpointDescriptor("http://b", "beta", [], [], null, 20, true));
        registry.Register(new EndpointDescriptor("http://a", "alpha", [], [], null, 10, true));
        registry.Register(new EndpointDescriptor("http://c", "charlie", [], [], null, 10, true));

        registry.GetAll().Select(d => d.Name).Should().ContainInOrder("alpha", "charlie", "beta");
    }

    /// <summary>Creates registry.</summary>
    private static InMemoryEndpointRegistry CreateRegistry() =>
        new(Options.Create(new RoutingOptions()), NullLogger<InMemoryEndpointRegistry>.Instance);
}
