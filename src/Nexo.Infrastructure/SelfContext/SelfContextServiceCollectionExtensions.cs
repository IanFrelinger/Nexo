using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.SelfContext;

namespace Nexo.Infrastructure;

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

        var basePath = !string.IsNullOrEmpty(patternStorePath)
            ? Path.GetDirectoryName(patternStorePath) ?? "."
            : RepoPathResolver.FindRepoRoot();
        var tracerDbPath = Path.Combine(basePath, "nexo-execution.db");
        var testFailuresDbPath = Path.Combine(basePath, "nexo-test-failures.db");
        services.AddSingleton<IExecutionTracer>(sp => new LiteDbExecutionTracer(tracerDbPath));
        services.AddSingleton<ITestFailureStore>(sp => new LiteDbTestFailureStore(testFailuresDbPath));
        services.AddSingleton<ISelfContextAssembler, SelfContextAssembler>();
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
