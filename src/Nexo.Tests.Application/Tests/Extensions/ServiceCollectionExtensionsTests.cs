using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agents;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Extensions;

/// <summary>Tests for service collection extensions.</summary>
public class ServiceCollectionExtensionsTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test add nexo agents.</summary>
            await TestAddNexoAgents();

            return new TestResult
            {
                Name = nameof(ServiceCollectionExtensionsTests),
                Category = "Application",
                Passed = true,
                Message = "All ServiceCollectionExtensions tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ServiceCollectionExtensionsTests),
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
                Name = nameof(ServiceCollectionExtensionsTests),
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
        /// <summary>Assert not null.</summary>
        AssertNotNull(factory);
        /// <summary>Assert true.</summary>
        /// <param name="AgentFactory">Agent factory.</param>
        AssertTrue(factory is AgentFactory);

        return Task.CompletedTask;
    }
}

