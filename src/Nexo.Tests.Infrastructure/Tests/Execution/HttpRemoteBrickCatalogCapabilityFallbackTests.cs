using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BrickContracts;
using Nexo.BrickContracts.Capabilities;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class HttpRemoteBrickCatalogCapabilityFallbackTests
{
    [Fact]
    public async Task GetCapabilitiesAsync_ReturnsStaleSnapshot_WhenEndpointUnavailable()
    {
        var freshGeneratedAt = DateTimeOffset.UtcNow.AddSeconds(-5).ToString("O");
        var responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(
        [
            _ => JsonResponse($$"""
            {
              "nodeId": "node-a",
              "tier": 1,
              "platform": 2,
              "hotModelIds": [],
              "availableModelIds": ["phi3-mini"],
              "supportedCapabilities": [0],
              "acceptingRemoteWork": true,
              "generatedAt": "{{freshGeneratedAt}}"
            }
            """),
            static _ => throw new HttpRequestException("network down")
        ]);

        using var httpClient = new HttpClient(new FakeHttpMessageHandler((req, _) => Task.FromResult(responses.Dequeue().Invoke(req))))
        {
            BaseAddress = new Uri("http://remote:7777", UriKind.Absolute)
        };

        var staleStore = new StaleCapabilitiesSnapshotStore();
        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero);

        var first = await sut.GetCapabilitiesWithStalenessAsync();
        var second = await sut.GetCapabilitiesWithStalenessAsync();

        first.Capabilities.Should().NotBeNull();
        first.IsStale.Should().BeFalse();
        second.Capabilities.Should().NotBeNull();
        second.IsStale.Should().BeTrue();
        second.Capabilities!.NodeId.Should().Be("node-a");
        second.Capabilities.Platform.Should().Be(PlatformTypeDto.Linux);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_DropsStaleSnapshot_WhenOlderThanMaxAge()
    {
        var staleStore = new StaleCapabilitiesSnapshotStore();
        var tooOld = DateTimeOffset.UtcNow.AddMinutes(-20);
        staleStore.Put("http://remote:7777/", new NodeCapabilityManifestDto
        {
            NodeId = "node-stale",
            Tier = NodeTierDto.Micro,
            Platform = PlatformTypeDto.Linux,
            HotModelIds = [],
            AvailableModelIds = ["phi3-mini"],
            SupportedCapabilities = [TaskCapabilityDto.TextGeneration],
            AcceptingRemoteWork = true,
            GeneratedAt = tooOld
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))))
        {
            BaseAddress = new Uri("http://remote:7777", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero,
            maxStaleCapabilityAge: TimeSpan.FromMinutes(5));

        var fetch = await sut.GetCapabilitiesWithStalenessAsync();
        fetch.Capabilities.Should().BeNull();
        fetch.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilitiesAsync_DropsFutureTimestampSnapshot()
    {
        var staleStore = new StaleCapabilitiesSnapshotStore();
        staleStore.Put("http://remote:7777/", new NodeCapabilityManifestDto
        {
            NodeId = "node-future",
            Tier = NodeTierDto.Standard,
            Platform = PlatformTypeDto.Linux,
            HotModelIds = [],
            AvailableModelIds = ["phi3-mini"],
            SupportedCapabilities = [TaskCapabilityDto.TextGeneration],
            AcceptingRemoteWork = true,
            GeneratedAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))))
        {
            BaseAddress = new Uri("http://remote:7777", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero,
            maxStaleCapabilityAge: TimeSpan.FromMinutes(15));

        var fetch = await sut.GetCapabilitiesWithStalenessAsync();
        fetch.Capabilities.Should().BeNull();
        fetch.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_AttachesCapabilitiesFromStaleCache_WhenFreshFetchFails()
    {
        var freshGeneratedAt = DateTimeOffset.UtcNow.AddSeconds(-5).ToString("O");
        var capabilitiesCalls = 0;
        var brickCalls = 0;
        using var httpClient = new HttpClient(new FakeHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath == "/api/capabilities")
            {
                capabilitiesCalls++;
                return capabilitiesCalls == 1
                    ? Task.FromResult(JsonResponse($$"""
                    {
                      "nodeId": "node-cache",
                      "tier": 2,
                      "platform": 2,
                      "hotModelIds": [],
                      "availableModelIds": ["phi3-mini"],
                      "supportedCapabilities": [0],
                      "acceptingRemoteWork": true,
                      "generatedAt": "{{freshGeneratedAt}}"
                    }
                    """))
                    : Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            if (req.RequestUri.AbsolutePath.StartsWith("/api/bricks", StringComparison.Ordinal))
            {
                brickCalls++;
                if (brickCalls > 1)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }

                return Task.FromResult(JsonResponse("""
                {
                  "wireFormatVersion": "2025-02",
                  "bricks": [
                    {
                      "wireFormatVersion": "2025-02",
                      "id": "remote.gen",
                      "name": "RemoteGen",
                      "version": "1.0.0",
                      "icon": "pkg",
                      "category": "Generation",
                      "description": "desc",
                      "interface": { "inputs": [], "outputs": [] },
                      "hasDeterministic": true,
                      "hasAgentic": true,
                      "metadata": { "author": "n", "license": "MIT", "repository": "", "usageCount": 1, "lastUpdated": "2026-01-01T00:00:00Z" }
                    }
                  ]
                }
                """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }))
        {
            BaseAddress = new Uri("http://remote:7777", UriKind.Absolute)
        };

        var staleStore = new StaleCapabilitiesSnapshotStore();
        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero);

        var warm = await sut.GetCapabilitiesWithStalenessAsync();
        warm.Capabilities.Should().NotBeNull();
        warm.IsStale.Should().BeFalse();

        _ = await sut.GetCapabilitiesWithStalenessAsync();
        capabilitiesCalls.Should().BeGreaterThanOrEqualTo(2);
        var entries = await sut.GetAllAsync();
        entries.Should().HaveCount(1);
        entries[0].HostCapabilities.Should().NotBeNull();
        entries[0].HostCapabilities!.NodeId.Should().Be("node-cache");
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
