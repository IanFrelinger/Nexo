using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Host;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Host;

/// <summary>Tests for service host.</summary>
public class ServiceHostTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test build default.</summary>
            await TestBuildDefault();
            /// <summary>Test build default with configuration.</summary>
            await TestBuildDefaultWithConfiguration();
            /// <summary>Test run async.</summary>
            await TestRunAsync();

            return new TestResult
            {
                Name = nameof(ServiceHostTests),
                Category = "Application",
                Passed = true,
                Message = "All ServiceHost tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ServiceHostTests),
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
                Name = nameof(ServiceHostTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private Task TestBuildDefault()
    {
        var serviceProvider = ServiceHost.BuildDefault();

        /// <summary>Assert not null.</summary>
        AssertNotNull(serviceProvider);
        
        // Should have IOrchestrator registered
        var orchestrator = serviceProvider.GetService<IOrchestrator>();
        /// <summary>Assert not null.</summary>
        AssertNotNull(orchestrator);
        /// <summary>Assert true.</summary>
        /// <param name="GenericCommandOrchestrator">Generic command orchestrator.</param>
        AssertTrue(orchestrator is GenericCommandOrchestrator);

        // Should have GenericCommandOrchestrator registered
        var genericOrchestrator = serviceProvider.GetService<GenericCommandOrchestrator>();
        /// <summary>Assert not null.</summary>
        AssertNotNull(genericOrchestrator);

        // Should have IPreValidator registered
        var preValidator = serviceProvider.GetService<IPreValidator>();
        /// <summary>Assert not null.</summary>
        AssertNotNull(preValidator);
        /// <summary>Assert true.</summary>
        /// <param name="NoopPreValidator">Noop pre validator.</param>
        AssertTrue(preValidator is NoopPreValidator);

        return Task.CompletedTask;
    }

    private Task TestBuildDefaultWithConfiguration()
    {
        var configured = false;
        var serviceProvider = ServiceHost.BuildDefault(services =>
        {
            configured = true;
            services.AddSingleton<TestService>();
        });

        /// <summary>Assert true.</summary>
        AssertTrue(configured);
        /// <summary>Assert not null.</summary>
        AssertNotNull(serviceProvider);
        
        var testService = serviceProvider.GetService<TestService>();
        /// <summary>Assert not null.</summary>
        AssertNotNull(testService);

        return Task.CompletedTask;
    }

    private async Task TestRunAsync()
    {
        var serviceProvider = ServiceHost.BuildDefault();
        var host = new ServiceHost(serviceProvider);
        var command = new TestCommand();
        var input = "test-input";

        var result = await host.RunAsync(command, input, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert true.</summary>
        AssertTrue(result.Success);
    }

    /// <summary>Tests for test service.</summary>
    private class TestService { }

    /// <summary>Tests for test command.</summary>
    private class TestCommand : ICommand<string, string>
    {
        public async ValueTask<string> ExecuteAsync(string input, CancellationToken ct)
        {
            await Task.CompletedTask;
            return $"processed-{input}";
        }
    }
}

