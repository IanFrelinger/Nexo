using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Coordination.Conflicts;

/// <summary>
/// Detects conflicts between agents using four conflict types.
/// </summary>
public sealed class ConflictDetector
{
    private readonly ILogger<ConflictDetector> _logger;

    public ConflictDetector(ILogger<ConflictDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects all conflicts between agents.
    /// </summary>
    public IReadOnlyList<Conflict> DetectConflicts(IReadOnlyList<AgentContainer> agents)
    {
        var conflicts = new List<Conflict>();

        // Check all pairs of agents for conflicts
        for (int i = 0; i < agents.Count; i++)
        {
            for (int j = i + 1; j < agents.Count; j++)
            {
                var agent1 = agents[i];
                var agent2 = agents[j];

                // Check for schema conflicts
                var schemaConflict = DetectSchemaConflict(agent1, agent2);
                if (schemaConflict != null)
                {
                    conflicts.Add(schemaConflict);
                }

                // Check for resource conflicts
                var resourceConflict = DetectResourceConflict(agent1, agent2);
                if (resourceConflict != null)
                {
                    conflicts.Add(resourceConflict);
                }

                // Check for constraint conflicts
                var constraintConflict = DetectConstraintConflict(agent1, agent2);
                if (constraintConflict != null)
                {
                    conflicts.Add(constraintConflict);
                }

                // Check for philosophy conflicts
                var philosophyConflict = DetectPhilosophyConflict(agent1, agent2);
                if (philosophyConflict != null)
                {
                    conflicts.Add(philosophyConflict);
                }
            }
        }

        _logger.LogInformation("Detected {ConflictCount} conflicts among {AgentCount} agents", 
            conflicts.Count, agents.Count);

        return conflicts;
    }

    private Conflict? DetectSchemaConflict(AgentContainer agent1, AgentContainer agent2)
    {
        var spec1 = agent1.Agent.Spec;
        var spec2 = agent2.Agent.Spec;

        // Check if both agents have output schemas
        if (!spec1.OutputSchema.HasValue || !spec2.OutputSchema.HasValue)
        {
            return null;
        }

        // Check if schemas are incompatible (simplified check - compare JSON structure)
        var schema1 = spec1.OutputSchema.Value;
        var schema2 = spec2.OutputSchema.Value;

        if (!AreSchemasCompatible(schema1, schema2))
        {
            return new Conflict
            {
                ConflictType = ConflictType.Schema,
                AgentIds = new[] { agent1.AgentId, agent2.AgentId },
                Description = $"Agents {agent1.AgentId} and {agent2.AgentId} have incompatible output schemas",
                Severity = ConflictSeverity.High
            };
        }

        return null;
    }

    private Conflict? DetectResourceConflict(AgentContainer agent1, AgentContainer agent2)
    {
        var reqs1 = agent1.Agent.Spec.ResourceRequirements;
        var reqs2 = agent2.Agent.Spec.ResourceRequirements;

        if (reqs1 == null || reqs2 == null)
        {
            return null;
        }

        // Check if combined resource requirements exceed reasonable limits
        var totalCompute = (reqs1.EstimatedComputeSeconds ?? 0) + (reqs2.EstimatedComputeSeconds ?? 0);
        var totalMemory = (reqs1.RequiredMemoryMB ?? 0) + (reqs2.RequiredMemoryMB ?? 0);
        var totalContext = (reqs1.RequiredContextTokens ?? 0) + (reqs2.RequiredContextTokens ?? 0);

        // Thresholds for conflict detection
        const int maxComputeSeconds = 3600; // 1 hour
        const int maxMemoryMB = 16384; // 16GB
        const int maxContextTokens = 200000; // 200k tokens

        var issues = new List<string>();
        if (totalCompute > maxComputeSeconds)
        {
            issues.Add($"Combined compute time ({totalCompute}s) exceeds {maxComputeSeconds}s");
        }
        if (totalMemory > maxMemoryMB)
        {
            issues.Add($"Combined memory ({totalMemory}MB) exceeds {maxMemoryMB}MB");
        }
        if (totalContext > maxContextTokens)
        {
            issues.Add($"Combined context tokens ({totalContext}) exceeds {maxContextTokens}");
        }

        if (issues.Count > 0)
        {
            return new Conflict
            {
                ConflictType = ConflictType.Resource,
                AgentIds = new[] { agent1.AgentId, agent2.AgentId },
                Description = $"Resource contention between {agent1.AgentId} and {agent2.AgentId}: {string.Join(", ", issues)}",
                Severity = ConflictSeverity.Medium
            };
        }

        return null;
    }

    private Conflict? DetectConstraintConflict(AgentContainer agent1, AgentContainer agent2)
    {
        var constraints1 = agent1.Agent.Spec.Constraints;
        var constraints2 = agent2.Agent.Spec.Constraints;

        // Check if constraints from different agents contradict each other
        foreach (var c1 in constraints1)
        {
            foreach (var c2 in constraints2)
            {
                if (AreConstraintsContradictory(c1, c2))
                {
                    return new Conflict
                    {
                        ConflictType = ConflictType.Constraint,
                        AgentIds = new[] { agent1.AgentId, agent2.AgentId },
                        Description = $"Contradictory constraints: {agent1.AgentId} requires '{c1.Description}' but {agent2.AgentId} requires '{c2.Description}'",
                        Severity = ConflictSeverity.High
                    };
                }
            }
        }

        return null;
    }

    private Conflict? DetectPhilosophyConflict(AgentContainer agent1, AgentContainer agent2)
    {
        var goal1 = agent1.Agent.Spec.Goal.ToLowerInvariant();
        var goal2 = agent2.Agent.Spec.Goal.ToLowerInvariant();

        // Simple philosophy conflict detection using keyword analysis
        // In a real implementation, this would use embeddings for semantic similarity

        var philosophyKeywords = new Dictionary<string, string[]>
        {
            ["performance"] = new[] { "fast", "quick", "efficient", "optimize", "performance" },
            ["quality"] = new[] { "thorough", "comprehensive", "detailed", "quality", "polish" },
            ["simplicity"] = new[] { "simple", "minimal", "clean", "straightforward" },
            ["complexity"] = new[] { "complex", "advanced", "sophisticated", "feature-rich" }
        };

        var goal1Philosophy = DetectPhilosophy(goal1, philosophyKeywords);
        var goal2Philosophy = DetectPhilosophy(goal2, philosophyKeywords);

        // Check for opposing philosophies
        if ((goal1Philosophy == "performance" && goal2Philosophy == "quality") ||
            (goal1Philosophy == "simplicity" && goal2Philosophy == "complexity"))
        {
            return new Conflict
            {
                ConflictType = ConflictType.Philosophy,
                AgentIds = new[] { agent1.AgentId, agent2.AgentId },
                Description = $"Philosophical disagreement: {agent1.AgentId} emphasizes {goal1Philosophy} while {agent2.AgentId} emphasizes {goal2Philosophy}",
                Severity = ConflictSeverity.Low
            };
        }

        return null;
    }

    private static bool AreSchemasCompatible(JsonElement schema1, JsonElement schema2)
    {
        // Simplified compatibility check - compare top-level structure
        // In a real implementation, this would do proper JSON schema validation
        try
        {
            var json1 = schema1.GetRawText();
            var json2 = schema2.GetRawText();
            
            // If schemas are identical, they're compatible
            if (json1 == json2)
            {
                return true;
            }

            // Parse and compare structure
            var doc1 = JsonDocument.Parse(json1);
            var doc2 = JsonDocument.Parse(json2);

            // Basic check: if both have "type" property, compare them
            if (doc1.RootElement.TryGetProperty("type", out var type1) &&
                doc2.RootElement.TryGetProperty("type", out var type2))
            {
                return type1.GetString() == type2.GetString();
            }

            // Default to compatible if we can't determine
            return true;
        }
        catch
        {
            // If we can't parse, assume incompatible
            return false;
        }
    }

    private static bool AreConstraintsContradictory(AgentConstraint c1, AgentConstraint c2)
    {
        // Check for explicit contradictions
        var opposites = new[]
        {
            ("fast", "slow"),
            ("high", "low"),
            ("maximum", "minimum"),
            ("must", "must not"),
            ("required", "forbidden"),
            ("enabled", "disabled")
        };

        var desc1 = c1.Description.ToLowerInvariant();
        var desc2 = c2.Description.ToLowerInvariant();

        foreach (var (word1, word2) in opposites)
        {
            if ((desc1.Contains(word1) && desc2.Contains(word2)) ||
                (desc1.Contains(word2) && desc2.Contains(word1)))
            {
                return true;
            }
        }

        return false;
    }

    private static string DetectPhilosophy(string goal, Dictionary<string, string[]> philosophyKeywords)
    {
        foreach (var (philosophy, keywords) in philosophyKeywords)
        {
            if (keywords.Any(keyword => goal.Contains(keyword)))
            {
                return philosophy;
            }
        }

        return "neutral";
    }
}

