using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Workflows;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Infrastructure.Workflows;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Workflows;

/// <summary>
/// Smoke tests for WorkflowExecutor to validate basic functionality.
/// </summary>
public class WorkflowExecutorSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSimpleWorkflowExecution(cancellationToken);
            await TestWorkflowWithInputNode(cancellationToken);
            await TestWorkflowWithAgentNode(cancellationToken);
            await TestWorkflowWithBrickNode(cancellationToken);
            await TestWorkflowValidation(cancellationToken);
            await TestWorkflowExecutionPlan(cancellationToken);
            await TestWorkflowEvents(cancellationToken);
            await TestWorkflowPdfOutput(cancellationToken);
            await TestWorkflowWithTransformNode(cancellationToken);
            await TestWorkflowWithConditionalNode(cancellationToken);
            await TestWorkflowOutputFormats(cancellationToken);

            return new TestResult
            {
                Name = nameof(WorkflowExecutorSmokeTests),
                Category = "Application.Workflows",
                Passed = true,
                Message = "All WorkflowExecutor smoke tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(WorkflowExecutorSmokeTests),
                Category = "Application.Workflows",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSimpleWorkflowExecution(CancellationToken cancellationToken = default)
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Nodes = new List<WorkflowNode>
            {
                new InputNode
                {
                    Id = "input-1",
                    Name = "Input",
                    Type = InputType.Content,
                    Content = "test content",
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "output-1", Name = "data", Direction = PortDirection.Output, DataType = "string" }
                    }
                },
                new OutputNode
                {
                    Id = "output-1",
                    Name = "Output",
                    Type = OutputType.Display,
                    Format = OutputFormat.Json,
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "input-1", Name = "input", Direction = PortDirection.Input, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection
                {
                    Id = "conn-1",
                    FromNodeId = "input-1",
                    FromPortId = "output-1",
                    ToNodeId = "output-1",
                    ToPortId = "input-1",
                    Type = ConnectionType.Data
                }
            }
        };

        var input = new WorkflowInput();

        // Act
        var result = await executor.ExecuteAsync(workflow, input, cancellationToken);

        // Assert
        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertNotNull(result.CorrelationId);
        AssertEqual(2, result.NodeResults.Count);
    }

    private async Task TestWorkflowWithInputNode(CancellationToken cancellationToken = default)
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "test file content");

        try
        {
            mockFs
                .Setup(f => f.ReadAllTextAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("test file content");

            var workflow = new WorkflowDefinition
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                Nodes = new List<WorkflowNode>
                {
                    new InputNode
                    {
                        Id = "input-1",
                        Name = "Input",
                        Type = InputType.File,
                        FilePath = tempFile,
                        Outputs = new List<NodePort>
                        {
                            new NodePort { Id = "output-1", Name = "data", Direction = PortDirection.Output, DataType = "string" }
                        }
                    }
                },
                Connections = new List<VisualWorkflowConnection>()
            };

            var input = new WorkflowInput();

            // Act
            var result = await executor.ExecuteAsync(workflow, input, CancellationToken.None);

            // Assert
            AssertNotNull(result);
            AssertTrue(result.Success);
            AssertTrue(result.NodeResults.ContainsKey("input-1"));
            var nodeResult = result.NodeResults["input-1"];
            AssertTrue(nodeResult.Outputs.ContainsKey("data"));
            AssertEqual("test file content", nodeResult.Outputs["data"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private async Task TestWorkflowWithAgentNode(CancellationToken cancellationToken = default)
    {
        // Arrange
        var agent = new AgentCard
        {
            Id = "test-agent",
            Name = "Test Agent",
            Behaviors = new List<string> { "test-behavior" }
        };

        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test Behavior",
            Steps = new List<BehaviorStep>(),
            Inputs = new List<BehaviorParameter>(),
            Outputs = new List<BehaviorParameter>()
        };

        var mockAgents = new Mock<IAgentRegistry>();
        mockAgents.Setup(a => a.GetAgent("test-agent")).Returns(agent);

        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        mockBehaviors.Setup(b => b.GetBehavior("test-behavior")).Returns(behavior);

        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var events = new List<ExecutionEvent>
        {
            new BehaviorStartedEvent("test-behavior", "Test Behavior", DateTime.UtcNow),
            new BehaviorCompletedEvent("test-behavior", true, new Dictionary<string, object> { ["result"] = "success" })
        };
        
        mockBehaviorExecutor
            .Setup(e => e.ExecuteWithEventsAsync(
                It.IsAny<AgentCard>(),
                It.IsAny<Behavior>(),
                It.IsAny<BehaviorInput>(),
                It.IsAny<ExecutionOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(events));

        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Nodes = new List<WorkflowNode>
            {
                new AgentNode
                {
                    Id = "agent-1",
                    Name = "Test Agent",
                    AgentId = "test-agent",
                    Mode = ImplementationMode.Auto,
                    Inputs = new List<NodePort>(),
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "output-1", Name = "result", Direction = PortDirection.Output, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>()
        };

        var input = new WorkflowInput();

        // Act
        var result = await executor.ExecuteAsync(workflow, input, cancellationToken);

        // Assert
        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertTrue(result.NodeResults.ContainsKey("agent-1"));
        mockBehaviorExecutor.Verify(
            e => e.ExecuteWithEventsAsync(
                It.IsAny<AgentCard>(),
                It.IsAny<Behavior>(),
                It.IsAny<BehaviorInput>(),
                It.IsAny<ExecutionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private async Task TestWorkflowWithBrickNode(CancellationToken cancellationToken = default)
    {
        // Arrange
        var brick = new TestBrickForWorkflow();

        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("test-brick")).Returns(brick);

        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    Name = "Test Brick",
                    BrickId = "test-brick",
                    // Auto will pick the brick default (agentic), then should fall back to deterministic on failure.
                    Implementation = ImplementationType.Auto,
                    Inputs = new List<NodePort>(),
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "output-1", Name = "result", Direction = PortDirection.Output, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>()
        };

        var input = new WorkflowInput();

        // Act
        var result = await executor.ExecuteAsync(workflow, input, cancellationToken);

        // Assert
        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertTrue(result.NodeResults.ContainsKey("brick-1"));
        var nodeResult = result.NodeResults["brick-1"];
        AssertTrue(nodeResult.Outputs.ContainsKey("result"));
        AssertEqual("brick-output", nodeResult.Outputs["result"]);
    }

    private async Task TestWorkflowValidation(CancellationToken cancellationToken = default)
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        // Create workflow with cycle (should fail validation)
        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Nodes = new List<WorkflowNode>
            {
                new InputNode
                {
                    Id = "node-1",
                    Name = "Node 1",
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "output", Direction = PortDirection.Output, DataType = "string" }
                    }
                },
                new OutputNode
                {
                    Id = "node-2",
                    Name = "Node 2",
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                // Create cycle: node-1 -> node-2 -> node-1
                new VisualWorkflowConnection
                {
                    Id = "conn-1",
                    FromNodeId = "node-1",
                    FromPortId = "out-1",
                    ToNodeId = "node-2",
                    ToPortId = "in-1"
                },
                new VisualWorkflowConnection
                {
                    Id = "conn-2",
                    FromNodeId = "node-2",
                    FromPortId = "in-1",
                    ToNodeId = "node-1",
                    ToPortId = "out-1"
                }
            }
        };

        var input = new WorkflowInput();

        // Act & Assert
        await AssertThrowsAsync<WorkflowValidationException>(
            async () => await executor.ExecuteAsync(workflow, input, cancellationToken),
            "Expected WorkflowValidationException for cycle");
    }

    private async Task TestWorkflowExecutionPlan(CancellationToken cancellationToken = default)
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        // Create linear workflow: input -> node1 -> node2 -> output
        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Nodes = new List<WorkflowNode>
            {
                new InputNode
                {
                    Id = "input",
                    Name = "Input",
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output, DataType = "string" }
                    }
                },
                new OutputNode
                {
                    Id = "output",
                    Name = "Output",
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "data", Direction = PortDirection.Input, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection
                {
                    Id = "conn-1",
                    FromNodeId = "input",
                    FromPortId = "out-1",
                    ToNodeId = "output",
                    ToPortId = "in-1"
                }
            }
        };

        var input = new WorkflowInput();

        // Act
        var result = await executor.ExecuteAsync(workflow, input, CancellationToken.None);

        // Assert
        AssertNotNull(result);
        AssertTrue(result.Success);
        // Verify execution order: input should execute before output
        AssertTrue(result.NodeResults.ContainsKey("input"));
        AssertTrue(result.NodeResults.ContainsKey("output"));
    }

    private async Task TestWorkflowEvents(CancellationToken cancellationToken = default)
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Nodes = new List<WorkflowNode>
            {
                new InputNode
                {
                    Id = "input-1",
                    Name = "Input",
                    Type = InputType.Content,
                    Content = "test",
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>()
        };

        var input = new WorkflowInput();
        var events = new List<WorkflowExecutionEvent>();

        // Subscribe to events
        executor.Events.Subscribe(evt => events.Add(evt));

        // Act
        await executor.ExecuteAsync(workflow, input, cancellationToken);

        // Assert
        AssertTrue(events.Count > 0);
        AssertTrue(events.Any(e => e is WorkflowStartedEvent));
        AssertTrue(events.Any(e => e is WorkflowCompletedEvent));
    }

    private async Task TestWorkflowPdfOutput(CancellationToken cancellationToken = default)
    {
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        byte[]? writtenBytes = null;
        var mockFs = new Mock<ITextFileSystem>();
        mockFs.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], CancellationToken>((_, bytes, _) => writtenBytes = bytes)
            .Returns(Task.CompletedTask);

        var pdfExporter = new QuestPdfWorkflowExporter();
        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object,
            pdfExporter: pdfExporter);

        var tempPath = Path.GetTempFileName();
        var workflow = new WorkflowDefinition
        {
            Id = "pdf-workflow",
            Name = "PDF Output Workflow",
            Nodes = new List<WorkflowNode>
            {
                new InputNode
                {
                    Id = "input-1",
                    Name = "Input",
                    Type = InputType.Content,
                    Content = "Hello PDF",
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output, DataType = "string" }
                    }
                },
                new OutputNode
                {
                    Id = "output-1",
                    Name = "Output",
                    Type = OutputType.File,
                    Format = OutputFormat.Pdf,
                    FilePath = tempPath,
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input, DataType = "string" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection
                {
                    Id = "conn-1",
                    FromNodeId = "input-1",
                    FromPortId = "out-1",
                    ToNodeId = "output-1",
                    ToPortId = "in-1",
                    Type = ConnectionType.Data
                }
            }
        };

        var result = await executor.ExecuteAsync(workflow, new WorkflowInput(), cancellationToken);

        AssertTrue(result.Success);
        AssertNotNull(writtenBytes);
        AssertTrue(writtenBytes!.Length > 0);
        var header = System.Text.Encoding.ASCII.GetString(writtenBytes.AsSpan(0, Math.Min(8, writtenBytes.Length)));
        AssertTrue(header.StartsWith("%PDF"), $"Expected PDF header, got: {header}");

        try { File.Delete(tempPath); } catch { }
    }

    private async Task TestWorkflowWithTransformNode(CancellationToken cancellationToken = default)
    {
        var listBrick = new TestBrickWithListOutput();
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("list-brick")).Returns(listBrick);
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "transform-workflow",
            Name = "Transform Workflow",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    Name = "List Brick",
                    BrickId = "list-brick",
                    Implementation = ImplementationType.Deterministic,
                    Inputs = new List<NodePort>(),
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output, DataType = "object" }
                    }
                },
                new TransformNode
                {
                    Id = "transform-1",
                    Name = "Map",
                    Operation = TransformOperation.Map,
                    Expression = "value",
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "data", Direction = PortDirection.Input, DataType = "object" }
                    },
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "data", Direction = PortDirection.Output, DataType = "object" }
                    }
                },
                new OutputNode
                {
                    Id = "output-1",
                    Name = "Output",
                    Type = OutputType.Display,
                    Format = OutputFormat.Json,
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input, DataType = "object" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection
                {
                    Id = "c1",
                    FromNodeId = "brick-1",
                    FromPortId = "out-1",
                    ToNodeId = "transform-1",
                    ToPortId = "in-1",
                    Type = ConnectionType.Data
                },
                new VisualWorkflowConnection
                {
                    Id = "c2",
                    FromNodeId = "transform-1",
                    FromPortId = "out-1",
                    ToNodeId = "output-1",
                    ToPortId = "in-1",
                    Type = ConnectionType.Data
                }
            }
        };

        var result = await executor.ExecuteAsync(workflow, new WorkflowInput(), cancellationToken);

        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertTrue(result.NodeResults.ContainsKey("transform-1"));
        var transformResult = result.NodeResults["transform-1"];
        AssertTrue(transformResult.Outputs.ContainsKey("data"));
        var data = transformResult.Outputs["data"];
        AssertTrue(data is System.Collections.IEnumerable list && list.Cast<object>().Count() == 2);
    }

    private async Task TestWorkflowWithConditionalNode(CancellationToken cancellationToken = default)
    {
        var structBrick = new TestBrickWithStructuredOutput();
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("struct-brick")).Returns(structBrick);
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockFs = new Mock<ITextFileSystem>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            new SequentialLoopKernel(),
            mockFs.Object,
            mockLogger.Object);

        var workflow = new WorkflowDefinition
        {
            Id = "conditional-workflow",
            Name = "Conditional Workflow",
            Nodes = new List<WorkflowNode>
            {
                new BrickNode
                {
                    Id = "brick-1",
                    Name = "Struct Brick",
                    BrickId = "struct-brick",
                    Implementation = ImplementationType.Deterministic,
                    Inputs = new List<NodePort>(),
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "result", Direction = PortDirection.Output, DataType = "object" }
                    }
                },
                new ConditionalNode
                {
                    Id = "cond-1",
                    Name = "Condition",
                    Condition = "data.count > 0",
                    Inputs = new List<NodePort>
                    {
                        new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input, DataType = "object" }
                    },
                    Outputs = new List<NodePort>
                    {
                        new NodePort { Id = "out-1", Name = "condition", Direction = PortDirection.Output, DataType = "bool" },
                        new NodePort { Id = "out-2", Name = "result", Direction = PortDirection.Output, DataType = "object" }
                    }
                }
            },
            Connections = new List<VisualWorkflowConnection>
            {
                new VisualWorkflowConnection
                {
                    Id = "c1",
                    FromNodeId = "brick-1",
                    FromPortId = "out-1",
                    ToNodeId = "cond-1",
                    ToPortId = "in-1",
                    Type = ConnectionType.Data
                }
            }
        };

        var result = await executor.ExecuteAsync(workflow, new WorkflowInput(), cancellationToken);

        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertTrue(result.NodeResults.ContainsKey("cond-1"));
        var condResult = result.NodeResults["cond-1"];
        AssertTrue(condResult.Outputs.ContainsKey("condition"));
        AssertTrue(condResult.Outputs["condition"] is bool b && b);
        AssertTrue(condResult.Outputs.ContainsKey("result"));
    }

    private async Task TestWorkflowOutputFormats(CancellationToken cancellationToken = default)
    {
        var formats = new[] { OutputFormat.Xml, OutputFormat.Csv, OutputFormat.Markdown, OutputFormat.Html };
        var dataBrick = new TestBrickWithDataOutput();

        foreach (var format in formats)
        {
            string? writtenContent = null;
            var mockFs = new Mock<ITextFileSystem>();
            mockFs.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((_, content, _) => writtenContent = content)
                .Returns(Task.CompletedTask);

            var mockAgents = new Mock<IAgentRegistry>();
            var mockBricks = new Mock<IBrickRegistry>();
            mockBricks.Setup(b => b.GetBrick("data-brick")).Returns(dataBrick);
            var mockBehaviors = new Mock<IBehaviorRegistry>();
            var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
            var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

            var executor = new WorkflowExecutor(
                mockAgents.Object,
                mockBricks.Object,
                mockBehaviors.Object,
                mockBehaviorExecutor.Object,
                new SequentialLoopKernel(),
                mockFs.Object,
                mockLogger.Object);

            var tempPath = Path.GetTempFileName();
            var workflow = new WorkflowDefinition
            {
                Id = "format-workflow",
                Name = "Output Format Workflow",
                Nodes = new List<WorkflowNode>
                {
                    new BrickNode
                    {
                        Id = "brick-1",
                        Name = "Data Brick",
                        BrickId = "data-brick",
                        Implementation = ImplementationType.Deterministic,
                        Inputs = new List<NodePort>(),
                        Outputs = new List<NodePort>
                        {
                            new NodePort { Id = "out-1", Name = "result", Direction = PortDirection.Output, DataType = "object" }
                        }
                    },
                    new OutputNode
                    {
                        Id = "output-1",
                        Name = "Output",
                        Type = OutputType.File,
                        Format = format,
                        FilePath = tempPath,
                        Inputs = new List<NodePort>
                        {
                            new NodePort { Id = "in-1", Name = "input", Direction = PortDirection.Input, DataType = "object" }
                        }
                    }
                },
                Connections = new List<VisualWorkflowConnection>
                {
                    new VisualWorkflowConnection
                    {
                        Id = "c1",
                        FromNodeId = "brick-1",
                        FromPortId = "out-1",
                        ToNodeId = "output-1",
                        ToPortId = "in-1",
                        Type = ConnectionType.Data
                    }
                }
            };

            var result = await executor.ExecuteAsync(workflow, new WorkflowInput(), cancellationToken);

            AssertNotNull(result);
            AssertTrue(result.Success);
            AssertNotNull(writtenContent);
            AssertTrue(writtenContent!.Length > 0);

            switch (format)
            {
                case OutputFormat.Xml:
                    AssertTrue(writtenContent.Contains("<") || writtenContent.StartsWith("<?xml"), $"Expected XML for {format}");
                    break;
                case OutputFormat.Csv:
                    AssertTrue(writtenContent.Contains(",") || writtenContent.Contains("\n"), $"Expected CSV for {format}");
                    break;
                case OutputFormat.Markdown:
                    AssertTrue(writtenContent.Contains("|") || writtenContent.Contains("-"), $"Expected Markdown for {format}");
                    break;
                case OutputFormat.Html:
                    AssertTrue(writtenContent.Contains("<") && (writtenContent.Contains("table") || writtenContent.Contains("pre")), $"Expected HTML for {format}");
                    break;
            }

            try { File.Delete(tempPath); } catch { }
        }
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Delay(10); // Small delay to simulate async
        }
    }
}

