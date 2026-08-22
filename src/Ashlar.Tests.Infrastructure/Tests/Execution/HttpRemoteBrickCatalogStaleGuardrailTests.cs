using Ashlar.Agents.TestKit;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Infrastructure.Execution;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for http remote brick catalog stale guardrail.</summary>
public sealed class HttpRemoteBrickCatalogStaleGuardrailTests
{
    [Fact]
    public async Task GetCapabilitiesWithStalenessAsync_DropsTooOldStaleManifest()
    {
        var responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(
        [
            static _ => JsonResponse("""
            {
              "nodeId": "node-old",
              "tier": 1,
              "platform": 2,
              "hotModelIds": [],
              "availableModelIds": ["phi3-mini"],
              "supportedCapabilities": [0],
              "acceptingRemoteWork": true,
              "generatedAt": "2020-01-01T00:00:00Z"
            }
            """),
            static _ => throw new HttpRequestException("network down")
        ]);

        using var httpClient = new HttpClient(new StubHttpMessageHandler((req, _) => Task.FromResult(responses.Dequeue().Invoke(req))))
        {
            BaseAddress = new Uri("http://remote:7777", UriKind.Absolute)
        };

        var staleStore = new StaleCapabilitiesSnapshotStore();
        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero,
            maxStaleCapabilityAge: TimeSpan.FromSeconds(5));

        var first = await sut.GetCapabilitiesWithStalenessAsync();
        var second = await sut.GetCapabilitiesWithStalenessAsync();

        first.Capabilities.Should().NotBeNull();
        second.Capabilities.Should().BeNull("manifest is older than MaxStaleAge and should be discarded");
        second.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilitiesWithStalenessAsync_DropsFutureGeneratedManifest()
    {
        var responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(
        [
            _ => JsonResponse($$"""
            {
              "nodeId": "node-future",
              "tier": 1,
              "platform": 2,
              "hotModelIds": [],
              "availableModelIds": ["phi3-mini"],
              "supportedCapabilities": [0],
              "acceptingRemoteWork": true,
              "generatedAt": "{{DateTimeOffset.UtcNow.AddMinutes(10):O}}"
            }
            """),
            static _ => throw new HttpRequestException("network down")
        ]);

        using var httpClient = new HttpClient(new StubHttpMessageHandler((req, _) => Task.FromResult(responses.Dequeue().Invoke(req))))
        {
            BaseAddress = new Uri("http://remote:7777", UriKind.Absolute)
        };

        var staleStore = new StaleCapabilitiesSnapshotStore();
        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero,
            maxStaleCapabilityAge: TimeSpan.FromMinutes(5));

        var first = await sut.GetCapabilitiesWithStalenessAsync();
        var second = await sut.GetCapabilitiesWithStalenessAsync();

        first.Capabilities.Should().NotBeNull();
        second.Capabilities.Should().BeNull("future generatedAt should be treated as invalid stale metadata");
        second.IsStale.Should().BeFalse();
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>Tests for fake http message handler.</summary>
}
