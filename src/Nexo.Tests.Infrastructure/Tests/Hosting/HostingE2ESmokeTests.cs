using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Hosting;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Hosting;

/// <summary>
/// E2E smoke tests for the AddNexo hosting API.
/// Validates that the full kernel (orchestration, adaptation, persistence, validation) can be resolved and used.
/// </summary>
[Trait("Category", "E2E")]
public sealed class HostingE2ESmokeTests
{
    [Fact(Timeout = TestTimeouts.E2E)]
    public void AddNexo_ShouldBuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        sp.Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_ShouldResolveAndRunValidation()
    {
        var host = Host.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureServices(services =>
            {
                services.AddNexo();
            })
            .Build();

        var validationService = host.Services.GetRequiredService<IValidationService>();
        validationService.Should().NotBeNull();

        var result = await validationService.ValidateAsync(filter: null, progress: null, CancellationToken.None);

        result.Should().NotBeNull();
        result.TestsRun.Should().BeGreaterThanOrEqualTo(0);
        result.TestsPassed.Should().BeGreaterThanOrEqualTo(0);
        result.TestsFailed.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void AddNexo_ShouldResolveAnalysisService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        var analysisService = sp.GetRequiredService<Nexo.Core.Application.Analysis.Ports.IAnalysisService>();
        analysisService.Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void AddNexo_RegistersObservationPipeline_ByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        var patternStore = sp.GetRequiredService<Nexo.Core.Application.Observation.Ports.IPatternStore>();
        patternStore.Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void AddNexo_WithDisableObservationPipeline_DoesNotRegister()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(o => o.DisableObservationPipeline = true);
        var sp = services.BuildServiceProvider();

        var patternStore = sp.GetService<Nexo.Core.Application.Observation.Ports.IPatternStore>();
        patternStore.Should().BeNull("observation pipeline should not register IPatternStore when disabled");
    }
}