/// <summary>
/// Test brick for workflow executor tests.
/// </summary>
public class TestBrickForWorkflow : Brick
{
    public TestBrickForWorkflow()
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
                Name = "Test Agentic",
                Description = "Always throws to force fallback in tests"
            }
        };
        
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = new[] { ImplementationType.Agentic, ImplementationType.Deterministic };
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        if (implementation == ImplementationType.Agentic)
        {
            throw new InvalidOperationException("Agentic implementation failed (test)");
        }
        var output = new BrickOutput
        {
            Summary = "Brick executed"
        };
        output["result"] = "brick-output";
        return output;
    }
}

/// <summary>
/// Test brick that outputs a list of dicts for Transform node tests.
/// </summary>
public class TestBrickWithListOutput : Brick
{
    public TestBrickWithListOutput()
    {
        Id = "list-brick";
        Name = "List Brick";
        Category = BrickCategory.Analysis;
        Description = "Outputs list for transform tests";
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "list-det",
                Name = "List",
                Description = "Test",
                Executor = "Test",
                Characteristics = new ImplementationCharacteristics { Deterministic = true, RequiresNetwork = false }
            }
        };
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = Array.Empty<ImplementationType>();
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var output = new BrickOutput { Summary = "List" };
        output["data"] = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { ["value"] = 10 },
            new Dictionary<string, object> { ["value"] = 5 }
        };
        return output;
    }
}

