using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Planning;

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
