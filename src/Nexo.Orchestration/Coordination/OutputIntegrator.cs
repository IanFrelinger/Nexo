using Microsoft.Extensions.Logging;
using System.Text.Json;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Integrates outputs from multiple agents into a unified result.
/// 
/// Responsibilities:
/// - Groups agent outputs by domain
/// - Merges outputs from agents in the same domain
/// - Handles output format conversion and validation
/// - Creates integrated output structure
/// - Reports integration errors
/// 
/// Used by Orchestrator to synthesize final results from all agent outputs.
/// </summary>
public sealed class OutputIntegrator
{
    private readonly ILogger<OutputIntegrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputIntegrator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public OutputIntegrator(ILogger<OutputIntegrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Integrates outputs from multiple agents based on their specifications.
    /// 
    /// Process:
    /// 1. Groups outputs by domain
    /// 2. Merges outputs within each domain
    /// 3. Validates outputs against schemas
    /// 4. Checks cross-domain consistency
    /// 5. Validates required fields from constraints
    /// 
    /// Returns an IntegratedOutput with integrated results, raw outputs, and any errors.
    /// </summary>
    /// <param name="agentSpecs">The list of agent spawn specifications.</param>
    /// <param name="agentOutputs">Dictionary mapping agent IDs to their outputs.</param>
    /// <returns>An <see cref="IntegratedOutput"/> containing integrated results and validation information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if agentSpecs or agentOutputs is null.</exception>
    public IntegratedOutput Integrate(
        IReadOnlyList<AgentSpawnSpec> agentSpecs,
        IReadOnlyDictionary<string, object> agentOutputs)
    {
        if (agentSpecs == null)
        {
            throw new ArgumentNullException(nameof(agentSpecs));
        }

        if (agentOutputs == null)
        {
            throw new ArgumentNullException(nameof(agentOutputs));
        }

        _logger.LogInformation("Integrating outputs from {AgentCount} agents", agentSpecs.Count);

        var integrated = new Dictionary<string, object>();
        var errors = new List<string>();

        // Group outputs by domain
        var outputsByDomain = agentSpecs
            .Where(spec => agentOutputs.ContainsKey(spec.AgentId))
            .GroupBy(spec => spec.Domain)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (domain, specs) in outputsByDomain)
        {
            try
            {
                var domainOutputs = specs
                    .Select(spec => new
                    {
                        Spec = spec,
                        Output = agentOutputs[spec.AgentId]
                    })
                    .ToList();

                var domainOutputsList = domainOutputs
                    .Select(item => (item.Spec, item.Output))
                    .ToList();
                var domainIntegrated = IntegrateDomainOutputs(domain, domainOutputsList);
                integrated[domain] = domainIntegrated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error integrating outputs for domain {Domain}", domain);
                errors.Add($"Failed to integrate {domain} domain: {ex.Message}");
            }
        }

        // Create unified output structure
        var result = new IntegratedOutput
        {
            IntegratedResults = integrated,
            AgentOutputs = agentOutputs,
            IntegrationErrors = errors,
            IntegratedAt = DateTimeOffset.UtcNow
        };

        // Validate integration
        var validationErrors = ValidateIntegration(result, agentSpecs);
        
        // Create new result with validation errors
        result = result with { ValidationErrors = validationErrors };

        _logger.LogInformation("Integration complete: {DomainCount} domains, {ErrorCount} errors",
            integrated.Count, errors.Count + validationErrors.Count);

        return result with { ValidationErrors = validationErrors };
    }

    /// <summary>
    /// Integrates outputs from multiple agents within the same domain.
    /// 
    /// If there's only one output, returns it directly.
    /// If there are multiple outputs, combines them into a dictionary keyed by agent ID.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <param name="domainOutputs">List of agent specs and their outputs for this domain.</param>
    /// <returns>The integrated domain output (single output or combined dictionary).</returns>
    private object IntegrateDomainOutputs(string domain, List<(AgentSpawnSpec Spec, object Output)> domainOutputs)
    {
        // Simple integration: combine outputs into a domain-specific structure
        // In a real implementation, this would use domain-specific integration logic

        if (domainOutputs.Count == 1)
        {
            return domainOutputs[0].Output;
        }

        // Multiple outputs in same domain - combine them
        var combined = new Dictionary<string, object>();
        foreach (var (spec, output) in domainOutputs)
        {
            combined[spec.AgentId] = output;
        }

        return combined;
    }

