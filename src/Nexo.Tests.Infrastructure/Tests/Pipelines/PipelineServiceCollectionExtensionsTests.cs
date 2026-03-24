using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddPipelineCompositionLayer();
        var provider = services.BuildServiceProvider();

        provider.GetService<IPipelineTemplateValidator>().Should().BeOfType<PipelineTemplateValidator>();
        provider.GetService<IPipelineDecomposer>().Should().BeOfType<PipelineDecomposer>();
        provider.GetService<IPipelineScheduler>().Should().BeOfType<PipelineScheduler>();
        provider.GetService<IPipelineScalingPolicy>().Should().BeOfType<ThresholdScalingPolicy>();
        provider.GetService<IPipelineRunStore>().Should().BeOfType<InMemoryPipelineRunStore>();
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
    }
}
