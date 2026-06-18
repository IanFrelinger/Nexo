using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;

namespace Nexo.Spike.S0.Roles;

/// <summary>
/// Agent hook for a single loop stage. Spike uses file-based agents; production wires LLM roles.
/// </summary>
public interface IStageAgent
{
    string RoleName { get; }

    /// <summary>
    /// Produce or update artifacts for the stage within the scoped workspace.
    /// Returns false when the agent cannot proceed (e.g. ambiguous spec).
    /// </summary>
    Task<StageAgentResult> ExecuteAsync(
        SpikeStage stage,
        RoleView scope,
        string workspaceRoot,
        IReadOnlyList<string>? mutationFeedback,
        CancellationToken ct = default);
}

public sealed record StageAgentResult(
    bool Success,
    string Summary,
    IReadOnlyList<string> OpenQuestions)
{
    public static StageAgentResult Ok(string summary) => new(true, summary, []);
    public static StageAgentResult Blocked(string summary, params string[] questions) =>
        new(false, summary, questions);
}
