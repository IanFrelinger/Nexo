using System.Text.Json.Serialization;

namespace Nexo.Agent.Models;

/// <summary>
/// A plan containing a sequence of tool invocations to achieve a goal.
/// </summary>
/// <param name="Id">Plan identifier</param>
/// <param name="Goal">Original goal</param>
/// <param name="Context">Additional context</param>
/// <param name="Steps">Sequence of tool invocations</param>
/// <param name="CreatedAt">Plan creation timestamp</param>
/// <param name="EstimatedDuration">Estimated total execution time</param>
public record Plan(
    string Id,
    string Goal,
    string? Context,
    IReadOnlyList<ToolInvocation> Steps,
    DateTime CreatedAt,
    TimeSpan EstimatedDuration)
{
    /// <summary>
    /// Creates a new plan.
    /// </summary>
    public static Plan Create(string goal, string? context = null, IReadOnlyList<ToolInvocation>? steps = null)
    {
        return new Plan(
            Id: Guid.NewGuid().ToString(),
            Goal: goal,
            Context: context,
            Steps: steps ?? Array.Empty<ToolInvocation>(),
            CreatedAt: DateTime.UtcNow,
            EstimatedDuration: TimeSpan.Zero
        );
    }

    /// <summary>
    /// Gets the total number of steps in the plan.
    /// </summary>
    public int StepCount => Steps.Count;

    /// <summary>
    /// Checks if the plan has any steps.
    /// </summary>
    public bool HasSteps => Steps.Count > 0;

    /// <summary>
    /// Gets all unique tool IDs used in this plan.
    /// </summary>
    public IReadOnlyList<string> GetRequiredToolIds()
    {
        return Steps.Select(s => s.ToolId).Distinct().ToList();
    }
}

/// <summary>
/// A single tool invocation within a plan.
/// </summary>
/// <param name="StepNumber">Step number in the plan (1-based)</param>
/// <param name="ToolId">Tool identifier to invoke</param>
/// <param name="Input">JSON input for the tool</param>
/// <param name="Description">Human-readable description of this step</param>
/// <param name="DependsOn">Previous step numbers this step depends on</param>
/// <param name="Timeout">Step-specific timeout (overrides tool default)</param>
public record ToolInvocation(
    int StepNumber,
    string ToolId,
    string Input,
    string Description,
    IReadOnlyList<int> DependsOn,
    TimeSpan? Timeout = null)
{
    /// <summary>
    /// Creates a tool invocation.
    /// </summary>
    public static ToolInvocation Create(
        int stepNumber,
        string toolId,
        string input,
        string description,
        IReadOnlyList<int>? dependsOn = null,
        TimeSpan? timeout = null)
    {
        return new ToolInvocation(
            StepNumber: stepNumber,
            ToolId: toolId,
            Input: input,
            Description: description,
            DependsOn: dependsOn ?? Array.Empty<int>(),
            Timeout: timeout
        );
    }

    /// <summary>
    /// Checks if this step depends on a specific step number.
    /// </summary>
    /// <param name="stepNumber">Step number to check</param>
    /// <returns>True if this step depends on the specified step</returns>
    public bool DependsOnStep(int stepNumber)
    {
        return DependsOn.Contains(stepNumber);
    }
}
