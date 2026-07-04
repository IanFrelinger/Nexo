using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Attribute to declare agent capabilities for registry discovery.
/// When present, AgentRegistryAdapter uses these instead of deriving from the type name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CapabilityAttribute : Attribute
{
    /// <summary>Declared capability identifiers exposed by the annotated agent type.</summary>
    public IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    /// Declares one or more capability identifiers for agent registry discovery.
    /// </summary>
    /// <param name="capabilities">Capability names used by routing and endpoint matching.</param>
    public CapabilityAttribute(params string[] capabilities)
    {
        Capabilities = capabilities;
    }
}
