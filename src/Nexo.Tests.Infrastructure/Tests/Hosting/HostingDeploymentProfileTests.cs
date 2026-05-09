using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Hosting;
using Nexo.Infrastructure.Execution;
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
    public void AllProfiles_BuildWithoutException(NexoDeploymentProfile profile)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(profile);

        var act = () => services.BuildServiceProvider(validateScopes: true);
        act.Should().NotThrow();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void FullProfile_ResolvesConfigurationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void SystemProfile_MinimalRegistration_StillResolvesCore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.System);
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
        sp.GetRequiredService<StrictModeOptions>().Should().NotBeNull();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void EdgeProfile_OmitsBackgroundAgents()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.Edge);
        var sp = services.BuildServiceProvider();

        sp.GetService<Nexo.BackgroundAgents.Registry.IBackgroundAgentRegistry>().Should().BeNull(
            "Edge profile should not register background agents");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void AirGappedProfile_OmitsTrustServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(NexoDeploymentProfile.AirGapped);
        var sp = services.BuildServiceProvider();

        sp.GetService<Nexo.BackgroundAgents.Registry.IBackgroundAgentRegistry>().Should().BeNull(
            "AirGapped profile should not register background agents");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void DeploymentProfile_FromEnvironmentVariable()
    {
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
    public void DeploymentProfile_ExplicitOverridesEnvVar()
    {
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
    public void StrictMode_FromEnvironmentVariable()
    {
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
    public void StrictMode_ExplicitOverridesEnvVar()
    {
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
    public void AddNexo_CalledTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        services.AddNexo();

        var act = () => services.BuildServiceProvider();
        act.Should().NotThrow("AddNexo should be idempotent");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void DeploymentProfile_InvalidValue_Throws()
    {
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
