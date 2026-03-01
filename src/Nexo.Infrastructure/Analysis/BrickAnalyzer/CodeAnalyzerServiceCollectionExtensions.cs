using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis.BrickAnalyzer;

namespace Nexo.Infrastructure.Analysis;

/// <summary>
/// DI registration for Block 2 code analyzers.
/// </summary>
public static class CodeAnalyzerServiceCollectionExtensions
{
    /// <summary>
    /// Adds Block 2 code analyzer services.
    /// </summary>
    public static IServiceCollection AddCodeAnalyzers(this IServiceCollection services)
    {
        services.AddSingleton<IBrickStaticAnalyzer, RoslynBrickStaticAnalyzer>();
        services.AddSingleton<IBehavioralAnalyzer, BehavioralAnalyzer>();
        services.AddSingleton<ISelfAnalysisLogger, InMemorySelfAnalysisLogger>();
        services.AddSingleton<IPermissionScopeEnforcer, PermissionScopeEnforcer>();
        services.AddSingleton<IRegressionTestRunner, DotNetRegressionTestRunner>();
        return services;
    }
}
