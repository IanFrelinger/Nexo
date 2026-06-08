using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

public sealed class MeshLeaseSweepBackgroundServiceGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_skips_when_sweep_disabled()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("job", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Assigned,
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
        });

        var service = new MeshLeaseSweepBackgroundService(
            registry,
            new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions { SweepEnabled = false }),
            NullLogger<MeshLeaseSweepBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        (await registry.GetAsync(created.TaskId))!.Status.Should().Be(MeshTaskStatus.Assigned);
    }

    [Fact]
    public async Task ExecuteAsync_reclaims_expired_running_tasks()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("running-job", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Running,
            AssignedPeerId = "peer-1",
            AssignedApiBaseUrl = "http://peer:8080",
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        var service = new MeshLeaseSweepBackgroundService(
            registry,
            new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions
            {
                SweepEnabled = true,
                SweepIntervalMinutes = 60,
            }),
            NullLogger<MeshLeaseSweepBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(250);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        var updated = await registry.GetAsync(created.TaskId);
        updated!.Status.Should().Be(MeshTaskStatus.Pending);
        updated.PlacementReason.Should().Be("lease.expired");
    }

    [Fact]
    public async Task ExecuteAsync_leaves_non_expired_leases_unchanged()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("active-job", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Assigned,
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
        });

        var service = new MeshLeaseSweepBackgroundService(
            registry,
            new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions
            {
                SweepEnabled = true,
                SweepIntervalMinutes = 60,
            }),
            NullLogger<MeshLeaseSweepBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        (await registry.GetAsync(created.TaskId))!.Status.Should().Be(MeshTaskStatus.Assigned);
    }
}

public sealed class MeshPendingTaskRebalancerBackgroundServiceGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_skips_when_disabled()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("stale", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
        });

        var placement = new Mock<IMeshTaskPlacementService>();
        var service = new MeshPendingTaskRebalancerBackgroundService(
            registry,
            placement.Object,
            new StaticOptionsMonitor<MeshElasticSchedulingOptions>(new MeshElasticSchedulingOptions { Enabled = false }),
            NullLogger<MeshPendingTaskRebalancerBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        placement.Verify(
            p => p.TryScheduleAsync(It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

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
                return Json(HttpStatusCode.NotFound, "{}");

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

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeFleetHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }

    private sealed class CountingHandler(Action onRequest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onRequest();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

internal sealed class StaticOptionsMonitor<T>(T value) : Microsoft.Extensions.Options.IOptionsMonitor<T> where T : class
{
    public T CurrentValue { get; } = value;
    public T Get(string? name) => CurrentValue;
    public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
}

internal sealed class NullDisposable : IDisposable
{
    public static readonly NullDisposable Instance = new();
    public void Dispose() { }
}