    /// <summary>
    /// Validates the integrated output against agent specifications.
    /// 
    /// Checks:
    /// - All agents have outputs
    /// - Outputs conform to declared schemas
    /// - Cross-domain consistency
    /// - Required fields from constraints
    /// </summary>
    /// <param name="result">The integrated output to validate.</param>
    /// <param name="agentSpecs">The agent specifications to validate against.</param>
    /// <returns>A list of validation error messages (empty if valid).</returns>
    private List<string> ValidateIntegration(IntegratedOutput result, IReadOnlyList<AgentSpawnSpec> agentSpecs)
    {
        var errors = new List<string>();

        // Check that all agents with outputs are included
        var agentsWithOutputs = result.AgentOutputs.Keys.ToHashSet();
        var allAgentIds = agentSpecs.Select(s => s.AgentId).ToHashSet();

        var missingOutputs = allAgentIds.Except(agentsWithOutputs).ToList();
        if (missingOutputs.Count > 0)
        {
            errors.Add($"Missing outputs from agents: {string.Join(", ", missingOutputs)}");
        }

        // Validate schema compatibility for each agent's output
        foreach (var spec in agentSpecs)
        {
            if (!agentsWithOutputs.Contains(spec.AgentId))
            {
                continue; // Already reported as missing
            }

            if (spec.OutputSchema.HasValue && result.AgentOutputs.TryGetValue(spec.AgentId, out var output))
            {
                var schemaErrors = ValidateOutputAgainstSchema(output, spec.OutputSchema.Value, spec.AgentId);
                errors.AddRange(schemaErrors);
            }
        }

        // Validate cross-domain consistency
        var consistencyErrors = ValidateCrossDomainConsistency(result, agentSpecs);
        errors.AddRange(consistencyErrors);

        // Validate required fields are present
        var requiredFieldErrors = ValidateRequiredFields(result, agentSpecs);
        errors.AddRange(requiredFieldErrors);

        return errors;
    }

