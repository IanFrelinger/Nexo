namespace Nexo.Abstractions.Resources;

/// <summary>
/// Classifies provisioned resources for teardown ordering and diagnostics.
/// </summary>
public enum ProvisionedResourceKind
{
    /// <summary>Unclassified or uninitialized resource handle.</summary>
    None = 0,

    /// <summary>Spawned agent runtime handle (<see cref="Agents.IAgentHandle"/>).</summary>
    AgentHandle = 1,

    /// <summary>Provisioned isolated database instance (<see cref="Database.IIsolatedDatabase"/>).</summary>
    IsolatedDatabase = 2
}
