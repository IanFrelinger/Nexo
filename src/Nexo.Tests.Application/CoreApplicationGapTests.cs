using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using Nexo.Core.Application.Persistence.Ports;
using Nexo.Core.Application.Knowledge.Models;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Middleware;
using Nexo.Core.Application.Middleware.Ports;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.Orchestration.Profiles;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.SelfImprovement.Models;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Workflows;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Xunit;

namespace Nexo.Tests.Application;

public class CoreApplicationGapTests
{
    [Fact]
    public void AggregatedResult_and_InstanceResult_round_trip()
    {
        var instance = new InstanceResult
        {
            InstanceId = "i1",
            ParameterSet = new Dictionary<string, object> { ["p"] = 1 },
            Passed = true,
            Duration = TimeSpan.FromSeconds(3),
            Adaptations = new List<AdaptationRecord>
            {
                new()
                {
                    Id = "r1",
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "timeout",
                    FixApplied = AdaptationFixType.Brick,
                    RegressionPassed = true,
                },
            },
        };

        var agg = new AggregatedResult
        {
            Results = new[] { instance },
            BestCandidate = instance,
            AllPassed = true,
        };
        agg.Results.Should().ContainSingle();
        agg.BestCandidate!.InstanceId.Should().Be("i1");
        agg.AllPassed.Should().BeTrue();
    }

    [Fact]
    public void NoOpNexoIngressAccessor_returns_nulls()
    {
        var accessor = new NoOpNexoIngressAccessor();
        accessor.CorrelationId.Should().BeNull();
        accessor.Transport.Should().BeNull();
        accessor.TenantId.Should().BeNull();
        accessor.AppId.Should().BeNull();
        accessor.IdempotencyKey.Should().BeNull();
        accessor.PayloadVersion.Should().BeNull();
    }

    [Fact]
    public void AssemblyProfile_record_round_trips()
    {
        var profile = new AssemblyProfile(true, true);
        profile.EnableDecompile.Should().BeTrue();
        profile.EnableSecurityScan.Should().BeTrue();
    }

