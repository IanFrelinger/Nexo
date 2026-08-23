using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Agents;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Application.Tests.Agents;

/// <summary>Tests for agent factory.</summary>
public class AgentFactoryTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test create.</summary>
            await TestCreate();
            /// <summary>Test create throws when not registered.</summary>
            await TestCreateThrowsWhenNotRegistered();

            return new TestResult
            {
                Name = nameof(AgentFactoryTests),
                Category = "Application",
                Passed = true,
                Message = "All AgentFactory tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(AgentFactoryTests),
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
                Name = nameof(AgentFactoryTests),
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(agent);
        /// <summary>Assert true.</summary>
        /// <param name="TestAgent">Test agent.</param>
        AssertTrue(agent is TestAgent);
        
        // Should return same instance if registered as singleton
        var agent2 = factory.Create<TestAgent>();
        /// <summary>Assert equal.</summary>
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
            /// <summary>Assertion exception.</summary>
            /// <param name="thrown"">Thrown".</param>
            throw new AssertionException("Expected InvalidOperationException to be thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        return Task.CompletedTask;
    }

    /// <summary>Tests for test agent.</summary>
    private class TestAgent { }
}

