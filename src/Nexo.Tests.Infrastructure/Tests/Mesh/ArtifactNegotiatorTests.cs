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
[Trait("Category", "ProdStyle")]
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

    [Fact]
    public void ArtifactNegotiator_SelectsSourceCode_WhenTargetCanCompile()
    {
        var requester = new InstanceCapabilities(
            new[] { ArtifactFormat.Source, ArtifactFormat.Binary },
            preferredFormat: null,
            canCompile: true);
        var fulfiller = new InstanceCapabilities(
            new[] { ArtifactFormat.Source, ArtifactFormat.Binary },
            ArtifactFormat.Binary);

        var result = _negotiator.Negotiate(requester, fulfiller);

        result.Should().Be(ArtifactFormat.Source, "requester CanCompile prefers Source");
    }

    [Fact]
    public void ArtifactNegotiator_SelectsDockerImage_WhenTargetHasDockerRuntime()
    {
        var requester = new InstanceCapabilities(
            new[] { ArtifactFormat.DockerImage, ArtifactFormat.Binary },
            preferredFormat: null,
            hasDockerRuntime: true);
        var fulfiller = new InstanceCapabilities(
            new[] { ArtifactFormat.DockerImage, ArtifactFormat.Binary },
            ArtifactFormat.Binary);

        var result = _negotiator.Negotiate(requester, fulfiller);

        result.Should().Be(ArtifactFormat.DockerImage, "requester HasDockerRuntime prefers DockerImage");
    }

    [Fact]
    public void ArtifactNegotiator_SelectsWasmModule_WhenTargetHasWasmRuntime()
    {
        var requester = new InstanceCapabilities(
            new[] { ArtifactFormat.WasmModule, ArtifactFormat.Binary },
            preferredFormat: null,
            hasWasmRuntime: true);
        var fulfiller = new InstanceCapabilities(
            new[] { ArtifactFormat.WasmModule, ArtifactFormat.Binary },
            ArtifactFormat.Binary);

        var result = _negotiator.Negotiate(requester, fulfiller);

        result.Should().Be(ArtifactFormat.WasmModule, "requester HasWasmRuntime prefers WasmModule");
    }

    [Fact]
    public void InstanceCapabilities_LocalNexo_HasCanCompileAndComponents()
    {
        var caps = InstanceCapabilities.LocalNexo;

        caps.CanCompile.Should().BeTrue();
        caps.SupportedFormats.Should().Contain(ArtifactFormat.Source);
        caps.AvailableComponents.Should().Contain("nexo-cli");
    }
}
