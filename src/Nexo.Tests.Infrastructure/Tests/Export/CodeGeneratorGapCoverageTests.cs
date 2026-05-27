using FluentAssertions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Infrastructure.Export;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Export;

public sealed class CodeGeneratorGapCoverageTests
{
    private readonly CodeGenerator _generator = new();

    [Theory]
    [InlineData(ExportTarget.CSharp, "namespace Generated")]
    [InlineData(ExportTarget.Python, "def execute")]
    [InlineData(ExportTarget.TypeScript, "export function execute")]
    [InlineData(ExportTarget.BehaviorTree, "\"type\": \"sequence\"")]
    [InlineData(ExportTarget.StateMachine, "\"states\"")]
    public async Task GenerateDeterministicAsync_supports_all_export_targets(ExportTarget target, string expectedSnippet)
    {
        var code = await _generator.GenerateDeterministicAsync(SampleBrick(), target);

        code.Should().Contain(expectedSnippet);
    }

    [Fact]
    public async Task GenerateDeterministicAsync_uses_generic_stub_for_unknown_target()
    {
        var code = await _generator.GenerateDeterministicAsync(SampleBrick(), (ExportTarget)999);

        code.Should().Contain("Generated stub");
        code.Should().Contain("Target: 999");
    }

    [Fact]
    public async Task GenerateDeterministicWithDataAsync_appends_data_comment()
    {
        var code = await _generator.GenerateDeterministicWithDataAsync(SampleBrick(), ExportTarget.CSharp);

        code.Should().Contain("Uses generated data from data files");
    }

    [Theory]
    [InlineData(ExportTarget.CSharp, "class Orchestrator")]
    [InlineData(ExportTarget.Python, "async def execute")]
    [InlineData(ExportTarget.TypeScript, "export async function execute")]
    [InlineData(ExportTarget.BehaviorTree, "\"type\": \"sequence\"")]
    [InlineData(ExportTarget.StateMachine, "\"states\"")]
    public async Task GenerateOrchestrationAsync_supports_all_export_targets(ExportTarget target, string expectedSnippet)
    {
        var code = await _generator.GenerateOrchestrationAsync(SampleWorkflow(), target);

        code.Should().Contain(expectedSnippet);
        if (target is ExportTarget.CSharp or ExportTarget.Python or ExportTarget.TypeScript)
            code.Should().Contain("2 instances");
    }

    [Fact]
    public async Task GenerateOrchestrationAsync_uses_generic_stub_for_unknown_target()
    {
        var code = await _generator.GenerateOrchestrationAsync(SampleWorkflow(), (ExportTarget)999);

        code.Should().Contain("Orchestration stub");
        code.Should().Contain("Target: 999");
    }

    [Fact]
    public async Task GenerateRuntimeBootstrapAsync_includes_workflow_name_and_fallback_flag()
    {
        var code = await _generator.GenerateRuntimeBootstrapAsync(SampleWorkflow(), ExportTarget.CSharp, includeFallbacks: true);

        code.Should().Contain("Export Workflow");
        code.Should().Contain("Include Fallbacks: True");
        code.Should().Contain("WorkflowExecutor");
    }

    [Fact]
    public async Task GenerateStaticDataAsync_serializes_variations_to_json_file()
    {
        var content = new GeneratedContent
        {
            Variations =
            [
                new GeneratedVariation { Content = "alpha" },
                new GeneratedVariation { Content = "beta" },
            ],
        };

        var file = await _generator.GenerateStaticDataAsync(SampleBrick(), content, ExportTarget.CSharp);

        file.Path.Should().Be("data/test-brick.json");
        file.Language.Should().Be("json");
        file.Content.Should().Contain("alpha").And.Contain("beta");
    }

    private static TestBrick SampleBrick()
        => new("test-brick", "Test Brick");

    private static Workflow SampleWorkflow()
        => new()
        {
            Id = "wf-codegen",
            Name = "Export Workflow",
            Description = "coverage",
            Instances =
            [
                new ClusterInstance { InstanceId = "i1", ClusterId = "c1" },
                new ClusterInstance { InstanceId = "i2", ClusterId = "c1" },
            ],
        };

    private sealed class TestBrick : Brick
    {
        public TestBrick(string id, string name)
        {
            Id = id;
            Name = name;
            Description = "codegen test";
            Category = BrickCategory.Generation;
            DomainKnowledge = new DomainKnowledge { Standards = ["ISO-9001"] };
            Implementations = new BrickImplementations();
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }
}
