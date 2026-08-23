using Ashlar.Core.Domain.Agents;

// Namespace is deliberately Ashlar.Hosting.Sdk (its pre-#223 home), not the folder
// path. See the note in AshlarHostingOptions.cs.
namespace Ashlar.Hosting.Sdk;
/// <summary>
/// Options for SDK-registered components. Populated by IAshlarSdkBuilder.
/// </summary>
public sealed class AshlarSdkOptions
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
