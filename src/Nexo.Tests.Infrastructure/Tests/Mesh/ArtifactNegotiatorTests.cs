using FluentAssertions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure.Mesh;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Mesh;

/// <summary>
/// P2.2: Artifact format negotiation tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArtifactNegotiatorTests
{
    private readonly IArtifactNegotiator _negotiator = new ArtifactNegotiator();

    [Fact]
    public void Negotiate_PreferredFormatInCommon_ReturnsPreferred()
    {
        var requester = new InstanceCapabilities(new[] { ArtifactFormat.Source }, ArtifactFormat.Source);
        var fulfiller = new InstanceCapabilities(new[] { ArtifactFormat.Source }, ArtifactFormat.Source);

        var result = _negotiator.Negotiate(requester, fulfiller, ArtifactFormat.Source);

        result.Should().Be(ArtifactFormat.Source);
    }

    [Fact]
    public void Negotiate_PreferredNotInCommon_FallsBackToFulfillerPreferred()
    {
        var requester = new InstanceCapabilities(new[] { ArtifactFormat.Binary, ArtifactFormat.Config });
        var fulfiller = new InstanceCapabilities(
            new[] { ArtifactFormat.Source, ArtifactFormat.Binary, ArtifactFormat.Config },
            ArtifactFormat.Binary);

        var result = _negotiator.Negotiate(requester, fulfiller, ArtifactFormat.Source);

        result.Should().Be(ArtifactFormat.Binary, "requester preferred Source not in common; fulfiller preferred Binary is in common");
    }

    [Fact]
    public void Negotiate_NoCommonFormat_ReturnsNull()
    {
        var requester = new InstanceCapabilities(new[] { ArtifactFormat.Source });
        var fulfiller = new InstanceCapabilities(new[] { ArtifactFormat.Binary });

        var result = _negotiator.Negotiate(requester, fulfiller);

        result.Should().BeNull();
    }

    [Fact]
    public void Negotiate_AllFormats_ReturnsPreferredWhenSupported()
    {
        var result = _negotiator.Negotiate(
            InstanceCapabilities.AllFormats,
            InstanceCapabilities.AllFormats,
            ArtifactFormat.Binary);

        result.Should().Be(ArtifactFormat.Binary);
    }

    [Fact]
    public void InstanceCapabilities_AllFormats_HasAllThreeFormats()
    {
        var caps = InstanceCapabilities.AllFormats;

        caps.SupportedFormats.Should().Contain(ArtifactFormat.Source);
        caps.SupportedFormats.Should().Contain(ArtifactFormat.Binary);
        caps.SupportedFormats.Should().Contain(ArtifactFormat.Config);
        caps.PreferredFormat.Should().Be(ArtifactFormat.Source);
    }
}
