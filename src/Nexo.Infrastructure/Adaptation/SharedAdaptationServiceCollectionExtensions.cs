using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// DI extensions for P2.3 shared adaptation cache.
/// </summary>
public static class SharedAdaptationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared adaptation broadcaster and sync.
    /// Requires AddAdaptationInfrastructure and AddCodeAnalyzers.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="sharedPath">Base path for shared adaptations. Default: ~/.nexo/shared-adaptations.</param>
    /// <param name="regressionOverride">Optional override for tests. When provided, used instead of resolving from DI.</param>
    public static IServiceCollection AddSharedAdaptationCache(
        this IServiceCollection services,
        string? sharedPath = null,
        IRegressionTestRunner? regressionOverride = null)
    {
        services.AddSingleton<ISharedAdaptationBroadcaster>(sp =>
        {
            var log = sp.GetRequiredService<IAdaptationLog>();
            var regression = regressionOverride ?? sp.GetRequiredService<IRegressionTestRunner>();
            var immutable = sp.GetRequiredService<IImmutableCoreRegistry>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<FileBasedSharedAdaptationStore>>();
            return new FileBasedSharedAdaptationStore(sharedPath, log, regression, immutable, logger);
        });
        services.AddSingleton<ISharedAdaptationSync>(sp =>
        {
            var store = (FileBasedSharedAdaptationStore)sp.GetRequiredService<ISharedAdaptationBroadcaster>();
            return store;
        });
        return services;
    }
}
