using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Core.Application.Agents;

public interface IAgentFactory
{
    T Create<T>() where T : class;
}

public sealed class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _sp;
    public AgentFactory(IServiceProvider sp) => _sp = sp;
    public T Create<T>() where T : class => _sp.GetRequiredService<T>();
}
