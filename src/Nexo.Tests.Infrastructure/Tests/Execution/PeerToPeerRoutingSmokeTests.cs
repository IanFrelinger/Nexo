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

[Trait("Category", "Smoke")]
public sealed class PeerToPeerRoutingSmokeTests
{
    [Fact]
    public async Task PeerExecutor_FallsBackToNextPeer_WhenFirstPeerFails()
    {
        var requestedHosts = new List<string>();
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(async (request, _) =>
        {
            requestedHosts.Add(request.RequestUri?.Host ?? string.Empty);
            if (string.Equals(request.RequestUri?.Host, "peer-a", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            var json = CreateSuccessResponseJson([9, 8, 7], "/tmp/peer-b-output.bin", "peer-b ok");
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }));

        var snapshot = new StaticPeerSnapshot(
        [
            new PeerExecutionCandidate
            {
                PeerId = "peer-a",
                Endpoint = "http://peer-a:8080",
                AvailableVramBytes = 12L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0
            },
            new PeerExecutionCandidate
            {
                PeerId = "peer-b",
                Endpoint = "http://peer-b:8080",
                AvailableVramBytes = 16L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.Extreme,
                QueueDepth = 1
            }
        ]);
        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = 10,
            PeerRequestTimeout = TimeSpan.FromSeconds(1),
            PeerRoutingBrickId = "generation.capability-routing"
        });
        var sut = new NexoPeerBrickExecutor(
            new StaticHttpClientFactory(httpClient),
            NullLogger<NexoPeerBrickExecutor>.Instance,
            snapshot,
            config);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "model-x", Prompt = "hello" },
            new JobRequirements { ModelId = "model-x", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new TestExecutionContext());

        result.IsSuccess.Should().BeTrue($"{result.Error?.Code}:{result.Error?.Message}:{result.Error?.Detail}");
        result.Value.Should().NotBeNull();
        result.Value!.Provider.Should().Be("nexo-peer");
        result.Value.Payload.Should().Equal([9, 8, 7]);
        requestedHosts.Should().ContainInOrder("peer-a", "peer-b");
    }

    [Fact]
    public async Task PeerExecutor_FailsOverWhenPeerTimesOut()
    {
        var requestedHosts = new List<string>();
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestedHosts.Add(request.RequestUri?.Host ?? string.Empty);
            if (string.Equals(request.RequestUri?.Host, "peer-slow", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            var json = CreateSuccessResponseJson([1, 2, 3], "/tmp/peer-fast-output.bin", "peer-fast ok");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));

        var snapshot = new StaticPeerSnapshot(
        [
            new PeerExecutionCandidate
            {
                PeerId = "peer-slow",
                Endpoint = "http://peer-slow:8080",
                AvailableVramBytes = 12L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0
            },
            new PeerExecutionCandidate
            {
                PeerId = "peer-fast",
                Endpoint = "http://peer-fast:8080",
                AvailableVramBytes = 12L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 1
            }
        ]);
        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = 10,
            PeerRequestTimeout = TimeSpan.FromMilliseconds(40),
            PeerRoutingBrickId = "generation.capability-routing"
        });
        var sut = new NexoPeerBrickExecutor(
            new StaticHttpClientFactory(httpClient),
            NullLogger<NexoPeerBrickExecutor>.Instance,
            snapshot,
            config);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "model-timeout", Prompt = "timeout test" },
            new JobRequirements { ModelId = "model-timeout", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new TestExecutionContext());

        result.IsSuccess.Should().BeTrue($"{result.Error?.Code}:{result.Error?.Message}:{result.Error?.Detail}");
        result.Value.Should().NotBeNull();
        result.Value!.Payload.Should().Equal([1, 2, 3]);
        requestedHosts.Should().ContainInOrder("peer-slow", "peer-fast");
    }

    [Fact]
    public async Task PeerExecutor_ReturnsAggregatedFailure_WhenAllPeersFail()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler((request, _) =>
        {
            if (string.Equals(request.RequestUri?.Host, "peer-down", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException("connection refused");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
        }));

        var snapshot = new StaticPeerSnapshot(
        [
            new PeerExecutionCandidate
            {
                PeerId = "peer-down",
                Endpoint = "http://peer-down:8080",
                AvailableVramBytes = 12L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0
            },
            new PeerExecutionCandidate
            {
                PeerId = "peer-bad-gateway",
                Endpoint = "http://peer-bad-gateway:8080",
                AvailableVramBytes = 12L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 1
            }
        ]);
        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = 10,
            PeerRequestTimeout = TimeSpan.FromSeconds(1),
            PeerRoutingBrickId = "generation.capability-routing"
        });
        var sut = new NexoPeerBrickExecutor(
            new StaticHttpClientFactory(httpClient),
            NullLogger<NexoPeerBrickExecutor>.Instance,
            snapshot,
            config);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "model-fail", Prompt = "all fail" },
            new JobRequirements { ModelId = "model-fail", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new TestExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("peer-routing.all_peers_failed");
        result.Error.Detail.Should().NotBeNullOrWhiteSpace();
        result.Error.Detail!.Should().Contain("peer-down");
        result.Error.Detail.Should().Contain("peer-bad-gateway");
    }

    private static string CreateSuccessResponseJson(byte[] payload, string outputPath, string summary)
    {
        var base64 = Convert.ToBase64String(payload);
        return $$"""
        {
          "wireFormatVersion": "2025-02",
          "success": true,
          "summary": "{{summary}}",
          "output": {
            "payload": {
              "__type": "bytes",
              "base64": "{{base64}}"
            },
            "outputPath": "{{outputPath}}"
          }
        }
        """;
    }

    private sealed class StaticPeerSnapshot : IPeerCapabilitySnapshot
    {
        public StaticPeerSnapshot(IReadOnlyList<PeerExecutionCandidate> candidates)
        {
            Candidates = candidates;
        }

        public IReadOnlyList<PeerExecutionCandidate> Candidates { get; }
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StaticHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
            => _client;
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

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId { get; init; } = "smoke-agent";
        public string BehaviorId { get; init; } = "smoke-behavior";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; } = true;
        public string Provider { get; init; } = "nexo";
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }
}
