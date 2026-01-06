using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Export;
using Workflow = Nexo.Core.Domain.Workflows.Workflow;

namespace Nexo.Tests.Infrastructure.Tests.Export;

/// <summary>
/// Smoke tests for WorkflowExporter.
/// </summary>
public class WorkflowExporterSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestPureDeterministicExport();
            await TestWithRuntimeAIExport();
            await TestAIGeneratedThenDeterministicExport();
            
            return new TestResult
            {
                TestName = nameof(WorkflowExporterSmokeTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All WorkflowExporter smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(WorkflowExporterSmokeTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(WorkflowExporterSmokeTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestPureDeterministicExport()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockCodeGenerator = new Mock<ICodeGenerator>();
        var mockContentGenerator = new Mock<IContentGenerator>();
        var mockLogger = new Mock<ILogger<WorkflowExporter>>();
        
        var testBrick = new TestBrick();
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test Cluster",
            Description = "Test",
            Bricks = [
                new ClusterBrick
                {
                    BrickId = "test-brick",
                    LocalId = "b1",
                    DefaultImplementation = ImplementationType.Deterministic
                }
            ],
            Interface = new ClusterInterface()
        };
        
        var workflow = new Workflow
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Description = "Test",
            Instances = [
                new ClusterInstance
                {
                    ClusterId = "test-cluster",
                    InstanceId = "inst-1"
                }
            ],
            Connections = []
        };
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockCodeGenerator.Setup(g => g.GenerateDeterministicAsync(
            It.IsAny<Brick>(), It.IsAny<ExportTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("// Generated code");
        mockCodeGenerator.Setup(g => g.GenerateOrchestrationAsync(
            It.IsAny<Workflow>(), It.IsAny<ExportTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("// Orchestration code");
        
        var exporter = new WorkflowExporter(
            mockClusterRegistry.Object,
            mockBrickRegistry.Object,
            mockCodeGenerator.Object,
            mockContentGenerator.Object,
            mockLogger.Object);
        
        var config = new ExportConfig
        {
            Mode = ExportMode.PureDeterministic,
            Target = ExportTarget.CSharp,
            Output = new OutputConfig { Format = OutputFormat.Files }
        };
        
        var result = await exporter.ExportAsync(workflow, config, ct: default);
        
        AssertTrue(result.Success);
        AssertEqual(ExportMode.PureDeterministic, result.Mode);
        AssertTrue(result.Files.Count > 0);
        AssertEqual(0, result.RuntimeRequirements.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestWithRuntimeAIExport()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockCodeGenerator = new Mock<ICodeGenerator>();
        var mockContentGenerator = new Mock<IContentGenerator>();
        var mockLogger = new Mock<ILogger<WorkflowExporter>>();
        
        var testBrick = new TestBrick();
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test",
            Description = "Test",
            Bricks = [
                new ClusterBrick
                {
                    BrickId = "test-brick",
                    LocalId = "b1",
                    DefaultImplementation = ImplementationType.Agentic
                }
            ],
            Interface = new ClusterInterface()
        };
        
        var workflow = new Workflow
        {
            Id = "test-workflow",
            Name = "Test",
            Description = "Test",
            Instances = [
                new ClusterInstance { ClusterId = "test-cluster", InstanceId = "inst-1" }
            ],
            Connections = []
        };
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockCodeGenerator.Setup(g => g.GenerateRuntimeBootstrapAsync(
            It.IsAny<Workflow>(), It.IsAny<ExportTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("// Bootstrap code");
        
        var exporter = new WorkflowExporter(
            mockClusterRegistry.Object,
            mockBrickRegistry.Object,
            mockCodeGenerator.Object,
            mockContentGenerator.Object,
            mockLogger.Object);
        
        var config = new ExportConfig
        {
            Mode = ExportMode.WithRuntimeAI,
            Target = ExportTarget.CSharp,
            IncludeFallbacks = true,
            Output = new OutputConfig { Format = OutputFormat.Files }
        };
        
        var result = await exporter.ExportAsync(workflow, config, ct: default);
        
        AssertTrue(result.Success);
        AssertEqual(ExportMode.WithRuntimeAI, result.Mode);
        AssertTrue(result.Files.Count > 0);
        AssertTrue(result.RuntimeRequirements.Count > 0);
        AssertTrue(result.RuntimeRequirements.Contains("Nexo.Runtime"));
        
        await Task.CompletedTask;
    }
    
    private async Task TestAIGeneratedThenDeterministicExport()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockCodeGenerator = new Mock<ICodeGenerator>();
        var mockContentGenerator = new Mock<IContentGenerator>();
        var mockLogger = new Mock<ILogger<WorkflowExporter>>();
        
        var testBrick = new TestBrick();
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test",
            Description = "Test",
            Bricks = [
                new ClusterBrick
                {
                    BrickId = "test-brick",
                    LocalId = "b1",
                    DefaultImplementation = ImplementationType.Agentic
                }
            ],
            Interface = new ClusterInterface()
        };
        
        var workflow = new Workflow
        {
            Id = "test-workflow",
            Name = "Test",
            Description = "Test",
            Instances = [
                new ClusterInstance { ClusterId = "test-cluster", InstanceId = "inst-1" }
            ],
            Connections = []
        };
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockContentGenerator.Setup(g => g.GenerateAsync(
            It.IsAny<Brick>(), It.IsAny<GenerationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedContent
            {
                Variations = [
                    new GeneratedVariation { Content = "Generated content", Metadata = new Dictionary<string, object>() }
                ]
            });
        mockCodeGenerator.Setup(g => g.GenerateStaticDataAsync(
            It.IsAny<Brick>(), It.IsAny<GeneratedContent>(), It.IsAny<ExportTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExportedFile { Path = "data.json", Content = "{}", Language = "json" });
        mockCodeGenerator.Setup(g => g.GenerateDeterministicWithDataAsync(
            It.IsAny<Brick>(), It.IsAny<ExportTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("// Code with data");
        
        var exporter = new WorkflowExporter(
            mockClusterRegistry.Object,
            mockBrickRegistry.Object,
            mockCodeGenerator.Object,
            mockContentGenerator.Object,
            mockLogger.Object);
        
        var config = new ExportConfig
        {
            Mode = ExportMode.AIGeneratedThenDeterministic,
            Target = ExportTarget.CSharp,
            GenerationBrickIds = ["test-brick"],
            GenerationConfig = new GenerationConfig
            {
                VariationsPerItem = 5,
                Provider = "mock",
                Model = "gpt-4",
                RequireReview = false
            },
            Output = new OutputConfig { Format = OutputFormat.Files }
        };
        
        var result = await exporter.ExportAsync(workflow, config, ct: default);
        
        AssertTrue(result.Success);
        AssertEqual(ExportMode.AIGeneratedThenDeterministic, result.Mode);
        AssertTrue(result.Files.Count > 0);
        AssertNotNull(result.GenerationSummary);
        AssertTrue(result.GenerationSummary!.ItemsGenerated > 0);
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test brick for export tests.
/// </summary>
public class TestBrick : Brick
{
    public TestBrick()
    {
        Id = "test-brick";
        Name = "Test Brick";
        Category = BrickCategory.Analysis;
        Description = "Test";
        
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "test-det",
                Name = "Test",
                Description = "Test",
                Executor = "Test",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "test-agentic",
                Name = "Test",
                Description = "Test",
                Executor = "LLMExecutor",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Test",
                    Temperature = 0.1,
                    MaxTokens = 2000
                },
                ProviderMappings = new Dictionary<string, ProviderConfig>
                {
                    ["openai"] = new("gpt-4"),
                    ["ollama"] = new("llama2")
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = false,
                    RequiresNetwork = true
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
    }
    
    public override async Task<Core.Domain.Execution.BrickOutput> ExecuteAsync(
        Core.Domain.Execution.BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new Core.Domain.Execution.BrickOutput());
    }
}

