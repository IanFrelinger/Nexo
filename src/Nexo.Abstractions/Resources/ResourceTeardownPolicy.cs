namespace Nexo.Abstractions.Resources;

/// <summary>
/// How an orchestration resource scope orders disposal when multiple resource kinds are tracked.
/// Within each partition, resources are disposed in reverse registration order (LIFO).
/// </summary>
public enum ResourceTeardownPolicy
{
    /// <summary>
    /// Dispose all agent handles first, then all isolated databases.
    /// </summary>
    AgentsBeforeDatabases = 0,

    /// <summary>
    /// Dispose all isolated databases first, then all agent handles.
    /// </summary>
    DatabasesBeforeAgents = 1
}
