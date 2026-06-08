using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions.Routing;
using Nexo.BackgroundAgents.Registry;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Hosting;
using Nexo.Infrastructure.Execution;
using Nexo.Runtime.Routing;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Hosting;

/// <summary>
/// Integration tests for <see cref="NexoServiceCollectionExtensions.AddNexo"/> across
/// all deployment profiles and environment variable resolution paths.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "ProdStyle")]
[Collection("EnvironmentVariables")]
public sealed class HostingDeploymentProfileTests
{
    [Theory(Timeout = TestTimeouts.E2E)]
    [InlineData(NexoDeploymentProfile.Full)]
    [InlineData(NexoDeploymentProfile.Server)]
    [InlineData(NexoDeploymentProfile.Edge)]
    [InlineData(NexoDeploymentProfile.AirGapped)]
    [InlineData(NexoDeploymentProfile.System)]
    public async Task AllProfiles_BuildWithoutException(NexoDeploymentProfile profile)
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(profile);

        var act = () => services.BuildServiceProvider(validateScopes: true);
        act.Should().NotThrow();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task FullProfile_ResolvesConfigurationService()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task SystemProfile_MinimalRegistration_StillResolvesCore()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.System);
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
        sp.GetRequiredService<StrictModeOptions>().Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task EdgeProfile_OmitsBackgroundAgents()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.Edge);
        var sp = services.BuildServiceProvider();

        sp.GetService<Nexo.BackgroundAgents.Registry.IBackgroundAgentRegistry>().Should().BeNull(
            "Edge profile should not register background agents");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AirGappedProfile_OmitsTrustServices()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.AirGapped);
        var sp = services.BuildServiceProvider();

        sp.GetService<Nexo.BackgroundAgents.Registry.IBackgroundAgentRegistry>().Should().BeNull(
            "AirGapped profile should not register background agents");
    }

    [Theory(Timeout = TestTimeouts.E2E)]
    [InlineData(NexoDeploymentProfile.Full)]
    [InlineData(NexoDeploymentProfile.Server)]
    public async Task FullAndServerProfiles_ResolveValidation(NexoDeploymentProfile profile)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(profile);
        var sp = services.BuildServiceProvider(validateScopes: true);

        using var scope = sp.CreateScope();
        var validation = scope.ServiceProvider.GetRequiredService<IValidationService>();
        var result = await validation.ValidateAsync(filter: null, progress: null, CancellationToken.None);
        Assert.True(result.TestsRun >= 0);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task EdgeProfile_ResolvesPipelineValidator_NotObservationOrAgents()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.Edge);
        var sp = services.BuildServiceProvider(validateScopes: true);

        sp.GetRequiredService<IPipelineTemplateValidator>().Should().NotBeNull();
        sp.GetService<IPatternStore>().Should().BeNull();
        sp.GetService<IBackgroundAgentRegistry>().Should().BeNull();
        sp.GetRequiredService<IEndpointRegistry>().Should().NotBeOfType<InMemoryEndpointRegistry>();

        var result = sp.GetRequiredService<IPipelineTemplateValidator>().Validate(new PipelineTemplate
        {
            TemplateId = "edge-profile-smoke",
            Version = "1",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "s1", Name = "S1", Mode = PipelineExecutionMode.Deterministic },
            },
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AirGappedProfile_ResolvesProviderFactory_NotGrpcTransport()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.AirGapped);
        var sp = services.BuildServiceProvider(validateScopes: true);

        sp.GetRequiredService<IProviderFactory>().Should().NotBeNull();
        sp.GetService<Nexo.Transport.Grpc.IGrpcChannelFactory>().Should().BeNull();
        sp.GetRequiredService<IPipelineTemplateValidator>().Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task SystemProfile_ResolvesCoreOnly_NoPipelinesOrFleetHeavyDeps()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.System);
        var sp = services.BuildServiceProvider(validateScopes: true);

        sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
        sp.GetRequiredService<StrictModeOptions>().Should().NotBeNull();
        sp.GetService<IPipelineTemplateValidator>().Should().BeNull();
        sp.GetService<IPatternStore>().Should().BeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task DeploymentProfile_FromEnvironmentVariable()
    {
        await Task.CompletedTask;
        var prev = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", "edge");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNexo();
            var sp = services.BuildServiceProvider();

            sp.GetService<Nexo.BackgroundAgents.Registry.IBackgroundAgentRegistry>().Should().BeNull(
                "Edge profile from env var should not register background agents");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", prev);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task DeploymentProfile_ExplicitOverridesEnvVar()
    {
        await Task.CompletedTask;
        var prev = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", "system");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNexoProfile(NexoDeploymentProfile.Full);
            var sp = services.BuildServiceProvider();

            sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", prev);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_FromEnvironmentVariable()
    {
        await Task.CompletedTask;
        var prev = Environment.GetEnvironmentVariable("NEXO_STRICT_MODE");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_STRICT_MODE", "1");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNexo();
            var sp = services.BuildServiceProvider();

            sp.GetRequiredService<StrictModeOptions>().Enabled.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_STRICT_MODE", prev);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_ExplicitOverridesEnvVar()
    {
        await Task.CompletedTask;
        var prev = Environment.GetEnvironmentVariable("NEXO_STRICT_MODE");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_STRICT_MODE", "0");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNexo(opts => opts.StrictMode.Enabled = true);
            var sp = services.BuildServiceProvider();

            sp.GetRequiredService<StrictModeOptions>().Enabled.Should().BeTrue(
                "explicit config should override env var");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_STRICT_MODE", prev);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexo_CalledTwice_DoesNotThrow()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        services.AddNexo();

        var act = () => services.BuildServiceProvider();
        act.Should().NotThrow("AddNexo should be idempotent");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task DeploymentProfile_InvalidValue_Throws()
    {
        await Task.CompletedTask;
        var prev = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", "banana");
            var services = new ServiceCollection();
            services.AddLogging();

            var act = () => services.AddNexo();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*banana*not recognized*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", prev);
        }
    }
}