    [Fact]
    public void Environment_models_round_trip()
    {
        new VoxelChunkKey(0, 1, 2, 3).LayerId.Should().BeNull();
        new VoxelChunkKey(0, 1, 2, 3, "layer").LayerId.Should().Be("layer");

        var ctx = new MapDataRequestContext("corr", new Dictionary<string, string?> { ["k"] = "v" });
        ctx.CorrelationId.Should().Be("corr");

        var req = new VectorMapIntelligenceRequest(
            new byte[] { 1, 2 },
            "geojson",
            ctx);
        req.MaxOutputBytes.Should().Be(512 * 1024);

        new VectorMapIntelligenceResult(new byte[] { 1 }, "geojson", "notes", true)
            .WasModified.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Category=Holdout", "Category!=Holdout")]
    [InlineData("invalid", null)]
    public void HoldoutTestOptions_regression_filter(string? input, string? expected)
    {
        new HoldoutTestOptions { HoldoutFilter = input }
            .GetRegressionExclusionFilter()
            .Should().Be(expected);
    }

    [Fact]
    public void Trust_and_pipeline_models_round_trip()
    {
        new TrustPolicyPackStatus("p1", "1.0", DateTimeOffset.UtcNow).ActivePackId.Should().Be("p1");

        new TrustPolicyPackProjectRule("/proj", new Dictionary<string, bool> { ["r"] = true })
            .SourceRules["r"].Should().BeTrue();

        new PipelineWorkerBinding { DeterministicPoolId = "d", AgenticPoolId = "a" }
            .DeterministicPoolId.Should().Be("d");
    }

    [Fact]
    public void Knowledge_and_networking_models_round_trip()
    {
        new PullProgress { ModelId = "m", BytesDownloaded = 10, TotalBytes = 100 }
            .BytesDownloaded.Should().Be(10);

        var query = new KnowledgeQueryRequest { MaxCount = 50, Offset = 5, Sources = new[] { KnowledgeSource.Pattern } };
        query.MaxCount.Should().Be(50);

        new KnowledgeQueryResult { Total = 0, HasMore = false }.HasMore.Should().BeFalse();

        new KnowledgeQueryEntry
        {
            Id = "k1",
            Source = KnowledgeSource.Adaptation,
            Timestamp = DateTimeOffset.UtcNow,
            Provenance = new Dictionary<string, string> { ["n"] = "1" },
        }.Source.Should().Be(KnowledgeSource.Adaptation);

        new SelfAnalysisEntry
        {
            Id = "s1",
            Timestamp = DateTimeOffset.UtcNow,
            DecisionType = "adapt",
            TargetId = "brick",
            Outcome = "ok",
            Improved = true,
            Reason = "fixed",
        }.Improved.Should().BeTrue();

        new KnowledgeChunk { Id = "c1", SourceNodeId = "n1", ContentHash = "abc" }
            .ContentHash.Should().Be("abc");

        new PlasticityMetrics { NodeId = "n1", PeerCount = 2, HotBrickCount = 1 }.PeerCount.Should().Be(2);

        new KnowledgeSyncStatus { NodeId = "n1", PeerIds = new[] { "p1" } }.NodeId.Should().Be("n1");

        new NetworkAgentEntry { AgentId = "a1", NodeId = "n1" }.AgentId.Should().Be("a1");
    }

    [Fact]
    public void Environment_port_results_and_map_bounds_round_trip()
    {
        new MapDataGeographicBounds(-1, -1, 1, 1).MaxLatitude.Should().Be(1);

        new TerrainMapDataResult(new byte[] { 1 }, "image/tiff", 5.0, "dem")
            .ResolutionMeters.Should().Be(5.0);

        new VectorMapDataResult(new byte[] { 2 }, "application/geo+json", "osm")
            .ContentType.Should().Be("application/geo+json");

        new SuggestedMaterialSpec("wood", "Wood", "URP/Lit", "#FFFFFF", 0.1, 0.5, "Opaque", new Dictionary<string, string>())
            .ShaderName.Should().Be("URP/Lit");

        new VoxelChunkKey(1, 10, 20, 30, "terrain").LayerId.Should().Be("terrain");
    }

    [Fact]
    public void Knowledge_and_execution_model_properties_round_trip()
    {
        new Nexo.Core.Application.Knowledge.Models.KnowledgeQueryRequest
        {
            Since = DateTimeOffset.UtcNow.AddDays(-1),
            DataType = "pattern",
            MaxCount = 5,
            Offset = 1,
            Sources = new[] { Nexo.Core.Application.Knowledge.Models.KnowledgeSource.Pattern },
        }.MaxCount.Should().Be(5);

        new Nexo.Core.Application.Knowledge.Models.KnowledgeQueryEntry
        {
            Id = "k1",
            Source = Nexo.Core.Application.Knowledge.Models.KnowledgeSource.Adaptation,
            Timestamp = DateTimeOffset.UtcNow,
            Summary = "snippet",
        }.Summary.Should().Be("snippet");

        new Nexo.Core.Application.Execution.Models.HotSwapResult(
                "step-1",
                Nexo.Core.Application.Execution.Models.ExecutionMode.Deterministic,
                Nexo.Core.Application.Execution.Models.ExecutionMode.Agentic,
                true,
                null,
                DateTimeOffset.UtcNow)
            .Success.Should().BeTrue();

        new Nexo.Core.Application.Adaptation.ImmutableCoreViolationException("/core/file.cs")
            .TargetPathOrComponentId.Should().Contain("core");
    }

    [Fact]
    public void Workflow_execution_metrics_and_events_round_trip()
    {
        new WorkflowMetrics
        {
            TotalDuration = TimeSpan.FromSeconds(2),
            NodesExecuted = 3,
            DeterministicBricks = 2,
            AgenticBricks = 1,
            TotalTokensUsed = 100,
            CacheHits = 1,
        }.CacheHits.Should().Be(1);

        new NodeMetrics
        {
            Duration = TimeSpan.FromMilliseconds(50),
            Implementation = Nexo.Core.Domain.Bricks.ImplementationType.Deterministic,
            TokensUsed = 10,
            CacheHit = true,
        }.CacheHit.Should().BeTrue();

        new BrickImplementationSelectedEvent("c1", "n1", "Echo", Nexo.Core.Domain.Bricks.ImplementationType.Agentic)
            .BrickName.Should().Be("Echo");
        new BrickImplementationSelectedEvent("c1", "n1", "Echo", Nexo.Core.Domain.Bricks.ImplementationType.Agentic)
            .NodeId.Should().Be("n1");
        new BrickImplementationSelectedEvent("c1", "n1", "Echo", Nexo.Core.Domain.Bricks.ImplementationType.Agentic)
            .Implementation.Should().Be(Nexo.Core.Domain.Bricks.ImplementationType.Agentic);

        var inputNode = new InputNode
        {
            Id = "n1",
            Type = InputType.Content,
            Content = "x",
            Outputs = new List<NodePort>(),
        };
        new NodeStartedEvent("c1", inputNode).Node.Id.Should().Be("n1");
        new NodeCompletedEvent("c1", inputNode, new NodeResult { Success = true }).Result.Success.Should().BeTrue();

        new NodeBrickEvent("c1", "n1", new StepCompletedEvent("s1", "b1", Nexo.Core.Domain.Bricks.ImplementationType.Deterministic, 1))
            .NodeId.Should().Be("n1");
    }

    [Fact]
    public void Workflow_execution_events_and_remaining_models_round_trip()
    {
        new WorkflowStartedEvent("c1", "wf").WorkflowName.Should().Be("wf");
        new ExecutionPlanCreatedEvent("c1", new ExecutionPlan()).CorrelationId.Should().Be("c1");
        new WorkflowCompletedEvent("c1", new WorkflowResult { Success = true }).Result.Success.Should().BeTrue();
        new WorkflowFailedEvent("c1", new InvalidOperationException("x")).Error.Message.Should().Be("x");

        new VoxelChunkDataResult(true, new byte[] { 1 }, "application/octet-stream", "etag")
            .ContentType.Should().Be("application/octet-stream");

        new ArtifactCleanupOptions { CleanBeforeTest = true, RepoRoot = "/repo" }.RepoRoot.Should().Be("/repo");
        new EphemeralDbOptions("postgres", Database: "test").Engine.Should().Be("postgres");
        new TrustPolicyPackInfo("pack", "1.0", "Pack", "desc").Version.Should().Be("1.0");
        new MapVerificationRequest(
            new MapDataGeographicBounds(0, 0, 1, 1),
            TierIndex: 1,
            EnvironmentManifestId: "env",
            DataBinding: null,
            VectorSamplePayload: null,
            VectorFormatHint: "geojson",
            Context: new MapDataRequestContext("corr")).TierIndex.Should().Be(1);
    }

    [Fact]
    public async Task ParallelLoopKernel_runs_parallel_iterations_when_enabled()
    {
        var kernel = new ParallelLoopKernel(new SequentialLoopKernel());
        var result = await kernel.ForEachAsync(
            Enumerable.Range(0, 4),
            async (_, _, ct) =>
            {
                await Task.Delay(10, ct);
                return LoopAction.Continue;
            },
            new LoopOptions
            {
                Name = "parallel-loop",
                EnableParallel = true,
                MaxDegreeOfParallelism = 2,
            },
            CancellationToken.None);

        result.Iterations.Should().Be(4);
        result.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task InstrumentedLoopKernel_delegates_async_for_each()
    {
        var kernel = new InstrumentedLoopKernel(new SequentialLoopKernel(), NullLogger<InstrumentedLoopKernel>.Instance);
        var result = await kernel.ForEachAsync(
            new[] { 1, 2 },
            async (_, _, ct) =>
            {
                await Task.Yield();
                return LoopAction.Continue;
            },
            new LoopOptions { Name = "instrumented-async" },
            CancellationToken.None);

        result.Iterations.Should().Be(2);
        kernel.SelectToList(
            new[] { 1, 2, 3 },
            (value, _, _) => value * 2,
            new LoopOptions { Name = "instrumented-select" },
            CancellationToken.None).Should().Equal(2, 4, 6);
    }

    [Fact]
    public void InstrumentedLoopKernel_delegates_to_inner_kernel()
    {
        var inner = new SequentialLoopKernel();
        var kernel = new InstrumentedLoopKernel(
            inner,
            NullLogger<InstrumentedLoopKernel>.Instance);

        var result = kernel.ForEach(
            new[] { 1, 2, 3 },
            (_, _, _) => LoopAction.Continue,
            new LoopOptions { Name = "test-loop" },
            CancellationToken.None);

        result.Iterations.Should().Be(3);
        result.Completed.Should().BeTrue();

        var selected = kernel.SelectToList(
            new[] { "a", "b" },
            (item, _, _) => item.ToUpperInvariant(),
            new LoopOptions { Name = "select" },
            CancellationToken.None);

        selected.Should().Equal("A", "B");
    }

    [Fact]
    public async Task IngressLoggingPipelineBehavior_scopes_logs_when_correlation_present()
    {
        var ingress = new Mock<INexoIngressAccessor>();
        ingress.Setup(i => i.CorrelationId).Returns("corr-1");
        ingress.Setup(i => i.Transport).Returns("http");
        ingress.Setup(i => i.TenantId).Returns("tenant");
        ingress.Setup(i => i.AppId).Returns("app");
        ingress.Setup(i => i.IdempotencyKey).Returns("idem");
        ingress.Setup(i => i.PayloadVersion).Returns("v1");

        var behavior = new Nexo.Core.Application.Behaviors.IngressLoggingPipelineBehavior<DummyRequest, string>(
            ingress.Object,
            NullLogger<Nexo.Core.Application.Behaviors.IngressLoggingPipelineBehavior<DummyRequest, string>>.Instance);

        var result = await behavior.Handle(new DummyRequest(), () => Task.FromResult("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task IngressLoggingPipelineBehavior_skips_scope_without_correlation()
    {
        var behavior = new Nexo.Core.Application.Behaviors.IngressLoggingPipelineBehavior<DummyRequest, string>(
            new NoOpNexoIngressAccessor(),
            NullLogger<Nexo.Core.Application.Behaviors.IngressLoggingPipelineBehavior<DummyRequest, string>>.Instance);

        (await behavior.Handle(new DummyRequest(), () => Task.FromResult("plain"), CancellationToken.None))
            .Should().Be("plain");
    }

    [Fact]
    public void AssertionException_carries_message()
    {
        new Nexo.Core.Application.Testing.Abstractions.AssertionException("assert failed")
            .Message.Should().Be("assert failed");
    }

    [Fact]
    public async Task RunTestsHandler_runs_cleanup_before_and_after_when_configured()
    {
        var runner = new Mock<ITestRunner>();
        runner.Setup(r => r.RunTestsAsync(It.IsAny<string?>(), It.IsAny<IProgress<Nexo.Core.Application.Common.Models.ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestExecutionResult
            {
                TotalTests = 1,
                PassedTests = 1,
                FailedTests = 0,
                TotalDuration = TimeSpan.FromMilliseconds(10),
                Results = [],
            });

        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(c => c.CleanAsync("test-artifacts", It.IsAny<ArtifactCleanupContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactCleanupResult("test-artifacts", 1024, Array.Empty<string>(), Array.Empty<string>()));

        var handler = new RunTestsHandler(
            runner.Object,
            NullLogger<RunTestsHandler>.Instance,
            cleanup.Object,
            Options.Create(new ArtifactCleanupOptions
            {
                CleanBeforeTest = true,
                CleanAfterTest = true,
                RepoRoot = Directory.GetCurrentDirectory(),
            }));

        var result = await handler.Handle(new RunTestsCommand("Category=Unit"), CancellationToken.None);

        result.PassedTests.Should().Be(1);
        cleanup.Verify(c => c.CleanAsync("test-artifacts", It.IsAny<ArtifactCleanupContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunTestsHandler_continues_when_cleanup_throws()
    {
        var runner = new Mock<ITestRunner>();
        runner.Setup(r => r.RunTestsAsync(null, It.IsAny<IProgress<Nexo.Core.Application.Common.Models.ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestExecutionResult
            {
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                TotalDuration = TimeSpan.Zero,
                Results = [],
            });

        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(c => c.CleanAsync("test-artifacts", It.IsAny<ArtifactCleanupContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cleanup failed"));

        var handler = new RunTestsHandler(
            runner.Object,
            NullLogger<RunTestsHandler>.Instance,
            cleanup.Object,
            Options.Create(new ArtifactCleanupOptions { CleanBeforeTest = true }));

        (await handler.Handle(new RunTestsCommand(null), CancellationToken.None)).TotalTests.Should().Be(0);
    }

    private sealed record DummyRequest : MediatR.IRequest<string>;
}
