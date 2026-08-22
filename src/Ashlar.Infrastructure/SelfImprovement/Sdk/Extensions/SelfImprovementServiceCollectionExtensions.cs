using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Core.Application.SelfImprovement.Ports;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.SelfImprovement;
using Ashlar.Infrastructure.Trust;
using Ashlar.Infrastructure.Trust.Sdk.Extensions;

namespace Ashlar.Infrastructure.SelfImprovement.Sdk.Extensions;
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
        Ashlar.Core.Application.SelfImprovement.Models.HoldoutTestOptions? holdoutOptions = null)
    {
        services.AddAccessBoundary(null);
        services.TryAddSingleton<ISelfImprovementMetricsStore>(_ => new FileBasedSelfImprovementMetricsStore());
        services.TryAddSingleton<TestFailureIngestionBridge>(sp =>
            new TestFailureIngestionBridge(sp.GetRequiredService<ITestFailureStore>()));
        services.AddSingleton<ISelfImprovementLoop>(sp => new SelfImprovementLoop(
            sp.GetRequiredService<ITestFailureStore>(),
            sp.GetRequiredService<Ashlar.Core.Application.Analysis.Ports.IBrickStaticAnalyzer>(),
            sp.GetRequiredService<IImmutableCoreRegistry>(),
            sp.GetRequiredService<IAccessBoundary>(),
            sp.GetRequiredService<IRegressionTestRunner>(),
            sp.GetRequiredService<Ashlar.Core.Application.Adaptation.Ports.IAdaptationPromoter>(),
            sp.GetRequiredService<Ashlar.Core.Application.Adaptation.Ports.IAdaptationAuditLog>(),
            sp.GetRequiredService<Ashlar.Core.Application.Rollback.Ports.IRollbackManager>(),
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
