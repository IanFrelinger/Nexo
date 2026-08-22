using FluentAssertions;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Infrastructure.Mesh;
using Xunit;
using Ashlar.Tests.Infrastructure.Helpers;

namespace Ashlar.Tests.Infrastructure.Tests.Mesh;

/// <summary>
/// P2.2: Artifact format negotiation tests.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ArtifactNegotiatorTests
{
    private readonly IArtifactNegotiator _negotiator = new ArtifactNegotiator();

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Negotiate_PreferredFormatInCommon_ReturnsPreferred()
    {
        await Task.CompletedTask;
        var requester = new InstanceCapabilities(new[] { ArtifactFormat.Source }, ArtifactFormat.Source);
        var fulfiller = new InstanceCapabilities(new[] { ArtifactFormat.Source }, ArtifactFormat.Source);

        var result = _negotiator.Negotiate(requester, fulfiller, ArtifactFormat.Source);

        result.Should().Be(ArtifactFormat.Source);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Negotiate_PreferredNotInCommon_FallsBackToFulfillerPreferred()
    {
        await Task.CompletedTask;
        var requester = new InstanceCapabilities(new[] { ArtifactFormat.Binary, ArtifactFormat.Config });
        var fulfiller = new InstanceCapabilities(
            new[] { ArtifactFormat.Source, ArtifactFormat.Binary, ArtifactFormat.Config },
            ArtifactFormat.Binary);

        var result = _negotiator.Negotiate(requester, fulfiller, ArtifactFormat.Source);

        result.Should().Be(ArtifactFormat.Binary, "requester preferred Source not in common; fulfiller preferred Binary is in common");
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Negotiate_NoCommonFormat_ReturnsNull()
    {
        await Task.CompletedTask;
        var requester = new InstanceCapabilities(new[] { ArtifactFormat.Source });
        var fulfiller = new InstanceCapabilities(new[] { ArtifactFormat.Binary });

        var result = _negotiator.Negotiate(requester, fulfiller);

        result.Should().BeNull();
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Negotiate_AllFormats_ReturnsPreferredWhenSupported()
    {
        await Task.CompletedTask;
        var result = _negotiator.Negotiate(
            InstanceCapabilities.AllFormats,
            InstanceCapabilities.AllFormats,
            ArtifactFormat.Binary);

        result.Should().Be(ArtifactFormat.Binary);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task InstanceCapabilities_AllFormats_HasAllThreeFormats()
    {
        await Task.CompletedTask;
        var caps = InstanceCapabilities.AllFormats;

        caps.SupportedFormats.Should().Contain(ArtifactFormat.Source);
        caps.SupportedFormats.Should().Contain(ArtifactFormat.Binary);
        caps.SupportedFormats.Should().Contain(ArtifactFormat.Config);
        caps.PreferredFormat.Should().Be(ArtifactFormat.Source);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task ArtifactNegotiator_SelectsSourceCode_WhenTargetCanCompile()
    {
        await Task.CompletedTask;
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

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task ArtifactNegotiator_SelectsDockerImage_WhenTargetHasDockerRuntime()
    {
        await Task.CompletedTask;
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

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task ArtifactNegotiator_SelectsWasmModule_WhenTargetHasWasmRuntime()
    {
        await Task.CompletedTask;
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

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task InstanceCapabilities_LocalAshlar_HasCanCompileAndComponents()
    {
        await Task.CompletedTask;
        var caps = InstanceCapabilities.LocalAshlar;

        caps.CanCompile.Should().BeTrue();
        caps.SupportedFormats.Should().Contain(ArtifactFormat.Source);
        caps.AvailableComponents.Should().Contain("ashlar-cli");
    }
}
