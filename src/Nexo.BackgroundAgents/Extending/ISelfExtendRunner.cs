namespace Nexo.BackgroundAgents.Extending;

/// <summary>
/// Abstraction for running a self-extend cycle from a background agent.
/// The host supplies an implementation that builds a toolbox (e.g. repo.fs.write, repo.fs.search_replace),
/// policy (path allowlist, max write size), and an LLM-backed tool-calling agent, then runs ThinkAsync and executes approved tool calls.
/// Used so the framework can extend its own codebase within guardrails.
/// </summary>
public interface ISelfExtendRunner
{
    /// <summary>
    /// Run one self-extend cycle: agent thinks (LLM) and executes approved tool calls.
    /// </summary>
    /// <param name="repoRoot">Repository root path for world state and tool arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary result for logging and agent use.</returns>
    Task<SelfExtendRunResult> RunAsync(string repoRoot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run one self-extend cycle with an explicit objective and agent role name. Used by
    /// background agents whose config carries an <c>Objective</c> and/or <c>AgentName</c>
    /// parameter — without these the LLM has no goal context and rationally returns no actions.
    ///
    /// <para>Default implementation delegates to the single-arg overload so existing test
    /// fakes continue to compile without changes.</para>
    /// </summary>
    Task<SelfExtendRunResult> RunAsync(
        string repoRoot,
        string? objective,
        string? agentName,
        CancellationToken cancellationToken = default)
        => RunAsync(repoRoot, cancellationToken);

    /// <summary>
    /// Run one self-extend cycle with explicit objective, agent role name, and model routing
    /// (provider/model). Without provider routing the underlying model chain (HotSwappableModel)
    /// falls back to a deterministic echo and the agent never actually calls the configured LLM.
    ///
    /// <para>Default implementation delegates to the lower-arity overload so existing test fakes
    /// continue to compile without changes.</para>
    /// </summary>
    Task<SelfExtendRunResult> RunAsync(
        string repoRoot,
        string? objective,
        string? agentName,
        string? modelProvider,
        string? modelName,
        CancellationToken cancellationToken = default)
        => RunAsync(repoRoot, objective, agentName, modelProvider, modelName, agentId: null, cancellationToken);

    /// <summary>
    /// Run one self-extend cycle with explicit registry agent id for policy enforcement.
    /// </summary>
    Task<SelfExtendRunResult> RunAsync(
        string repoRoot,
        string? objective,
        string? agentName,
        string? modelProvider,
        string? modelName,
        string? agentId,
        CancellationToken cancellationToken = default)
        => RunAsync(repoRoot, objective, agentName, modelProvider, modelName, cancellationToken);
}
