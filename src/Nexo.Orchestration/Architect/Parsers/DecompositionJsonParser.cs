using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Architect.Parsers;

/// <summary>
/// Parses JSON output from the model into DecompositionResult.
/// 
/// Responsibilities:
/// - Extracts JSON from markdown code blocks
/// - Parses JSON into DecompositionResult structure
/// - Handles parsing errors gracefully
/// - Validates parsed structure
/// 
/// Used by ArchitectAgent to parse LLM output into structured decomposition results.
/// </summary>
public sealed class DecompositionJsonParser
{
    private readonly ILogger<DecompositionJsonParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecompositionJsonParser"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DecompositionJsonParser(ILogger<DecompositionJsonParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
    }

    /// <summary>
    /// Parses model output text into a DecompositionResult.
    /// 
    /// Process:
    /// 1. Extracts JSON from markdown code blocks (if present)
    /// 2. Parses JSON into DecompositionResult structure
    /// 3. Handles parsing errors gracefully (returns minimal fallback decomposition on failure)
    /// 
    /// Returns a minimal fallback decomposition when parsing fails or model output is empty.
    /// </summary>
    /// <param name="modelOutput">The raw text output from the LLM model.</param>
    /// <param name="originalRequest">The original user request (for context).</param>
    /// <returns>A DecompositionResult if parsing succeeds, null otherwise.</returns>
    public DecompositionResult? Parse(string modelOutput, string originalRequest)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            _logger.LogWarning("Model output is empty");
            return BuildFallbackDecomposition(originalRequest, "Model output was empty");
        }

        try
        {
            // Try to extract JSON from markdown code blocks if present
            var jsonText = ExtractJsonFromMarkdown(modelOutput);
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                jsonText = modelOutput;
            }

            // Parse the JSON
            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            var agents = ParseAgents(root.GetProperty("agents"));
            var reasoning = root.TryGetProperty("reasoning", out var reasoningProp)
                ? reasoningProp.GetString()
                : null;
            var confidence = root.TryGetProperty("confidence", out var confidenceProp)
                ? confidenceProp.GetDouble()
                : 0.0;

            return new DecompositionResult
            {
                Agents = agents,
                OriginalRequest = originalRequest,
                Reasoning = reasoning,
                Confidence = confidence
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Provider output was not valid decomposition JSON; using fallback decomposition. {Reason}", ex.Message);
            return BuildFallbackDecomposition(originalRequest, "Invalid JSON from provider output");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Unexpected parser error; using fallback decomposition. {Reason}", ex.Message);
            return BuildFallbackDecomposition(originalRequest, "Unexpected parser error");
        }
    }

    private static DecompositionResult BuildFallbackDecomposition(string originalRequest, string reason)
    {
        var safeRequest = string.IsNullOrWhiteSpace(originalRequest) ? "request" : originalRequest.Trim();
        return new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "fallback-1",
                    Domain = "Gameplay",
                    Goal = $"Handle request: {safeRequest}",
                    Description = "Fallback decomposition used when provider output is not valid JSON.",
                    Dependencies = Array.Empty<string>(),
                    Constraints = Array.Empty<AgentConstraint>(),
                    Priority = 1
                }
            },
            OriginalRequest = originalRequest,
            Reasoning = $"Parser fallback: {reason}",
            Confidence = 0.2
        };
    }

    private IReadOnlyList<AgentSpawnSpec> ParseAgents(JsonElement agentsArray)
    {
        var agents = new List<AgentSpawnSpec>();

        foreach (var agentElement in agentsArray.EnumerateArray())
        {
            try
            {
                var agent = ParseAgent(agentElement);
                if (agent != null)
                {
                    agents.Add(agent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse agent from JSON");
            }
        }

        return agents;
    }

    private AgentSpawnSpec? ParseAgent(JsonElement agentElement)
    {
        try
        {
            var agentId = agentElement.GetProperty("agentId").GetString();
            var domain = agentElement.GetProperty("domain").GetString();
            var goal = agentElement.GetProperty("goal").GetString();

            if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(goal))
            {
                _logger.LogWarning("Agent missing required fields: agentId, domain, or goal");
                return null;
            }

            var description = agentElement.TryGetProperty("description", out var descProp)
                ? descProp.GetString()
                : null;

            var goals = agentElement.TryGetProperty("goals", out var goalsProp)
                ? goalsProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();
            if (goals.Count == 0 && !string.IsNullOrWhiteSpace(goal))
            {
                goals.Add(goal);
            }

            var clusterId = agentElement.TryGetProperty("clusterId", out var clusterProp)
                ? clusterProp.GetString()
                : null;

            var reportsToAgentId = agentElement.TryGetProperty("reportsToAgentId", out var reportsToProp)
                ? reportsToProp.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(reportsToAgentId)
                && agentElement.TryGetProperty("chainOfCommand", out var legacyChainOfCommandProp)
                && legacyChainOfCommandProp.ValueKind == JsonValueKind.Array)
            {
                reportsToAgentId = legacyChainOfCommandProp
                    .EnumerateArray()
                    .Select(e => e.GetString())
                    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            }

            var commandChain = agentElement.TryGetProperty("commandChain", out var chainProp)
                ? chainProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();
            if (commandChain.Count == 0
                && agentElement.TryGetProperty("chainOfCommand", out var legacyCommandChainProp)
                && legacyCommandChainProp.ValueKind == JsonValueKind.Array)
            {
                commandChain = legacyCommandChainProp
                    .EnumerateArray()
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            if (commandChain.Count == 0 && !string.IsNullOrWhiteSpace(reportsToAgentId))
            {
                commandChain.Add(reportsToAgentId);
            }

            var ollamaModel = agentElement.TryGetProperty("ollamaModel", out var ollamaModelProp)
                ? ollamaModelProp.GetString()
                : null;

            var dependencies = agentElement.TryGetProperty("dependencies", out var depsProp)
                ? depsProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();

            var outputSchema = agentElement.TryGetProperty("outputSchema", out var schemaProp)
                ? (JsonElement?)schemaProp
                : null;

            var constraints = agentElement.TryGetProperty("constraints", out var constraintsProp)
                ? ParseConstraints(constraintsProp)
                : new List<AgentConstraint>();

            var resourceRequirements = agentElement.TryGetProperty("resourceRequirements", out var reqsProp)
                ? ParseResourceRequirements(reqsProp)
                : null;

            var priority = agentElement.TryGetProperty("priority", out var priorityProp)
                ? priorityProp.GetInt32()
                : 0;
            var name = agentElement.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()
                : null;
            var requiredCapabilities = agentElement.TryGetProperty("requiredCapabilities", out var capabilitiesProp)
                ? capabilitiesProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();
            var metadata = agentElement.TryGetProperty("metadata", out var metadataProp)
                ? ParseMetadata(metadataProp)
                : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            return new AgentSpawnSpec
            {
                AgentId = agentId,
                Name = string.IsNullOrWhiteSpace(name) ? agentId : name!,
                Domain = domain,
                Goal = goal,
                Goals = goals,
                Description = description,
                ClusterId = clusterId,
                ReportsToAgentId = reportsToAgentId,
                CommandChain = commandChain,
                OllamaModel = ollamaModel,
                Dependencies = dependencies,
                OutputSchema = outputSchema,
                Constraints = constraints,
                ResourceRequirements = resourceRequirements,
                Priority = priority,
                RequiredCapabilities = requiredCapabilities,
                Metadata = metadata
            };
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Agent missing required property");
            return null;
        }
    }

    private IReadOnlyList<AgentConstraint> ParseConstraints(JsonElement constraintsArray)
    {
        var constraints = new List<AgentConstraint>();

        foreach (var constraintElement in constraintsArray.EnumerateArray())
        {
            try
            {
                var type = constraintElement.GetProperty("type").GetString();
                var description = constraintElement.GetProperty("description").GetString();
                var isMandatory = constraintElement.TryGetProperty("isMandatory", out var mandatoryProp)
                    ? mandatoryProp.GetBoolean()
                    : true;

                if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(description))
                {
                    constraints.Add(new AgentConstraint
                    {
                        Type = type,
                        Description = description,
                        IsMandatory = isMandatory
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse constraint");
            }
        }

        return constraints;
    }

    private ResourceRequirements? ParseResourceRequirements(JsonElement reqsElement)
    {
        try
        {
            var estimatedCompute = reqsElement.TryGetProperty("estimatedComputeSeconds", out var computeProp)
                ? computeProp.GetInt32()
                : (int?)null;

            var requiredContext = reqsElement.TryGetProperty("requiredContextTokens", out var contextProp)
                ? contextProp.GetInt32()
                : (int?)null;

            var requiredMemory = reqsElement.TryGetProperty("requiredMemoryMB", out var memoryProp)
                ? memoryProp.GetInt32()
                : (int?)null;

            if (estimatedCompute.HasValue || requiredContext.HasValue || requiredMemory.HasValue)
            {
                return new ResourceRequirements
                {
                    EstimatedComputeSeconds = estimatedCompute,
                    RequiredContextTokens = requiredContext,
                    RequiredMemoryMB = requiredMemory
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse resource requirements");
        }

        return null;
    }

    private Dictionary<string, object?> ParseMetadata(JsonElement metadataElement)
    {
        try
        {
            var value = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                metadataElement.GetRawText(),
                _jsonOptions);
            return value ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse metadata object");
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string ExtractJsonFromMarkdown(string text)
    {
        // Try to extract JSON from markdown code blocks
        var jsonBlockPattern = @"```(?:json)?\s*(\{.*?\})\s*```";
        var match = System.Text.RegularExpressions.Regex.Match(text, jsonBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        // Try to find JSON object boundaries
        var startIdx = text.IndexOf('{');
        var endIdx = text.LastIndexOf('}');
        if (startIdx >= 0 && endIdx > startIdx)
        {
            return text.Substring(startIdx, endIdx - startIdx + 1);
        }

        return string.Empty;
    }
}

