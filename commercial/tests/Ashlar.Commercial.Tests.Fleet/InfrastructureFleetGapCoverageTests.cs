using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Commercial.Fleet.Infrastructure;
using Moq;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for infrastructure fleet gap coverage.</summary>
public class InfrastructureFleetGapCoverageTests
{
    [Fact]
    public void MeshFleetRegistrationOptions_exposes_defaults()
    {
        MeshFleetRegistrationOptions.SectionPath.Should().Be("Ashlar:Mesh:Fleet");
        new MeshFleetRegistrationOptions
        {
            RequirePeerRegistrationKey = true,
            MinRegistrationKeyLength = 16,
        }.MinRegistrationKeyLength.Should().Be(16);
    }

    [Fact]
    public async Task LiteDbFleetNodeRegistry_persists_node_lifecycle()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "fleet-" + Guid.NewGuid() + ".db");
        var registry = new LiteDbFleetNodeRegistry(dbPath);
        var node = SampleNode("peer-lite");

        await registry.RegisterOrUpdateAsync(node);
        (await registry.GetAsync("peer-lite"))!.PeerId.Should().Be("peer-lite");
        (await registry.ListAsync()).Should().ContainSingle();

        (await registry.SetDrainedAsync("peer-lite", true)).Should().BeTrue();
        (await registry.SetAdmittedAsync("peer-lite", false)).Should().BeTrue();
        await registry.HeartbeatAsync("peer-lite", reportedQueueDepth: 2);
        (await registry.GetAsync("peer-lite"))!.ReportedQueueDepth.Should().Be(2);

        (await registry.RemoveAsync("peer-lite")).Should().BeTrue();
        (await registry.GetAsync("peer-lite")).Should().BeNull();

        if (File.Exists(dbPath)) File.Delete(dbPath);
    }

    [Fact]
    public async Task LiteDbFleetNodeRegistry_returns_false_for_missing_peer_mutations()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "fleet-missing-" + Guid.NewGuid() + ".db");
        var registry = new LiteDbFleetNodeRegistry(dbPath);

        (await registry.SetDrainedAsync("missing", true)).Should().BeFalse();
        (await registry.SetAdmittedAsync("missing", false)).Should().BeFalse();
        await registry.HeartbeatAsync("missing");

        if (File.Exists(dbPath)) File.Delete(dbPath);
    }

    [Fact]
    public async Task MeshLeaseSweepBackgroundService_reclaims_expired_leases()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("job", 1, Array.Empty<string>(), null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Assigned,
            AssignedPeerId = "peer-1",
            AssignedApiBaseUrl = "http://peer:8080",
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
        });

        var monitor = new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions
        {
            SweepEnabled = true,
            SweepIntervalMinutes = 60,
        });
        var service = new MeshLeaseSweepBackgroundService(
            registry,
            monitor,
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
    public async Task MeshPeerKnowledgePullBackgroundService_imports_export_payload()
    {
        var payload = new MeshKnowledgeExportPayload(
            DateTimeOffset.UtcNow,
            new[]
            {
                new AdaptationRecord
                {
                    Id = "adapt-import",
                    Timestamp = DateTimeOffset.UtcNow,
                    BrickId = "brick",
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Source,
                    RegressionPassed = true,
                    Promoted = false,
                    Message = "from-peer",
                },
            },
            new[]
            {
                new ObservedPattern
                {
                    PatternId = "pat-import",
                    EventType = "edit",
                    Frequency = 1,
                    FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-1),
                    LastSeen = DateTimeOffset.UtcNow,
                    ProjectPath = "/tmp",
                },
            });

        var handler = new FakeFleetHandler((req, _) =>
        {
            req.RequestUri!.AbsolutePath.Should().Contain("/api/mesh/knowledge/export");
            return Json(HttpStatusCode.OK, JsonSerializer.Serialize(payload));
        });

        var adaptLog = new Mock<Ashlar.Core.Application.Adaptation.Ports.IAdaptationLog>();
        adaptLog.Setup(l => l.LogAsync(It.IsAny<AdaptationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var patternStore = new Mock<Ashlar.Core.Application.Observation.Ports.IPatternStore>();
        patternStore.Setup(s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var import = new MeshKnowledgeImportService(adaptLog.Object, patternStore.Object);
        var services = new ServiceCollection();
        services.AddHttpClient(string.Empty)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var monitor = new StaticOptionsMonitor<MeshPeerKnowledgeSyncOptions>(new MeshPeerKnowledgeSyncOptions
        {
            Enabled = true,
            PeerBaseUrls = new[] { "http://peer-a:8080" },
            IntervalMinutes = 1,
        });

        var service = new MeshPeerKnowledgePullBackgroundService(
            factory,
            import,
            monitor,
            NullLogger<MeshPeerKnowledgePullBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await Task.Delay(300);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        adaptLog.Verify(l => l.LogAsync(It.IsAny<AdaptationRecord>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        patternStore.Verify(s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MeshPendingTaskRebalancerBackgroundService_reschedules_stale_pending_tasks()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("stale-job", 1, Array.Empty<string>(), null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
        });

        var placement = new Mock<IMeshTaskPlacementService>();
        placement.Setup(p => p.TryScheduleAsync(created.TaskId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (MeshTaskState?)null, (string?)null));

        var monitor = new StaticOptionsMonitor<MeshElasticSchedulingOptions>(new MeshElasticSchedulingOptions
        {
            Enabled = true,
            IntervalMinutes = 60,
            PendingStaleSeconds = 30,
        });

        var service = new MeshPendingTaskRebalancerBackgroundService(
            registry,
            placement.Object,
            monitor,
            NullLogger<MeshPendingTaskRebalancerBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(250);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        placement.Verify(
            p => p.TryScheduleAsync(created.TaskId, null, null, null, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>Json.</summary>
    /// <param name="status">Status.</param>
    /// <param name="json">Json.</param>
    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Handles fake fleet requests.</summary>
    private sealed class FakeFleetHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : HttpMessageHandler
    {
        /// <summary>Send async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }

    /// <summary>Static options monitor.</summary>
    private sealed class StaticOptionsMonitor<T>(T value) : Microsoft.Extensions.Options.IOptionsMonitor<T> where T : class
    {
        /// <summary>Current value.</summary>
        public T CurrentValue { get; } = value;
        /// <summary>Gets the value.</summary>
        /// <param name="name">Name.</param>
        public T Get(string? name) => CurrentValue;
        /// <summary>On change.</summary>
        /// <param name="listener">Listener.</param>
        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
    }

    /// <summary>Null disposable.</summary>
    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        /// <summary>Dispose.</summary>
        public void Dispose() { }
    }

    /// <summary>Sample node.</summary>
    /// <param name="peerId">Peer id.</param>
    private static MeshFleetNodeState SampleNode(string peerId) => new(
        PeerId: peerId,
        ApiBaseUrl: "http://localhost:8080",
        Labels: new Dictionary<string, string> { ["zone"] = "a" },
        AdvertisedBrickIds: new[] { "brick-1" },
        Drained: false,
        LastHeartbeatUtc: DateTimeOffset.UtcNow,
        RegisteredAtUtc: DateTimeOffset.UtcNow,
        ReportedQueueDepth: 0,
        Admitted: true);
}
