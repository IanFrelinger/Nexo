namespace Nexo.Abstractions.Execution;

/// <summary>
/// Declares how strongly an agent invocation should be isolated from peers and the host process.
/// Hosts map each tier to concrete runtimes (in-process, child process, pooled worker container,
/// dedicated container per agent, external mesh worker, etc.).
/// </summary>
public enum AgentExecutionIsolationLevel
{
    /// <summary>
    /// Same CLR as the orchestrator; <c>AgentContainer</c> is a logical wrapper only (default).
    /// </summary>
    InProcess = 0,

    /// <summary>
    /// Separate OS process on the host; shared kernel, distinct address space.
    /// </summary>
    OutOfProcess = 1,

    /// <summary>
    /// Container-based execution with pooling (shared image, slot-per-invocation or session lease).
    /// </summary>
    ContainerPooled = 2,

    /// <summary>
    /// Dedicated container (or equivalent sandbox) for this agent instance for the invocation lifecycle.
    /// Strongest isolation; highest per-agent overhead.
    /// </summary>
    ContainerPerAgent = 3,
}
