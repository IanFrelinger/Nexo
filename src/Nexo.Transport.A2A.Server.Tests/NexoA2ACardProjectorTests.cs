using FluentAssertions;
using Xunit;

namespace Nexo.Transport.A2A.Server.Tests;

public sealed class NexoA2ACardProjectorTests
{
    [Fact]
    public void Projects_descriptor_fields_onto_the_spec_card()
    {
        var descriptor = NexoA2AExposurePolicyTests.Agent("security-analyst");
        var options = new NexoA2AServerOptions { PublicBaseUrl = "https://nexo.example.com/" };

        var card = new NexoA2ACardProjector().Project(descriptor, options, "/api/a2a");

        card.Name.Should().Be("security-analyst-name");
        card.Description.Should().Be("security-analyst description");
        card.Version.Should().Be("1.0.0");
        var iface = card.SupportedInterfaces.Should().ContainSingle().Subject;
        iface.Url.Should().Be("https://nexo.example.com/api/a2a/security-analyst");
        card.Capabilities!.Streaming.Should().BeFalse("v1 is synchronous");
        card.Capabilities.PushNotifications.Should().BeFalse();
        card.Skills.Should().HaveCount(2);
        card.Skills![0].Id.Should().Be("analyze");
        card.Skills[0].Tags.Should().Contain("testing");
        card.DefaultInputModes.Should().Contain("application/json");
    }
}
