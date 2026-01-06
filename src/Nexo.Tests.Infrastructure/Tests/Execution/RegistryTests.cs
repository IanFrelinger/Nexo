using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for registry implementations.
/// </summary>
public class RegistryTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBrickRegistry();
            await TestBehaviorRegistry();
            await TestAgentRegistry();
            
            return new TestResult
            {
                TestName = nameof(RegistryTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All registry tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(RegistryTests),
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
                TestName = nameof(RegistryTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestBrickRegistry()
    {
        var brick1 = new TestBrick { Id = "brick-1" };
        var brick2 = new TestBrick { Id = "brick-2" };
        var bricks = new List<Brick> { brick1, brick2 };
        
        var registry = new BrickRegistry(bricks);
        
        var found = registry.GetBrick("brick-1");
        AssertNotNull(found);
        AssertEqual("brick-1", found!.Id);
        
        var notFound = registry.GetBrick("missing");
        AssertNull(notFound);
        
        var all = registry.GetAllBricks();
        AssertEqual(2, all.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestBehaviorRegistry()
    {
        var behavior1 = new Behavior { Id = "behavior-1", Name = "Behavior 1", Description = "Test" };
        var behavior2 = new Behavior { Id = "behavior-2", Name = "Behavior 2", Description = "Test" };
        var behaviors = new List<Behavior> { behavior1, behavior2 };
        
        var registry = new BehaviorRegistry(behaviors);
        
        var found = registry.GetBehavior("behavior-1");
        AssertNotNull(found);
        AssertEqual("behavior-1", found!.Id);
        
        var notFound = registry.GetBehavior("missing");
        AssertNull(notFound);
        
        var all = registry.GetAllBehaviors();
        AssertEqual(2, all.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestAgentRegistry()
    {
        var agent1 = new AgentCard { Id = "agent-1", Name = "Agent 1", Domain = "Test", Description = "Test" };
        var agent2 = new AgentCard { Id = "agent-2", Name = "Agent 2", Domain = "Test", Description = "Test" };
        var agents = new List<AgentCard> { agent1, agent2 };
        
        var registry = new AgentRegistry(agents);
        
        var found = registry.GetAgent("agent-1");
        AssertNotNull(found);
        AssertEqual("agent-1", found!.Id);
        
        var notFound = registry.GetAgent("missing");
        AssertNull(notFound);
        
        var all = registry.GetAllAgents();
        AssertEqual(2, all.Count);
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test brick for registry tests.
/// </summary>
public class RegistryTestBrick : Brick
{
    public RegistryTestBrick()
    {
        Id = "test-brick";
        Name = "Test";
        Category = BrickCategory.Analysis;
        Description = "Test";
        
        Interface = new BrickInterface();
        Implementations = new BrickImplementations();
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

