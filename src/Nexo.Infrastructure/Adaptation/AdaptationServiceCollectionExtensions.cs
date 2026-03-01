using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Observation;

namespace Nexo.Infrastructure;

/// <summary>
/// DI extension methods for the adaptation engine (Block 3 + Block 4).
/// </summary>
public static class AdaptationServiceCollectionExtensions
{
    /// <summary>
    /// Registers adaptation infrastructure: decomposer, fix generator, recompiler, rewirer, generators, source fixers,
    /// adaptation log, promoter, rollback helper, conflict resolver, version manager.
    /// When <paramref name="patternStorePath"/> is provided, also registers observation core (IPatternStore, IContextAssembler)
    /// required for BrickRecompiler and ObservationContextBrick.
    /// </summary>
    public static IServiceCollection AddAdaptationInfrastructure(this IServiceCollection services, string? patternStorePath = null)
    {
        if (!string.IsNullOrEmpty(patternStorePath))
            services.AddObservationCore(patternStorePath);

        services.AddSingleton<IBrickDecomposer, BrickDecomposer>();
        services.AddSingleton<IFixGenerator, FixGenerator>();
        services.AddSingleton<IBrickRecompiler, BrickRecompiler>();
        services.AddSingleton<IBehaviorRewirer, BehaviorRewirer>();
        services.AddSingleton<INewBrickGenerator, NewBrickGenerator>();
        services.AddSingleton<INewBehaviorAssembler, NewBehaviorAssembler>();
        services.AddSingleton<ISourceCodeFixer, EmptyCatchCodeFixer>();

        // Block 4: inheritance system
        var adaptationDbPath = !string.IsNullOrEmpty(patternStorePath)
            ? Path.Combine(Path.GetDirectoryName(patternStorePath) ?? ".", "nexo-adaptation.db")
            : Path.Combine(RepoPathResolver.FindRepoRoot(), "nexo-adaptation.db");
        services.AddSingleton<IAdaptationLog>(sp => new LiteDbAdaptationLog(adaptationDbPath));
        services.AddSingleton<IAdaptationPromoter, AdaptationPromoter>();
        services.AddSingleton<IInstanceResultAggregator, InstanceResultAggregator>();
        services.AddSingleton<IConflictResolver, ConflictResolver>();
        services.AddSingleton<ICoreVersionManager>(sp => new CoreVersionManager(RepoPathResolver.FindRepoRoot()));
        services.AddTransient<AdaptationRollbackHelper>();

        // Block 5: autonomy controls
        var auditDbPath = !string.IsNullOrEmpty(patternStorePath)
            ? Path.Combine(Path.GetDirectoryName(patternStorePath) ?? ".", "nexo-adaptation-audit.db")
            : Path.Combine(RepoPathResolver.FindRepoRoot(), "nexo-adaptation-audit.db");
        services.AddSingleton<IAdaptationAuditLog>(sp => new LiteDbAdaptationAuditLog(auditDbPath));
        services.AddSingleton<IUserFeedbackCapture, CliUserFeedbackCapture>();

        return services;
    }
}
