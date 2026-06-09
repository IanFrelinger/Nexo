using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;
using Nexo.Infrastructure.Networking;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet.Networking;

public class InfrastructureNetworkingGapCoverageTests
{
    [Fact]
    public void Networking_options_expose_defaults()
    {
        PlasticityOptions.SectionName.Should().Be("Plasticity");
        new PlasticityOptions { HotBrickTopN = 5, ColdBrickWindowMinutes = 30 }.HotBrickTopN.Should().Be(5);

        KnowledgeSyncServiceOptions.SectionName.Should().Be("KnowledgeSync");
        new KnowledgeSyncServiceOptions { NodeId = "node-a", PeerUrls = new[] { "https://peer" } }
            .PeerUrls.Should().ContainSingle();

        NetworkAgentDirectoryOptions.SectionName.Should().Be("NetworkAgentDirectory");
        new NetworkAgentDirectoryOptions { DiscoveredEntryMaxAgeSeconds = 120 }.DiscoveredEntryMaxAgeSeconds
            .Should().Be(120);
    }

    [Fact]
    public async Task InMemoryKnowledgeChunkStore_adds_filters_and_trims()
    {
        var store = new InMemoryKnowledgeChunkStore(maxChunks: 2);
        await store.AddAsync(Chunk("a", "text/plain"));
        await store.AddAsync(Chunk("b", "text/plain"));
        await store.AddAsync(Chunk("c", "application/json"));

        (await store.GetAsync()).Should().HaveCount(2);
        (await store.GetAsync("application/json")).Should().ContainSingle(c => c.Id == "c");

        await store.RemoveAsync("c");
        (await store.GetAsync()).Should().NotContain(c => c.Id == "c");
    }

