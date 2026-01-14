using Nexo.Agents.AutonomousDev.Models;

namespace Nexo.Agents.AutonomousDev;

/// <summary>
/// Callback interface for interactive approval in supervised mode.
/// </summary>
public interface IApprovalCallback
{
    /// <summary>
    /// Called when the agent needs approval for open questions.
    /// </summary>
    Task<bool> ApproveOpenQuestionsAsync(Specification specification, CancellationToken ct = default);
    
    /// <summary>
    /// Called when the agent needs approval for a development plan.
    /// </summary>
    Task<bool> ApprovePlanAsync(DevelopmentPlan plan, CancellationToken ct = default);
    
    /// <summary>
    /// Called when the agent needs approval before applying changes.
    /// </summary>
    Task<bool> ApproveChangesAsync(IReadOnlyList<GeneratedArtifact> artifacts, CancellationToken ct = default);
    
    /// <summary>
    /// Called when the agent needs clarification.
    /// </summary>
    Task<string?> GetClarificationAsync(string question, CancellationToken ct = default);
}