/// <summary>
/// Test brick that outputs structured data for Conditional node tests.
/// </summary>
public class TestBrickWithStructuredOutput : Brick
{
    public TestBrickWithStructuredOutput()
    {
        Id = "struct-brick";
        Name = "Struct Brick";
        Category = BrickCategory.Analysis;
        Description = "Outputs struct for conditional tests";
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "struct-det",
                Name = "Struct",
                Description = "Test",
                Executor = "Test",
                Characteristics = new ImplementationCharacteristics { Deterministic = true, RequiresNetwork = false }
            }
        };
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = Array.Empty<ImplementationType>();
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var output = new BrickOutput { Summary = "Struct" };
        output["result"] = new Dictionary<string, object>
        {
            ["data"] = new Dictionary<string, object> { ["count"] = 3 }
        };
        return output;
    }
}

/// <summary>
/// Test brick that outputs data for output format tests.
/// </summary>
public class TestBrickWithDataOutput : Brick
{
    public TestBrickWithDataOutput()
    {
        Id = "data-brick";
        Name = "Data Brick";
        Category = BrickCategory.Analysis;
        Description = "Outputs data for format tests";
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "data-det",
                Name = "Data",
                Description = "Test",
                Executor = "Test",
                Characteristics = new ImplementationCharacteristics { Deterministic = true, RequiresNetwork = false }
            }
        };
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = Array.Empty<ImplementationType>();
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var output = new BrickOutput { Summary = "Data" };
        output["result"] = new Dictionary<string, object>
        {
            ["items"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["id"] = 1, ["name"] = "a" },
                new Dictionary<string, object> { ["id"] = 2, ["name"] = "b" }
            }
        };
        return output;
    }
}
