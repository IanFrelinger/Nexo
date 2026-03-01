using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Adaptation;

namespace Nexo.Infrastructure;

/// <summary>
/// DI extension methods for the adaptation engine (Block 3 + Block 4).
/// </summary>
public static class AdaptationServiceCollectionExtensions
{
    /// <summary>
    /// Registers adaptation infrastructure: decomposer, fix generator, recompiler, rewirer, generators, source fixers.
    /// </summary>
    public static IServiceCollection AddAdaptationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IBrickDecomposer, BrickDecomposer>();
        services.AddSingleton<IFixGenerator, FixGenerator>();
        services.AddSingleton<IBrickRecompiler, BrickRecompiler>();
        services.AddSingleton<IBehaviorRewirer, BehaviorRewirer>();
        services.AddSingleton<INewBrickGenerator, NewBrickGenerator>();
        services.AddSingleton<INewBehaviorAssembler, NewBehaviorAssembler>();
        services.AddSingleton<ISourceCodeFixer, EmptyCatchCodeFixer>();
        return services;
    }
}
