using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Planning;

/// <summary>
/// Type of planning response.
/// 
/// Defines response types:
/// - Question: Agent is asking a question
/// - Recommendation: Agent is providing a recommendation
/// - Error: An error occurred
/// 
/// Used by PlanningResponse to indicate the type of response.
/// </summary>
public enum PlanningResponseType
{
    Question,
    Recommendation,
    Error
}
