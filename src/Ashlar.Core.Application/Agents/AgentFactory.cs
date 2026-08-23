using Microsoft.Extensions.DependencyInjection;

namespace Ashlar.Core.Application.Agents;

/// <summary>DI-backed implementation of <see cref="IAgentFactory"/>.</summary>
public sealed class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _sp;

    /// <summary>Creates a factory backed by the supplied service provider.</summary>
    public AgentFactory(IServiceProvider sp) => _sp = sp;

    /// <inheritdoc />
    public T Create<T>() where T : class => _sp.GetRequiredService<T>();
}
