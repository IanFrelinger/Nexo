namespace Nexo.Abstractions.Resources;

/// <summary>
/// Classifies provisioned resources for teardown ordering and diagnostics.
/// </summary>
public enum ProvisionedResourceKind
{
    None = 0,
    AgentHandle = 1,
    IsolatedDatabase = 2
}
