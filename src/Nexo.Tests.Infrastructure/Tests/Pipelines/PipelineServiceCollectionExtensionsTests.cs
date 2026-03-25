using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Hosting;
using Nexo.Infrastructure.Pipelines;
using Xunit;
using System.Collections.Generic;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

public sealed class PipelineServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPipelineCompositionLayer_RegistersAllPipelineContracts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPipelineCompositionLayer();
        using var provider = services.BuildServiceProvider();

        provider.GetService<IPipelineTemplateValidator>().Should().BeOfType<PipelineTemplateValidator>();
        provider.GetService<IPipelineDecomposer>().Should().BeOfType<PipelineDecomposer>();
        provider.GetService<IPipelineScheduler>().Should().BeOfType<PipelineScheduler>();
        provider.GetService<IPipelineScalingPolicy>().Should().BeOfType<ThresholdScalingPolicy>();
        provider.GetService<IPipelineRunStore>().Should().BeOfType<InMemoryPipelineRunStore>();
        provider.GetServices<IPipelineStageExecutor>().Should().HaveCount(2);
        provider.GetService<IPipelineOrchestrator>().Should().BeOfType<PipelineOrchestrator>();
    }

    [Fact]
    public void AddNexo_RegistersPipelineCompositionLayerByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        using var provider = services.BuildServiceProvider();

        provider.GetService<IPipelineTemplateValidator>().Should().NotBeNull();
        provider.GetService<IPipelineDecomposer>().Should().NotBeNull();
        provider.GetService<IPipelineScheduler>().Should().NotBeNull();
        provider.GetService<IPipelineScalingPolicy>().Should().NotBeNull();
        provider.GetService<IPipelineRunStore>().Should().NotBeNull();
        provider.GetService<IPipelineOrchestrator>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineCompositionLayer_WithLiteDbPersistence_RegistersLiteDbRunStore()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nexo-pipelines-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPipelineCompositionLayer(
                configureExecution: null,
                configurePersistence: options =>
                {
                    options.Provider = "LiteDb";
                    options.DatabasePath = tempPath;
                });

            using var provider = services.BuildServiceProvider();
            provider.GetService<IPipelineRunStore>().Should().BeOfType<LiteDbPipelineRunStore>();
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void AddPipelineCompositionLayer_WithUnknownProvider_ThrowsAtResolution()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPipelineCompositionLayer(
            configureExecution: null,
            configurePersistence: options =>
            {
                options.Provider = "NotAProvider";
            });

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IPipelineRunStore>();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown pipeline persistence provider*");
    }

    [Fact]
    public void AddPipelineCompositionLayer_WithConfiguration_BindsExecutionOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Pipelines:Execution:MaxRetryAttempts"] = "5",
                ["Nexo:Pipelines:Execution:RetryDelayMs"] = "15",
                ["Nexo:Pipelines:Execution:CompletionPolicy"] = "AllowNonCriticalStageFailures",
                ["Nexo:Pipelines:Execution:AllowMissingResumeSource"] = "true",
                ["Nexo:Pipelines:Execution:EnableTestHooks"] = "true",
                ["Nexo:Pipelines:Persistence:Provider"] = "InMemory",
                ["Nexo:Pipelines:Adapters:DeterministicAdapter"] = "default",
                ["Nexo:Pipelines:Adapters:AgenticAdapter"] = "default",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPipelineCompositionLayer(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PipelineExecutionOptions>>().Value;
        options.MaxRetryAttempts.Should().Be(5);
        options.RetryDelayMs.Should().Be(15);
        options.CompletionPolicy.Should().Be(PipelineCompletionPolicy.AllowNonCriticalStageFailures);
        options.AllowMissingResumeSource.Should().BeTrue();
        options.EnableTestHooks.Should().BeTrue();
    }

    [Fact]
    public void AddPipelineCompositionLayer_WithConfiguration_EnvironmentOverridesConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Pipelines:Execution:MaxRetryAttempts"] = "2"
            })
            .Build();

        Environment.SetEnvironmentVariable("NEXO_PIPELINE_MAX_RETRIES", "7");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPipelineCompositionLayer(configuration);
            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<PipelineExecutionOptions>>().Value;
            options.MaxRetryAttempts.Should().Be(7);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_PIPELINE_MAX_RETRIES", null);
        }
    }

    [Fact]
    public void AddPipelineCompositionLayer_WithNumericBooleanEnv_ParsesAsBoolean()
    {
        Environment.SetEnvironmentVariable("NEXO_PIPELINE_ENABLE_TEST_HOOKS", "1");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPipelineCompositionLayer();
            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<PipelineExecutionOptions>>().Value;
            options.EnableTestHooks.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_PIPELINE_ENABLE_TEST_HOOKS", null);
        }
    }

    [Fact]
    public void AddPipelineCompositionLayer_RegistersDefaultExecutionAdapters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPipelineCompositionLayer();
        using var provider = services.BuildServiceProvider();

        var adapters = provider.GetServices<IPipelineStageExecutionAdapter>().ToArray();
        adapters.Should().HaveCount(2);
        adapters.Should().ContainSingle(x =>
            x.WorkerType == Nexo.Core.Application.Pipelines.Models.PipelineWorkerType.Deterministic &&
            string.Equals(x.AdapterKey, "default", StringComparison.OrdinalIgnoreCase));
        adapters.Should().ContainSingle(x =>
            x.WorkerType == Nexo.Core.Application.Pipelines.Models.PipelineWorkerType.Agentic &&
            string.Equals(x.AdapterKey, "default", StringComparison.OrdinalIgnoreCase));
    }
}
