using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Export;
using Xunit;
using OutputFormat = Nexo.Core.Domain.Export.OutputFormat;

namespace Nexo.Tests.Infrastructure.Tests.Export;

public sealed class WorkflowExporterGapCoverageTests
{
    [Fact]
    public async Task ExportAsync_throws_for_unknown_mode()
    {
        var exporter = CreateExporter();

        var act = () => exporter.ExportAsync(
            SampleWorkflow("cluster-1"),
            new ExportConfig { Mode = (ExportMode)999, Target = ExportTarget.CSharp });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Unknown export mode*");
    }

    [Fact]
    public async Task ExportAsync_skips_missing_cluster_and_brick_references()
    {
        var exporter = CreateExporter();

        var workflow = new Workflow
        {
            Id = "wf-missing",
            Name = "Missing refs",
            Description = "coverage",
            Instances =
            [
                new ClusterInstance { InstanceId = "missing-cluster", ClusterId = "does-not-exist" },
                new ClusterInstance { InstanceId = "missing-brick", ClusterId = "cluster-empty" },
            ],
        };

        var result = await exporter.ExportAsync(workflow, new ExportConfig
        {
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
        });

        result.Success.Should().BeTrue();
        result.Files.Should().ContainSingle(f => f.Path == "Orchestration.cs");
    }

