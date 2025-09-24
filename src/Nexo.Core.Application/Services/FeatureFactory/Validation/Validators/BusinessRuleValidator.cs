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
/// Validates business rules and their consistency
/// </summary>
public class BusinessRuleValidator
{
    private readonly ILogger<BusinessRuleValidator> _logger;

    public BusinessRuleValidator(ILogger<BusinessRuleValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BusinessRuleValidationResult> ValidateBusinessRulesAsync(List<BusinessRule> rules, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating {RuleCount} business rules", rules.Count);

            var result = new BusinessRuleValidationResult
            {
                Success = true,
                ValidatedAt = DateTime.UtcNow
            };

            foreach (var rule in rules)
            {
                var ruleIssues = await ValidateBusinessRuleAsync(rule.Name, cancellationToken);
                result.Issues.AddRange(ruleIssues);
            }

            // Check for rule conflicts
            var conflictIssues = await CheckBusinessRuleConflictsAsync(rules.Select(r => r.Name).ToList(), cancellationToken);
            result.Issues.AddRange(conflictIssues);

            // Check rule completeness
            var completenessWarnings = await CheckBusinessRuleCompletenessAsync(rules, cancellationToken);
            result.Warnings.AddRange(completenessWarnings);

            result.Success = result.Issues.Count == 0;
            result.Message = result.Success ? "Business rule validation completed successfully" : $"Business rule validation completed with {result.Issues.Count} issues";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during business rule validation");
            return new BusinessRuleValidationResult
            {
                Success = false,
                Message = $"Business rule validation failed: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<List<BusinessRuleIssue>> ValidateBusinessRuleAsync(string rule, CancellationToken cancellationToken)
    {
        var issues = new List<BusinessRuleIssue>();

        try
        {
            _logger.LogDebug("Validating business rule: {RuleName}", rule);

            // Check rule name validity
            if (string.IsNullOrWhiteSpace(rule))
            {
                issues.Add(new BusinessRuleIssue
                {
                    RuleName = rule,
                    IssueType = BusinessRuleIssueType.InvalidName,
                    Message = "Business rule name cannot be null or empty",
                    Severity = IssueSeverity.Error
                });
            }

            // Check naming conventions
            if (!string.IsNullOrEmpty(rule) && !IsValidRuleName(rule))
            {
                issues.Add(new BusinessRuleIssue
                {
                    RuleName = rule,
                    IssueType = BusinessRuleIssueType.NamingConvention,
                    Message = "Business rule name should follow PascalCase convention",
                    Severity = IssueSeverity.Warning
                });
            }

            // Check for reserved keywords
            if (IsReservedKeyword(rule))
            {
                issues.Add(new BusinessRuleIssue
                {
                    RuleName = rule,
                    IssueType = BusinessRuleIssueType.ReservedKeyword,
                    Message = $"Business rule name '{rule}' is a reserved keyword",
                    Severity = IssueSeverity.Error
                });
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rule: {RuleName}", rule);
            issues.Add(new BusinessRuleIssue
            {
                RuleName = rule,
                IssueType = BusinessRuleIssueType.ValidationError,
                Message = $"Error validating business rule: {ex.Message}",
                Severity = IssueSeverity.Error
            });
            return issues;
        }
    }

    private async Task<List<BusinessRuleIssue>> CheckBusinessRuleConflictsAsync(List<string> rules, CancellationToken cancellationToken)
    {
        var issues = new List<BusinessRuleIssue>();

        try
        {
            _logger.LogDebug("Checking business rule conflicts for {RuleCount} rules", rules.Count);

            // Check for duplicate rule names
            var duplicateRules = rules.GroupBy(r => r)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateRules)
            {
                issues.Add(new BusinessRuleIssue
                {
                    RuleName = duplicate,
                    IssueType = BusinessRuleIssueType.DuplicateRule,
                    Message = $"Duplicate business rule name: {duplicate}",
                    Severity = IssueSeverity.Error
                });
            }

            // Check for conflicting rule logic
            var conflictingRules = FindConflictingRules(rules);
            foreach (var conflict in conflictingRules)
            {
                issues.Add(new BusinessRuleIssue
                {
                    RuleName = conflict,
                    IssueType = BusinessRuleIssueType.ConflictingLogic,
                    Message = $"Conflicting business rule logic detected: {conflict}",
                    Severity = IssueSeverity.Warning
                });
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking business rule conflicts");
            return issues;
        }
    }

    private async Task<List<BusinessRuleWarning>> CheckBusinessRuleCompletenessAsync(List<BusinessRule> rules, CancellationToken cancellationToken)
    {
        var warnings = new List<BusinessRuleWarning>();

        try
        {
            _logger.LogDebug("Checking business rule completeness for {RuleCount} rules", rules.Count);

            // Check for missing rule descriptions
            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Description))
                {
                    warnings.Add(new BusinessRuleWarning
                    {
                        RuleName = rule.Name,
                        WarningType = BusinessRuleWarningType.MissingDescription,
                        Message = $"Business rule '{rule.Name}' is missing a description",
                        Severity = WarningSeverity.Medium
                    });
                }
            }

            // Check for incomplete rule logic
            var incompleteRules = rules.Where(r => string.IsNullOrWhiteSpace(r.Logic)).ToList();
            foreach (var rule in incompleteRules)
            {
                warnings.Add(new BusinessRuleWarning
                {
                    RuleName = rule.Name,
                    WarningType = BusinessRuleWarningType.IncompleteLogic,
                    Message = $"Business rule '{rule.Name}' has incomplete logic",
                    Severity = WarningSeverity.High
                });
            }

            return warnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking business rule completeness");
            return warnings;
        }
    }

    private bool IsValidRuleName(string ruleName)
    {
        if (string.IsNullOrEmpty(ruleName))
            return false;

        // Check if starts with uppercase letter
        if (!char.IsUpper(ruleName[0]))
            return false;

        // Check if contains only letters, numbers, and underscores
        return ruleName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private bool IsReservedKeyword(string ruleName)
    {
        var reservedKeywords = new HashSet<string>
        {
            "class", "interface", "enum", "struct", "namespace", "using",
            "public", "private", "protected", "internal", "static", "readonly",
            "const", "virtual", "abstract", "override", "sealed", "partial"
        };

        return reservedKeywords.Contains(ruleName.ToLower());
    }

    private List<string> FindConflictingRules(List<string> rules)
    {
        var conflictingRules = new List<string>();
        
        // Simple conflict detection
        // In a real implementation, this would analyze actual rule logic
        foreach (var rule in rules)
        {
            if (rule.Contains("Conflict") || rule.Contains("Contradict"))
            {
                conflictingRules.Add(rule);
            }
        }

        return conflictingRules;
    }
}
