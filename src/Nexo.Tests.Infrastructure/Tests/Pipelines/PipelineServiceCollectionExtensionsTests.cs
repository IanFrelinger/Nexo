using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Hosting;
using Nexo.Infrastructure.Pipelines;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

public sealed class PipelineServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPipelineCompositionLayer_RegistersAllPipelineContracts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPipelineCompositionLayer();
        var provider = services.BuildServiceProvider();

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
        var provider = services.BuildServiceProvider();

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

            var provider = services.BuildServiceProvider();
            provider.GetService<IPipelineRunStore>().Should().BeOfType<LiteDbPipelineRunStore>();
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
