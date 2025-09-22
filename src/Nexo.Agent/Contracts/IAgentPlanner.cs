using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Models;

namespace Nexo.Agent.Contracts;

/// <summary>
/// Interface for planning agent tasks into executable steps.
/// </summary>
public interface IAgentPlanner
{
    /// <summary>
    /// Creates a plan for achieving the given goal.
    /// </summary>
    /// <param name="goal">The user's goal</param>
    /// <param name="context">Additional context</param>
    /// <param name="availableTools">Currently available tools</param>
    /// <param name="mode">Agent mode (affects planning strategy)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A plan with executable steps</returns>
    Task<Plan> CreatePlanAsync(
        string goal,
        string? context,
        IReadOnlyList<ToolManifest> availableTools,
        AgentMode mode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies missing tools needed to execute the plan.
    /// </summary>
    /// <param name="plan">The plan to analyze</param>
    /// <param name="availableTools">Currently available tools</param>
    /// <returns>List of tool requests for missing tools</returns>
    IReadOnlyList<ToolRequest> IdentifyMissingTools(Plan plan, IReadOnlyList<ToolManifest> availableTools);
}
