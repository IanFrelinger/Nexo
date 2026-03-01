using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Host;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Host;

public class ServiceHostTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBuildDefault();
            await TestBuildDefaultWithConfiguration();
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

        AssertNotNull(serviceProvider);
        
        // Should have IOrchestrator registered
        var orchestrator = serviceProvider.GetService<IOrchestrator>();
        AssertNotNull(orchestrator);
        AssertTrue(orchestrator is GenericCommandOrchestrator);

        // Should have GenericCommandOrchestrator registered
        var genericOrchestrator = serviceProvider.GetService<GenericCommandOrchestrator>();
        AssertNotNull(genericOrchestrator);

        // Should have IPreValidator registered
        var preValidator = serviceProvider.GetService<IPreValidator>();
        AssertNotNull(preValidator);
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

        AssertTrue(configured);
        AssertNotNull(serviceProvider);
        
        var testService = serviceProvider.GetService<TestService>();
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

        AssertNotNull(result);
        AssertTrue(result.Success);
    }

    private class TestService { }

    private class TestCommand : ICommand<string, string>
    {
        public async ValueTask<string> ExecuteAsync(string input, CancellationToken ct)
        {
            await Task.CompletedTask;
            return $"processed-{input}";
        }
    }
}

