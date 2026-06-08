using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BrickContracts;
using Nexo.BrickContracts.Capabilities;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class HttpRemoteBrickCatalogGapCoverageTests
{
    [Fact]
    public async Task GetByIdAsync_returns_null_for_empty_id()
    {
        using var httpClient = new HttpClient(new NoOpHandler()) { BaseAddress = new Uri("http://remote:7777/") };
        var sut = new HttpRemoteBrickCatalog(httpClient);

        (await sut.GetByIdAsync("", CancellationToken.None)).Should().BeNull();
        (await sut.GetByIdAsync("   ", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_entry_with_capabilities_when_found()
    {
        using var httpClient = new HttpClient(new FakeHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath == "/api/capabilities")
            {
                return Task.FromResult(JsonResponse("""
                    {
                      "nodeId": "node-by-id",
                      "tier": 1,
                      "platform": 2,
                      "hotModelIds": [],
                      "availableModelIds": ["phi3-mini"],
                      "supportedCapabilities": [0],
                      "acceptingRemoteWork": true,
                      "generatedAt": "2026-06-08T00:00:00Z"
                    }
                    """));
            }

            if (req.RequestUri.AbsolutePath == "/api/bricks/remote.gen")
            {
                return Task.FromResult(JsonResponse("""
                    {
                      "id": "remote.gen",
                      "name": "RemoteGen",
                      "category": "Generation",
                      "description": "desc",
                      "interface": { "inputs": [], "outputs": [] },
                      "hasDeterministic": true
                    }
                    """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(httpClient, NullLogger<HttpRemoteBrickCatalog>.Instance);
        var entry = await sut.GetByIdAsync("remote.gen");

        entry.Should().NotBeNull();
        entry!.Id.Should().Be("remote.gen");
        entry.HostCapabilities.Should().NotBeNull();
        entry.HostCapabilities!.NodeId.Should().Be("node-by-id");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_remote_returns_not_found()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(httpClient, NullLogger<HttpRemoteBrickCatalog>.Instance);
        (await sut.GetByIdAsync("missing-brick")).Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_remote_errors()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(httpClient, NullLogger<HttpRemoteBrickCatalog>.Instance);
        (await sut.GetByIdAsync("broken")).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_parses_full_brick_catalog_payload()
    {
        using var httpClient = new HttpClient(new FakeHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath == "/api/capabilities")
            {
                return Task.FromResult(JsonResponse("""
                    {
                      "nodeId": "node-rich",
                      "tier": "Standard",
                      "platform": "Linux",
                      "hotModelIds": ["hot-1"],
                      "availableModelIds": [42],
                      "supportedCapabilities": ["TextGeneration"],
                      "acceptingRemoteWork": "true",
                      "generatedAt": "not-a-date"
                    }
                    """));
            }

            if (req.RequestUri.AbsolutePath == "/api/bricks")
            {
                return Task.FromResult(JsonResponse("""
                    {
                      "bricks": [
                        "skip-non-object",
                        {
                          "wireFormatVersion": "2025-02",
                          "id": "rich.brick",
                          "name": "RichBrick",
                          "version": "2.0.0",
                          "icon": "star",
                          "category": "Generation",
                          "description": "full parse",
                          "hostBaseUrl": "http://peer:8080",
                          "hasDeterministic": true,
                          "hasAgentic": false,
                          "interface": {
                            "inputs": [
                              { "name": "prompt", "type": "string", "description": "in", "required": true, "default": "hi" }
                            ],
                            "outputs": [
                              { "name": "result", "type": "string", "description": "out" }
                            ]
                          },
                          "metadata": {
                            "author": "nexo",
                            "license": "MIT",
                            "repository": "https://example.com",
                            "usageCount": "7",
                            "lastUpdated": "2026-06-08T12:00:00Z"
                          }
                        }
                      ]
                    }
                    """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(httpClient, NullLogger<HttpRemoteBrickCatalog>.Instance);
        var entries = await sut.GetAllAsync();

        entries.Should().HaveCount(1);
        var brick = entries[0];
        brick.Id.Should().Be("rich.brick");
        brick.Interface.Inputs.Should().ContainSingle(i => i.Name == "prompt" && i.Required);
        brick.Interface.Outputs.Should().ContainSingle(o => o.Name == "result");
        brick.Metadata.Author.Should().Be("nexo");
        brick.Metadata.UsageCount.Should().Be(7);
        brick.Metadata.LastUpdated.Should().NotBeNull();
        brick.HostCapabilities.Should().NotBeNull();
        brick.HostCapabilities!.NodeId.Should().Be("node-rich");
        brick.HostCapabilities.Tier.Should().Be(NodeTierDto.Standard);
    }

    [Fact]
    public async Task GetAllAsync_returns_empty_when_catalog_fetch_throws()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            throw new HttpRequestException("network down")))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(httpClient, NullLogger<HttpRemoteBrickCatalog>.Instance);
        (await sut.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCapabilitiesWithStaleness_returns_cached_value_within_ttl_without_refetch()
    {
        var capabilityCalls = 0;
        using var httpClient = new HttpClient(new FakeHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath == "/api/capabilities")
            {
                Interlocked.Increment(ref capabilityCalls);
                return Task.FromResult(JsonResponse("""
                    {
                      "nodeId": "node-cache",
                      "tier": 1,
                      "platform": 2,
                      "hotModelIds": [],
                      "availableModelIds": [],
                      "supportedCapabilities": [0],
                      "acceptingRemoteWork": true,
                      "generatedAt": "2026-06-08T00:00:00Z"
                    }
                    """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            capabilityTtl: TimeSpan.FromMinutes(5));

        var first = await sut.GetCapabilitiesWithStalenessAsync();
        var second = await sut.GetCapabilitiesWithStalenessAsync();

        first.Capabilities!.NodeId.Should().Be("node-cache");
        second.Capabilities!.NodeId.Should().Be("node-cache");
        second.IsStale.Should().BeFalse();
        capabilityCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_returns_empty_when_payload_has_no_bricks_array()
    {
        using var httpClient = new HttpClient(new FakeHandler((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath == "/api/bricks")
                return Task.FromResult(JsonResponse("""{"items":[]}"""));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(httpClient, NullLogger<HttpRemoteBrickCatalog>.Instance);
        (await sut.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCapabilitiesWithStaleness_uses_stale_store_on_capabilities_not_found()
    {
        var staleStore = new StaleCapabilitiesSnapshotStore();
        staleStore.Put("http://remote:7777/", new NodeCapabilityManifestDto
        {
            NodeId = "stale-node",
            Tier = NodeTierDto.Micro,
            Platform = PlatformTypeDto.Linux,
            HotModelIds = [],
            AvailableModelIds = ["phi3-mini"],
            SupportedCapabilities = [TaskCapabilityDto.TextGeneration],
            AcceptingRemoteWork = true,
            GeneratedAt = DateTimeOffset.UtcNow.AddSeconds(-10),
        });

        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))))
        {
            BaseAddress = new Uri("http://remote:7777/", UriKind.Absolute)
        };

        var sut = new HttpRemoteBrickCatalog(
            httpClient,
            NullLogger<HttpRemoteBrickCatalog>.Instance,
            staleStore,
            capabilityTtl: TimeSpan.Zero);

        var result = await sut.GetCapabilitiesWithStalenessAsync();

        result.Capabilities.Should().NotBeNull();
        result.Capabilities!.NodeId.Should().Be("stale-node");
        result.IsStale.Should().BeTrue();
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }

    private sealed class NoOpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
