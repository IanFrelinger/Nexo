using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Core.Application.SelfImprovement.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.SelfImprovement;
using Nexo.Infrastructure.Trust;
using Nexo.Infrastructure.Trust.Sdk.Extensions;

namespace Nexo.Infrastructure.SelfImprovement.Sdk.Extensions;
/// <summary>
/// DI extensions for self-improvement loop.
/// </summary>
public static class SelfImprovementServiceCollectionExtensions
{
    /// <summary>
    /// Registers the self-improvement loop. Requires adaptation, self-context, and trust services.
    /// Adds IAccessBoundary if not already registered.
    /// When IPatternStore and IPatternProcessedStore are registered (e.g. via AddObservationPipeline),
    /// the loop also processes observed patterns (repeated-edits, edit-then-build) as improvement triggers.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="maxIterationsPerRun">Max adaptations per run.</param>
    /// <param name="holdoutOptions">Optional holdout test options (P3.4). When set, regression excludes holdout; holdout runs at end.</param>
    public static IServiceCollection AddSelfImprovementLoop(
        this IServiceCollection services,
        int maxIterationsPerRun = 5,
        Nexo.Core.Application.SelfImprovement.Models.HoldoutTestOptions? holdoutOptions = null)
    {
        services.AddAccessBoundary(null);
        services.TryAddSingleton<ISelfImprovementMetricsStore>(_ => new FileBasedSelfImprovementMetricsStore());
        services.TryAddSingleton<TestFailureIngestionBridge>(sp =>
            new TestFailureIngestionBridge(sp.GetRequiredService<ITestFailureStore>()));
        services.AddSingleton<ISelfImprovementLoop>(sp => new SelfImprovementLoop(
            sp.GetRequiredService<ITestFailureStore>(),
            sp.GetRequiredService<Nexo.Core.Application.Analysis.Ports.IBrickStaticAnalyzer>(),
            sp.GetRequiredService<IImmutableCoreRegistry>(),
            sp.GetRequiredService<IAccessBoundary>(),
            sp.GetRequiredService<IRegressionTestRunner>(),
            sp.GetRequiredService<Nexo.Core.Application.Adaptation.Ports.IAdaptationPromoter>(),
            sp.GetRequiredService<Nexo.Core.Application.Adaptation.Ports.IAdaptationAuditLog>(),
            sp.GetRequiredService<Nexo.Core.Application.Rollback.Ports.IRollbackManager>(),
            sp.GetRequiredService<ISourceCodeFixer>(),
            sp.GetRequiredService<AdaptationRollbackHelper>(),
            sp.GetService<Microsoft.Extensions.Logging.ILogger<SelfImprovementLoop>>(),
            maxIterationsPerRun,
            holdoutOptions,
            sp.GetService<IPatternStore>(),
            sp.GetService<IPatternProcessedStore>(),
            sp.GetService<ISelfImprovementMetricsStore>()));
        return services;
    }
}
