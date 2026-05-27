using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Composition.Models;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.ParallelTesting.Models;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis.BrickAnalyzer;
using Nexo.Infrastructure.Composition;
using Nexo.Infrastructure.Environments;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Sdk;
using Nexo.Infrastructure.Fleet;
using Nexo.Infrastructure.Maintenance.Adapters;
using Nexo.Infrastructure.Mesh;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.ParallelTesting;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests;

public class InfrastructureGapCoverageTests
{
    [Fact]
    public async Task AutoApproveUserFeedbackCapture_always_approves()
    {
        (await new AutoApproveUserFeedbackCapture().ApproveAsync("deploy?")).Should().BeTrue();
    }

    [Fact]
    public async Task NullBlobStorageLifecycle_is_noop()
    {
        var lifecycle = new NullBlobStorageLifecycle();
        await lifecycle.PauseAsync();
        await lifecycle.ResumeAsync();
    }

    [Fact]
    public void LocalNexoInstanceCapabilitiesProvider_returns_local()
    {
        new LocalNexoInstanceCapabilitiesProvider().GetCapabilities().Should().Be(InstanceCapabilities.LocalNexo);
    }

    [Fact]
    public async Task NoOp_environment_services_return_empty_results()
    {
        var matReq = new MaterialIntelligenceRequest("forest", new Dictionary<string, string>(), new MapDataRequestContext("s1"));
        var mat = await new NoOpMaterialIntelligenceService().SuggestMaterialsAsync(matReq);
        mat.Materials.Should().BeEmpty();
        mat.Summary.Should().BeNull();

        var vecReq = new VectorMapIntelligenceRequest(
            new byte[] { 1, 2 },
            "geojson",
            new MapDataRequestContext("s1"));
        var vec = await new NoOpVectorMapIntelligenceService().RefineAsync(vecReq);
        vec.RefinedPayload.ToArray().Should().Equal(1, 2);
        vec.WasModified.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyPatternStore_stores_and_queries_empty()
    {
        var store = new EmptyPatternStore();
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "p1",
            EventType = "edit",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        });
        (await store.QueryAsync(new PatternStoreQueryParams())).Should().BeEmpty();
    }

    [Theory]
    [InlineData("supervised", false, false)]
    [InlineData("semi", true, false)]
    [InlineData("full", true, true)]
    public void PermissionEnvelope_parses_autonomy(string autonomy, bool canApply, bool canPromote)
    {
        var env = new PermissionEnvelope(autonomy);
        env.CanApplySourceFix.Should().Be(canApply);
        env.CanApplyBrickFix.Should().Be(canApply);
        env.CanPromoteWithoutApproval.Should().Be(canPromote);
    }

    [Fact]
    public void MeshFleetRegistrationKeys_fingerprint_and_director_check()
    {
        MeshFleetRegistrationKeys.Fingerprint(null).Should().BeNull();
        MeshFleetRegistrationKeys.Fingerprint("  ").Should().BeNull();
        var fp = MeshFleetRegistrationKeys.Fingerprint("my-key");
        fp.Should().NotBeNullOrWhiteSpace();
        fp!.Length.Should().Be(16);
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("abc", "abc").Should().BeFalse();
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("abc", "def").Should().BeTrue();
    }

    [Fact]
    public void ConvergenceDetector_detects_pass_and_stagnation()
    {
        var detector = new ConvergenceDetector();
        detector.HasConverged(Array.Empty<AggregatedTestResult>()).Should().BeFalse();

        var passed = new AggregatedTestResult
        {
            Results = new[]
            {
                new TestInstance
                {
                    InstanceId = "i",
                    ParameterSet = new ParameterSet(),
                    Passed = true,
                },
            },
            AllPassed = true,
        };
        detector.HasConverged(new[] { passed }).Should().BeTrue();

        AggregatedTestResult Round(int passCount) => new()
        {
            Results = Enumerable.Range(0, passCount).Select(i => new TestInstance
            {
                InstanceId = $"i{i}",
                ParameterSet = new ParameterSet(),
                Passed = true,
            }).Concat(Enumerable.Range(0, 3 - passCount).Select(i => new TestInstance
            {
                InstanceId = $"f{i}",
                ParameterSet = new ParameterSet(),
                Passed = false,
            })).ToList(),
            AllPassed = false,
        };

        detector.HasConverged(new[] { Round(2), Round(1), Round(0) }).Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryCompositionCache_stores_and_retrieves()
    {
        var cache = new InMemoryCompositionCache();
        var agent = new ComposedAgent
        {
            Id = "a1",
            ProblemDescription = "unique-problem",
            ComponentIds = new[] { "c1" },
        };
        await cache.StoreAsync(agent, validated: true);
        var key = agent.ProblemDescription.Trim().ToLowerInvariant().GetHashCode().ToString();
        (await cache.TryGetAsync(key)).Should().BeTrue();
    }

    [Fact]
    public async Task InMemorySelfAnalysisLogger_logs_and_trims()
    {
        var logger = new InMemorySelfAnalysisLogger();
        for (var i = 0; i < 5; i++)
        {
            await logger.LogAsync(new SelfAnalysisEntry
            {
                Id = $"e{i}",
                Timestamp = DateTimeOffset.UtcNow,
                DecisionType = "brick",
                TargetId = "t",
            });
        }
        (await logger.GetRecentAsync(3)).Should().HaveCount(3);
    }

    [Fact]
    public async Task CliUserFeedbackCapture_reads_yes_no_from_reader()
    {
        var yes = new CliUserFeedbackCapture(new StringReader("yes\n"), new StringWriter());
        (await yes.ApproveAsync("change")).Should().BeTrue();

        var no = new CliUserFeedbackCapture(new StringReader("n\n"), new StringWriter());
        (await no.ApproveAsync("change")).Should().BeFalse();
    }

    [Fact]
    public void BehaviorRegistry_registers_and_queries()
    {
        var behavior = new Behavior { Id = "b1", Name = "name", Description = "desc" };
        var registry = new BehaviorRegistry(new[] { behavior });
        registry.GetBehavior("b1").Should().Be(behavior);
        registry.GetBehavior("missing").Should().BeNull();
        registry.GetAllBehaviors().Should().ContainSingle();
    }

    [Fact]
    public async Task ConflictResolver_picks_first_candidate_or_null()
    {
        var resolver = new ConflictResolver();
        var manifest = new BrickManifest
        {
            Id = "brick-1",
            Name = "n",
            Interface = new Nexo.Core.Domain.Bricks.BrickInterface(),
        };

        (await resolver.ResolveAsync(Array.Empty<BrickManifest>())).Should().BeNull();
        (await resolver.ResolveAsync(new[] { manifest })).Should().Be(manifest);
    }

    [Fact]
    public async Task InstanceResultAggregator_prefers_fastest_pass_or_fewest_adaptations()
    {
        var aggregator = new InstanceResultAggregator();
        var slowPass = new InstanceResult
        {
            InstanceId = "slow",
            ParameterSet = new Dictionary<string, object>(),
            Passed = true,
            Duration = TimeSpan.FromSeconds(10),
        };
        var fastPass = new InstanceResult
        {
            InstanceId = "fast",
            ParameterSet = new Dictionary<string, object>(),
            Passed = true,
            Duration = TimeSpan.FromSeconds(1),
        };
        var failedMany = new InstanceResult
        {
            InstanceId = "fail-many",
            ParameterSet = new Dictionary<string, object>(),
            Passed = false,
            Adaptations = new[]
            {
                new AdaptationRecord
                {
                    Id = "a1",
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "x",
                    FixApplied = AdaptationFixType.Source,
                },
                new AdaptationRecord
                {
                    Id = "a2",
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "y",
                    FixApplied = AdaptationFixType.Source,
                },
            },
        };
        var failedFew = new InstanceResult
        {
            InstanceId = "fail-few",
            ParameterSet = new Dictionary<string, object>(),
            Passed = false,
            Adaptations = new[]
            {
                new AdaptationRecord
                {
                    Id = "a3",
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "z",
                    FixApplied = AdaptationFixType.Source,
                },
            },
        };

        var passAgg = await aggregator.AggregateAsync(new[] { slowPass, fastPass });
        passAgg.AllPassed.Should().BeTrue();
        passAgg.BestCandidate!.InstanceId.Should().Be("fast");

        var failAgg = await aggregator.AggregateAsync(new[] { failedMany, failedFew });
        failAgg.AllPassed.Should().BeFalse();
        failAgg.BestCandidate!.InstanceId.Should().Be("fail-few");
    }

    [Fact]
    public async Task NoOpMapVerificationService_always_passes()
    {
        var service = new NoOpMapVerificationService();
        var request = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 0,
            EnvironmentManifestId: null,
            DataBinding: null,
            VectorSamplePayload: null,
            VectorFormatHint: null,
            Context: new MapDataRequestContext("session-1"));

        var report = await service.VerifyAsync(request);
        report.PassedCoreChecks.Should().BeTrue();
        report.Issues.Should().BeEmpty();
    }

    [Fact]
    public void OverrideProviderExecutionContext_overrides_provider()
    {
        var inner = new Mock<IExecutionContext>();
        inner.Setup(c => c.AgentId).Returns("agent");
        inner.Setup(c => c.BehaviorId).Returns("behavior");
        inner.Setup(c => c.IsAirGapped).Returns(true);
        inner.Setup(c => c.AuditMode).Returns(false);
        inner.Setup(c => c.Provider).Returns("openai");
        inner.Setup(c => c.Variables).Returns(new Dictionary<string, object>());

        var ctx = new OverrideProviderExecutionContext(inner.Object, "ollama");
        ctx.Provider.Should().Be("ollama");
        ctx.AgentId.Should().Be("agent");
        ctx.IsAirGapped.Should().BeTrue();
    }

    [Fact]
    public async Task CoreVersionManager_records_changes_and_returns_version()
    {
        var manager = new CoreVersionManager(Directory.GetCurrentDirectory());
        var version = await manager.GetCurrentVersionAsync();
        version.Should().NotBeNullOrWhiteSpace();

        await manager.RecordChangeAsync("coverage test");
    }

    [Fact]
    public async Task SneakernetTransport_exports_and_imports_package()
    {
        var entry = new SharedAdaptationEntry
        {
            Id = "adapt-1",
            Record = new AdaptationRecord
            {
                Id = "rec-1",
                Timestamp = DateTimeOffset.UtcNow,
                FailureType = "test",
                FixApplied = AdaptationFixType.Source,
            },
            Files = new Dictionary<string, byte[]> { ["a.txt"] = "hello"u8.ToArray() },
            SourcePeerId = "peer-a",
            BroadcastAt = DateTimeOffset.UtcNow,
        };

        var sync = new Mock<ISharedAdaptationSync>();
        sync.Setup(s => s.PullAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entry });
        sync.Setup(s => s.ValidateAndAdoptAsync(It.IsAny<SharedAdaptationEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var exportPath = Path.Combine(root, "mesh-export");
        var transport = new SneakernetTransport(sync.Object);

        await transport.ExportAsync(exportPath);
        var nxpkg = Directory.GetFiles(root, "*.nxpkg").Single();
        File.Exists(nxpkg).Should().BeTrue();

        sync.Setup(s => s.PullAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SharedAdaptationEntry>());

        var imported = await transport.ImportAsync(nxpkg);
        imported.Should().Be(1);

        (await transport.ImportAsync(Path.Combine(root, "missing.nxpkg"))).Should().Be(0);
    }

    [Fact]
    public void AdaptationRollbackHelper_snapshots_and_restores_files()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "target.txt");
        File.WriteAllText(file, "original");

        var helper = new AdaptationRollbackHelper();
        helper.Snapshot(file);
        helper.SnapshotCount.Should().Be(1);

        File.WriteAllText(file, "modified");
        helper.Rollback(file);
        File.ReadAllText(file).Should().Be("original");
        helper.SnapshotCount.Should().Be(0);

        helper.Snapshot(file);
        helper.Clear();
        helper.SnapshotCount.Should().Be(0);
    }

    [Fact]
    public void AdaptationRollbackHelper_snapshot_all_and_rollback_all()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var a = Path.Combine(root, "a.txt");
        var b = Path.Combine(root, "b.txt");
        File.WriteAllText(a, "a1");
        File.WriteAllText(b, "b1");

        var helper = new AdaptationRollbackHelper();
        helper.SnapshotAll(new[] { a, b });
        File.WriteAllText(a, "a2");
        File.WriteAllText(b, "b2");

        helper.RollbackAll();
        File.ReadAllText(a).Should().Be("a1");
        File.ReadAllText(b).Should().Be("b1");
    }

    [Fact]
    public async Task ModelBackedVectorMapIntelligenceService_skips_oversized_payload()
    {
        var model = new Mock<Nexo.Abstractions.IModel>();
        var service = new ModelBackedVectorMapIntelligenceService(model.Object, maxInputChars: 10);
        var huge = new byte[20];
        var req = new VectorMapIntelligenceRequest(huge, "geojson", new MapDataRequestContext("c1"));

        var result = await service.RefineAsync(req);
        result.WasModified.Should().BeFalse();
        result.Notes.Should().Contain("size limit");
    }

    [Fact]
    public async Task ModelBackedVectorMapIntelligenceService_refines_small_payload()
    {
        var model = new Mock<Nexo.Abstractions.IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<Nexo.Abstractions.ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Nexo.Abstractions.ModelOutput("<osm version=\"0.6\"></osm>"));

        var service = new ModelBackedVectorMapIntelligenceService(model.Object);
        var payload = "<osm broken"u8.ToArray();
        var req = new VectorMapIntelligenceRequest(payload, "osm-xml", new MapDataRequestContext("c1"));

        var result = await service.RefineAsync(req);
        result.WasModified.Should().BeTrue();
        result.Notes.Should().Be("model_refined");
    }

    [Fact]
    public async Task ModelBackedVectorMapIntelligenceService_handles_model_errors()
    {
        var model = new Mock<Nexo.Abstractions.IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<Nexo.Abstractions.ModelInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("model down"));

        var service = new ModelBackedVectorMapIntelligenceService(model.Object);
        var req = new VectorMapIntelligenceRequest("ok"u8.ToArray(), "geojson", new MapDataRequestContext("c1"));

        var result = await service.RefineAsync(req);
        result.WasModified.Should().BeFalse();
        result.Notes.Should().StartWith("model_error:");
    }

    [Fact]
    public async Task InMemoryFleetNodeRegistry_tracks_registration_and_heartbeat()
    {
        var registry = new InMemoryFleetNodeRegistry();
        var node = new MeshFleetNodeState(
            PeerId: "peer-1",
            ApiBaseUrl: "http://localhost:7777",
            Labels: new Dictionary<string, string> { ["role"] = "worker" },
            AdvertisedBrickIds: new[] { "b1" },
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
            RegisteredAtUtc: DateTimeOffset.UtcNow,
            ReportedQueueDepth: 0,
            Admitted: true);

        await registry.RegisterOrUpdateAsync(node);
        (await registry.GetAsync("peer-1"))!.PeerId.Should().Be("peer-1");
        (await registry.ListAsync()).Should().ContainSingle();

        (await registry.SetDrainedAsync("peer-1", true)).Should().BeTrue();
        (await registry.GetAsync("peer-1"))!.Drained.Should().BeTrue();

        (await registry.SetAdmittedAsync("peer-1", false)).Should().BeTrue();
        await registry.HeartbeatAsync("peer-1", reportedQueueDepth: 3);
        (await registry.GetAsync("peer-1"))!.ReportedQueueDepth.Should().Be(3);

        (await registry.RemoveAsync("peer-1")).Should().BeTrue();
        (await registry.GetAsync("peer-1")).Should().BeNull();
    }

    [Fact]
    public void BrickHostServiceCollectionExtensions_binds_options()
    {
        var services = new ServiceCollection();
        services.AddNexoBrickHostOptions(o => o.RemoteCatalogBaseUrls = new[] { "http://remote" });
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BrickHostOptions>>().Value.RemoteCatalogBaseUrls
            .Should().Contain("http://remote");
    }
}
