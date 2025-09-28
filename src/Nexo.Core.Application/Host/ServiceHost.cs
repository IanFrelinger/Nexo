using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;

namespace Nexo.Core.Application.Host;

public sealed class ServiceHost
{
    private readonly IServiceProvider _sp;
    public ServiceHost(IServiceProvider sp) => _sp = sp;

    public static IServiceProvider BuildDefault(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOrchestrator, GenericCommandOrchestrator>();
        services.AddSingleton<GenericCommandOrchestrator>();
        services.AddSingleton<IPreValidator, NoopPreValidator>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public async ValueTask<OrchestrationResult> RunAsync<TIn,TOut>(ICommand<TIn,TOut> cmd, TIn input, CancellationToken ct)
        => await _sp.GetRequiredService<IOrchestrator>().RunAsync(cmd, input, ct);
}
