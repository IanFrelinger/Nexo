using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agents;

namespace Nexo.Core.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNexoAgents(this IServiceCollection services)
    {
        services.AddSingleton<IAgentFactory, AgentFactory>();
        // services.AddTransient<YourAgent>(); // register concrete agents here
        return services;
    }
}
