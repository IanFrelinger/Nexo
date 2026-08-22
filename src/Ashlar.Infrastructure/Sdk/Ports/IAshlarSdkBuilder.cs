using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Sdk.Ports;

/// <summary>
/// Fluent builder for registering external components (bricks, agents) with Ashlar at runtime.
/// Call before <c>AddAshlar()</c>. Enables runtime registration without recompiling Ashlar.
/// Default implementation in the Ashlar.Hosting.Sdk assembly (<c>HostAshlarSdkBuilder</c>).
/// </summary>
public interface IAshlarSdkBuilder
{
    /// <summary>
    /// Registers a brick type. The brick will be available in the brick registry at runtime.
    /// Call before AddAshlar().
    /// </summary>
    /// <typeparam name="T">DomainBrick type (must inherit from <see cref="DomainBrick"/>).</typeparam>
    /// <returns>This builder for chaining.</returns>
    IAshlarSdkBuilder RegisterBrick<T>() where T : DomainBrick;

    /// <summary>
    /// Registers an agent type (e.g. implementing Ashlar.Abstractions.IAgent).
    /// The agent will be discoverable by the agent executor.
    /// </summary>
    /// <typeparam name="T">Agent type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    IAshlarSdkBuilder RegisterAgent<T>() where T : class;

    /// <summary>
    /// Registers an agent card for workflow execution.
    /// Agent cards define personas with behaviors for the behavior executor.
    /// </summary>
    /// <param name="card">The agent card to register.</param>
    /// <returns>This builder for chaining.</returns>
    IAshlarSdkBuilder RegisterAgentCard(AgentCard card);
}
