using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Demo.Api.Controllers;
using Nexo.Infrastructure.Execution;

namespace Nexo.Demo.Api.Tests.Controllers;

/// <summary>
/// Tests for CatalogController.
/// </summary>
public class CatalogControllerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestGetBricks();
            await TestGetBrick();
            await TestGetBehaviors();
            await TestGetBehavior();
            await TestGetAgents();
            await TestGetAgent();
            await TestNotFound();
            
            return new TestResult
            {
                TestName = nameof(CatalogControllerTests),
                Category = "API",
                Passed = true,
                Message = "All CatalogController tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(CatalogControllerTests),
                Category = "API",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(CatalogControllerTests),
                Category = "API",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestGetBricks()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        var testBrick = new TestBrick { Id = "test-brick" };
        mockBrickRegistry.Setup(r => r.GetAllBricks()).Returns([testBrick]);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var result = controller.GetBricks();
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        var bricks = okResult.Value as IEnumerable<object>;
        AssertNotNull(bricks);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetBrick()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        var testBrick = new TestBrick { Id = "test-brick" };
        mockBrickRegistry.Setup(r => r.GetBrick("test-brick")).Returns(testBrick);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var result = controller.GetBrick("test-brick");
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetBehaviors()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        var testBehavior = new Behavior { Id = "test-behavior", Name = "Test", Description = "Test" };
        mockBehaviorRegistry.Setup(r => r.GetAllBehaviors()).Returns([testBehavior]);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var result = controller.GetBehaviors();
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetBehavior()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        var testBehavior = new Behavior { Id = "test-behavior", Name = "Test", Description = "Test" };
        mockBehaviorRegistry.Setup(r => r.GetBehavior("test-behavior")).Returns(testBehavior);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var result = controller.GetBehavior("test-behavior");
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetAgents()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        var testAgent = new AgentCard { Id = "test-agent", Name = "Test", Domain = "Test", Description = "Test" };
        mockAgentRegistry.Setup(r => r.GetAllAgents()).Returns([testAgent]);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var result = controller.GetAgents();
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetAgent()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        var testAgent = new AgentCard { Id = "test-agent", Name = "Test", Domain = "Test", Description = "Test" };
        mockAgentRegistry.Setup(r => r.GetAgent("test-agent")).Returns(testAgent);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var result = controller.GetAgent("test-agent");
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestNotFound()
    {
        var mockBrickRegistry = new Mock<IBrickRegistry>();
        var mockBehaviorRegistry = new Mock<IBehaviorRegistry>();
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        
        mockBrickRegistry.Setup(r => r.GetBrick("missing")).Returns((Brick?)null);
        mockBehaviorRegistry.Setup(r => r.GetBehavior("missing")).Returns((Behavior?)null);
        mockAgentRegistry.Setup(r => r.GetAgent("missing")).Returns((AgentCard?)null);
        
        var controller = new CatalogController(
            mockBrickRegistry.Object,
            mockBehaviorRegistry.Object,
            mockAgentRegistry.Object);
        
        var brickResult = controller.GetBrick("missing");
        AssertTrue(brickResult.Result is NotFoundResult);
        
        var behaviorResult = controller.GetBehavior("missing");
        AssertTrue(behaviorResult.Result is NotFoundResult);
        
        var agentResult = controller.GetAgent("missing");
        AssertTrue(agentResult.Result is NotFoundResult);
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test brick for API tests.
/// </summary>
public class TestBrick : Brick
{
    public TestBrick()
    {
        Id = "test-brick";
        Name = "Test";
        Category = BrickCategory.Analysis;
        Description = "Test";
        
        Interface = new BrickInterface();
        Implementations = new BrickImplementations();
        DefaultImplementation = ImplementationType.Deterministic;
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new BrickOutput());
    }
}

