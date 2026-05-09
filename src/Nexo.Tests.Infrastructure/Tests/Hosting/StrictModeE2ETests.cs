using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Exceptions;
using Nexo.Hosting;
using Nexo.Infrastructure.Configuration;
using Nexo.Infrastructure.Pipelines;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Hosting;

/// <summary>
/// E2E tests for strict mode: fail-fast behavior, verbose diagnostics,
/// configuration warnings-as-errors, and permissive defaults.
/// </summary>
[Trait("Category", "E2E")]
[Collection("EnvironmentVariables")]
public sealed class StrictModeE2ETests
{
    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_Disabled_ByDefault()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        var sp = services.BuildServiceProvider();

        var strict = sp.GetRequiredService<StrictModeOptions>();
        strict.Enabled.Should().BeFalse();
        strict.ShouldFailFastOnValidation.Should().BeFalse();
        strict.ShouldFailFastOnProvider.Should().BeFalse();
        strict.ShouldFailFastOnPipeline.Should().BeFalse();
        strict.ShouldEmitVerboseDiagnostics.Should().BeFalse();
        strict.ShouldFailOnConfigurationWarnings.Should().BeFalse();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_Enabled_AllSubFlagsFollowMaster()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(opts => opts.StrictMode.Enabled = true);
        var sp = services.BuildServiceProvider();

        var strict = sp.GetRequiredService<StrictModeOptions>();
        strict.Enabled.Should().BeTrue();
        strict.ShouldFailFastOnValidation.Should().BeTrue();
        strict.ShouldFailFastOnProvider.Should().BeTrue();
        strict.ShouldFailFastOnPipeline.Should().BeTrue();
        strict.ShouldEmitVerboseDiagnostics.Should().BeTrue();
        strict.ShouldFailOnConfigurationWarnings.Should().BeTrue();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_IndividualOverrides_TakePrecedence()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(opts =>
        {
            opts.StrictMode.Enabled = true;
            opts.StrictMode.FailFastOnPipelineErrors = false;
            opts.StrictMode.VerboseDiagnostics = false;
        });
        var sp = services.BuildServiceProvider();

        var strict = sp.GetRequiredService<StrictModeOptions>();
        strict.ShouldFailFastOnValidation.Should().BeTrue("follows master");
        strict.ShouldFailFastOnProvider.Should().BeTrue("follows master");
        strict.ShouldFailFastOnPipeline.Should().BeFalse("explicitly overridden");
        strict.ShouldEmitVerboseDiagnostics.Should().BeFalse("explicitly overridden");
        strict.ShouldFailOnConfigurationWarnings.Should().BeTrue("follows master");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_SelectiveEnable_WhenMasterDisabled()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(opts =>
        {
            opts.StrictMode.Enabled = false;
            opts.StrictMode.FailFastOnProviderErrors = true;
        });
        var sp = services.BuildServiceProvider();

        var strict = sp.GetRequiredService<StrictModeOptions>();
        strict.Enabled.Should().BeFalse();
        strict.ShouldFailFastOnProvider.Should().BeTrue("explicitly enabled");
        strict.ShouldFailFastOnValidation.Should().BeFalse("follows disabled master");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_ConfigWarnings_ThrowsOnMissingConfig()
    {
        var prev = Environment.GetEnvironmentVariable("NEXO_CONFIG_PATH");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_CONFIG_PATH", Path.Combine(Path.GetTempPath(), "nexo-nonexistent-" + Guid.NewGuid().ToString("N"), "config.json"));
            var logger = new LoggerFactory().CreateLogger<ConfigurationServiceAdapter>();
            var adapter = new ConfigurationServiceAdapter(logger, failOnConfigWarnings: true);

            var act = async () => await adapter.LoadAsync();
            await act.Should().ThrowAsync<ConfigurationException>()
                .WithMessage("*strict mode*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_CONFIG_PATH", prev);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_Permissive_FallsBackToDefaultsOnMissingConfig()
    {
        var prev = Environment.GetEnvironmentVariable("NEXO_CONFIG_PATH");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_CONFIG_PATH", Path.Combine(Path.GetTempPath(), "nexo-nonexistent-" + Guid.NewGuid().ToString("N"), "config.json"));
            var logger = new LoggerFactory().CreateLogger<ConfigurationServiceAdapter>();
            var adapter = new ConfigurationServiceAdapter(logger, failOnConfigWarnings: false);

            var config = await adapter.LoadAsync();
            config.Should().NotBeNull();
            config.Analysis.MaxComplexityThreshold.Should().Be(NexoDefaults.AnalysisMaxComplexityThreshold);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_CONFIG_PATH", prev);
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StrictMode_RegisteredAsSingleton_SameInstanceEverywhere()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo(opts => opts.StrictMode.Enabled = true);
        var sp = services.BuildServiceProvider();

        var s1 = sp.GetRequiredService<StrictModeOptions>();
        var s2 = sp.GetRequiredService<StrictModeOptions>();
        ReferenceEquals(s1, s2).Should().BeTrue("StrictModeOptions is a singleton");
    }
}
