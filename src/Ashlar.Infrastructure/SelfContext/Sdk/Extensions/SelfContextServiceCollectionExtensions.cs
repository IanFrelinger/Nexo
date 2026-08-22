using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Knowledge.Ports;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Infrastructure.Knowledge;
using Ashlar.Infrastructure.Observation;
using Ashlar.Infrastructure.Observation.Sdk.Extensions;
using Ashlar.Infrastructure.SelfContext;

namespace Ashlar.Infrastructure.SelfContext.Sdk.Extensions;
/// <summary>
/// DI extensions for Block 6 self-context.
/// </summary>
public static class SelfContextServiceCollectionExtensions
{
    /// <summary>
    /// Adds execution tracer and self-context assembler.
    /// When <paramref name="patternStorePath"/> is provided, adds observation core so IPatternStore is available.
    /// Requires IAdaptationLog to be registered (from AddAdaptationInfrastructure).
    /// </summary>
    public static IServiceCollection AddSelfContextInfrastructure(this IServiceCollection services, string? patternStorePath = null)
    {
        if (!string.IsNullOrEmpty(patternStorePath))
            services.AddObservationCore(patternStorePath);
        else
            services.AddSingleton<IPatternStore, EmptyPatternStore>();

        // Co-located with the pattern store; otherwise the state directory (ASHLAR_STATE_DIR, else
        // <repo root>/.ashlar/state) so LiteDB files never land in the CWD / repo root.
        var basePath = !string.IsNullOrEmpty(patternStorePath)
            ? Path.GetDirectoryName(patternStorePath) ?? "."
            : RepoPathResolver.ResolveStateDirectory();
        var tracerDbPath = Path.Combine(basePath, "ashlar-execution.db");
        var testFailuresDbPath = Path.Combine(basePath, "ashlar-test-failures.db");
        services.AddSingleton<IExecutionTracer>(sp => new LiteDbExecutionTracer(tracerDbPath));
        services.AddSingleton<ITestFailureStore>(sp => new LiteDbTestFailureStore(testFailuresDbPath));
        services.AddSingleton<ISelfContextAssembler, SelfContextAssembler>();
        services.AddSingleton<IKnowledgeQueryService>(sp =>
        {
            var adaptationLog = sp.GetRequiredService<IAdaptationLog>();
            var patternStore = sp.GetRequiredService<IPatternStore>();
            var userKnowledgeStore = sp.GetService<IUserKnowledgeLogStore>()
                ?? new Ashlar.Infrastructure.Trust.InMemoryUserKnowledgeLogStore();
            return new KnowledgeQueryService(adaptationLog, patternStore, userKnowledgeStore);
        });
        services.AddChangelogGenerator();
        services.AddDocumentationUpdater();
        return services;
    }

    /// <summary>
    /// Adds IChangelogGenerator. Requires IAdaptationLog (from AddAdaptationInfrastructure). Phase F.
    /// </summary>
    public static IServiceCollection AddChangelogGenerator(this IServiceCollection services)
    {
        services.AddSingleton<IChangelogGenerator, ChangelogGenerator>();
        return services;
    }

    /// <summary>
    /// Adds IDocumentationUpdater. Requires IAdaptationLog and IChangelogGenerator. Phase F.
    /// </summary>
    public static IServiceCollection AddDocumentationUpdater(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentationUpdater, DocumentationUpdater>();
        return services;
    }
}
