using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.ParallelTesting.Ports;
using Nexo.Infrastructure.ParallelTesting;

namespace Nexo.Infrastructure.Sdk.ParallelTesting;

/// <summary>
/// DI extensions for Block 8 parallel testing.
/// </summary>
public static class ParallelTestingServiceCollectionExtensions
{
    /// <summary>
    /// Adds instance spawner, result collector, parameter matrix generator, convergence detector.
    /// </summary>
    public static IServiceCollection AddParallelTestingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IInstanceSpawner, DotNetInstanceSpawner>();
        services.AddSingleton<IResultCollector, ResultCollector>();
        services.AddSingleton<IParameterMatrixGenerator, ParameterMatrixGenerator>();
        services.AddSingleton<IConvergenceDetector, ConvergenceDetector>();
        return services;
    }
}
