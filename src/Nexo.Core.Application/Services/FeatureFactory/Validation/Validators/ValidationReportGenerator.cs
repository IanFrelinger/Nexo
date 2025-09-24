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
/// Generates validation reports for domain logic
/// </summary>
public class ValidationReportGenerator
{
    private readonly ILogger<ValidationReportGenerator> _logger;

    public ValidationReportGenerator(ILogger<ValidationReportGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationReport> GenerateValidationReportAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating validation report for domain logic");

            var report = new ValidationReport
            {
                GeneratedAt = DateTime.UtcNow,
                DomainLogic = domainLogic
            };

            // Generate summary statistics
            report.Summary = GenerateSummaryStatistics(domainLogic);

            // Generate validation recommendations
            var recommendations = await GenerateValidationRecommendationsAsync(report, cancellationToken);
            report.Recommendations.AddRange(recommendations);

            // Generate quality metrics
            report.QualityMetrics = GenerateQualityMetrics(domainLogic);

            // Generate improvement suggestions
            report.ImprovementSuggestions = GenerateImprovementSuggestions(domainLogic);

            report.Success = true;
            report.Message = "Validation report generated successfully";

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating validation report");
            return new ValidationReport
            {
                Success = false,
                Message = $"Validation report generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private ValidationSummary GenerateSummaryStatistics(DomainLogicResult domainLogic)
    {
        return new ValidationSummary
        {
            EntityCount = domainLogic.Entities.Count,
            ValueObjectCount = domainLogic.ValueObjects.Count,
            BusinessRuleCount = domainLogic.BusinessRules.Count,
            ServiceCount = domainLogic.Services.Count,
            TotalComponents = domainLogic.Entities.Count + domainLogic.ValueObjects.Count + domainLogic.BusinessRules.Count + domainLogic.Services.Count,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<List<ValidationRecommendation>> GenerateValidationRecommendationsAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var recommendations = new List<ValidationRecommendation>();

        try
        {
            _logger.LogDebug("Generating validation recommendations");

            // Generate entity recommendations
            var entityRecommendations = GenerateEntityRecommendations(report.DomainLogic);
            recommendations.AddRange(entityRecommendations);

            // Generate value object recommendations
            var valueObjectRecommendations = GenerateValueObjectRecommendations(report.DomainLogic);
            recommendations.AddRange(valueObjectRecommendations);

            // Generate business rule recommendations
            var businessRuleRecommendations = GenerateBusinessRuleRecommendations(report.DomainLogic);
            recommendations.AddRange(businessRuleRecommendations);

            // Generate service recommendations
            var serviceRecommendations = GenerateServiceRecommendations(report.DomainLogic);
            recommendations.AddRange(serviceRecommendations);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating validation recommendations");
            return recommendations;
        }
    }

    private List<ValidationRecommendation> GenerateEntityRecommendations(DomainLogicResult domainLogic)
    {
        var recommendations = new List<ValidationRecommendation>();

        // Check for entities without business rules
        foreach (var entity in domainLogic.Entities)
        {
            var hasRelatedRules = domainLogic.BusinessRules.Any(rule => rule.Name.Contains(entity) || entity.Contains(rule.Name));
            if (!hasRelatedRules)
            {
                recommendations.Add(new ValidationRecommendation
                {
                    Type = ValidationRecommendationType.Entity,
                    Category = "Business Rules",
                    Description = $"Consider adding business rules for entity '{entity}'",
                    Priority = ValidationPriority.Medium,
                    Impact = "Improved domain logic completeness"
                });
            }
        }

        return recommendations;
    }

    private List<ValidationRecommendation> GenerateValueObjectRecommendations(DomainLogicResult domainLogic)
    {
        var recommendations = new List<ValidationRecommendation>();

        // Check for value objects without validation
        foreach (var valueObject in domainLogic.ValueObjects)
        {
            recommendations.Add(new ValidationRecommendation
            {
                Type = ValidationRecommendationType.ValueObject,
                Category = "Validation",
                Description = $"Consider adding validation logic for value object '{valueObject}'",
                Priority = ValidationPriority.High,
                Impact = "Improved data integrity and domain logic enforcement"
            });
        }

        return recommendations;
    }

    private List<ValidationRecommendation> GenerateBusinessRuleRecommendations(DomainLogicResult domainLogic)
    {
        var recommendations = new List<ValidationRecommendation>();

        // Check for business rules without documentation
        foreach (var rule in domainLogic.BusinessRules)
        {
            if (string.IsNullOrWhiteSpace(rule.Description))
            {
                recommendations.Add(new ValidationRecommendation
                {
                    Type = ValidationRecommendationType.BusinessRule,
                    Category = "Documentation",
                    Description = $"Add documentation for business rule '{rule.Name}'",
                    Priority = ValidationPriority.Low,
                    Impact = "Improved code understanding and maintenance"
                });
            }
        }

        return recommendations;
    }

    private List<ValidationRecommendation> GenerateServiceRecommendations(DomainLogicResult domainLogic)
    {
        var recommendations = new List<ValidationRecommendation>();

        // Check for services without error handling
        foreach (var service in domainLogic.Services)
        {
            recommendations.Add(new ValidationRecommendation
            {
                Type = ValidationRecommendationType.Service,
                Category = "Error Handling",
                Description = $"Consider adding error handling to service '{service}'",
                Priority = ValidationPriority.Medium,
                Impact = "Improved robustness and error recovery"
            });
        }

        return recommendations;
    }

    private QualityMetrics GenerateQualityMetrics(DomainLogicResult domainLogic)
    {
        return new QualityMetrics
        {
            NamingConsistency = CalculateNamingConsistency(domainLogic),
            DocumentationCoverage = CalculateDocumentationCoverage(domainLogic),
            ComplexityScore = CalculateComplexityScore(domainLogic),
            MaintainabilityScore = CalculateMaintainabilityScore(domainLogic),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private double CalculateNamingConsistency(DomainLogicResult domainLogic)
    {
        var allNames = new List<string>();
        allNames.AddRange(domainLogic.Entities);
        allNames.AddRange(domainLogic.ValueObjects);
        allNames.AddRange(domainLogic.BusinessRules.Select(r => r.Name));
        allNames.AddRange(domainLogic.Services);

        if (!allNames.Any())
            return 100.0;

        var consistentNames = allNames.Count(name => !string.IsNullOrEmpty(name) && char.IsUpper(name[0]));
        return (double)consistentNames / allNames.Count * 100.0;
    }

    private double CalculateDocumentationCoverage(DomainLogicResult domainLogic)
    {
        var documentedRules = domainLogic.BusinessRules.Count(rule => !string.IsNullOrWhiteSpace(rule.Description));
        var totalRules = domainLogic.BusinessRules.Count;

        if (totalRules == 0)
            return 100.0;

        return (double)documentedRules / totalRules * 100.0;
    }

    private double CalculateComplexityScore(DomainLogicResult domainLogic)
    {
        // Simple complexity calculation based on component count and relationships
        var totalComponents = domainLogic.Entities.Count + domainLogic.ValueObjects.Count + domainLogic.BusinessRules.Count + domainLogic.Services.Count;
        
        if (totalComponents == 0)
            return 0.0;

        // Higher component count = higher complexity
        return Math.Min(totalComponents * 10.0, 100.0);
    }

    private double CalculateMaintainabilityScore(DomainLogicResult domainLogic)
    {
        // Simple maintainability calculation based on documentation and naming
        var namingScore = CalculateNamingConsistency(domainLogic);
        var documentationScore = CalculateDocumentationCoverage(domainLogic);
        
        return (namingScore + documentationScore) / 2.0;
    }

    private List<ImprovementSuggestion> GenerateImprovementSuggestions(DomainLogicResult domainLogic)
    {
        var suggestions = new List<ImprovementSuggestion>();

        // Generate general improvement suggestions
        suggestions.Add(new ImprovementSuggestion
        {
            Category = "Code Organization",
            Description = "Consider organizing domain logic into logical modules",
            Priority = ImprovementPriority.Medium,
            Impact = "Improved code organization and maintainability"
        });

        suggestions.Add(new ImprovementSuggestion
        {
            Category = "Testing",
            Description = "Add unit tests for domain logic components",
            Priority = ImprovementPriority.High,
            Impact = "Improved code quality and reliability"
        });

        suggestions.Add(new ImprovementSuggestion
        {
            Category = "Performance",
            Description = "Consider implementing caching for frequently accessed domain logic",
            Priority = ImprovementPriority.Low,
            Impact = "Improved application performance"
        });

        return suggestions;
    }
}
