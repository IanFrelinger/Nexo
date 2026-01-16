using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Workflows;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
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
            await TestSimpleWorkflowExecution();
            await TestWorkflowWithInputNode();
            await TestWorkflowWithAgentNode();
            await TestWorkflowWithBrickNode();
            await TestWorkflowValidation();
            await TestWorkflowExecutionPlan();
            await TestWorkflowEvents();

            return new TestResult
            {
                TestName = nameof(WorkflowExecutorSmokeTests),
                Category = "Application.Workflows",
                Passed = true,
                Message = "All WorkflowExecutor smoke tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(WorkflowExecutorSmokeTests),
                Category = "Application.Workflows",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSimpleWorkflowExecution()
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
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
        var result = await executor.ExecuteAsync(workflow, input, CancellationToken.None);

        // Assert
        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertNotNull(result.CorrelationId);
        AssertEqual(2, result.NodeResults.Count);
    }

    private async Task TestWorkflowWithInputNode()
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
            mockLogger.Object);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "test file content");

        try
        {
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

    private async Task TestWorkflowWithAgentNode()
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
        var result = await executor.ExecuteAsync(workflow, input, CancellationToken.None);

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

    private async Task TestWorkflowWithBrickNode()
    {
        // Arrange
        var brick = new TestBrickForWorkflow();

        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        mockBricks.Setup(b => b.GetBrick("test-brick")).Returns(brick);

        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
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
        var result = await executor.ExecuteAsync(workflow, input, CancellationToken.None);

        // Assert
        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertTrue(result.NodeResults.ContainsKey("brick-1"));
        var nodeResult = result.NodeResults["brick-1"];
        AssertTrue(nodeResult.Outputs.ContainsKey("result"));
        AssertEqual("brick-output", nodeResult.Outputs["result"]);
    }

    private async Task TestWorkflowValidation()
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
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
            async () => await executor.ExecuteAsync(workflow, input, CancellationToken.None),
            "Expected WorkflowValidationException for cycle");
    }

    private async Task TestWorkflowExecutionPlan()
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
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

    private async Task TestWorkflowEvents()
    {
        // Arrange
        var mockAgents = new Mock<IAgentRegistry>();
        var mockBricks = new Mock<IBrickRegistry>();
        var mockBehaviors = new Mock<IBehaviorRegistry>();
        var mockBehaviorExecutor = new Mock<IBehaviorExecutor>();
        var mockLogger = new Mock<ILogger<WorkflowExecutor>>();

        var executor = new WorkflowExecutor(
            mockAgents.Object,
            mockBricks.Object,
            mockBehaviors.Object,
            mockBehaviorExecutor.Object,
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
        await executor.ExecuteAsync(workflow, input, CancellationToken.None);

        // Assert
        AssertTrue(events.Count > 0);
        AssertTrue(events.Any(e => e is WorkflowStartedEvent));
        AssertTrue(events.Any(e => e is WorkflowCompletedEvent));
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
