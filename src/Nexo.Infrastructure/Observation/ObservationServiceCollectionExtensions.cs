using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;

/// <summary>
/// DI extension methods for the observation pipeline (Infrastructure layer).
/// </summary>
public static class ObservationServiceCollectionExtensions
{
    /// <summary>
    /// Adds observation infrastructure: pattern store, context assembler.
    /// Does not add event sources or the pipeline service (those are in BackgroundAgents).
    /// </summary>
    public static IServiceCollection AddObservationInfrastructure(this IServiceCollection services, string patternStorePath)
    {
        services.AddSingleton<IPatternStore>(_ => new LiteDbPatternStore(patternStorePath));
        services.AddSingleton<IContextAssembler, ContextAssembler>();
        return services;
    }
}
