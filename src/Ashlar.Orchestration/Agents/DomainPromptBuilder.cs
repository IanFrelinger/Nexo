using System.Text;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// Builds domain-specific prompts for agent execution.
/// 
/// Responsibilities:
/// - Builds execution prompts from AgentSpawnSpec
/// - Includes goal, description, and constraints
/// - Incorporates dependency outputs from other agents
/// - Formats output schema requirements
/// 
/// Used by BaseDomainAgent to generate prompts for LLM-based agents.
/// </summary>
public sealed class DomainPromptBuilder
{
    /// <summary>
    /// Builds an execution prompt for a domain agent.
    /// 
    /// Creates a comprehensive prompt that includes:
    /// - Agent role and domain
    /// - Goal and description
    /// - Constraints (with mandatory indicators)
    /// - Dependency outputs from other agents
    /// - Expected output schema
    /// 
    /// Used by BaseDomainAgent to generate prompts for LLM-based agents.
    /// </summary>
    /// <param name="spec">The agent spawn specification.</param>
    /// <param name="dependencyOutputs">Optional dictionary of dependency agent IDs to their outputs.</param>
    /// <returns>The formatted execution prompt string.</returns>
    public string BuildExecutionPrompt(
        AgentSpawnSpec spec,
        IReadOnlyDictionary<string, object>? dependencyOutputs = null)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"You are a specialized {spec.Domain} domain agent.");
        sb.AppendLine();
        sb.AppendLine($"Your goal: {spec.Goal}");
        
        if (!string.IsNullOrWhiteSpace(spec.Description))
        {
            sb.AppendLine($"Description: {spec.Description}");
        }
        
        if (spec.Constraints != null && spec.Constraints.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Constraints:");
            foreach (var constraint in spec.Constraints)
            {
                var mandatory = constraint.IsMandatory ? " (MANDATORY)" : "";
                sb.AppendLine($"- {constraint.Description}{mandatory}");
            }
        }
        
        if (dependencyOutputs != null && dependencyOutputs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Dependency Outputs (from other agents):");
            foreach (var (agentId, output) in dependencyOutputs)
            {
                sb.AppendLine($"- {agentId}: {SerializeOutput(output)}");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("Please provide a detailed design/implementation for your goal. Output your response as JSON matching the expected output schema.");
        
        if (spec.OutputSchema.HasValue)
        {
            sb.AppendLine();
            sb.AppendLine("Expected Output Schema:");
            sb.AppendLine(SerializeOutputSchema(spec.OutputSchema.Value));
        }
        
        return sb.ToString();
    }
    
    private static string SerializeOutput(object output)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(output, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 2
            });
        }
        catch
        {
            return output.ToString() ?? "{}";
        }
    }
    
    private static string SerializeOutputSchema(System.Text.Json.JsonElement schema)
    {
        try
        {
            return schema.GetRawText();
        }
        catch
        {
            return "{}";
        }
    }
}