    [Fact]
    public async Task ExportAsync_warns_when_brick_has_no_deterministic_implementation()
    {
        var ctx = new ExportContext();
        ctx.RegisterCluster("cluster-stub", "stub-only", hasDeterministic: false);
        var exporter = CreateExporter(ctx);

        var result = await exporter.ExportAsync(
            SampleWorkflow("cluster-stub"),
            new ExportConfig { Mode = ExportMode.PureDeterministic, Target = ExportTarget.CSharp });

        result.Messages.Should().Contain(m => m.Contains("no deterministic implementation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExportAsync_filters_generation_to_requested_brick_ids()
    {
        var contentGen = new Mock<IContentGenerator>();
        contentGen.Setup(c => c.GenerateAsync(It.IsAny<Brick>(), It.IsAny<GenerationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedContent
            {
                Variations = [new GeneratedVariation { Content = "v1" }],
            });

        var ctx = new ExportContext();
        ctx.Bricks.Add(new ExportTestBrick("brick-a", hasDeterministic: true, hasAgentic: true));
        ctx.Bricks.Add(new ExportTestBrick("brick-b", hasDeterministic: true, hasAgentic: true));
        ctx.Clusters.Register(new Cluster
        {
            Id = "cluster-multi",
            Name = "multi",
            Description = "test",
            Bricks =
            [
                new ClusterBrick { LocalId = "a", BrickId = "brick-a" },
                new ClusterBrick { LocalId = "b", BrickId = "brick-b" },
            ],
        });

        var exporter = CreateExporter(ctx, contentGen.Object);

        var result = await exporter.ExportAsync(
            SampleWorkflow("cluster-multi"),
            new ExportConfig
            {
                Mode = ExportMode.AIGeneratedThenDeterministic,
                Target = ExportTarget.CSharp,
                GenerationConfig = new GenerationConfig { VariationsPerItem = 1, RequireReview = false },
                GenerationBrickIds = ["brick-a"],
            });

        result.GenerationSummary!.ItemsGenerated.Should().Be(1);
        contentGen.Verify(
            c => c.GenerateAsync(It.Is<Brick>(b => b.Id == "brick-a"), It.IsAny<GenerationConfig>(), It.IsAny<CancellationToken>()),
            Times.Once);
        contentGen.Verify(
            c => c.GenerateAsync(It.Is<Brick>(b => b.Id == "brick-b"), It.IsAny<GenerationConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExportAsync_typescript_project_mode_includes_package_json()
    {
        var exporter = CreateExporter();

        var result = await exporter.ExportAsync(
            SampleWorkflow("cluster-1"),
            new ExportConfig
            {
                Mode = ExportMode.PureDeterministic,
                Target = ExportTarget.TypeScript,
                Output = new OutputConfig { Format = OutputFormat.Project, Namespace = "Generated.Game" },
            });

        result.Files.Should().Contain(f => f.Path == "package.json" && f.Content.Contains("generated.game"));
        result.Files.Should().Contain(f => f.Path.EndsWith(".ts", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportAsync_runtime_mode_omits_ollama_when_not_configured()
    {
        var ctx = new ExportContext();
        ctx.RegisterCluster("cluster-runtime", "runtime-b1", hasDeterministic: true, hasAgentic: true, includeOllama: false);
        var exporter = CreateExporter(ctx);

        var result = await exporter.ExportAsync(
            SampleWorkflow("cluster-runtime"),
            new ExportConfig { Mode = ExportMode.WithRuntimeAI, Target = ExportTarget.CSharp });

        result.RuntimeRequirements.Should().Contain("Nexo.Provider.openai");
        result.RuntimeRequirements.Should().NotContain(r => r.Contains("Ollama", StringComparison.Ordinal));
        result.Files.Should().Contain(f => f.Path == "workflow.json");
        result.Files.Should().Contain(f => f.Path.StartsWith("bricks/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportAsync_runtime_bootstrap_reflects_include_fallbacks_flag()
    {
        var exporter = CreateExporter();

        var withFallbacks = await exporter.ExportAsync(
            SampleWorkflow("cluster-1"),
            new ExportConfig { Mode = ExportMode.WithRuntimeAI, Target = ExportTarget.CSharp, IncludeFallbacks = true });
        var withoutFallbacks = await exporter.ExportAsync(
            SampleWorkflow("cluster-1"),
            new ExportConfig { Mode = ExportMode.WithRuntimeAI, Target = ExportTarget.CSharp, IncludeFallbacks = false });

        withFallbacks.Files.Single(f => f.Path == "Program.cs").Content.Should().Contain("Include Fallbacks: True");
        withoutFallbacks.Files.Single(f => f.Path == "Program.cs").Content.Should().Contain("Include Fallbacks: False");
    }

    private static WorkflowExporter CreateExporter(ExportContext? ctx = null, IContentGenerator? contentGenerator = null)
    {
        ctx ??= CreateDefaultContext();
        return new WorkflowExporter(
            ctx.Clusters,
            ctx.Bricks,
            new CodeGenerator(),
            contentGenerator ?? Mock.Of<IContentGenerator>(),
            NullLogger<WorkflowExporter>.Instance);
    }

    private static ExportContext CreateDefaultContext()
    {
        var ctx = new ExportContext();
        ctx.RegisterCluster("cluster-1", "brick-a", hasDeterministic: true);
        ctx.Clusters.Register(new Cluster
        {
            Id = "cluster-empty",
            Name = "empty",
            Description = "missing brick ref",
            Bricks = [new ClusterBrick { LocalId = "b1", BrickId = "not-registered" }],
        });
        return ctx;
    }

    private static Workflow SampleWorkflow(string clusterId)
        => new()
        {
            Id = "wf-export-gap",
            Name = "Export Gap",
            Description = "coverage",
            Instances = [new ClusterInstance { InstanceId = "inst-1", ClusterId = clusterId }],
        };

    private sealed class ExportContext
    {
        public InMemoryClusterRegistry Clusters { get; } = new();
        public StubBrickRegistry Bricks { get; } = new();

        public void RegisterCluster(
            string clusterId,
            string brickId,
            bool hasDeterministic,
            bool hasAgentic = false,
            bool includeOllama = false)
        {
            var mappings = new Dictionary<string, ProviderConfig>
            {
                ["openai"] = new ProviderConfig("gpt-4"),
            };
            if (includeOllama)
                mappings["ollama"] = new ProviderConfig("llama3");

            Bricks.Add(new ExportTestBrick(brickId, hasDeterministic, hasAgentic, mappings));
            Clusters.Register(new Cluster
            {
                Id = clusterId,
                Name = clusterId,
                Description = "test",
                Bricks = [new ClusterBrick { LocalId = "b1", BrickId = brickId }],
            });
        }
    }

    private sealed class InMemoryClusterRegistry : IClusterRegistry
    {
        private readonly Dictionary<string, Cluster> _clusters = new(StringComparer.OrdinalIgnoreCase);

        public Cluster? Get(string id) => _clusters.GetValueOrDefault(id);

        public IReadOnlyList<Cluster> GetAll() => _clusters.Values.ToList();

        public void Register(Cluster cluster) => _clusters[cluster.Id] = cluster;

        public void Unregister(string id) => _clusters.Remove(id);
    }

    private sealed class StubBrickRegistry : IBrickRegistry
    {
        private readonly Dictionary<string, Brick> _bricks = new(StringComparer.OrdinalIgnoreCase);

        public void Add(Brick brick) => _bricks[brick.Id] = brick;

        public Brick? GetBrick(string id) => _bricks.GetValueOrDefault(id);

        public IReadOnlyList<Brick> GetAllBricks() => _bricks.Values.ToList();
    }

    private sealed class ExportTestBrick : Brick
    {
        public ExportTestBrick(
            string id,
            bool hasDeterministic,
            bool hasAgentic,
            IReadOnlyDictionary<string, ProviderConfig>? providerMappings = null)
        {
            Id = id;
            Name = id;
            Description = "export gap test";
            Category = BrickCategory.Generation;
            Implementations = new BrickImplementations
            {
                Deterministic = hasDeterministic
                    ? new DeterministicImplementation
                    {
                        Id = "det",
                        Name = "Det",
                        Description = "det",
                        Executor = "local",
                    }
                    : null,
                Agentic = hasAgentic
                    ? new AgenticImplementation
                    {
                        Id = "ag",
                        Name = "Ag",
                        Description = "ag",
                        LLMConfig = new LLMConfig { SystemPrompt = "test" },
                        ProviderMappings = providerMappings
                            ?? new Dictionary<string, ProviderConfig> { ["openai"] = new("gpt-4") },
                    }
                    : null,
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }
}
