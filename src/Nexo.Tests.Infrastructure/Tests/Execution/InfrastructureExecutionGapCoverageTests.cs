using Nexo.Agents.TestKit;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Brick.Contracts;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.LoadPolicy;
using Nexo.Infrastructure.Export;
using Xunit;
using ExecutionContext = Nexo.Infrastructure.Execution.ExecutionContext;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for infrastructure execution gap coverage.</summary>
public class InfrastructureExecutionGapCoverageTests
{
    [Fact]
    public async Task ClusterExecutor_yields_error_when_cluster_missing()
    {
        var clusters = new InMemoryClusterRegistry();
        var executor = CreateExecutor(clusters, new StubBrickRegistry());

        var events = await CollectEvents(executor.ExecuteAsync(
            new ClusterInstance { InstanceId = "i1", ClusterId = "missing" },
            new ClusterInput(),
            CreateContext()));

        events.Should().ContainSingle(e => e is StepErrorEvent);
    }

    [Fact]
    public async Task ClusterExecutor_executes_single_brick_and_maps_output()
    {
        var cluster = BuildSingleBrickCluster();
        var clusters = new InMemoryClusterRegistry();
        clusters.Register(cluster);

        var registry = new StubBrickRegistry();
        registry.Add(new EchoBrick("echo-brick"));

        var executor = CreateExecutor(clusters, registry);
        var events = await CollectEvents(executor.ExecuteAsync(
            new ClusterInstance { InstanceId = "inst-1", ClusterId = cluster.Id },
            new ClusterInput(),
            CreateContext()));

        events.Should().Contain(e => e is BehaviorStartedEvent);
        events.Should().Contain(e => e is StepStartedEvent);
        events.Should().Contain(e => e is StepCompletedEvent);
        events.Last().Should().BeOfType<BehaviorCompletedEvent>();
    }

    [Fact]
    public async Task ClusterExecutor_uses_semantic_cache_on_repeat_execution()
    {
        var cluster = BuildSingleBrickCluster();
        var clusters = new InMemoryClusterRegistry();
        clusters.Register(cluster);

        var registry = new StubBrickRegistry();
        registry.Add(new CountingEchoBrick("echo-brick"));

        var cache = new InMemorySemanticCache();
        var executor = CreateExecutor(clusters, registry, cache);
        var instance = new ClusterInstance { InstanceId = "inst-1", ClusterId = cluster.Id };
        var input = new ClusterInput();
        var ctx = CreateContext();

        await CollectEvents(executor.ExecuteAsync(instance, input, ctx));
        var second = await CollectEvents(executor.ExecuteAsync(instance, input, ctx));

        second.Should().Contain(e => e is CacheHitEvent);
        ((CountingEchoBrick)registry.GetBrick("echo-brick")!).Executions.Should().Be(1);
    }

    [Fact]
    public async Task ClusterExecutor_scaled_execution_yields_events_from_instances()
    {
        var cluster = BuildSingleBrickCluster();
        var clusters = new InMemoryClusterRegistry();
        clusters.Register(cluster);
        var registry = new StubBrickRegistry();
        registry.Add(new EchoBrick("echo-brick"));
        var executor = CreateExecutor(clusters, registry);

        var events = await CollectEvents(executor.ExecuteScaledAsync(
            cluster.Id,
            new[]
            {
                new ClusterInstance { InstanceId = "s1", ClusterId = cluster.Id },
                new ClusterInstance { InstanceId = "s2", ClusterId = cluster.Id },
            },
            new ClusterInput(),
            CreateContext()));

        events.Should().Contain(e => e is BehaviorStartedEvent);
        events.Should().Contain(e => e is BehaviorCompletedEvent);
    }

