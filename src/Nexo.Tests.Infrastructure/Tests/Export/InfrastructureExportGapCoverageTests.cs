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

public class InfrastructureExportGapCoverageTests
{
    [Fact]
    public async Task WorkflowExporter_pure_deterministic_mode_generates_files()
    {
        var (exporter, _) = CreateExporter();
        var workflow = SampleWorkflow("cluster-1", "brick-a");

        var result = await exporter.ExportAsync(workflow, new ExportConfig
        {
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
            Output = new OutputConfig { Format = OutputFormat.Project, Namespace = "Generated.Game" },
        });

        result.Success.Should().BeTrue();
        result.Mode.Should().Be(ExportMode.PureDeterministic);
        result.Files.Should().Contain(f => f.Path == "Orchestration.cs");
        result.Files.Should().Contain(f => f.Path == "Generated.csproj");
    }

    [Fact]
    public async Task WorkflowExporter_ai_then_deterministic_generates_data_and_code()
    {
        var contentGen = new Mock<IContentGenerator>();
        contentGen.Setup(c => c.GenerateAsync(It.IsAny<Brick>(), It.IsAny<GenerationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedContent
            {
                Variations = new[] { new GeneratedVariation { Content = "v1" } },
            });

        var (exporter, _) = CreateExporter(contentGen.Object);
        var workflow = SampleWorkflow("cluster-agentic", "agentic-b1");

        var result = await exporter.ExportAsync(workflow, new ExportConfig
        {
            Mode = ExportMode.AIGeneratedThenDeterministic,
            Target = ExportTarget.CSharp,
            GenerationConfig = new GenerationConfig { VariationsPerItem = 1, RequireReview = false },
        });

        result.Success.Should().BeTrue();
        result.GenerationSummary!.ItemsGenerated.Should().Be(1);
        result.Files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WorkflowExporter_runtime_mode_includes_providers_and_bootstrap()
    {
        var (exporter, _) = CreateExporter();
        var workflow = SampleWorkflow("cluster-runtime", "runtime-b1");

        var result = await exporter.ExportAsync(workflow, new ExportConfig
        {
            Mode = ExportMode.WithRuntimeAI,
            Target = ExportTarget.CSharp,
            IncludeFallbacks = true,
        });

        result.Success.Should().BeTrue();
        result.RuntimeRequirements.Should().Contain("Nexo.Runtime");
        result.RuntimeRequirements.Should().Contain("Nexo.Provider.openai");
        result.RuntimeRequirements.Should().Contain("Ollama (local installation)");
        result.Files.Should().Contain(f => f.Path == "workflow.json");
        result.Files.Should().Contain(f => f.Path == "Program.cs");
    }

    private static (WorkflowExporter Exporter, ExportTestContext Context) CreateExporter(IContentGenerator? contentGenerator = null)
    {
        var ctx = new ExportTestContext();
        ctx.RegisterCluster("cluster-1", "brick-a", hasDeterministic: true, hasAgentic: false);
        ctx.RegisterCluster("cluster-agentic", "agentic-b1", hasDeterministic: true, hasAgentic: true);
        ctx.RegisterCluster("cluster-runtime", "runtime-b1", hasDeterministic: true, hasAgentic: true, includeOllama: true);

        var codeGen = new CodeGenerator();
        var exporter = new WorkflowExporter(
            ctx.Clusters,
            ctx.Bricks,
            codeGen,
            contentGenerator ?? Mock.Of<IContentGenerator>(),
            NullLogger<WorkflowExporter>.Instance);

        return (exporter, ctx);
    }

    private static Workflow SampleWorkflow(string clusterId, string _) => new()
    {
        Id = "wf-export",
        Name = "Export Test",
        Description = "coverage",
        Instances = new[] { new ClusterInstance { InstanceId = "inst-1", ClusterId = clusterId } },
    };

    private sealed class ExportTestContext
    {
        public InMemoryClusterRegistry Clusters { get; } = new();
        public StubBrickRegistry Bricks { get; } = new();

        public void RegisterCluster(
            string clusterId,
            string brickId,
            bool hasDeterministic,
            bool hasAgentic,
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
                Bricks = new[] { new ClusterBrick { LocalId = "b1", BrickId = brickId } },
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
            IReadOnlyDictionary<string, ProviderConfig> providerMappings)
        {
            Id = id;
            Name = id;
            Description = "export test";
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
                        ProviderMappings = providerMappings,
                    }
                    : null,
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrickOutput());
    }
}
