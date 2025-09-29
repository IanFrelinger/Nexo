using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Application.Host;
using Xunit;

namespace Nexo.Tests.E2E;

[Trait("Category", "E2E")]
public class OrchestratorSmokeTests
{
    [Fact]
    public async Task Orchestrator_RunAsync_WithAssemblyAnalysisCommand_ShouldReturnSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<GenericCommandOrchestrator>();
        services.AddScoped<ICommand<AssemblyAnalysisRequest, AssemblyAnalysisResult>, AssemblyAnalysisCommand>();
        services.AddScoped<IPreValidator, NoopPreValidator>();
        services.AddScoped<IPostValidator<AssemblyAnalysisResult>, AssemblyAnalysisPostValidator>();

        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<GenericCommandOrchestrator>();

        // Use the test assembly itself for analysis
        var testAssemblyPath = typeof(OrchestratorSmokeTests).Assembly.Location;
        Assert.True(File.Exists(testAssemblyPath));

        var request = new AssemblyAnalysisRequest(testAssemblyPath);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act
        var result = await orchestrator.RunAsync<AssemblyAnalysisRequest, AssemblyAnalysisResult>(
            provider.GetRequiredService<ICommand<AssemblyAnalysisRequest, AssemblyAnalysisResult>>(),
            request, 
            cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task ServiceHost_RunAsync_WithAssemblyAnalysisCommand_ShouldReturnSuccess()
    {
        // Arrange
        var host = ServiceHost.BuildDefault(services =>
        {
            services.AddSingleton<ICommand<AssemblyAnalysisRequest, AssemblyAnalysisResult>, AssemblyAnalysisCommand>();
        });
        var serviceHost = new ServiceHost(host);
        var command = host.GetRequiredService<ICommand<AssemblyAnalysisRequest, AssemblyAnalysisResult>>();
        
        var testAssemblyPath = typeof(OrchestratorSmokeTests).Assembly.Location;
        var request = new AssemblyAnalysisRequest(testAssemblyPath);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act
        var result = await serviceHost.RunAsync(command, request, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Null(result.Message);
    }
}

// Post-validator for assembly analysis results
file sealed class AssemblyAnalysisPostValidator : IPostValidator<AssemblyAnalysisResult>
{
    public ValueTask<(bool ok, string? reason)> ValidateAsync(AssemblyAnalysisResult result, CancellationToken ct)
    {
        if (!result.Success)
        {
            return ValueTask.FromResult<(bool, string?)>((false, $"Analysis failed: {result.ErrorMessage}"));
        }

        if (string.IsNullOrEmpty(result.AssemblyName))
        {
            return ValueTask.FromResult<(bool, string?)>((false, "Assembly name is missing"));
        }

        if (result.TypeCount <= 0)
        {
            return ValueTask.FromResult<(bool, string?)>((false, "No types found in assembly"));
        }

        return ValueTask.FromResult<(bool, string?)>((true, null));
    }
}

