using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Application.Sdk.Ports;

/// <summary>
/// Fluent builder for registering external components (bricks, agents) with Nexo at runtime.
/// Call before AddNexo(). Enables runtime registration without recompiling Nexo.
/// </summary>
public interface INexoSdkBuilder
{
    /// <summary>
    /// Registers a brick type. The brick will be available in the brick registry at runtime.
    /// Call before AddNexo().
    /// </summary>
    /// <typeparam name="T">Brick type (must inherit from <see cref="Brick"/>).</typeparam>
    /// <returns>This builder for chaining.</returns>
    INexoSdkBuilder RegisterBrick<T>() where T : Brick;

    /// <summary>
    /// Registers an agent type (e.g. implementing Nexo.Abstractions.IAgent).
    /// The agent will be discoverable by the agent executor.
    /// </summary>
    /// <typeparam name="T">Agent type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    INexoSdkBuilder RegisterAgent<T>() where T : class;

    /// <summary>
    /// Registers an agent card for workflow execution.
    /// Agent cards define personas with behaviors for the behavior executor.
    /// </summary>
    /// <param name="card">The agent card to register.</param>
    /// <returns>This builder for chaining.</returns>
    INexoSdkBuilder RegisterAgentCard(AgentCard card);
}
