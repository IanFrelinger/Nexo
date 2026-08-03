using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Workflows;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Workflows;
using Nexo.Tests.Application.Tests.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

/// <summary>
/// xunit integration tests for WorkflowExecutor Transform, Conditional, and output formats.
/// Provides granular coverage with per-test timeout support via dotnet test.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class WorkflowExecutorIntegrationTests
{
    private static WorkflowExecutor CreateExecutor(IBrickRegistry brickRegistry)
    {
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();
        return new WorkflowExecutor(
            mockAgents.Object,
            brickRegistry,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);
    }

    [Fact(Timeout = 15000)]
    public async Task TransformNode_Map_ExtractsValues()
    {
        var brick = new TestBrickWithListOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateTransformWorkflow(TransformOperation.Map, "value");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        result.NodeResults.Should().ContainKey("transform-1");
        var data = result.NodeResults["transform-1"].Outputs["data"];
        var list = data.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject;
        list.Cast<object>().Should().HaveCount(2);
    }

    [Fact(Timeout = 15000)]
    public async Task TransformNode_Filter_ReturnsMatchingItems()
    {
        var brick = new TestBrickWithListOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateTransformWorkflow(TransformOperation.Filter, "value > 5");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        result.NodeResults.Should().ContainKey("transform-1");
        var data = result.NodeResults["transform-1"].Outputs["data"];
        var list = data.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject;
        list.Cast<object>().Should().HaveCount(1);
    }

    [Fact(Timeout = 15000)]
    public async Task TransformNode_Reduce_Count_ReturnsCount()
    {
        var brick = new TestBrickWithListOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateTransformWorkflow(TransformOperation.Reduce, "count");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        result.NodeResults.Should().ContainKey("transform-1");
        var data = result.NodeResults["transform-1"].Outputs["data"];
        data.Should().BeOfType<Dictionary<string, object>>();
        ((Dictionary<string, object>)data)["result"].Should().Be(2);
    }

    [Fact(Timeout = 15000)]
    public async Task TransformNode_Sort_OrdersByKey()
    {
        var brick = new TestBrickWithListOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateTransformWorkflow(TransformOperation.Sort, "value");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        result.NodeResults.Should().ContainKey("transform-1");
        var data = result.NodeResults["transform-1"].Outputs["data"];
        var list = data.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject;
        list.Cast<object>().Should().HaveCount(2);
    }

    [Fact(Timeout = 15000)]
    public async Task TransformNode_GroupBy_GroupsByKey()
    {
        var brick = new TestBrickWithListOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateTransformWorkflow(TransformOperation.GroupBy, "value");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        result.NodeResults.Should().ContainKey("transform-1");
        var data = result.NodeResults["transform-1"].Outputs["data"];
        data.Should().BeOfType<Dictionary<string, object>>();
        var dict = (Dictionary<string, object>)data;
        dict.Should().ContainKey("10").And.ContainKey("5");
    }

    [Fact(Timeout = 15000)]
    public async Task TransformNode_Merge_ConcatenatesInputs()
    {
        var listBrick = new TestBrickWithListOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(listBrick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "merge-workflow",
            Name = "Merge",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    BrickId = "list-brick",
                    Implementation = ImplementationType.Deterministic,
                    Outputs = new List<NodePort> { new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output } }
                },
                new BrickNode
                {
                    Id = "brick-2",
                    BrickId = "list-brick",
                    Implementation = ImplementationType.Deterministic,
                    Outputs = new List<NodePort> { new NodePort { Id = "out-2", Name = "data", Direction = PortDirection.Output } }
                },
                new TransformNode
                {
                    Id = "transform-1",
                    Operation = TransformOperation.Merge,
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "a", Direction = PortDirection.Input },
                        new NodePort { Id = "in-2", Name = "b", Direction = PortDirection.Input }
                    },
                    Outputs = new List<NodePort> { new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output } }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection { FromNodeId = "brick-1", FromPortId = "out-1", ToNodeId = "transform-1", ToPortId = "in-1" },
                new VisualWorkflowConnection { FromNodeId = "brick-2", FromPortId = "out-2", ToNodeId = "transform-1", ToPortId = "in-2" }
            }
        };

        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        var data = result.NodeResults["transform-1"].Outputs["data"];
        var list = data.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject;
        list.Cast<object>().Should().HaveCount(4);
    }

    [Fact(Timeout = 15000)]
    public async Task ConditionalNode_WhenTrue_OutputsConditionTrue()
    {
        var brick = new TestBrickWithStructuredOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("struct-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateConditionalWorkflow("data.count > 0");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        var condResult = result.NodeResults["cond-1"];
        condResult.Outputs["condition"].Should().Be(true);
    }

    [Fact(Timeout = 15000)]
    public async Task ConditionalNode_WhenFalse_OutputsConditionFalse()
    {
        var brick = new TestBrickWithZeroCountOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("zero-brick")).Returns(brick);
        var executor = CreateExecutor(mockBricks.Object);

        var workflow = CreateConditionalWorkflowForBrick("zero-brick", "data.count > 0");
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        var condResult = result.NodeResults["cond-1"];
        condResult.Outputs["condition"].Should().Be(false);
    }

    [Fact(Timeout = 15000)]
    public async Task SerializeOutput_Xml_ProducesValidXml()
    {
        string? written = null;
        var mockFs = new Mock<ITextFileSystem>();
        mockFs.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, c, _) => written = c)
            .Returns(Task.CompletedTask);

        var brick = new TestBrickWithDataOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("data-brick")).Returns(brick);
        var executor = CreateExecutorWithFs(mockBricks.Object, mockFs.Object);

        var workflow = CreateOutputFormatWorkflow(WorkflowOutputFormat.Xml);
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        written.Should().NotBeNullOrEmpty();
        written.Should().Contain("<");
    }

    [Fact(Timeout = 15000)]
    public async Task SerializeOutput_Csv_ProducesValidCsv()
    {
        string? written = null;
        var mockFs = new Mock<ITextFileSystem>();
        mockFs.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, c, _) => written = c)
            .Returns(Task.CompletedTask);

        var brick = new TestBrickWithDataOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("data-brick")).Returns(brick);
        var executor = CreateExecutorWithFs(mockBricks.Object, mockFs.Object);

        var workflow = CreateOutputFormatWorkflow(WorkflowOutputFormat.Csv);
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        written.Should().NotBeNullOrEmpty();
        (written!.Contains(",") || written.Contains("\n")).Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task SerializeOutput_Markdown_ProducesTableOrList()
    {
        string? written = null;
        var mockFs = new Mock<ITextFileSystem>();
        mockFs.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, c, _) => written = c)
            .Returns(Task.CompletedTask);

        var brick = new TestBrickWithDataOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("data-brick")).Returns(brick);
        var executor = CreateExecutorWithFs(mockBricks.Object, mockFs.Object);

        var workflow = CreateOutputFormatWorkflow(WorkflowOutputFormat.Markdown);
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        written.Should().NotBeNullOrEmpty();
        (written!.Contains("|") || written.Contains("-")).Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task SerializeOutput_Html_ProducesValidHtml()
    {
        string? written = null;
        var mockFs = new Mock<ITextFileSystem>();
        mockFs.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, c, _) => written = c)
            .Returns(Task.CompletedTask);

        var brick = new TestBrickWithDataOutput();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("data-brick")).Returns(brick);
        var executor = CreateExecutorWithFs(mockBricks.Object, mockFs.Object);

        var workflow = CreateOutputFormatWorkflow(WorkflowOutputFormat.Html);
        var result = await executor.ExecuteAsync(workflow, new WorkflowInput());

        result.Success.Should().BeTrue();
        written.Should().NotBeNullOrEmpty();
        written.Should().Contain("<");
        (written!.Contains("table") || written.Contains("pre")).Should().BeTrue();
    }

    private static WorkflowDefinition CreateTransformWorkflow(TransformOperation op, string expression)
    {
        return new WorkflowDefinition
        {
            Id = "transform",
            Name = "Transform",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    BrickId = "list-brick",
                    Implementation = ImplementationType.Deterministic,
                    Outputs = new List<NodePort> { new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output } }
                },
                new TransformNode
                {
                    Id = "transform-1",
                    Operation = op,
                    Expression = expression,
                    Inputs = new List<NodePort> { new NodePort { Id = "in-1", Name = "data", Direction = PortDirection.Input } },
                    Outputs = new List<NodePort> { new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output } }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection { FromNodeId = "brick-1", FromPortId = "out-1", ToNodeId = "transform-1", ToPortId = "in-1" }
            }
        };
    }

    private static WorkflowDefinition CreateConditionalWorkflow(string condition)
    {
        return CreateConditionalWorkflowForBrick("struct-brick", condition);
    }

    private static WorkflowDefinition CreateConditionalWorkflowForBrick(string brickId, string condition)
    {
        return new WorkflowDefinition
        {
            Id = "conditional",
            Name = "Conditional",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    BrickId = brickId,
                    Implementation = ImplementationType.Deterministic,
                    Outputs = new List<NodePort> { new NodePort { Id = "out-1", Name = "result", Direction = PortDirection.Output } }
                },
                new ConditionalNode
                {
                    Id = "cond-1",
                    Condition = condition,
                    Inputs = new List<NodePort> { new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input } },
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "condition", Direction = PortDirection.Output },
                        new NodePort { Id = "out-2", Name = "result", Direction = PortDirection.Output }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection { FromNodeId = "brick-1", FromPortId = "out-1", ToNodeId = "cond-1", ToPortId = "in-1" }
            }
        };
    }

    private static WorkflowDefinition CreateOutputFormatWorkflow(WorkflowOutputFormat format)
    {
        var tempPath = Path.GetTempFileName();
        return new WorkflowDefinition
        {
            Id = "format",
            Name = "Format",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    BrickId = "data-brick",
                    Implementation = ImplementationType.Deterministic,
                    Outputs = new List<NodePort> { new NodePort { Id = "out-1", Name = "result", Direction = PortDirection.Output } }
                },
                new OutputNode
                {
                    Id = "output-1",
                    Type = OutputType.File,
                    Format = format,
                    FilePath = tempPath,
                    Inputs = new List<NodePort> { new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input } }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection { FromNodeId = "brick-1", FromPortId = "out-1", ToNodeId = "output-1", ToPortId = "in-1" }
            }
        };
    }

    private static WorkflowExecutor CreateExecutorWithFs(IBrickRegistry brickRegistry, ITextFileSystem fs)
    {
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();
        return new WorkflowExecutor(
            mockAgents.Object,
            brickRegistry,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            fs,
            mockLogger.Object);
    }
}
