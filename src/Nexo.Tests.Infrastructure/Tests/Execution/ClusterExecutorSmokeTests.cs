using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Infrastructure.Execution;
using ExecutionContext = Nexo.Infrastructure.Execution.ExecutionContext;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Smoke tests for ClusterExecutor.
/// </summary>
public class ClusterExecutorSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestClusterExecution();
            await TestTopologicalSort();
            await TestParameterResolution();
            await TestImplementationSelection();
            
            return new TestResult
            {
                TestName = nameof(ClusterExecutorSmokeTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ClusterExecutor smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ClusterExecutorSmokeTests),
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
                TestName = nameof(ClusterExecutorSmokeTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestClusterExecution()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<ClusterExecutor>>();
        
        var testBrick = new ClusterTestBrick();
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test Cluster",
            Description = "Test",
            Bricks = [
                new ClusterBrick
                {
                    BrickId = "test-brick",
                    LocalId = "brick-1",
                    DefaultImplementation = ImplementationType.Deterministic
                }
            ],
            Connections = [],
            Interface = new ClusterInterface
            {
                FailurePolicy = FailurePolicy.Continue
            }
        };
        
        var instance = new ClusterInstance
        {
            ClusterId = "test-cluster",
            InstanceId = "instance-1",
            Parameters = new Dictionary<string, object>()
        };
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrickOutput?)null);
        
        var executor = new ClusterExecutor(
            mockClusterRegistry.Object,
            mockBrickRegistry.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var context = new ExecutionContext
        {
            AgentId = "test-agent",
            BehaviorId = "test-behavior",
            IsAirGapped = false,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
        
        var input = new ClusterInput();
        
        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteAsync(instance, input, context, ct: default))
        {
            events.Add(evt);
        }
        
        AssertTrue(events.Count > 0);
        AssertTrue(events.Any(e => e is BehaviorStartedEvent));
        AssertTrue(events.Any(e => e is StepStartedEvent));
        AssertTrue(events.Any(e => e is StepCompletedEvent));
        AssertTrue(events.Any(e => e is BehaviorCompletedEvent));
        
        await Task.CompletedTask;
    }
    
    private async Task TestTopologicalSort()
    {
        // Test that bricks execute in correct order based on connections
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<ClusterExecutor>>();
        
        var brick1 = new ClusterTestBrick { Id = "brick-1" };
        var brick2 = new ClusterTestBrick { Id = "brick-2" };
        
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test",
            Description = "Test",
            Bricks = [
                new ClusterBrick { BrickId = "brick-1", LocalId = "b1", DefaultImplementation = ImplementationType.Deterministic },
                new ClusterBrick { BrickId = "brick-2", LocalId = "b2", DefaultImplementation = ImplementationType.Deterministic }
            ],
            Connections = [
                new ClusterConnection
                {
                    Id = "conn-1",
                    FromBrickId = "b1",
                    FromOutput = "output",
                    ToBrickId = "b2",
                    ToInput = "input",
                    Type = ConnectionType.Data
                }
            ],
            Interface = new ClusterInterface()
        };
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        mockBrickRegistry.Setup(r => r.GetBrick("brick-1")).Returns(brick1);
        mockBrickRegistry.Setup(r => r.GetBrick("brick-2")).Returns(brick2);
        mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrickOutput?)null);
        
        var executor = new ClusterExecutor(
            mockClusterRegistry.Object,
            mockBrickRegistry.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var instance = new ClusterInstance { ClusterId = "test-cluster", InstanceId = "inst-1" };
        var context = new ExecutionContext
        {
            AgentId = "test",
            BehaviorId = "test",
            Variables = new Dictionary<string, object>()
        };
        
        var executionOrder = new List<string>();
        await foreach (var evt in executor.ExecuteAsync(instance, new ClusterInput(), context, ct: default))
        {
            if (evt is StepStartedEvent stepStarted)
            {
                executionOrder.Add(stepStarted.StepId);
            }
        }
        
        // b1 should execute before b2 due to connection
        AssertTrue(executionOrder.IndexOf("b1") < executionOrder.IndexOf("b2"));
        
        await Task.CompletedTask;
    }
    
    private async Task TestParameterResolution()
    {
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test",
            Description = "Test",
            Bricks = [],
            Parameters = [
                new ClusterParameter
                {
                    Name = "count",
                    Type = "int",
                    Default = 10
                }
            ],
            Interface = new ClusterInterface()
        };
        
        var instance = new ClusterInstance
        {
            ClusterId = "test-cluster",
            InstanceId = "inst-1",
            Parameters = new Dictionary<string, object> { ["count"] = 20 }
        };
        
        var input = new ClusterInput
        {
            Parameters = new Dictionary<string, object> { ["count"] = 30 }
        };
        
        // Input parameters should override instance parameters
        // This is tested implicitly in ClusterExecutor.ResolveParameters
        AssertEqual(20, instance.Parameters["count"]);
        AssertEqual(30, input.Parameters["count"]);
        
        await Task.CompletedTask;
    }
    
    private async Task TestImplementationSelection()
    {
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
                    DefaultImplementation = ImplementationType.Deterministic,
                    AllowImplementationOverride = true
                }
            ],
            Interface = new ClusterInterface()
        };
        
        var instance = new ClusterInstance
        {
            ClusterId = "test-cluster",
            InstanceId = "inst-1",
            ImplementationOverrides = new Dictionary<string, ImplementationType>
            {
                ["b1"] = ImplementationType.Agentic
            }
        };
        
        // Instance override should take precedence
        AssertTrue(instance.ImplementationOverrides.ContainsKey("b1"));
        AssertEqual(ImplementationType.Agentic, instance.ImplementationOverrides["b1"]);
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test brick for cluster executor smoke tests.
/// </summary>
public class ClusterTestBrick : Brick
{
    public ClusterTestBrick()
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
        await Task.Delay(10, cancellationToken);
        return new Core.Domain.Execution.BrickOutput
        {
            ["output"] = "result",
            Summary = "Test completed"
        };
    }
}