    [Fact]
    public async Task InMemoryKnowledgeChunkStore_rejects_invalid_chunks()
    {
        var store = new InMemoryKnowledgeChunkStore();
        var act = () => store.AddAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();

        var act2 = () => store.AddAsync(Chunk("", "text/plain") with { Id = "" });
        await act2.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HttpNetworkBus_delivers_subscribed_events_and_deduplicates()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [] });
        NetworkEvent? received = null;
        var sub = await bus.SubscribeAsync("test", (evt, _) =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var evt = Event("e1", "test");
        await bus.DeliverAsync(evt);
        await Task.Delay(50);
        received.Should().NotBeNull();

        await bus.DeliverAsync(evt);
        sub.Dispose();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_send_skips_unknown_peer_and_rejects_null_event()
    {
        var bus = CreateBus(new NetworkBusOptions
        {
            NodeId = "local",
            PeerUrls = new[] { "https://peer.example.com" },
            HeartbeatIntervalSeconds = 0,
        });

        var act = () => bus.SendAsync("unknown", null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();

        await bus.SendAsync("unknown", Event("e2", "ping"));
        await bus.SendAsync("", Event("e3", "ping"));
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_delivers_only_matching_event_types()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [] });
        var received = new List<string>();
        var sub = await bus.SubscribeAsync("ping", (evt, _) =>
        {
            received.Add(evt.EventType);
            return Task.CompletedTask;
        });

        await bus.DeliverAsync(Event("e4", "pong"));
        await bus.DeliverAsync(Event("e5", "ping"));
        await Task.Delay(50);

        received.Should().ContainSingle().Which.Should().Be("ping");
        sub.Dispose();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_send_drops_events_when_hops_exceed_max()
    {
        var sent = 0;
        var bus = CreateBus(
            new NetworkBusOptions
            {
                NodeId = "local",
                PeerUrls = new[] { "https://peer.example.com" },
                HeartbeatIntervalSeconds = 0,
            },
            new FakeHandler((_, _) =>
            {
                Interlocked.Increment(ref sent);
                return Json(HttpStatusCode.OK, "{}");
            }));

        var evt = Event("e6", "relay") with { Hops = 1, MaxHops = 1 };
        await bus.SendAsync("peer.example.com", evt);

        sent.Should().Be(0);
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_send_logs_non_success_http_status()
    {
        var bus = CreateBus(
            new NetworkBusOptions
            {
                NodeId = "local",
                PeerUrls = new[] { "https://peer.example.com" },
                HeartbeatIntervalSeconds = 0,
            },
            new FakeHandler((_, _) => Json(HttpStatusCode.InternalServerError, "{}")));

        await bus.SendAsync("peer.example.com", Event("e7", "fail"));
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_broadcast_skips_self_node()
    {
        var sent = 0;
        var bus = CreateBus(
            new NetworkBusOptions
            {
                NodeId = "peer.example.com",
                PeerUrls = new[] { "https://peer.example.com" },
                HeartbeatIntervalSeconds = 0,
            },
            new FakeHandler((_, _) =>
            {
                Interlocked.Increment(ref sent);
                return Json(HttpStatusCode.OK, "{}");
            }));

        await bus.BroadcastAsync(Event("e8", "heartbeat"));
        sent.Should().Be(0);
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_deliverAsync_rejects_null_event()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [] });
        var act = () => bus.DeliverAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_subscribe_rejects_null_handler()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [] });
        var act = () => bus.SubscribeAsync("ping", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_wildcard_subscription_receives_all_event_types()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [] });
        var received = new List<string>();
        var sub = await bus.SubscribeAsync("*", (evt, _) =>
        {
            received.Add(evt.EventType);
            return Task.CompletedTask;
        });

        await bus.DeliverAsync(Event("e9", "alpha"));
        await bus.DeliverAsync(Event("e10", "beta"));
        await Task.Delay(50);

        received.Should().BeEquivalentTo(new[] { "alpha", "beta" });
        sub.Dispose();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_evicts_old_event_ids_after_max_history()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [], MaxEventHistory = 2 });
        await bus.DeliverAsync(Event("evict-1", "a"));
        await bus.DeliverAsync(Event("evict-2", "b"));
        await bus.DeliverAsync(Event("evict-3", "c"));

        var redelivered = 0;
        var sub = await bus.SubscribeAsync("a", (_, _) =>
        {
            Interlocked.Increment(ref redelivered);
            return Task.CompletedTask;
        });
        await bus.DeliverAsync(Event("evict-1", "a"));
        await Task.Delay(50);

        redelivered.Should().Be(1);
        sub.Dispose();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_send_swallows_http_client_exceptions()
    {
        var bus = CreateBus(
            new NetworkBusOptions
            {
                NodeId = "local",
                PeerUrls = new[] { "https://peer.example.com" },
                HeartbeatIntervalSeconds = 0,
            },
            new FakeHandler((_, _) => throw new HttpRequestException("network down")));

        await bus.SendAsync("peer.example.com", Event("e11", "boom"));
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_deliver_relays_to_peers_except_source()
    {
        var sentTo = new List<string>();
        var bus = CreateBus(
            new NetworkBusOptions
            {
                NodeId = "local.example.com",
                PeerUrls = new[] { "https://peer-a.example.com", "https://peer-b.example.com" },
                HeartbeatIntervalSeconds = 0,
            },
            new FakeHandler((req, _) =>
            {
                sentTo.Add(req.RequestUri!.Host);
                return Json(HttpStatusCode.OK, "{}");
            }));

        var evt = Event("relay-1", "gossip") with
        {
            SourceNodeId = "peer-a.example.com",
            Hops = 0,
            MaxHops = 2,
        };
        await bus.DeliverAsync(evt);
        await Task.Delay(100);

        sentTo.Should().ContainSingle().Which.Should().Be("peer-b.example.com");
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_broadcast_rejects_null_event()
    {
        var bus = CreateBus(new NetworkBusOptions { NodeId = "local", PeerUrls = [] });
        var act = () => bus.BroadcastAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkBus_heartbeat_broadcasts_to_peers()
    {
        var sent = 0;
        var bus = CreateBus(
            new NetworkBusOptions
            {
                NodeId = "local-node",
                PeerUrls = new[] { "https://peer.example.com" },
                HeartbeatIntervalSeconds = 1,
            },
            new FakeHandler((_, _) =>
            {
                Interlocked.Increment(ref sent);
                return Json(HttpStatusCode.OK, "{}");
            }));

        await Task.Delay(1300);
        sent.Should().BeGreaterThan(0);
        bus.Dispose();
    }

    [Fact]
    public async Task HttpNetworkAgentDirectory_registers_and_queries_local_agents()
    {
        var directory = CreateDirectory(new NetworkAgentDirectoryOptions { NodeId = "node-1", PeerUrls = [] });
        var entry = new NetworkAgentEntry
        {
            AgentId = "agent-1",
            NodeId = "node-1",
            Domain = "combat",
            Capabilities = new[] { "scan" },
        };

        await directory.RegisterAsync(entry);
        (await directory.GetByNodeAsync("node-1")).Should().ContainSingle();
        (await directory.GetByAgentIdAsync("agent-1"))!.AgentId.Should().Be("agent-1");
        (await directory.GetAllAsync()).Should().ContainSingle();

        await directory.UnregisterAsync("agent-1");
        (await directory.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task NetworkNegotiationService_finds_agents_by_capability_and_domain()
    {
        var directory = new Mock<INetworkAgentDirectory>();
        directory.Setup(d => d.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new NetworkAgentEntry
                {
                    AgentId = "a1",
                    NodeId = "n1",
                    Domain = "security",
                    Capabilities = new[] { "scan", "audit" },
                },
                new NetworkAgentEntry
                {
                    AgentId = "a2",
                    NodeId = "n2",
                    Domain = "combat",
                    Capabilities = new[] { "plan" },
                },
            });

        var service = new NetworkNegotiationService(directory.Object, NullLogger<NetworkNegotiationService>.Instance);

        (await service.FindByCapabilityAsync(new[] { "scan" })).Should().ContainSingle(a => a.AgentId == "a1");
        (await service.FindByCapabilityAsync(Array.Empty<string>())).Should().BeEmpty();
        (await service.FindByDomainAsync("combat")).Should().ContainSingle(a => a.AgentId == "a2");
        (await service.FindByDomainAsync("")).Should().BeEmpty();
    }

    [Fact]
    public async Task HttpKnowledgeSyncService_reports_status_and_handles_push_pull()
    {
        var handler = new FakeHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Post)
                return Json(HttpStatusCode.OK, "{}");
            if (req.Method == HttpMethod.Get)
            {
                var json = JsonSerializer.Serialize(new[]
                {
                    new KnowledgeChunk { Id = "k1", SourceNodeId = "remote", ContentType = "text/plain" },
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }

            return Json(HttpStatusCode.NotFound, "{}");
        });

        var factory = CreateFactory(handler);
        var service = new HttpKnowledgeSyncService(
            Options.Create(new KnowledgeSyncServiceOptions
            {
                NodeId = "local",
                PeerUrls = new[] { "https://peer.example.com" },
            }),
            factory,
            NullLogger<HttpKnowledgeSyncService>.Instance);

        (await service.GetStatusAsync()).NodeId.Should().Be("local");
        await service.PushAsync(new KnowledgeChunk { Id = "k1", SourceNodeId = "local" });
        (await service.PullAsync("peer.example.com", maxCount: 10)).Should().ContainSingle();
        (await service.PullAsync("missing")).Should().BeEmpty();
    }

    [Fact]
    public async Task HttpNetworkAgentDirectory_discovers_peer_agents_on_refresh()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new NetworkAgentEntry
            {
                AgentId = "remote-agent",
                NodeId = "peer.example.com",
                Domain = "planning",
                Capabilities = new[] { "decompose" },
            },
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var directory = CreateDirectory(
            new NetworkAgentDirectoryOptions
            {
                NodeId = "local-node",
                PeerUrls = new[] { "https://peer.example.com" },
                DiscoveredEntryMaxAgeSeconds = 0,
            },
            new FakeHandler((req, _) =>
            {
                req.RequestUri!.AbsolutePath.Should().Be("/api/agents");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }));

        await directory.RefreshFromPeersAsync();
        (await directory.GetAllAsync()).Should().ContainSingle(a => a.AgentId == "remote-agent");
        (await directory.GetByAgentIdAsync("remote-agent"))!.Domain.Should().Be("planning");
    }

    [Fact]
    public async Task HttpNetworkAgentDirectory_skips_invalid_peer_urls_and_failed_discovery()
    {
        var directory = CreateDirectory(
            new NetworkAgentDirectoryOptions
            {
                NodeId = "local-node",
                PeerUrls = new[] { "not-a-url", "https://peer.example.com" },
                DiscoveredEntryMaxAgeSeconds = 0,
            },
            new FakeHandler((_, _) => throw new HttpRequestException("peer down")));

        await directory.RefreshFromPeersAsync();
        (await directory.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task PlasticityService_aggregates_optional_dependencies()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INetworkBus>(CreateBus(new NetworkBusOptions { NodeId = "p-node", PeerUrls = [] }));
        services.AddSingleton<IKnowledgeSyncService>(new HttpKnowledgeSyncService(
            Options.Create(new KnowledgeSyncServiceOptions { NodeId = "p-node" }),
            CreateFactory(new FakeHandler((_, _) => Json(HttpStatusCode.OK, "{}"))),
            NullLogger<HttpKnowledgeSyncService>.Instance));

        var provider = services.BuildServiceProvider();
        var metrics = await new PlasticityService(
            provider,
            Options.Create(new PlasticityOptions()),
            NullLogger<PlasticityService>.Instance).GetMetricsAsync();

        metrics.NodeId.Should().Be("p-node");
    }

    private static KnowledgeChunk Chunk(string id, string contentType) =>
        new() { Id = id, SourceNodeId = "node", ContentType = contentType };

    private static NetworkEvent Event(string id, string type) => new()
    {
        EventId = id,
        SourceNodeId = "local",
        EventType = type,
        Timestamp = DateTimeOffset.UtcNow,
        MaxHops = 1,
    };

    private static HttpNetworkBus CreateBus(NetworkBusOptions options)
    {
        var factory = CreateFactory(new FakeHandler((_, _) => Json(HttpStatusCode.OK, "{}")));
        return new HttpNetworkBus(Options.Create(options), factory, NullLogger<HttpNetworkBus>.Instance);
    }

    private static HttpNetworkBus CreateBus(NetworkBusOptions options, HttpMessageHandler handler)
    {
        var factory = CreateFactory(handler);
        return new HttpNetworkBus(Options.Create(options), factory, NullLogger<HttpNetworkBus>.Instance);
    }

    private static HttpNetworkAgentDirectory CreateDirectory(
        NetworkAgentDirectoryOptions options,
        HttpMessageHandler? handler = null) =>
        new(
            Options.Create(options),
            CreateFactory(handler ?? new FakeHandler((_, _) => Json(HttpStatusCode.OK, "[]"))),
            NullLogger<HttpNetworkAgentDirectory>.Instance);

    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHttpClient("Nexo.Networking").ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request, cancellationToken));
    }
}
