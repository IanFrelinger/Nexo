using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class NexoPeerBrickExecutorGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_returns_invalid_response_when_peer_returns_non_json()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [
            new PeerExecutionCandidate
            {
                PeerId = "peer-a",
                Endpoint = "http://peer-a:8080",
                AvailableVramBytes = 8L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0,
            },
        ]);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "hello" },
            new JobRequirements { ModelId = "m1", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
    }

    [Fact]
    public async Task ExecuteAsync_returns_execution_failed_when_peer_reports_success_false()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success":false,"errorMessage":"peer refused"}""", Encoding.UTF8, "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [
            new PeerExecutionCandidate
            {
                PeerId = "peer-a",
                Endpoint = "http://peer-a:8080",
                AvailableVramBytes = 8L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0,
            },
        ]);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "hello" },
            new JobRequirements { ModelId = "m1", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
    }

    [Fact]
    public async Task ExecuteAsync_skips_peers_with_invalid_endpoints()
    {
        var sut = CreateExecutor(new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))), [
            new PeerExecutionCandidate
            {
                PeerId = "bad-endpoint",
                Endpoint = "ftp://not-http",
                AvailableVramBytes = 8L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0,
            },
        ]);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "hello" },
            new JobRequirements { ModelId = "m1", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.no_eligible_peers");
    }

    [Fact]
    public async Task ExecuteAsync_filters_peers_by_vram_and_queue_depth()
    {
        var sut = CreateExecutor(new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))), [
            new PeerExecutionCandidate
            {
                PeerId = "low-vram",
                Endpoint = "http://peer-low:8080",
                AvailableVramBytes = 512,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0,
            },
            new PeerExecutionCandidate
            {
                PeerId = "deep-queue",
                Endpoint = "http://peer-queue:8080",
                AvailableVramBytes = 16L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 99,
            },
        ], queueDepthThreshold: 5);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "hello" },
            new JobRequirements { ModelId = "m1", MinimumVramBytes = 4096, ComputeClass = GpuComputeClass.Low },
            new GapExecutionContext());

        result.Error!.Code.Should().Be("peer-routing.no_eligible_peers");
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_output_when_success_has_empty_payload()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success":true,"payload":""}""", Encoding.UTF8, "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);

        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_response_when_success_flag_missing()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"summary":"no success flag"}""", Encoding.UTF8, "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
    }

    [Fact]
    public async Task ExecuteAsync_succeeds_with_inline_base64_payload()
    {
        var payload = Convert.ToBase64String(new byte[] { 5, 6, 7 });
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"inline"}""",
                    Encoding.UTF8,
                    "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Payload.Should().Equal([5, 6, 7]);
    }

    [Fact]
    public async Task ExecuteAsync_maps_http_request_failures()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("connection reset"))));
        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);

        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
    }

    [Fact]
    public async Task ExecuteAsync_times_out_slow_peer()
    {
        using var httpClient = new HttpClient(new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        var sut = CreateExecutor(
            httpClient,
            [EligiblePeer("peer-slow", "http://peer-slow:8080")],
            peerRequestTimeout: TimeSpan.FromMilliseconds(50));

        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
    }

    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var config = Options.Create(new RunPodBrickConfig());
        var snapshot = new StaticPeerSnapshot([]);

        Action actFactory = () => new NexoPeerBrickExecutor(null!, NullLogger<NexoPeerBrickExecutor>.Instance, snapshot, config);
        actFactory.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");

        Action actLogger = () => new NexoPeerBrickExecutor(new StaticHttpClientFactory(new HttpClient()), null!, snapshot, config);
        actLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");

        Action actSnapshot = () => new NexoPeerBrickExecutor(new StaticHttpClientFactory(new HttpClient()), NullLogger<NexoPeerBrickExecutor>.Instance, null!, config);
        actSnapshot.Should().Throw<ArgumentNullException>().WithParameterName("snapshot");
    }

    [Fact]
    public async Task ExecuteAsync_returns_null_response_when_peer_body_empty()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
        result.Error.Detail.Should().Contain("peer.null_response");
    }

    [Fact]
    public async Task ExecuteAsync_succeeds_with_output_wrapper_payload()
    {
        var payload = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var json = $$"""
        {
          "success": true,
          "summary": "wrapped",
          "output": {
            "payload": { "base64": "{{payload}}" },
            "outputPath": "/tmp/wrapped.bin"
          }
        }
        """;
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Payload.Should().Equal([1, 2, 3, 4]);
        result.Value.OutputPath.Should().Be("/tmp/wrapped.bin");
        result.Value.Summary.Should().Be("wrapped");
    }

    [Fact]
    public async Task ExecuteAsync_deduplicates_peers_by_endpoint_authority()
    {
        var callCount = 0;
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            var payload = Convert.ToBase64String(new byte[] { 9 });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"ok"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }));

        var sut = CreateExecutor(httpClient,
        [
            EligiblePeer("peer-a", "http://peer-a:8080/v1"),
            EligiblePeer("peer-b", "http://peer-a:8080/v2"),
        ]);

        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_filters_peers_by_compute_class()
    {
        var sut = CreateExecutor(new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))), [
            new PeerExecutionCandidate
            {
                PeerId = "low-compute",
                Endpoint = "http://peer-low:8080",
                AvailableVramBytes = 16L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.Low,
                QueueDepth = 0,
            },
        ]);

        var result = await sut.ExecuteAsync(
            Payload(),
            new JobRequirements { ModelId = "m1", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.High },
            new GapExecutionContext());

        result.Error!.Code.Should().Be("peer-routing.no_eligible_peers");
    }

    [Fact]
    public async Task ExecuteAsync_uses_custom_peer_routing_brick_id_in_request_uri()
    {
        string? requestedPath = null;
        using var httpClient = new HttpClient(new FakeHandler((request, _) =>
        {
            requestedPath = request.RequestUri?.AbsolutePath;
            var payload = Convert.ToBase64String(new byte[] { 7 });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"custom brick"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }));

        var sut = new NexoPeerBrickExecutor(
            new StaticHttpClientFactory(httpClient),
            NullLogger<NexoPeerBrickExecutor>.Instance,
            new StaticPeerSnapshot([EligiblePeer("peer-a", "http://peer-a:8080")]),
            Options.Create(new RunPodBrickConfig
            {
                PeerRoutingBrickId = "custom.peer.brick",
                PeerRequestTimeout = TimeSpan.FromSeconds(1),
            }));

        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsSuccess.Should().BeTrue();
        requestedPath.Should().Be("/api/bricks/custom.peer.brick/execute");
    }

    [Fact]
    public async Task ExecuteAsync_maps_http_status_errors()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                ReasonPhrase = "Bad Gateway",
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Detail.Should().Contain("peer.http_error");
    }

    [Fact]
    public async Task ExecuteAsync_maps_unexpected_dispatch_exceptions()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new InvalidOperationException("handler blew up"))));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Detail.Should().Contain("peer.dispatch_exception");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_invalid_base64_payload()
    {
        using var httpClient = new HttpClient(new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":true,"payload":"not-valid-base64!!!","summary":"bad"}""",
                    Encoding.UTF8,
                    "application/json"),
            })));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Detail.Should().Contain("peer.invalid_output");
    }

    [Fact]
    public async Task ExecuteAsync_serializes_rich_execution_context_variables()
    {
        string? body = null;
        using var httpClient = new HttpClient(new FakeHandler(async (request, _) =>
        {
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            var payload = Convert.ToBase64String(new byte[] { 3 });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"ctx"}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var context = new GapExecutionContext
        {
            Variables = new Dictionary<string, object>
            {
                ["count"] = 42,
                ["enabled"] = true,
                ["tags"] = new[] { "alpha", "beta" },
                ["meta"] = new Dictionary<string, object> { ["tier"] = "high" },
            },
        };

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload
            {
                ModelId = "m1",
                Prompt = "line1\nline2",
                OutputFormat = "application/json",
                IsPriority = true,
                GenerationParameters = new Dictionary<string, object> { ["temperature"] = 0.2 },
            },
            new JobRequirements
            {
                ModelId = "m1",
                MinimumVramBytes = 4096,
                ComputeClass = GpuComputeClass.Medium,
                EstimatedDuration = TimeSpan.FromSeconds(12),
                IsOvernightOrBackground = true,
            },
            context);

        result.IsSuccess.Should().BeTrue();
        body.Should().NotBeNullOrWhiteSpace();
        body.Should().Contain("line1\\nline2");
        body.Should().Contain("\"temperature\":0.2");
        body.Should().Contain("\"count\":42");
        body.Should().Contain("\"enabled\":true");
        body.Should().Contain("\"tags\":[\"alpha\",\"beta\"]");
        body.Should().Contain("\"tier\":\"high\"");
        body.Should().Contain("\"overnight\":true");
    }

    [Fact]
    public async Task ExecuteAsync_serializes_non_generic_idictionary_variables()
    {
        string? body = null;
        using var httpClient = new HttpClient(new FakeHandler(async (request, _) =>
        {
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            var payload = Convert.ToBase64String(new byte[] { 1 });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"dict"}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(
            Payload(),
            Requirements(),
            new GapExecutionContext
            {
                Variables = new Dictionary<string, object>
                {
                    ["legacy"] = new System.Collections.Hashtable
                    {
                        ["legacyKey"] = "legacyValue",
                    },
                },
            });

        result.IsSuccess.Should().BeTrue();
        body.Should().Contain("\"legacyKey\":\"legacyValue\"");
    }

    [Fact]
    public async Task ExecuteAsync_escapes_special_characters_in_request_json()
    {
        string? body = null;
        using var httpClient = new HttpClient(new FakeHandler(async (request, _) =>
        {
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            var payload = Convert.ToBase64String(new byte[] { 2 });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"escape"}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }));

        var sut = CreateExecutor(httpClient, [EligiblePeer("peer-a", "http://peer-a:8080")]);
        var result = await sut.ExecuteAsync(
            new RunPodJobPayload
            {
                ModelId = "m\"1",
                Prompt = "tab\there\"quote\\back",
            },
            Requirements(),
            new GapExecutionContext());

        result.IsSuccess.Should().BeTrue();
        body.Should().Contain("m\\\"1");
        body.Should().Contain("tab\\there\\\"quote\\\\back");
    }

    [Fact]
    public async Task ExecuteAsync_falls_back_to_second_peer_after_first_fails()
    {
        var hosts = new List<string>();
        using var httpClient = new HttpClient(new FakeHandler((request, _) =>
        {
            hosts.Add(request.RequestUri?.Host ?? string.Empty);
            if (request.RequestUri?.Host == "peer-a")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            var payload = Convert.ToBase64String(new byte[] { 8, 9 });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"success":true,"payload":"{{payload}}","summary":"peer-b"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }));

        var sut = CreateExecutor(httpClient,
        [
            EligiblePeer("peer-a", "http://peer-a:8080", queueDepth: 0),
            EligiblePeer("peer-b", "http://peer-b:8080", queueDepth: 1),
        ]);

        var result = await sut.ExecuteAsync(Payload(), Requirements(), new GapExecutionContext());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Payload.Should().Equal([8, 9]);
        hosts.Should().ContainInOrder("peer-a", "peer-b");
    }

    private static RunPodJobPayload Payload() => new() { ModelId = "m1", Prompt = "hello" };

    private static JobRequirements Requirements() =>
        new() { ModelId = "m1", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low };

    private static PeerExecutionCandidate EligiblePeer(string id, string endpoint, int queueDepth = 0) => new()
    {
        PeerId = id,
        Endpoint = endpoint,
        AvailableVramBytes = 8L * 1024 * 1024 * 1024,
        ComputeClass = GpuComputeClass.High,
        QueueDepth = queueDepth,
    };

    private static NexoPeerBrickExecutor CreateExecutor(
        HttpClient httpClient,
        IReadOnlyList<PeerExecutionCandidate> candidates,
        int queueDepthThreshold = 10,
        TimeSpan? peerRequestTimeout = null) =>
        new(
            new StaticHttpClientFactory(httpClient),
            NullLogger<NexoPeerBrickExecutor>.Instance,
            new StaticPeerSnapshot(candidates),
            Options.Create(new RunPodBrickConfig
            {
                QueueDepthThreshold = queueDepthThreshold,
                PeerRequestTimeout = peerRequestTimeout ?? TimeSpan.FromSeconds(1),
            }));

    private sealed class GapExecutionContext : IExecutionContext
    {
        public string AgentId { get; init; } = "gap-agent";
        public string BehaviorId { get; init; } = "gap-behavior";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; } = true;
        public string Provider { get; init; } = "local";
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }

    private sealed class StaticPeerSnapshot(IReadOnlyList<PeerExecutionCandidate> candidates) : IPeerCapabilitySnapshot
    {
        public IReadOnlyList<PeerExecutionCandidate> Candidates { get; } = candidates;
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
