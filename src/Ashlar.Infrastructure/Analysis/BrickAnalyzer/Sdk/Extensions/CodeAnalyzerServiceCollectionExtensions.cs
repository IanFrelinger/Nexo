using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Infrastructure.Analysis.BrickAnalyzer;

namespace Ashlar.Infrastructure.Analysis.BrickAnalyzer.Sdk.Extensions;
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