    [Fact]
    public void CompositeBrickRegistry_prefers_local_and_falls_back_to_remote()
    {
        var local = new StubBrickRegistry();
        local.Add(new EchoBrick("local-b1"));

        var remoteEntry = new BrickCatalogEntryDto
        {
            Id = "remote-b1",
            Name = "Remote",
            Category = "Control",
            Description = "remote brick",
            HasDeterministic = true,
            Interface = new BrickInterfaceDto
            {
                Inputs = new List<BrickInputDefinitionDto>(),
                Outputs = new List<BrickOutputDefinitionDto>(),
            },
        };

        var catalog = new Mock<IRemoteBrickCatalog>();
        catalog.Setup(c => c.BaseUrl).Returns("http://remote:7777");
        catalog.Setup(c => c.GetByIdAsync("remote-b1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteEntry);
        catalog.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { remoteEntry });
        catalog.Setup(c => c.GetCapabilitiesWithStalenessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CapabilitiesFetchResult { IsStale = true });

        using var http = new HttpClient();
        var registry = new CompositeBrickRegistry(
            local,
            new[] { catalog.Object },
            http,
            NullLogger<CompositeBrickRegistry>.Instance);

        registry.GetBrick("local-b1").Should().BeOfType<EchoBrick>();
        registry.GetBrick("remote-b1").Should().BeOfType<RemoteBrick>();
        registry.GetAllBricks().Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoteBrick_executes_via_http_and_maps_response()
    {
        var response = new BrickExecuteResponseDto
        {
            Success = true,
            Summary = "remote ok",
            Output = new Dictionary<string, object> { ["result"] = "done" },
        };
        var json = JsonSerializer.Serialize(response);
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            req.RequestUri!.AbsolutePath.Should().Contain("/api/bricks/remote-1/execute");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        });
        using var client = new HttpClient(handler);

        var entry = new BrickCatalogEntryDto
        {
            Id = "remote-1",
            Name = "Remote One",
            Category = "Control",
            Description = "remote",
            HasDeterministic = true,
            HasAgentic = true,
            Interface = new BrickInterfaceDto
            {
                Inputs = new List<BrickInputDefinitionDto> { new() { Name = "in", Type = "string", Required = true } },
                Outputs = new List<BrickOutputDefinitionDto> { new() { Name = "result", Type = "string" } },
            },
            HostCapabilities = new Nexo.Brick.Contracts.Capabilities.NodeCapabilityManifestDto
            {
                NodeId = "node-1",
                SupportedCapabilities = new[] { Nexo.Brick.Contracts.Capabilities.TaskCapabilityDto.CodeGeneration },
            },
        };

        var brick = new RemoteBrick(entry, client, "http://remote:7777");
        var output = await brick.ExecuteAsync(
            new BrickInput(),
            ImplementationType.Deterministic,
            CreateContext());

        output.Summary.Should().Be("remote ok");
        output.Get<string>("result").Should().Be("done");
        brick.Description.Should().Contain("host:node-1");
    }

    [Fact]
    public void Infrastructure_BehaviorResult_round_trips()
    {
        var result = new Nexo.Infrastructure.Execution.BehaviorResult
        {
            Success = false,
            Outputs = new Dictionary<string, object> { ["x"] = 1 },
            Errors = new[] { "failed" },
            Duration = TimeSpan.FromMilliseconds(250),
        };
        result.Success.Should().BeFalse();
        result.Outputs["x"].Should().Be(1);
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void RemoteCapabilitiesOptions_has_sensible_defaults()
    {
        var opts = new RemoteCapabilitiesOptions();
        opts.CapabilityTtl.Should().Be(TimeSpan.FromSeconds(30));
        opts.MaxStaleAge.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Theory]
    [InlineData("edge", LoadPreference.Edge)]
    [InlineData("server", LoadPreference.Server)]
    [InlineData("auto", LoadPreference.Auto)]
    [InlineData(null, LoadPreference.Auto)]
    public void PreferenceLoadPolicy_reads_env_preference(string? env, LoadPreference expected)
    {
        var key = "NEXO_LOAD_PREFERENCE";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, env);
            new PreferenceLoadPolicy().GetPreference().Should().Be(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }

    [Fact]
    public void PreferenceLoadPolicy_resolve_provider_prefers_edge_when_configured()
    {
        var factory = new Mock<IProviderFactory>();
        factory.Setup(f => f.IsProviderAvailable("ollama")).Returns(true);

        var key = "NEXO_LOAD_PREFERENCE";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "edge");
            new PreferenceLoadPolicy().ResolveProvider(factory.Object).Should().Be("ollama");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }

