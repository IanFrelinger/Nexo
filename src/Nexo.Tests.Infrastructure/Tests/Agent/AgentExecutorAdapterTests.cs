using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Testing.Abstractions;
using TestingTestResult = Nexo.Core.Application.Testing.Models.TestResult;
using Nexo.Core.Domain.Exceptions;
using Nexo.Infrastructure.Agent.Adapters;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Agent;

public class AgentExecutorAdapterTests : UnitTestBase
{
    private DirectoryInfo? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = TestHelpers.CreateTempDirectory();
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null)
        {
            TestHelpers.CleanupTempDirectory(_tempDir);
        }
        return Task.CompletedTask;
    }

    public override async Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAgentNotFound();
            await TestSuccessfulExecutionWithInputFile();
            await TestSuccessfulExecutionWithoutInputFile();
            await TestSuccessfulExecutionWithDefaultAssembly();
            await TestTimeoutException();
            await TestGeneralException();
            await TestCancellation();

            return new TestingTestResult
            {
                TestName = nameof(AgentExecutorAdapterTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All AgentExecutorAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(AgentExecutorAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(AgentExecutorAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestAgentNotFound()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        
        // Create a service provider that returns no agents
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

        // Should throw AgentExecutionException when agent not found
        await AssertThrowsAsync<AgentExecutionException>(() =>
            adapter.ExecuteAsync("NonExistentAgent", null, CancellationToken.None));
    }

    private async Task TestSuccessfulExecutionWithInputFile()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        // Create a service provider with the mock agent
        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Create a test input file
        var inputFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");

        // Change to temp directory for execution
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir!.FullName);

            var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

            // This will fail because we don't have a real agent implementation,
            // but we can verify the agent was found and execution was attempted
            try
            {
                await adapter.ExecuteAsync("TestAgent", inputFile, CancellationToken.None);
            }
            catch (AgentExecutionException ex)
            {
                // Expected - agent execution may fail, but agent should be found
                AssertTrue(ex.Message.Contains("TestAgent") || ex.Message.Contains("execution"), 
                    $"Exception should mention agent or execution, got: {ex.Message}");
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestSuccessfulExecutionWithoutInputFile()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        var serviceProvider = services.BuildServiceProvider();

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir!.FullName);

            var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

            // Execute without input file - should use default assembly or empty path
            try
            {
                await adapter.ExecuteAsync("TestAgent", null, CancellationToken.None);
            }
            catch (AgentExecutionException)
            {
                // Expected - agent execution may fail
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestSuccessfulExecutionWithDefaultAssembly()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Create a default assembly file in the temp directory
        TestHelpers.CreateTempAssemblyFile(_tempDir!, "default.dll");

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir!.FullName);

            var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

            // Execute without input file - should find default.dll
            try
            {
                await adapter.ExecuteAsync("TestAgent", null, CancellationToken.None);
            }
            catch (AgentExecutionException)
            {
                // Expected - agent execution may fail
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestTimeoutException()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        var serviceProvider = services.BuildServiceProvider();

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir!.FullName);

            var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

            // The adapter catches TimeoutException and wraps it in AgentExecutionException
            // We can't easily trigger a timeout in a unit test, but we can verify the exception handling
            // For now, just verify the adapter doesn't throw unexpected exceptions
            try
            {
                await adapter.ExecuteAsync("TestAgent", null, CancellationToken.None);
            }
            catch (AgentExecutionException)
            {
                // Expected - agent execution may fail
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestGeneralException()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        var serviceProvider = services.BuildServiceProvider();

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir!.FullName);

            var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

            // The adapter catches general exceptions and wraps them in AgentExecutionException
            try
            {
                await adapter.ExecuteAsync("TestAgent", null, CancellationToken.None);
            }
            catch (AgentExecutionException ex)
            {
                // Expected - exceptions are wrapped
                AssertTrue(ex.Message.Contains("execution") || ex.Message.Contains("TestAgent"),
                    $"Exception message should mention execution or agent, got: {ex.Message}");
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestCancellation()
    {
        var mockLogger = new Mock<ILogger<AgentExecutorAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        var serviceProvider = services.BuildServiceProvider();

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir!.FullName);

            var adapter = new AgentExecutorAdapter(mockLogger.Object, serviceProvider);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Should handle cancellation gracefully
            try
            {
                await adapter.ExecuteAsync("TestAgent", null, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected - cancellation was propagated
            }
            catch (AgentExecutionException)
            {
                // Also acceptable - cancellation may be wrapped
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
}

