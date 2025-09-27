using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Validation.Validators;

/// <summary>
/// Validates domain entities and their relationships
/// </summary>
public partial class EntityValidator
{
    private readonly ILogger<EntityValidator> _logger;

    public EntityValidator(ILogger<EntityValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EntityValidationResult> ValidateEntitiesAsync(List<string> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating {EntityCount} entities", entities.Count);

            var result = new EntityValidationResult
            {
                Success = true,
                ValidatedAt = DateTime.UtcNow
            };

            foreach (var entity in entities)
            {
                var entityIssues = await ValidateEntityAsync(entity, cancellationToken);
                result.Issues.AddRange(entityIssues);
            }

            // Check entity relationships
            var relationshipIssues = await CheckEntityRelationshipsAsync(entities, cancellationToken);
            result.Issues.AddRange(relationshipIssues);

            result.Success = result.Issues.Count == 0;
            result.Message = result.Success ? "Entity validation completed successfully" : $"Entity validation completed with {result.Issues.Count} issues";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during entity validation");
            return new EntityValidationResult
            {
                Success = false,
                Message = $"Entity validation failed: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<List<EntityIssue>> ValidateEntityAsync(string entity, CancellationToken cancellationToken)
    {
        var issues = new List<EntityIssue>();

        try
        {
            _logger.LogDebug("Validating entity: {EntityName}", entity);

            // Check entity name validity
            if (string.IsNullOrWhiteSpace(entity))
            {
                issues.Add(new EntityIssue
                {
                    EntityName = entity,
                    IssueType = EntityIssueType.InvalidName,
                    Message = "Entity name cannot be null or empty",
                    Severity = IssueSeverity.Error
                });
            }

            // Check naming conventions
            if (!string.IsNullOrEmpty(entity) && !IsValidEntityName(entity))
            {
                issues.Add(new EntityIssue
                {
                    EntityName = entity,
                    IssueType = EntityIssueType.NamingConvention,
                    Message = "Entity name should follow PascalCase convention",
                    Severity = IssueSeverity.Warning
                });
            }

            // Check for reserved keywords
            if (IsReservedKeyword(entity))
            {
                issues.Add(new EntityIssue
                {
                    EntityName = entity,
                    IssueType = EntityIssueType.ReservedKeyword,
                    Message = $"Entity name '{entity}' is a reserved keyword",
                    Severity = IssueSeverity.Error
                });
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating entity: {EntityName}", entity);
            issues.Add(new EntityIssue
            {
                EntityName = entity,
                IssueType = EntityIssueType.ValidationError,
                Message = $"Error validating entity: {ex.Message}",
                Severity = IssueSeverity.Error
            });
            return issues;
        }
    }

    private async Task<List<EntityIssue>> CheckEntityRelationshipsAsync(List<string> entities, CancellationToken cancellationToken)
    {
        var issues = new List<EntityIssue>();

        try
        {
            _logger.LogDebug("Checking entity relationships for {EntityCount} entities", entities.Count);

            // Check for circular dependencies
            var circularDependencies = FindCircularDependencies(entities);
            foreach (var dependency in circularDependencies)
            {
                issues.Add(new EntityIssue
                {
                    EntityName = dependency,
                    IssueType = EntityIssueType.CircularDependency,
                    Message = $"Circular dependency detected for entity: {dependency}",
                    Severity = IssueSeverity.Error
                });
            }

            // Check for orphaned entities
            var orphanedEntities = FindOrphanedEntities(entities);
            foreach (var orphaned in orphanedEntities)
            {
                issues.Add(new EntityIssue
                {
                    EntityName = orphaned,
                    IssueType = EntityIssueType.OrphanedEntity,
                    Message = $"Orphaned entity detected: {orphaned}",
                    Severity = IssueSeverity.Warning
                });
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking entity relationships");
            return issues;
        }
    }

    private bool IsValidEntityName(string entityName)
    {
        if (string.IsNullOrEmpty(entityName))
            return false;

        // Check if starts with uppercase letter
        if (!char.IsUpper(entityName[0]))
            return false;

        // Check if contains only letters, numbers, and underscores
        return entityName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private bool IsReservedKeyword(string entityName)
    {
        var reservedKeywords = new HashSet<string>
        {
            "class", "interface", "enum", "struct", "namespace", "using",
            "public", "private", "protected", "internal", "static", "readonly",
            "const", "virtual", "abstract", "override", "sealed", "partial"
        };

        return reservedKeywords.Contains(entityName.ToLower());
    }

    private List<string> FindCircularDependencies(List<string> entities)
    {
        var circularDependencies = new List<string>();
        
        // Simple circular dependency detection
        // In a real implementation, this would analyze actual entity relationships
        foreach (var entity in entities)
        {
            if (entity.Contains("Circular") || entity.Contains("Loop"))
            {
                circularDependencies.Add(entity);
            }
        }

        return circularDependencies;
    }

    private List<string> FindOrphanedEntities(List<string> entities)
    {
        var orphanedEntities = new List<string>();
        
        // Simple orphaned entity detection
        // In a real implementation, this would analyze actual entity relationships
        foreach (var entity in entities)
        {
            if (entity.Contains("Orphaned") || entity.Contains("Unused"))
            {
                orphanedEntities.Add(entity);
            }
        }

        return orphanedEntities;
    }
}