    /// <summary>
    /// Validates an agent's output against its declared schema.
    /// </summary>
    private List<string> ValidateOutputAgainstSchema(object output, JsonElement schema, string agentId)
    {
        var errors = new List<string>();

        try
        {
            // Convert output to JSON for validation
            var outputJson = JsonSerializer.SerializeToElement(output);

            // Basic schema validation
            if (schema.TryGetProperty("type", out var schemaType))
            {
                var expectedType = schemaType.GetString();
                var actualType = GetJsonType(outputJson);

                if (expectedType != null && !AreTypesCompatible(actualType, expectedType))
                {
                    errors.Add($"Agent {agentId}: Output type mismatch. Expected {expectedType}, got {actualType}");
                }
            }

            // Validate required properties (for object types)
            if (schema.TryGetProperty("required", out var requiredArray) && outputJson.ValueKind == JsonValueKind.Object)
            {
                var requiredProps = requiredArray.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => s != null)
                    .ToList();

                foreach (var prop in requiredProps)
                {
                    if (prop != null && !outputJson.TryGetProperty(prop, out _))
                    {
                        errors.Add($"Agent {agentId}: Missing required property '{prop}'");
                    }
                }
            }

            // Validate property types
            if (schema.TryGetProperty("properties", out var properties) && outputJson.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in properties.EnumerateObject())
                {
                    if (outputJson.TryGetProperty(prop.Name, out var outputProp))
                    {
                        if (prop.Value.TryGetProperty("type", out var propType))
                        {
                            var expectedPropType = propType.GetString();
                            var actualPropType = GetJsonType(outputProp);

                            if (expectedPropType != null && !AreTypesCompatible(actualPropType, expectedPropType))
                            {
                                errors.Add($"Agent {agentId}: Property '{prop.Name}' type mismatch. Expected {expectedPropType}, got {actualPropType}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Agent {agentId}: Schema validation error: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Validates consistency across domains (e.g., no conflicting values).
    /// </summary>
    private List<string> ValidateCrossDomainConsistency(IntegratedOutput result, IReadOnlyList<AgentSpawnSpec> agentSpecs)
    {
        var errors = new List<string>();

        // Check for conflicting values in integrated results
        // This is a simplified check - in a real implementation, we'd have domain-specific consistency rules

        var domainGroups = agentSpecs
            .Where(s => result.AgentOutputs.ContainsKey(s.AgentId))
            .GroupBy(s => s.Domain)
            .ToList();

        // Check for duplicate keys across domains that might indicate conflicts
        var allKeys = new Dictionary<string, List<string>>(); // key -> list of domains using it

        foreach (var domainGroup in domainGroups)
        {
            var domain = domainGroup.Key;
            foreach (var spec in domainGroup)
            {
                if (result.AgentOutputs.TryGetValue(spec.AgentId, out var output))
                {
                    var keys = ExtractKeys(output);
                    foreach (var key in keys)
                    {
                        if (!allKeys.ContainsKey(key))
                        {
                            allKeys[key] = new List<string>();
                        }
                        if (!allKeys[key].Contains(domain))
                        {
                            allKeys[key].Add(domain);
                        }
                    }
                }
            }
        }

        // Warn about potential conflicts (same key in multiple domains)
        foreach (var kvp in allKeys.Where(k => k.Value.Count > 1))
        {
            _logger.LogWarning("Potential cross-domain conflict: key '{Key}' appears in domains: {Domains}",
                kvp.Key, string.Join(", ", kvp.Value));
        }

        return errors;
    }

    /// <summary>
    /// Validates that required fields from constraints are present.
    /// </summary>
    private List<string> ValidateRequiredFields(IntegratedOutput result, IReadOnlyList<AgentSpawnSpec> agentSpecs)
    {
        var errors = new List<string>();

        foreach (var spec in agentSpecs)
        {
            if (!result.AgentOutputs.ContainsKey(spec.AgentId))
            {
                continue; // Already reported as missing
            }

            // Check constraints that require specific fields
            foreach (var constraint in spec.Constraints)
            {
                if (constraint.Type == "required_field" && !string.IsNullOrWhiteSpace(constraint.Description))
                {
                    // Parse field name from description (e.g., "Field 'name' is required" -> "name")
                    var fieldMatch = System.Text.RegularExpressions.Regex.Match(
                        constraint.Description,
                        @"['""]([^'""]+)['""]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (fieldMatch.Success && fieldMatch.Groups.Count > 1)
                    {
                        var requiredField = fieldMatch.Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(requiredField) && result.AgentOutputs.TryGetValue(spec.AgentId, out var output))
                        {
                            var outputJson = JsonSerializer.SerializeToElement(output);
                            if (outputJson.ValueKind == JsonValueKind.Object && requiredField != null)
                            {
                                if (!outputJson.TryGetProperty(requiredField, out _))
                                {
                                    errors.Add($"Agent {spec.AgentId}: Constraint requires field '{requiredField}' but it's missing");
                                }
                            }
                        }
                    }
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets the JSON type string for a JsonElement.
    /// </summary>
    /// <param name="element">The JSON element to get the type for.</param>
    /// <returns>The type string (e.g., "string", "number", "object", "array", "boolean", "null").</returns>
    private string GetJsonType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            JsonValueKind.Null => "null",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Determines if two JSON types are compatible.
    /// 
    /// Compatible pairs:
    /// - number and integer are compatible
    /// - Same types are compatible
    /// </summary>
    /// <param name="actualType">The actual type.</param>
    /// <param name="expectedType">The expected type.</param>
    /// <returns>True if the types are compatible, false otherwise.</returns>
    private bool AreTypesCompatible(string actualType, string expectedType)
    {
        if (actualType == expectedType) return true;

        // Type compatibility rules
        return (expectedType, actualType) switch
        {
            ("number", "integer") => true,
            ("integer", "number") => true,
            _ => false
        };
    }

    /// <summary>
    /// Extracts all keys from a JSON object recursively.
    /// </summary>
    /// <param name="obj">The object to extract keys from.</param>
    /// <returns>A set of all keys found in the object (including nested keys with dot notation).</returns>
    private HashSet<string> ExtractKeys(object obj)
    {
        var keys = new HashSet<string>();
        try
        {
            var json = JsonSerializer.SerializeToElement(obj);
            ExtractKeysRecursive(json, keys, "");
        }
        catch
        {
            // Ignore extraction errors
        }
        return keys;
    }

    /// <summary>
    /// Recursively extracts keys from a JSON element.
    /// 
    /// For objects, extracts property names.
    /// For arrays, includes index notation (e.g., "key[0]").
    /// Uses dot notation for nested keys (e.g., "parent.child").
    /// </summary>
    /// <param name="element">The JSON element to extract keys from.</param>
    /// <param name="keys">The set to populate with extracted keys.</param>
    /// <param name="prefix">The prefix for nested keys (empty for root).</param>
    private void ExtractKeysRecursive(JsonElement element, HashSet<string> keys, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    keys.Add(key);
                    ExtractKeysRecursive(prop.Value, keys, key);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{index}]";
                    ExtractKeysRecursive(item, keys, key);
                    index++;
                }
                break;
        }
    }
}
