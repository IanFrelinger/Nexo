using FluentAssertions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class NexoPeerBrickExecutorTrustTests
{
    [Fact]
    public async Task TrustedOnly_skips_untrusted_peer_and_returns_no_eligible_peers()
    {
        var snapshot = new StaticPeerSnapshot(
        [
            new PeerExecutionCandidate
            {
                PeerId = "untrusted-peer",
                Endpoint = "http://peer:8080/",
                AvailableVramBytes = 32L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                QueueDepth = 0,
                TrustTier = Nexo.Core.Application.Mesh.Models.PeerTrustTier.Untrusted,
                CapturedAt = DateTimeOffset.UtcNow
            }
        ]);
        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = 100,
            PeerRequestTimeout = TimeSpan.FromSeconds(2),
            PeerRoutingBrickId = "generation.capability-routing",
            PeerTrustPolicy = "trusted-only"
        });
        var sut = new NexoPeerBrickExecutor(
            new StaticHttpClientFactory(new HttpClient()),
            NullLogger<NexoPeerBrickExecutor>.Instance,
            snapshot,
            config);

        var result = await sut.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m", Prompt = "x" },
            new JobRequirements { ModelId = "m", MinimumVramBytes = 1, ComputeClass = GpuComputeClass.Low },
            new TestExecutionContext());

        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("peer-routing.no_eligible_peers");
    }

    private sealed class StaticPeerSnapshot : IPeerCapabilitySnapshot
    {
        public StaticPeerSnapshot(IReadOnlyList<PeerExecutionCandidate> candidates) => Candidates = candidates;
        public IReadOnlyList<PeerExecutionCandidate> Candidates { get; }
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public StaticHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId { get; init; } = "test-agent";
        public string BehaviorId { get; init; } = "test-behavior";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; } = true;
        public string Provider { get; init; } = "nexo";
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }
}
