using Nexo.Core.Domain.Agents;

namespace Nexo.Hosting.Sdk.Options;
/// <summary>
/// Options for SDK-registered components. Populated by INexoSdkBuilder.
/// </summary>
public sealed class NexoSdkOptions
{
    /// <summary>
    /// Brick types to register in the adaptation pipeline.
    /// </summary>
    public List<Type> BrickTypes { get; } = new();

    /// <summary>
    /// Agent types (IAgent implementations) to register in DI.
    /// </summary>
    public List<Type> AgentTypes { get; } = new();

    /// <summary>
    /// Agent cards to register for workflow execution.
    /// </summary>
    public List<AgentCard> AgentCards { get; } = new();
}
