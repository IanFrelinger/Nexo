using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Core.Application.Agents;

/// <summary>Factory for resolving agent instances from DI.</summary>
public interface IAgentFactory
{
    /// <summary>Creates an agent instance of type <typeparamref name="T"/>.</summary>
    T Create<T>() where T : class;
}
