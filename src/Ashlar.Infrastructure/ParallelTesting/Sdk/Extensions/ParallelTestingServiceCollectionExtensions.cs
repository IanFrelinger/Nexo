using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.ParallelTesting.Ports;
using Ashlar.Infrastructure.ParallelTesting;

namespace Ashlar.Infrastructure.ParallelTesting.Sdk.Extensions;
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
