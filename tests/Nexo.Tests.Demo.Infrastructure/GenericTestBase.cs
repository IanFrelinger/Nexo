using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Abstractions;

namespace Nexo.Tests.Demo.Infrastructure;

/// <summary>
/// Generic base class for command pattern tests.
/// Eliminates duplication across TestXxxCommand classes.
/// </summary>
public abstract class GenericCommandTestBase<TCommand, TInput, TOutput> : TestBase
    where TCommand : class
{
    protected GenericCommandTestBase(ITestOutputHelper? output = null) : base(output) { }

    /// <summary>
    /// Tests that a command can be created successfully.
    /// </summary>
    protected void AssertCommandCanBeCreated()
    {
        var command = CreateCommand();
        command.Should().NotBeNull("Command should be created successfully");
    }

    /// <summary>
    /// Tests that a command can execute successfully.
    /// </summary>
    protected async Task AssertCommandExecutesSuccessfully(TInput input)
    {
        var command = CreateCommand();
        
        // Use reflection to call ExecuteAsync if the command implements ICommand
        var executeMethod = command.GetType().GetMethod("ExecuteAsync");
        if (executeMethod != null)
        {
            var result = await (Task<TOutput>)executeMethod.Invoke(command, new object[] { input! })!;
            result.Should().NotBeNull("Command execution should return result");
        }
        else
        {
            // Fallback for commands that don't implement ICommand interface
            LogMessage($"Command {typeof(TCommand).Name} does not implement ExecuteAsync method");
        }
    }

    /// <summary>
    /// Creates an instance of the command under test.
    /// </summary>
    protected abstract TCommand CreateCommand();
}

/// <summary>
/// Generic base class for orchestrator pattern tests.
/// Eliminates duplication across TestXxxOrchestrator classes.
/// </summary>
public abstract class GenericOrchestratorTestBase<TOrchestrator> : TestBase
    where TOrchestrator : class
{
    protected GenericOrchestratorTestBase(ITestOutputHelper? output = null) : base(output) { }

    /// <summary>
    /// Tests that an orchestrator can be created successfully.
    /// </summary>
    protected void AssertOrchestratorCanBeCreated()
    {
        var orchestrator = CreateOrchestrator();
        orchestrator.Should().NotBeNull("Orchestrator should be created successfully");
    }

    /// <summary>
    /// Tests that an orchestrator can execute successfully.
    /// </summary>
    protected async Task AssertOrchestratorExecutesSuccessfully()
    {
        var orchestrator = CreateOrchestrator();
        
        // Use reflection to call ExecuteAsync if the orchestrator implements IOrchestrator
        var executeMethod = orchestrator.GetType().GetMethod("ExecuteAsync");
        if (executeMethod != null)
        {
            var result = await (Task<object>)executeMethod.Invoke(orchestrator, null)!;
            result.Should().NotBeNull("Orchestrator execution should return result");
        }
        else
        {
            // Fallback for orchestrators that don't implement IOrchestrator interface
            LogMessage($"Orchestrator {typeof(TOrchestrator).Name} does not implement ExecuteAsync method");
        }
    }

    /// <summary>
    /// Creates an instance of the orchestrator under test.
    /// </summary>
    protected abstract TOrchestrator CreateOrchestrator();
}

/// <summary>
/// Generic base class for service pattern tests.
/// Eliminates duplication across service test classes.
/// </summary>
public abstract class GenericServiceTestBase<TService> : TestBase
    where TService : class
{
    protected GenericServiceTestBase(ITestOutputHelper? output = null) : base(output) { }

    /// <summary>
    /// Tests that a service can be created successfully.
    /// </summary>
    protected void AssertServiceCanBeCreated()
    {
        var service = CreateService();
        service.Should().NotBeNull("Service should be created successfully");
    }

    /// <summary>
    /// Tests that a service can be disposed properly.
    /// </summary>
    protected void AssertServiceCanBeDisposed()
    {
        var service = CreateService();
        if (service is IDisposable disposable)
        {
            // Should not throw when disposing
            disposable.Invoking(d => d.Dispose()).Should().NotThrow("Service disposal should not throw");
        }
        else
        {
            LogMessage($"Service {typeof(TService).Name} does not implement IDisposable");
        }
    }

    /// <summary>
    /// Creates an instance of the service under test.
    /// </summary>
    protected abstract TService CreateService();
}
