using Nexo.Agents.TestKit;
using FluentAssertions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for nexo peer brick executor trust.</summary>
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
            TestExecutionContext());

        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("peer-routing.no_eligible_peers");
    }

    /// <summary>Tests for static peer snapshot.</summary>
    private sealed class StaticPeerSnapshot : IPeerCapabilitySnapshot
    {
        /// <summary>Static peer snapshot.</summary>
        /// <param name="candidates">Candidates.</param>
        public StaticPeerSnapshot(IReadOnlyList<PeerExecutionCandidate> candidates) => Candidates = candidates;
        /// <summary>Candidates.</summary>
        public IReadOnlyList<PeerExecutionCandidate> Candidates { get; }
    }

    /// <summary>Tests for static http client factory.</summary>
    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        /// <summary>Static http client factory.</summary>
        /// <param name="client">Client.</param>
        public StaticHttpClientFactory(HttpClient client) => _client = client;
        /// <summary>Creates client.</summary>
        /// <param name="name">Name.</param>
        public HttpClient CreateClient(string name) => _client;
    }

    /// <summary>Tests for test execution context.</summary>
    // Execution context for these tests. The IExecutionContext implementation now
    // lives in Nexo.Agents.TestKit.FakeExecutionContext; only the fixture values
    // that are specific to this suite stay here.
    private static FakeExecutionContext TestExecutionContext() => new()
    {
        AgentId = "test-agent",
        BehaviorId = "test-behavior",
        AuditMode = true,
        Provider = "nexo"
    };
}
