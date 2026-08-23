using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Common.Services;
using Ashlar.Core.Application.Interfaces;
using Ashlar.Core.Application.Orchestration;

namespace Ashlar.Core.Application.Host;

/// <summary>
/// Lightweight application host for running commands outside the full Ashlar kernel.
/// Useful for tests, samples, and minimal CLI scenarios.
/// </summary>
public sealed class ServiceHost
{
    private readonly IServiceProvider _sp;

    /// <summary>Creates a host backed by an existing service provider.</summary>
    public ServiceHost(IServiceProvider sp) => _sp = sp;

    /// <summary>
    /// Builds a default service provider with logging, loop kernel, and command orchestration.
    /// </summary>
    /// <param name="configure">Optional callback to register additional services.</param>
    public static IServiceProvider BuildDefault(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoopKernel, SequentialLoopKernel>();
        services.AddSingleton<IOrchestrator, GenericCommandOrchestrator>();
        services.AddSingleton<GenericCommandOrchestrator>();
        services.AddSingleton<IPreValidator, NoopPreValidator>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>Runs a command through the registered orchestrator.</summary>
    public async ValueTask<OrchestrationResult> RunAsync<TIn, TOut>(ICommand<TIn, TOut> cmd, TIn input, CancellationToken ct)
        => await _sp.GetRequiredService<IOrchestrator>().RunAsync(cmd, input, ct);
}
