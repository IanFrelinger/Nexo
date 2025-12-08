using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Validation;

/// <summary>
/// Detects contradictions and conflicts in agent constraints and requirements.
/// </summary>
public sealed class ConstraintSolver : IValidator
{
    private readonly ILogger<ConstraintSolver> _logger;

    public string Name => "ConstraintSolver";

    public ConstraintSolver(ILogger<ConstraintSolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<ValidationError>> ValidateAsync(
        DecompositionResult result,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Check for contradictory constraints within agents
        foreach (var agent in result.Agents)
        {
            var contradictions = DetectContradictions(agent);
            errors.AddRange(contradictions);
        }

        // Check for conflicting resource requirements across agents
        var resourceConflicts = DetectResourceConflicts(result.Agents);
        errors.AddRange(resourceConflicts);

        // Check for conflicting output schemas (if specified)
        var schemaConflicts = DetectSchemaConflicts(result.Agents);
        errors.AddRange(schemaConflicts);

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }

    private List<ValidationError> DetectContradictions(AgentSpawnSpec agent)
    {
        var errors = new List<ValidationError>();

        // Check for contradictory constraint types
        var constraintTypes = agent.Constraints
            .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var type in constraintTypes)
        {
            var constraintsOfType = agent.Constraints
                .Where(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Check for explicit contradictions in descriptions
            if (HasContradictoryDescriptions(constraintsOfType))
            {
                errors.Add(new ValidationError
                {
                    ErrorType = "Constraint",
                    Message = $"Agent '{agent.AgentId}' has contradictory constraints of type '{type}'",
                    AgentId = agent.AgentId,
                    Severity = ValidationSeverity.Error
                });
            }
        }

        // Check for performance vs quality contradictions
        var hasPerformanceConstraint = agent.Constraints.Any(c =>
            c.Type.Contains("Performance", StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains("fast", StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains("quick", StringComparison.OrdinalIgnoreCase));

        var hasQualityConstraint = agent.Constraints.Any(c =>
            c.Type.Contains("Quality", StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains("thorough", StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains("comprehensive", StringComparison.OrdinalIgnoreCase));

        if (hasPerformanceConstraint && hasQualityConstraint)
        {
            // Check if they conflict (e.g., "must be fast" vs "must be comprehensive")
            var perfDesc = agent.Constraints
                .FirstOrDefault(c => c.Type.Contains("Performance", StringComparison.OrdinalIgnoreCase) ||
                                    c.Description.Contains("fast", StringComparison.OrdinalIgnoreCase))
                ?.Description.ToLowerInvariant() ?? "";

            var qualityDesc = agent.Constraints
                .FirstOrDefault(c => c.Type.Contains("Quality", StringComparison.OrdinalIgnoreCase) ||
                                    c.Description.Contains("thorough", StringComparison.OrdinalIgnoreCase))
                ?.Description.ToLowerInvariant() ?? "";

            if (perfDesc.Contains("fast") && qualityDesc.Contains("thorough"))
            {
                errors.Add(new ValidationError
                {
                    ErrorType = "Constraint",
                    Message = $"Agent '{agent.AgentId}' has conflicting performance and quality constraints",
                    AgentId = agent.AgentId,
                    Severity = ValidationSeverity.Warning
                });
            }
        }

        return errors;
    }

    private static bool HasContradictoryDescriptions(List<AgentConstraint> constraints)
    {
        // Simple contradiction detection: look for opposite keywords
        var descriptions = constraints.Select(c => c.Description.ToLowerInvariant()).ToList();

        var opposites = new[]
        {
            ("fast", "slow"),
            ("high", "low"),
            ("maximum", "minimum"),
            ("must", "must not"),
            ("required", "forbidden"),
            ("enabled", "disabled"),
            ("allow", "prevent")
        };

        foreach (var (word1, word2) in opposites)
        {
            var hasWord1 = descriptions.Any(d => d.Contains(word1));
            var hasWord2 = descriptions.Any(d => d.Contains(word2));

            if (hasWord1 && hasWord2)
            {
                return true;
            }
        }

        return false;
    }

    private List<ValidationError> DetectResourceConflicts(IReadOnlyList<AgentSpawnSpec> agents)
    {
        var errors = new List<ValidationError>();

        // Check if total resource requirements exceed reasonable limits
        var totalCompute = agents
            .Where(a => a.ResourceRequirements?.EstimatedComputeSeconds.HasValue == true)
            .Sum(a => a.ResourceRequirements!.EstimatedComputeSeconds!.Value);

        if (totalCompute > 3600) // More than 1 hour total
        {
            errors.Add(new ValidationError
            {
                ErrorType = "Constraint",
                Message = $"Total estimated compute time ({totalCompute}s) exceeds 1 hour. Consider parallelization or optimization.",
                Severity = ValidationSeverity.Warning
            });
        }

        var totalMemory = agents
            .Where(a => a.ResourceRequirements?.RequiredMemoryMB.HasValue == true)
            .Sum(a => a.ResourceRequirements!.RequiredMemoryMB!.Value);

        if (totalMemory > 16384) // More than 16GB total
        {
            errors.Add(new ValidationError
            {
                ErrorType = "Constraint",
                Message = $"Total required memory ({totalMemory}MB) exceeds 16GB. This may cause resource contention.",
                Severity = ValidationSeverity.Warning
            });
        }

        return errors;
    }

    private List<ValidationError> DetectSchemaConflicts(IReadOnlyList<AgentSpawnSpec> agents)
    {
        var errors = new List<ValidationError>();

        // Group agents by domain and check for schema conflicts
        var agentsByDomain = agents
            .Where(a => a.OutputSchema.HasValue)
            .GroupBy(a => a.Domain, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var domainGroup in agentsByDomain)
        {
            var agentsInDomain = domainGroup.ToList();
            
            // Check if agents in the same domain have incompatible output schemas
            // This is a simplified check - in a real implementation, we'd do proper JSON schema comparison
            var schemaStrings = agentsInDomain
                .Select(a => a.OutputSchema!.Value.ToString())
                .ToList();

            // If schemas are identical, that's fine
            // If they're different, check if they're compatible
            var uniqueSchemas = schemaStrings.Distinct().Count();
            if (uniqueSchemas > 1)
            {
                // Multiple different schemas in same domain - potential conflict
                var agentIds = string.Join(", ", agentsInDomain.Select(a => a.AgentId));
                errors.Add(new ValidationError
                {
                    ErrorType = "Constraint",
                    Message = $"Agents in domain '{domainGroup.Key}' have different output schemas: {agentIds}. This may cause integration issues.",
                    Severity = ValidationSeverity.Warning
                });
            }
        }

        return errors;
    }
}

