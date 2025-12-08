using System.Text;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Builds domain-specific prompts for agent execution.
/// </summary>
public sealed class DomainPromptBuilder
{
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

