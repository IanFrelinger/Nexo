using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Validation;

/// <summary>
/// Validates that agent specifications conform to the expected JSON schema.
/// </summary>
public sealed class SchemaValidator : IValidator
{
    private readonly ILogger<SchemaValidator> _logger;

    public string Name => "SchemaValidator";

    public SchemaValidator(ILogger<SchemaValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<ValidationError>> ValidateAsync(
        DecompositionResult result,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        foreach (var agent in result.Agents)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(agent.AgentId))
            {
                errors.Add(new ValidationError
                {
                    ErrorType = "Schema",
                    Message = "Agent must have a non-empty agentId",
                    AgentId = agent.AgentId ?? "unknown",
                    Severity = ValidationSeverity.Error
                });
            }

            if (string.IsNullOrWhiteSpace(agent.Domain))
            {
                errors.Add(new ValidationError
                {
                    ErrorType = "Schema",
                    Message = "Agent must have a non-empty domain",
                    AgentId = agent.AgentId,
                    Severity = ValidationSeverity.Error
                });
            }

            if (string.IsNullOrWhiteSpace(agent.Goal))
            {
                errors.Add(new ValidationError
                {
                    ErrorType = "Schema",
                    Message = "Agent must have a non-empty goal",
                    AgentId = agent.AgentId,
                    Severity = ValidationSeverity.Error
                });
            }

            // Validate agentId format (should be alphanumeric with hyphens/underscores)
            if (!string.IsNullOrWhiteSpace(agent.AgentId) &&
                !System.Text.RegularExpressions.Regex.IsMatch(agent.AgentId, @"^[a-zA-Z0-9_-]+$"))
            {
                errors.Add(new ValidationError
                {
                    ErrorType = "Schema",
                    Message = $"AgentId '{agent.AgentId}' contains invalid characters. Use only alphanumeric, hyphens, and underscores.",
                    AgentId = agent.AgentId,
                    Severity = ValidationSeverity.Error
                });
            }

            // Validate dependencies reference existing agents
            var agentIds = result.Agents.Select(a => a.AgentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var dep in agent.Dependencies)
            {
                if (!agentIds.Contains(dep))
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' depends on non-existent agent '{dep}'",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Error
                    });
                }

                // Self-dependency check
                if (string.Equals(agent.AgentId, dep, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' cannot depend on itself",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            // Validate constraints
            foreach (var constraint in agent.Constraints)
            {
                if (string.IsNullOrWhiteSpace(constraint.Type))
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' has constraint with empty type",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Warning
                    });
                }

                if (string.IsNullOrWhiteSpace(constraint.Description))
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' has constraint with empty description",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Warning
                    });
                }
            }

            // Validate resource requirements
            if (agent.ResourceRequirements != null)
            {
                if (agent.ResourceRequirements.EstimatedComputeSeconds.HasValue &&
                    agent.ResourceRequirements.EstimatedComputeSeconds.Value < 0)
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' has negative estimated compute seconds",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Error
                    });
                }

                if (agent.ResourceRequirements.RequiredContextTokens.HasValue &&
                    agent.ResourceRequirements.RequiredContextTokens.Value < 0)
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' has negative required context tokens",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Error
                    });
                }

                if (agent.ResourceRequirements.RequiredMemoryMB.HasValue &&
                    agent.ResourceRequirements.RequiredMemoryMB.Value < 0)
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Schema",
                        Message = $"Agent '{agent.AgentId}' has negative required memory",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Error
                    });
                }
            }
        }

        // Check for duplicate agent IDs
        var duplicateIds = result.Agents
            .GroupBy(a => a.AgentId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateId in duplicateIds)
        {
            errors.Add(new ValidationError
            {
                ErrorType = "Schema",
                Message = $"Duplicate agent ID: '{duplicateId}'",
                AgentId = duplicateId,
                Severity = ValidationSeverity.Error
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }
}

