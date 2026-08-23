using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh peer knowledge pull background service gap coverage.</summary>
public sealed class MeshPeerKnowledgePullBackgroundServiceGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_skips_http_when_disabled()
    {
        var requestCount = 0;
        var services = new ServiceCollection();
        services.AddHttpClient(string.Empty)
            .ConfigurePrimaryHttpMessageHandler(() => new CountingHandler(() => Interlocked.Increment(ref requestCount)));
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var import = new MeshKnowledgeImportService(
            Mock.Of<IAdaptationLog>(),
            Mock.Of<IPatternStore>());

        var service = new MeshPeerKnowledgePullBackgroundService(
            factory,
            import,
            new StaticOptionsMonitor<MeshPeerKnowledgeSyncOptions>(new MeshPeerKnowledgeSyncOptions
            {
                Enabled = false,
                PeerBaseUrls = ["http://peer:8080"],
            }),
            NullLogger<MeshPeerKnowledgePullBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));
        await service.StartAsync(cts.Token);
        await Task.Delay(400);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        requestCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_skips_invalid_peer_urls_and_failed_responses()
    {
        var requestCount = 0;
        var adaptLog = new Mock<IAdaptationLog>();
        var handler = new FakeFleetHandler((req, _) =>
        {
            Interlocked.Increment(ref requestCount);
            if (req.RequestUri!.AbsolutePath.Contains("bad-peer", StringComparison.Ordinal))
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.NotFound, "{}");

            /// <summary>Json.</summary>
            return Json(HttpStatusCode.OK, "null");
        });

        var services = new ServiceCollection();
        services.AddHttpClient(string.Empty)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var import = new MeshKnowledgeImportService(adaptLog.Object, Mock.Of<IPatternStore>());
        var service = new MeshPeerKnowledgePullBackgroundService(
            factory,
            import,
            new StaticOptionsMonitor<MeshPeerKnowledgeSyncOptions>(new MeshPeerKnowledgeSyncOptions
            {
                Enabled = true,
                PeerBaseUrls = ["not-a-url", "http://bad-peer:8080", "http://empty-payload:8080"],
                IntervalMinutes = 1,
            }),
            NullLogger<MeshPeerKnowledgePullBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        requestCount.Should().BeGreaterThanOrEqualTo(2);
        adaptLog.Verify(l => l.LogAsync(It.IsAny<AdaptationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Json.</summary>
    /// <param name="status">Status.</param>
    /// <param name="json">Json.</param>
    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Handles fake fleet requests.</summary>
    private sealed class FakeFleetHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        /// <summary>Send async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }

    /// <summary>Handles counting requests.</summary>
    private sealed class CountingHandler(Action onRequest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            /// <summary>On request.</summary>
            onRequest();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
