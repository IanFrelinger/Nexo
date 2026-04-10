using System.Text;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Architect.Prompts;

/// <summary>
/// Builds prompts for the Architect Agent to decompose requests.
/// 
/// Responsibilities:
/// - Builds system prompts for decomposition
/// - Builds user prompts with request, context, and examples
/// - Includes domain hints and resource budgets
/// - Formats prompts for LLM consumption
/// 
/// Used by ArchitectAgent to generate prompts for request decomposition.
/// </summary>
public sealed class DecompositionPromptBuilder
{
    /// <summary>
    /// Builds the system prompt for decomposition.
    /// 
    /// Creates a comprehensive system prompt that instructs the LLM on how to decompose
    /// requests into agent specifications, including guidelines and output format.
    /// </summary>
    /// <returns>The system prompt string.</returns>
    public string BuildSystemPrompt()
    {
        return """
            You are an Architect Agent that decomposes complex requests into specialized agent specifications.
            
            Your task is to:
            1. Analyze the user's request
            2. Identify the domains involved (Combat, Economy, AI, Infrastructure, Security, Gameplay, etc.)
            3. Break down the request into discrete, specialized agent tasks
            4. Define dependencies between agents
            5. Specify constraints and resource requirements
            
            Output your decomposition as a JSON object matching this schema:
            {
              "agents": [
                {
                  "agentId": "unique-id",
                  "domain": "DomainName",
                  "goal": "Clear goal statement",
                  "goals": ["Goal 1", "Goal 2"],
                  "description": "Detailed description",
                  "clusterId": "optional-cluster-id",
                  "reportsToAgentId": "optional-supervisor-agent-id",
                  "commandChain": ["supervisor-agent-id", "director-agent-id"],
                  "ollamaModel": "optional-ollama-model-name",
                  "dependencies": ["other-agent-id"],
                  "outputSchema": { ... },
                  "constraints": [
                    {
                      "type": "ConstraintType",
                      "description": "Constraint description",
                      "isMandatory": true
                    }
                  ],
                  "resourceRequirements": {
                    "estimatedComputeSeconds": 300,
                    "requiredContextTokens": 4000,
                    "requiredMemoryMB": 512
                  },
                  "priority": 0
                }
              ],
              "reasoning": "Explanation of the decomposition",
              "confidence": 0.9
            }
            
            Guidelines:
            - Each agent should have a clear, focused goal
            - Use "clusterId" to group agents that belong to the same work cluster
            - Use "reportsToAgentId"/"commandChain" only when a chain of command is needed
            - Use "ollamaModel" when a specific agent should run on a specific Ollama model
            - Dependencies should reflect actual data/result dependencies
            - Resource requirements should be realistic estimates
            - Priority: higher numbers = more critical
            - Ensure all aspects of the request are covered by at least one agent
            """;
    }

    /// <summary>
    /// Builds the user prompt with request and context.
    /// 
    /// Includes:
    /// - The original request
    /// - Similar examples (if available)
    /// - Domain hints (if available)
    /// - Resource budget constraints (if available)
    /// 
    /// Used for initial decomposition attempts.
    /// </summary>
    /// <param name="request">The user request to decompose.</param>
    /// <param name="context">Optional decomposition context with examples and hints.</param>
    /// <returns>The formatted user prompt string.</returns>
    public string BuildUserPrompt(string request, DecompositionContext? context = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Request: {request}");
        sb.AppendLine();

        if (context != null && context.SimilarExamples.Count > 0)
        {
            sb.AppendLine("Similar examples:");
            foreach (var example in context.SimilarExamples.Take(3))
            {
                sb.AppendLine($"- Request: {example.OriginalRequest}");
                sb.AppendLine($"  Agents: {example.Agents.Count}");
                sb.AppendLine($"  Domains: {string.Join(", ", example.Agents.Select(a => a.Domain).Distinct())}");
            }
            sb.AppendLine();
        }

        if (context != null && context.DomainHints.Count > 0)
        {
            sb.AppendLine($"Domain hints: {string.Join(", ", context.DomainHints)}");
            sb.AppendLine();
        }

        if (context != null && context.ResourceBudget != null)
        {
            sb.AppendLine("Resource budget:");
            if (context.ResourceBudget.MaxComputeSeconds.HasValue)
            {
                sb.AppendLine($"  Max compute: {context.ResourceBudget.MaxComputeSeconds} seconds");
            }
            if (context.ResourceBudget.MaxContextTokens.HasValue)
            {
                sb.AppendLine($"  Max context: {context.ResourceBudget.MaxContextTokens} tokens");
            }
            if (context.ResourceBudget.MaxMemoryMB.HasValue)
            {
                sb.AppendLine($"  Max memory: {context.ResourceBudget.MaxMemoryMB} MB");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Please decompose this request into agent specifications in JSON format.");

        return sb.ToString();
    }

    /// <summary>
    /// Builds a correction prompt when validation fails.
    /// 
    /// Creates a prompt that:
    /// - Explains that the previous decomposition had validation errors
    /// - Lists all validation errors with details
    /// - Includes the previous decomposition for reference
    /// - Asks for a corrected decomposition
    /// 
    /// Used in the self-correction loop of ArchitectAgent.
    /// </summary>
    /// <param name="originalRequest">The original user request.</param>
    /// <param name="failedResult">The failed decomposition result.</param>
    /// <param name="errors">The list of validation errors.</param>
    /// <returns>The formatted correction prompt string.</returns>
    public string BuildCorrectionPrompt(string originalRequest, DecompositionResult failedResult, IReadOnlyList<ValidationError> errors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Your previous decomposition had validation errors. Please correct them.");
        sb.AppendLine();
        sb.AppendLine($"Original request: {originalRequest}");
        sb.AppendLine();
        sb.AppendLine("Validation errors:");
        foreach (var error in errors)
        {
            sb.AppendLine($"- [{error.ErrorType}] {error.Message}");
            if (!string.IsNullOrEmpty(error.AgentId))
            {
                sb.AppendLine($"  Agent: {error.AgentId}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("Previous decomposition (for reference):");
        sb.AppendLine(SerializeDecompositionResult(failedResult));
        sb.AppendLine();
        sb.AppendLine("Please provide a corrected decomposition in JSON format.");

        return sb.ToString();
    }

    private static string SerializeDecompositionResult(DecompositionResult result)
    {
        // Simple serialization for prompt context
        var sb = new StringBuilder();
        sb.AppendLine($"Agents: {result.Agents.Count}");
        foreach (var agent in result.Agents)
        {
            sb.AppendLine($"  - {agent.AgentId}: {agent.Domain} - {agent.Goal}");
            if (agent.Dependencies.Count > 0)
            {
                sb.AppendLine($"    Dependencies: {string.Join(", ", agent.Dependencies)}");
            }
        }
        return sb.ToString();
    }
}

