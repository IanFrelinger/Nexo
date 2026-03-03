using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Core.Application.SelfImprovement.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.SelfImprovement;
using Nexo.Infrastructure.Trust;

namespace Nexo.Infrastructure;

/// <summary>
/// DI extensions for self-improvement loop.
/// </summary>
public static class SelfImprovementServiceCollectionExtensions
{
    /// <summary>
    /// Registers the self-improvement loop. Requires adaptation, self-context, and trust services.
    /// Adds IAccessBoundary if not already registered.
    /// </summary>
    public static IServiceCollection AddSelfImprovementLoop(this IServiceCollection services, int maxIterationsPerRun = 5)
    {
        services.AddAccessBoundary(null);
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
            maxIterationsPerRun));
        return services;
    }
}
