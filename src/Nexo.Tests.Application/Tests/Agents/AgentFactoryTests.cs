using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agents;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Agents;

public class AgentFactoryTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestCreate();
            await TestCreateThrowsWhenNotRegistered();

            return new TestResult
            {
                TestName = nameof(AgentFactoryTests),
                Category = "Application",
                Passed = true,
                Message = "All AgentFactory tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(AgentFactoryTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(AgentFactoryTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private Task TestCreate()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestAgent>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new AgentFactory(serviceProvider);

        var agent = factory.Create<TestAgent>();

        AssertNotNull(agent);
        AssertTrue(agent is TestAgent);
        
        // Should return same instance if registered as singleton
        var agent2 = factory.Create<TestAgent>();
        AssertEqual(agent, agent2);

        return Task.CompletedTask;
    }

    private Task TestCreateThrowsWhenNotRegistered()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new AgentFactory(serviceProvider);

        try
        {
            factory.Create<TestAgent>();
            throw new AssertionException("Expected InvalidOperationException to be thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        return Task.CompletedTask;
    }

    private class TestAgent { }
}

