using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Trust;
using Nexo.Abstractions.Routing;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Hosting;
using Nexo.Runtime.Routing;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Hosting;

/// <summary>
/// E2E smoke tests for the AddNexo hosting API.
/// Validates that the full kernel (orchestration, adaptation, persistence, validation) can be resolved and used.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "ProdStyle")]
public sealed class HostingE2ESmokeTests
{
    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_ShouldBuildServiceProvider()
    {
        await Task.CompletedTask;
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
        Assert.True(result.TestsRun >= 0);
        Assert.True(result.TestsPassed >= 0);
        Assert.True(result.TestsFailed >= 0);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_ShouldResolveAnalysisService()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        var analysisService = sp.GetRequiredService<Nexo.Core.Application.Analysis.Ports.IAnalysisService>();
        analysisService.Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_RegistersObservationPipeline_ByDefault()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        var patternStore = sp.GetRequiredService<Nexo.Core.Application.Observation.Ports.IPatternStore>();
        patternStore.Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_WithDisableObservationPipeline_DoesNotRegister()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(o => o.DisableObservationPipeline = true);
        var sp = services.BuildServiceProvider();

        var patternStore = sp.GetService<Nexo.Core.Application.Observation.Ports.IPatternStore>();
        patternStore.Should().BeNull("observation pipeline should not register IPatternStore when disabled");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_WithDisableObservationPipeline_StartsBackgroundAgentHost()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddNexo(o =>
                {
                    o.DisableObservationPipeline = true;
                    o.RegisterBackgroundAgentHostedService = true;
                });
            })
            .Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexoProfile_Edge_PeelsOffOptionalServices()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.Edge);
        var sp = services.BuildServiceProvider();

        sp.GetService<IPatternStore>().Should().BeNull();
        sp.GetService<IBackgroundAgentRegistry>().Should().BeNull();
        sp.GetRequiredService<IEndpointRegistry>().Should().NotBeOfType<InMemoryEndpointRegistry>();
        sp.GetService<ICloudSanitizationProxy>().Should().BeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_UsesEnvironmentDeploymentProfile_WhenOptionsDoNotOverride()
    {
        await Task.CompletedTask;
        const string profileKey = "NEXO_DEPLOYMENT_PROFILE";
        var previous = Environment.GetEnvironmentVariable(profileKey);
        Environment.SetEnvironmentVariable(profileKey, "edge");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNexo();
            var sp = services.BuildServiceProvider();

            sp.GetService<IPatternStore>().Should().BeNull();
            sp.GetService<IBackgroundAgentRegistry>().Should().BeNull();
            sp.GetRequiredService<IEndpointRegistry>().Should().NotBeOfType<InMemoryEndpointRegistry>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(profileKey, previous);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_Defaults_To_Meai_Pipeline_And_VectorData_Rag()
    {
        await Task.CompletedTask;
        var previous = Environment.GetEnvironmentVariable("NEXO_USE_MEAI_PIPELINE");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_USE_MEAI_PIPELINE", null);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNexo();
            using var sp = services.BuildServiceProvider();

            sp.GetService<Microsoft.Extensions.AI.IChatClient>().Should().NotBeNull(
                "Phase 6 defaults the MEAI governed chat pipeline on");
            sp.GetRequiredService<Nexo.BackgroundAgents.RAG.IRAGService>()
                .Should().BeOfType<Nexo.Hosting.Meai.MeaiVectorDataRagAdapter>();
            sp.GetRequiredService<Nexo.AI.Pipeline.Rag.VectorDataRagService>().Should().NotBeNull();
            sp.GetRequiredService<Nexo.Infrastructure.Execution.Models.HotSwappableModel>().Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_USE_MEAI_PIPELINE", previous);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_Meai_OptOut_Skips_ChatClient_But_Keeps_VectorData_Rag()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(o => o.UseMeaiPipeline = false);
        using var sp = services.BuildServiceProvider();

        sp.GetService<Microsoft.Extensions.AI.IChatClient>().Should().BeNull();
        sp.GetRequiredService<Nexo.BackgroundAgents.RAG.IRAGService>()
            .Should().BeOfType<Nexo.Hosting.Meai.MeaiVectorDataRagAdapter>();
    }
}
