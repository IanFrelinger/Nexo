using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agents;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Extensions;

public class ServiceCollectionExtensionsTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAddNexoAgents();

            return new TestResult
            {
                TestName = nameof(ServiceCollectionExtensionsTests),
                Category = "Application",
                Passed = true,
                Message = "All ServiceCollectionExtensions tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ServiceCollectionExtensionsTests),
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
                TestName = nameof(ServiceCollectionExtensionsTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private Task TestAddNexoAgents()
    {
        var services = new ServiceCollection();
        
        var result = services.AddNexoAgents();

        // Should return the same service collection instance
        AssertEqual(services, result);
        
        // Should register IAgentFactory
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IAgentFactory>();
        AssertNotNull(factory);
        AssertTrue(factory is AgentFactory);

        return Task.CompletedTask;
    }
}

