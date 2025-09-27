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
/// Checks consistency across domain logic
/// </summary>
public partial class ConsistencyChecker
{
    private readonly ILogger<ConsistencyChecker> _logger;

    public ConsistencyChecker(ILogger<ConsistencyChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConsistencyCheckResult> CheckConsistencyAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking consistency for domain logic with {EntityCount} entities, {ValueObjectCount} value objects, {RuleCount} business rules", 
                domainLogic.Entities.Count, domainLogic.ValueObjects.Count, domainLogic.BusinessRules.Count);

            var result = new ConsistencyCheckResult
            {
                Success = true,
                CheckedAt = DateTime.UtcNow
            };

            // Check entity-rule consistency
            var entityRuleIssues = await CheckEntityRuleConsistencyAsync(domainLogic.Entities, domainLogic.BusinessRules.Select(r => r.Name).ToList(), cancellationToken);
            result.Issues.AddRange(entityRuleIssues);

            // Check entity-service consistency
            var entityServiceIssues = await CheckEntityServiceConsistencyAsync(domainLogic.Entities, domainLogic.Services, cancellationToken);
            result.Issues.AddRange(entityServiceIssues);

            // Check value object consistency
            var valueObjectIssues = await CheckValueObjectConsistencyAsync(domainLogic.ValueObjects, cancellationToken);
            result.Issues.AddRange(valueObjectIssues);

            // Check naming consistency
            var namingWarnings = await CheckNamingConsistencyAsync(domainLogic, cancellationToken);
            result.Warnings.AddRange(namingWarnings);

            result.Success = result.Issues.Count == 0;
            result.Message = result.Success ? "Consistency check completed successfully" : $"Consistency check completed with {result.Issues.Count} issues";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during consistency check");
            return new ConsistencyCheckResult
            {
                Success = false,
                Message = $"Consistency check failed: {ex.Message}",
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<List<ConsistencyIssue>> CheckEntityRuleConsistencyAsync(List<string> entities, List<string> rules, CancellationToken cancellationToken)
    {
        var issues = new List<ConsistencyIssue>();

        try
        {
            _logger.LogDebug("Checking entity-rule consistency for {EntityCount} entities and {RuleCount} rules", entities.Count, rules.Count);

            // Check for entities without corresponding rules
            foreach (var entity in entities)
            {
                var hasRelatedRules = rules.Any(rule => rule.Contains(entity) || entity.Contains(rule));
                if (!hasRelatedRules)
                {
                    issues.Add(new ConsistencyIssue
                    {
                        EntityName = entity,
                        IssueType = ConsistencyIssueType.MissingBusinessRules,
                        Message = $"Entity '{entity}' has no associated business rules",
                        Severity = IssueSeverity.Warning
                    });
                }
            }

            // Check for rules without corresponding entities
            foreach (var rule in rules)
            {
                var hasRelatedEntities = entities.Any(entity => rule.Contains(entity) || entity.Contains(rule));
                if (!hasRelatedEntities)
                {
                    issues.Add(new ConsistencyIssue
                    {
                        RuleName = rule,
                        IssueType = ConsistencyIssueType.OrphanedBusinessRule,
                        Message = $"Business rule '{rule}' has no associated entities",
                        Severity = IssueSeverity.Warning
                    });
                }
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking entity-rule consistency");
            return issues;
        }
    }

    private async Task<List<ConsistencyIssue>> CheckEntityServiceConsistencyAsync(List<string> entities, List<string> services, CancellationToken cancellationToken)
    {
        var issues = new List<ConsistencyIssue>();

        try
        {
            _logger.LogDebug("Checking entity-service consistency for {EntityCount} entities and {ServiceCount} services", entities.Count, services.Count);

            // Check for entities without corresponding services
            foreach (var entity in entities)
            {
                var hasRelatedServices = services.Any(service => service.Contains(entity) || entity.Contains(service));
                if (!hasRelatedServices)
                {
                    issues.Add(new ConsistencyIssue
                    {
                        EntityName = entity,
                        IssueType = ConsistencyIssueType.MissingServices,
                        Message = $"Entity '{entity}' has no associated services",
                        Severity = IssueSeverity.Warning
                    });
                }
            }

            // Check for services without corresponding entities
            foreach (var service in services)
            {
                var hasRelatedEntities = entities.Any(entity => service.Contains(entity) || entity.Contains(service));
                if (!hasRelatedEntities)
                {
                    issues.Add(new ConsistencyIssue
                    {
                        ServiceName = service,
                        IssueType = ConsistencyIssueType.OrphanedService,
                        Message = $"Service '{service}' has no associated entities",
                        Severity = IssueSeverity.Warning
                    });
                }
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking entity-service consistency");
            return issues;
        }
    }

    private async Task<List<ConsistencyIssue>> CheckValueObjectConsistencyAsync(List<string> valueObjects, CancellationToken cancellationToken)
    {
        var issues = new List<ConsistencyIssue>();

        try
        {
            _logger.LogDebug("Checking value object consistency for {ValueObjectCount} value objects", valueObjects.Count);

            // Check for duplicate value object names
            var duplicateValueObjects = valueObjects.GroupBy(vo => vo)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateValueObjects)
            {
                issues.Add(new ConsistencyIssue
                {
                    ValueObjectName = duplicate,
                    IssueType = ConsistencyIssueType.DuplicateValueObject,
                    Message = $"Duplicate value object name: {duplicate}",
                    Severity = IssueSeverity.Error
                });
            }

            // Check for value objects with invalid names
            foreach (var valueObject in valueObjects)
            {
                if (string.IsNullOrWhiteSpace(valueObject))
                {
                    issues.Add(new ConsistencyIssue
                    {
                        ValueObjectName = valueObject,
                        IssueType = ConsistencyIssueType.InvalidValueObjectName,
                        Message = "Value object name cannot be null or empty",
                        Severity = IssueSeverity.Error
                    });
                }
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking value object consistency");
            return issues;
        }
    }

    private async Task<List<ConsistencyWarning>> CheckNamingConsistencyAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken)
    {
        var warnings = new List<ConsistencyWarning>();

        try
        {
            _logger.LogDebug("Checking naming consistency for domain logic");

            // Check for inconsistent naming patterns
            var allNames = new List<string>();
            allNames.AddRange(domainLogic.Entities);
            allNames.AddRange(domainLogic.ValueObjects);
            allNames.AddRange(domainLogic.BusinessRules.Select(r => r.Name));
            allNames.AddRange(domainLogic.Services);

            // Check for mixed naming conventions
            var hasPascalCase = allNames.Any(name => !string.IsNullOrEmpty(name) && char.IsUpper(name[0]));
            var hasCamelCase = allNames.Any(name => !string.IsNullOrEmpty(name) && char.IsLower(name[0]));

            if (hasPascalCase && hasCamelCase)
            {
                warnings.Add(new ConsistencyWarning
                {
                    WarningType = ConsistencyWarningType.MixedNamingConventions,
                    Message = "Mixed naming conventions detected across domain logic",
                    Severity = WarningSeverity.Medium
                });
            }

            // Check for naming conflicts
            var nameConflicts = allNames.GroupBy(name => name.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var conflict in nameConflicts)
            {
                warnings.Add(new ConsistencyWarning
                {
                    WarningType = ConsistencyWarningType.NamingConflict,
                    Message = $"Naming conflict detected: '{conflict}'",
                    Severity = WarningSeverity.High
                });
            }

            return warnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking naming consistency");
            return warnings;
        }
    }
}
