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
}
