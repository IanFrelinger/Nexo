using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Planning;

/// <summary>Structured response from the planning agent.</summary>
public class PlanningResponse
{
    /// <summary>Response category (question, recommendation, or error).</summary>
    public required PlanningResponseType Type { get; set; }

    /// <summary>Follow-up question when <see cref="Type"/> is <see cref="PlanningResponseType.Question"/>.</summary>
    public PlanningQuestion? Question { get; set; }

    /// <summary>Recommendation text when <see cref="Type"/> is <see cref="PlanningResponseType.Recommendation"/>.</summary>
    public string? Recommendation { get; set; }

    /// <summary>Feature suggestions surfaced during planning.</summary>
    public List<string> RecommendedFeatures { get; set; } = new();

    /// <summary>Suggested iteration priorities for subsequent development cycles.</summary>
    public List<string> IterationPriorities { get; set; } = new();

    /// <summary>Additional considerations or caveats for the operator.</summary>
    public List<string> Considerations { get; set; } = new();

    /// <summary>Planning progress indicator in the range 0.0 to 1.0.</summary>
    public double Progress { get; set; }

    /// <summary>Arbitrary context values carried across planning turns.</summary>
    public Dictionary<string, object> Context { get; set; } = new();
}
