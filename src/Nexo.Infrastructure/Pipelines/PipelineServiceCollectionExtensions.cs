using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Registers pipeline composition layer services.
/// </summary>
public static class PipelineServiceCollectionExtensions
{
    /// <summary>
    /// Adds default pipeline composition services.
    /// </summary>
    public static IServiceCollection AddPipelineCompositionLayer(this IServiceCollection services)
    {
        services.TryAddSingleton<IPipelineTemplateValidator, PipelineTemplateValidator>();
        services.TryAddSingleton<IPipelineDecomposer, PipelineDecomposer>();
        services.TryAddSingleton<IPipelineScheduler, PipelineScheduler>();
        services.TryAddSingleton<IPipelineScalingPolicy, ThresholdScalingPolicy>();
        services.TryAddSingleton<IPipelineRunStore, InMemoryPipelineRunStore>();
        return services;
    }
}
