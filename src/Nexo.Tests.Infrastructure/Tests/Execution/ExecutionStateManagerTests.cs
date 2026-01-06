using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for ExecutionStateManager.
/// </summary>
public class ExecutionStateManagerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestProviderManagement();
            await TestOfflineMode();
            await TestBrickImplementation();
            
            return new TestResult
            {
                TestName = nameof(ExecutionStateManagerTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ExecutionStateManager tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ExecutionStateManagerTests),
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
                TestName = nameof(ExecutionStateManagerTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestProviderManagement()
    {
        var mockLogger = new Mock<ILogger<ExecutionStateManager>>();
        var manager = new ExecutionStateManager(mockLogger.Object);
        
        var connectionId = "conn-1";
        
        manager.SetProvider(connectionId, "azure");
        var provider = manager.GetCurrentProvider(connectionId);
        AssertEqual("azure", provider);
        
        manager.SetProvider(connectionId, "ollama");
        provider = manager.GetCurrentProvider(connectionId);
        AssertEqual("ollama", provider);
        
        // Test default
        var defaultProvider = manager.GetCurrentProvider("new-connection");
        AssertEqual("openai", defaultProvider);
        
        await Task.CompletedTask;
    }
    
    private async Task TestOfflineMode()
    {
        var mockLogger = new Mock<ILogger<ExecutionStateManager>>();
        var manager = new ExecutionStateManager(mockLogger.Object);
        
        var connectionId = "conn-1";
        
        AssertFalse(manager.IsOffline(connectionId));
        
        manager.SetOffline(connectionId, true);
        AssertTrue(manager.IsOffline(connectionId));
        
        manager.SetOffline(connectionId, false);
        AssertFalse(manager.IsOffline(connectionId));
        
        await Task.CompletedTask;
    }
    
    private async Task TestBrickImplementation()
    {
        var mockLogger = new Mock<ILogger<ExecutionStateManager>>();
        var manager = new ExecutionStateManager(mockLogger.Object);
        
        var connectionId = "conn-1";
        var brickId = "test-brick";
        
        manager.SetBrickImplementation(connectionId, brickId, ImplementationType.Agentic);
        var impl = manager.GetBrickImplementation(connectionId, brickId);
        
        AssertNotNull(impl);
        AssertEqual(ImplementationType.Agentic, impl);
        
        manager.SetBrickImplementation(connectionId, brickId, ImplementationType.Deterministic);
        impl = manager.GetBrickImplementation(connectionId, brickId);
        
        AssertNotNull(impl);
        AssertEqual(ImplementationType.Deterministic, impl);
        
        // Test missing brick
        var missing = manager.GetBrickImplementation(connectionId, "missing-brick");
        AssertNull(missing);
        
        await Task.CompletedTask;
    }
}

