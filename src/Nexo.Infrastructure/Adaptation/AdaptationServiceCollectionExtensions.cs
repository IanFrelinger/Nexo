using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Rollback;

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

        services.AddOptions<AdaptationBrickOptions>();

        services.AddSingleton<Nexo.Core.Domain.Execution.IBrickRegistry>(sp =>
        {
            var bricks = new List<Brick>();
            if (patternStorePath != null)
            {
                var contextAssembler = sp.GetRequiredService<IContextAssembler>();
                bricks.Add(new ObservationContextBrick(contextAssembler));
            }

            var options = sp.GetService<IOptions<AdaptationBrickOptions>>();
            if (options?.Value?.AdditionalBrickTypes is { Count: > 0 } types)
            {
                foreach (var type in types)
                {
                    try
                    {
                        var brick = (Brick?)ActivatorUtilities.CreateInstance(sp, type);
                        if (brick != null)
                            bricks.Add(brick);
                    }
                    catch (Exception)
                    {
                        // Skip bricks that fail to instantiate (missing DI, etc.)
                    }
                }
            }

            return new Nexo.Infrastructure.Execution.BrickRegistry(bricks);
        });

        services.AddSingleton<IBrickDecomposer, BrickDecomposer>();
        services.AddSingleton<IFixGenerator, FixGenerator>();
        services.AddSingleton<IBrickRecompiler, BrickRecompiler>();
        services.AddSingleton<IBehaviorRewirer, BehaviorRewirer>();
        services.AddSingleton<INewBrickGenerator>(sp => new NewBrickGenerator(sp.GetService<IAdaptationLog>()));
        services.AddSingleton<INewBehaviorAssembler, NewBehaviorAssembler>();
        services.AddSingleton<ISourceCodeFixer, EmptyCatchCodeFixer>();
        services.AddSingleton<IImmutableCoreRegistry, ImmutableCoreRegistry>();

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

        // Rollback infrastructure (P0.3)
        var snapshotPath = !string.IsNullOrEmpty(patternStorePath)
            ? Path.Combine(Path.GetDirectoryName(patternStorePath) ?? ".", "nexo-snapshots")
            : Path.Combine(RepoPathResolver.FindRepoRoot(), "nexo-snapshots");
        services.AddRollbackInfrastructure(snapshotPath);

        return services;
    }

    /// <summary>
    /// Registers additional brick types for the adaptation pipeline.
    /// Bricks are resolved via DI (e.g. <see cref="IProviderFactory"/> for OWASPScannerBrick).
    /// Call after <see cref="AddAdaptationInfrastructure"/> and ensure required services (e.g. IProviderFactory) are registered.
    /// </summary>
    public static IServiceCollection AddAdaptationBricks(this IServiceCollection services, params Type[] brickTypes)
    {
        if (brickTypes.Length == 0) return services;
        services.Configure<AdaptationBrickOptions>(o => o.AdditionalBrickTypes.AddRange(brickTypes));
        return services;
    }
}
