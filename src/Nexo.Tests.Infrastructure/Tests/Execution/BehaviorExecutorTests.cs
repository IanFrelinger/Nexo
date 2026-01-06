using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for BehaviorExecutor.
/// </summary>
public class BehaviorExecutorTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestExecuteBehavior();
            await TestExecuteWithEvents();
            await TestStepFailureHandling();
            await TestImplementationSelection();
            await TestCacheUsage();
            
            return new TestResult
            {
                TestName = nameof(BehaviorExecutorTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All BehaviorExecutor tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(BehaviorExecutorTests),
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
                TestName = nameof(BehaviorExecutorTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestExecuteBehavior()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<BehaviorExecutor>>();
        
        var testBrick = new TestBrick();
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        
        var executor = new BehaviorExecutor(
            mockBrickRegistry.Object,
            mockProviderFactory.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var agent = new AgentCard
        {
            Id = "test-agent",
            Name = "Test Agent",
            Domain = "Test",
            Description = "Test"
        };
        
        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test Behavior",
            Description = "Test",
            Steps = [
                new BehaviorStep
                {
                    Id = "step-1",
                    BrickId = "test-brick",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string> { ["input"] = "code" },
                    OutputMapping = new Dictionary<string, string> { ["result"] = "output" }
                }
            ]
        };
        
        var input = new BehaviorInput(new Dictionary<string, object> { ["code"] = "test code" });
        var options = new ExecutionOptions { Provider = "openai" };
        
        var result = await executor.ExecuteAsync(agent, behavior, input, options);
        
        AssertTrue(result.Success);
        AssertEqual(0, result.Errors.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestExecuteWithEvents()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<BehaviorExecutor>>();
        
        var testBrick = new TestBrick();
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrickOutput?)null);
        
        var executor = new BehaviorExecutor(
            mockBrickRegistry.Object,
            mockProviderFactory.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var agent = new AgentCard { Id = "test-agent", Name = "Test", Domain = "Test", Description = "Test" };
        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test",
            Description = "Test",
            Steps = [
                new BehaviorStep
                {
                    Id = "step-1",
                    BrickId = "test-brick",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string> { ["input"] = "code" },
                    OutputMapping = new Dictionary<string, string>()
                }
            ]
        };
        
        var input = new BehaviorInput(new Dictionary<string, object> { ["code"] = "test" });
        var options = new ExecutionOptions { Provider = "openai" };
        
        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(agent, behavior, input, options))
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
    
    private async Task TestStepFailureHandling()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<BehaviorExecutor>>();
        
        mockBrickRegistry.Setup(r => r.GetBrick("missing-brick")).Returns((Brick?)null);
        
        var executor = new BehaviorExecutor(
            mockBrickRegistry.Object,
            mockProviderFactory.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var agent = new AgentCard { Id = "test-agent", Name = "Test", Domain = "Test", Description = "Test" };
        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test",
            Description = "Test",
            Steps = [
                new BehaviorStep
                {
                    Id = "step-1",
                    BrickId = "missing-brick",
                    Implementation = ImplementationType.Deterministic
                }
            ],
            OnStepFailure = FailurePolicy.Abort
        };
        
        var input = new BehaviorInput();
        var options = new ExecutionOptions();
        
        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(agent, behavior, input, options))
        {
            events.Add(evt);
        }
        
        AssertTrue(events.Any(e => e is StepErrorEvent));
        
        await Task.CompletedTask;
    }
    
    private async Task TestImplementationSelection()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<BehaviorExecutor>>();
        
        var testBrick = new TestBrick(); // DefaultImplementation is already set in constructor
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrickOutput?)null);
        
        var executor = new BehaviorExecutor(
            mockBrickRegistry.Object,
            mockProviderFactory.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var agent = new AgentCard { Id = "test-agent", Name = "Test", Domain = "Test", Description = "Test" };
        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test",
            Description = "Test",
            Steps = [
                new BehaviorStep
                {
                    Id = "step-1",
                    BrickId = "test-brick",
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string> { ["input"] = "code" },
                    OutputMapping = new Dictionary<string, string>()
                }
            ]
        };
        
        var input = new BehaviorInput(new Dictionary<string, object> { ["code"] = "test" });
        var options = new ExecutionOptions { Provider = "openai" };
        
        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(agent, behavior, input, options))
        {
            if (evt is StepStartedEvent stepStarted)
            {
                AssertEqual(ImplementationType.Deterministic, stepStarted.Implementation);
            }
            events.Add(evt);
        }
        
        await Task.CompletedTask;
    }
    
    private async Task TestCacheUsage()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockCache = new Mock<ISemanticCache>();
        var mockLogger = new Mock<ILogger<BehaviorExecutor>>();
        
        var testBrick = new TestBrick();
        var cachedOutput = new BrickOutput { ["result"] = "cached", Summary = "Cached" };
        
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedOutput);
        
        var executor = new BehaviorExecutor(
            mockBrickRegistry.Object,
            mockProviderFactory.Object,
            mockCache.Object,
            mockLogger.Object);
        
        var agent = new AgentCard { Id = "test-agent", Name = "Test", Domain = "Test", Description = "Test" };
        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test",
            Description = "Test",
            Steps = [
                new BehaviorStep
                {
                    Id = "step-1",
                    BrickId = "test-brick",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string> { ["input"] = "code" },
                    OutputMapping = new Dictionary<string, string>()
                }
            ]
        };
        
        var input = new BehaviorInput(new Dictionary<string, object> { ["code"] = "test" });
        var options = new ExecutionOptions { Provider = "openai" };
        
        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(agent, behavior, input, options))
        {
            events.Add(evt);
        }
        
        AssertTrue(events.Any(e => e is CacheHitEvent));
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test brick for execution tests.
/// </summary>
public class TestBrick : Brick
{
    public TestBrick()
    {
        Id = "test-brick";
        Name = "Test Brick";
        Category = BrickCategory.Analysis;
        Description = "Test";
        
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("input", "string", required: true)],
            Outputs = [new BrickOutputDefinition("output", "string")]
        };
        
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
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        return new BrickOutput
        {
            ["output"] = "result",
            Summary = "Test completed"
        };
    }
}