    [Fact]
    public void PreferenceLoadPolicy_resolve_provider_prefers_server_when_configured()
    {
        var factory = new Mock<IProviderFactory>();
        factory.Setup(f => f.IsProviderAvailable("openai")).Returns(true);

        var key = "NEXO_LOAD_PREFERENCE";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "server");
            new PreferenceLoadPolicy().ResolveProvider(factory.Object).Should().Be("openai");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }

    [Fact]
    public void PreferenceLoadPolicy_returns_null_when_no_providers_available()
    {
        var factory = new Mock<IProviderFactory>();
        factory.Setup(f => f.IsProviderAvailable(It.IsAny<string>())).Returns(false);

        new PreferenceLoadPolicy(NullLogger<PreferenceLoadPolicy>.Instance)
            .ResolveProvider(factory.Object)
            .Should()
            .BeNull();
    }

    [Fact]
    public void AdaptiveProviderFactoryExtensions_registers_load_policy()
    {
        var services = new ServiceCollection();
        services.AddLoadPolicy();
        services.BuildServiceProvider().GetService<ILoadPolicy>().Should().BeOfType<PreferenceLoadPolicy>();
    }

    [Fact]
    public async Task AdaptiveProviderFactory_executes_llm_and_vision_with_fallback()
    {
        var inner = new Mock<IProviderFactory>();
        inner.Setup(f => f.IsProviderAvailable(It.IsAny<string>())).Returns(true);
        inner.Setup(f => f.ExecuteLLMAsync("ollama", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ollama down"));
        inner.Setup(f => f.ExecuteLLMAsync("openai", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cloud-result");

        var policy = new Mock<ILoadPolicy>();
        policy.Setup(p => p.ResolveProvider(inner.Object)).Returns("ollama");

        var factory = new AdaptiveProviderFactory(inner.Object, policy.Object, NullLogger<AdaptiveProviderFactory>.Instance);
        (await factory.ExecuteLLMAsync("ignored", "sys", "user", new object())).Should().Be("cloud-result");

        inner.Setup(f => f.ExecuteVisionAsync("ollama", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("vision-ok");
        (await factory.ExecuteVisionAsync("ignored", "sys", "user", [1], new object())).Should().Be("vision-ok");

        inner.Setup(f => f.ExecuteVisionMultiFrameAsync("ollama", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("multi-frame");
        (await factory.ExecuteVisionMultiFrameAsync("ignored", "sys", "user", [[1], [2]], new object())).Should().Be("multi-frame");

        inner.Setup(f => f.ExecuteVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("video");
        (await factory.ExecuteVideoAsync("sys", "user", [[1]], new object())).Should().Be("video");

        inner.Setup(f => f.EnsureOllamaReachableAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await factory.EnsureOllamaReachableAsync(false);
        inner.Verify(f => f.EnsureOllamaReachableAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HttpRemoteBrickCatalog_returns_empty_on_failures()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("catalog down"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://remote:8080/") };
        var catalog = new HttpRemoteBrickCatalog(client, NullLogger<HttpRemoteBrickCatalog>.Instance);

        (await catalog.GetAllAsync()).Should().BeEmpty();
        (await catalog.GetByIdAsync("")).Should().BeNull();
        (await catalog.GetByIdAsync("missing")).Should().BeNull();
    }

    [Theory]
    [InlineData(ExportTarget.CSharp)]
    [InlineData(ExportTarget.Python)]
    [InlineData(ExportTarget.TypeScript)]
    [InlineData(ExportTarget.BehaviorTree)]
    [InlineData(ExportTarget.StateMachine)]
    public async Task CodeGenerator_generates_for_all_export_targets(ExportTarget target)
    {
        var generator = new CodeGenerator();
        var brick = SampleBrick();
        var workflow = SampleWorkflow();

        (await generator.GenerateDeterministicAsync(brick, target)).Should().NotBeNullOrWhiteSpace();
        (await generator.GenerateDeterministicWithDataAsync(brick, target)).Should().Contain("data files");
        (await generator.GenerateOrchestrationAsync(workflow, target)).Should().NotBeNullOrWhiteSpace();
        (await generator.GenerateRuntimeBootstrapAsync(workflow, target, includeFallbacks: true))
            .Should().Contain("WorkflowLoader");
    }

    [Fact]
    public async Task CodeGenerator_generate_static_data_serializes_variations()
    {
        var generator = new CodeGenerator();
        var file = await generator.GenerateStaticDataAsync(
            SampleBrick(),
            new GeneratedContent
            {
                Variations = new[]
                {
                    new GeneratedVariation { Content = "a", Metadata = new Dictionary<string, object> { ["i"] = 0 } },
                },
            },
            ExportTarget.CSharp);

        file.Path.Should().Contain("data/");
        file.Content.Should().Contain("a");
        file.Language.Should().Be("json");
    }

    [Fact]
    public async Task ContentGenerator_generates_variations_via_provider()
    {
        var factory = new Mock<IProviderFactory>();
        factory.Setup(f => f.ExecuteLLMAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("generated text");

        var generator = new ContentGenerator(factory.Object, NullLogger<ContentGenerator>.Instance);
        var content = await generator.GenerateAsync(
            SampleBrick(),
            new GenerationConfig { VariationsPerItem = 2, Provider = "ollama", Model = "phi3" });

        content.Variations.Should().HaveCount(2);
        content.Variations[0].Content.Should().Be("generated text");
    }

    private static ClusterExecutor CreateExecutor(
        IClusterRegistry clusters,
        IBrickRegistry bricks,
        ISemanticCache? cache = null) =>
        new(
            clusters,
            bricks,
            cache ?? new InMemorySemanticCache(),
            NullLogger<ClusterExecutor>.Instance);

    /// <summary>Creates single brick cluster.</summary>
    private static Cluster BuildSingleBrickCluster() => new()
    {
        Id = "cluster-1",
        Name = "Echo Cluster",
        Description = "test cluster",
        Bricks = new[]
        {
            new ClusterBrick
            {
                LocalId = "b1",
                BrickId = "echo-brick",
                DefaultImplementation = ImplementationType.Deterministic,
            },
        },
        Interface = new ClusterInterface
        {
            FailurePolicy = FailurePolicy.Abort,
            Outputs = new[]
            {
                new ClusterPort
                {
                    Name = "out",
                    Type = "string",
                    InternalMapping = "b1.result",
                },
            },
        },
    };

    /// <summary>Sample workflow.</summary>
    private static Workflow SampleWorkflow() => new()
    {
        Id = "wf-1",
        Name = "Sample Workflow",
        Description = "desc",
        Instances = Array.Empty<ClusterInstance>(),
    };

    /// <summary>Sample brick.</summary>
    private static EchoBrick SampleBrick() => new("sample-brick") { Name = "Sample DomainBrick" };

    /// <summary>Creates context.</summary>
    private static IExecutionContext CreateContext() => new ExecutionContext
    {
        AgentId = "agent",
        BehaviorId = "behavior",
        IsAirGapped = false,
        AuditMode = false,
        Provider = "mock",
        Variables = new Dictionary<string, object>(),
    };

    private static async Task<List<ExecutionEvent>> CollectEvents(
        IAsyncEnumerable<ExecutionEvent> stream)
    {
        var events = new List<ExecutionEvent>();
        await foreach (var evt in stream)
            events.Add(evt);
        return events;
    }

    /// <summary>Tests for in memory cluster registry.</summary>
    private sealed class InMemoryClusterRegistry : IClusterRegistry
    {
        private readonly Dictionary<string, Cluster> _clusters = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets the value.</summary>
        /// <param name="id">Id.</param>
        public Cluster? Get(string id) => _clusters.GetValueOrDefault(id);

        /// <summary>Gets all.</summary>
        public IReadOnlyList<Cluster> GetAll() => _clusters.Values.ToList();

        /// <summary>Register.</summary>
        /// <param name="cluster">Cluster.</param>
        public void Register(Cluster cluster) => _clusters[cluster.Id] = cluster;

        /// <summary>Unregister.</summary>
        /// <param name="id">Id.</param>
        public void Unregister(string id) => _clusters.Remove(id);
    }

    /// <summary>Tests for stub brick registry.</summary>
    private sealed class StubBrickRegistry : IBrickRegistry
    {
        private readonly Dictionary<string, DomainBrick> _bricks = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Add.</summary>
        /// <param name="brick">Brick.</param>
        public void Add(DomainBrick brick) => _bricks[brick.Id] = brick;

        /// <summary>Gets brick.</summary>
        /// <param name="id">Id.</param>
        public DomainBrick? GetBrick(string id) => _bricks.GetValueOrDefault(id);

        /// <summary>Gets all bricks.</summary>
        public IReadOnlyList<DomainBrick> GetAllBricks() => _bricks.Values.ToList();
    }

    /// <summary>Tests for in memory semantic cache.</summary>
    private sealed class InMemorySemanticCache : ISemanticCache
    {
        private readonly Dictionary<string, BrickOutput> _cache = new();

        /// <summary>Gets async.</summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="default">Default.</param>
        public Task<BrickOutput?> GetAsync(string cacheKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_cache.GetValueOrDefault(cacheKey));

        public Task SetAsync(string cacheKey, BrickOutput output, CancellationToken cancellationToken = default)
        {
            _cache[cacheKey] = output;
            return Task.CompletedTask;
        }
    }

    /// <summary>Tests for echo brick.</summary>
    private class EchoBrick : DomainBrick
    {
        public EchoBrick(string id)
        {
            Id = id;
            Name = id;
            Description = "echo";
            Category = BrickCategory.Control;
            Implementations = new BrickImplementations
            {
                Deterministic = new DeterministicImplementation
                {
                    Id = "det",
                    Name = "Deterministic",
                    Description = "det",
                    Executor = "local",
                },
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            var output = new BrickOutput { Summary = "ok" };
            output.Set("result", "done");
            return Task.FromResult(output);
        }
    }

    /// <summary>Tests for counting echo brick.</summary>
    private sealed class CountingEchoBrick : EchoBrick
    {
        /// <summary>Executions.</summary>
        public int Executions { get; private set; }

        public CountingEchoBrick(string id) : base(id) { }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            Executions++;
            return base.ExecuteAsync(input, implementation, context, cancellationToken);
        }
    }

    /// <summary>Tests for fake http message handler.</summary>
}
