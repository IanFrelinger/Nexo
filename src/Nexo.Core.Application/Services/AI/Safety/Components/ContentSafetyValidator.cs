using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Safety;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Safety.Components
{
    public class ContentSafetyValidator
    {
        private readonly ILogger _logger;
        private readonly List<SafetyRule> _safetyRules;
        private readonly List<ContentFilter> _contentFilters;

        public ContentSafetyValidator(ILogger logger, List<SafetyRule> safetyRules, List<ContentFilter> contentFilters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyRules = safetyRules ?? throw new ArgumentNullException(nameof(safetyRules));
            _contentFilters = contentFilters ?? throw new ArgumentNullException(nameof(contentFilters));
        }

        public async Task<SafetyValidationResult> ValidateContentAsync(string content, SafetyLevel safetyLevel, string context = "")
        {
            try
            {
                _logger.LogDebug("Validating content for safety level {SafetyLevel}", safetyLevel);

                var result = new SafetyValidationResult
                {
                    IsValid = true,
                    SafetyLevel = safetyLevel,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>(),
                    Recommendations = new List<string>()
                };

                // Apply content filters
                var filteredContent = await ApplyContentFiltersAsync(content, safetyLevel);
                if (filteredContent != content)
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.ContentFiltered,
                        Severity = SafetySeverity.Low,
                        Message = "Content was filtered for safety",
                        Line = 0
                    });
                    result.FilteredContent = filteredContent;
                }

                // Validate against safety rules
                await ValidateSafetyRulesAsync(content, result);

                // Check for harmful patterns
                await CheckHarmfulPatternsAsync(content, result);

                // Validate context-specific safety
                await ValidateContextSafetyAsync(content, context, result);

                result.IsValid = result.Issues.All(issue => issue.Severity != SafetySeverity.Critical);

                _logger.LogDebug("Content validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Content safety validation failed");
                return new SafetyValidationResult
                {
                    IsValid = false,
                    SafetyLevel = safetyLevel,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>
                    {
                        new SafetyIssue
                        {
                            Type = SafetyIssueType.ValidationError,
                            Severity = SafetySeverity.Critical,
                            Message = $"Validation error: {ex.Message}",
                            Line = 0
                        }
                    },
                    Recommendations = new List<string> { "Review content manually" }
                };
            }
        }

        private async Task<string> ApplyContentFiltersAsync(string content, SafetyLevel safetyLevel)
        {
            var filteredContent = content;

            foreach (var filter in _contentFilters)
            {
                try
                {
                    var regex = new Regex(filter.Pattern, RegexOptions.IgnoreCase);
                    
                    switch (filter.Action.ToLower())
                    {
                        case "replace":
                            filteredContent = regex.Replace(filteredContent, "[FILTERED]");
                            break;
                        case "remove":
                            filteredContent = regex.Replace(filteredContent, "");
                            break;
                        case "block":
                            if (regex.IsMatch(filteredContent))
                            {
                                throw new InvalidOperationException($"Content blocked by filter: {filter.Name}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error applying content filter {FilterName}", filter.Name);
                }
            }

            return filteredContent;
        }

        private async Task ValidateSafetyRulesAsync(string content, SafetyValidationResult result)
        {
            foreach (var rule in _safetyRules)
            {
                try
                {
                    var violation = await CheckSafetyRuleViolationAsync(content, rule);
                    if (violation != null)
                    {
                        result.Issues.Add(violation);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error validating safety rule {RuleName}", rule.Name);
                }
            }
        }

        private async Task<SafetyIssue> CheckSafetyRuleViolationAsync(string content, SafetyRule rule)
        {
            // Implementation would check for specific rule violations
            await Task.Delay(10); // Simulate async operation
            
            // Placeholder logic - in real implementation, this would check for specific patterns
            if (rule.Name.Contains("Harmful") && content.Contains("dangerous"))
            {
                return new SafetyIssue
                {
                    Type = SafetyIssueType.SafetyRuleViolation,
                    Severity = SafetySeverity.High,
                    Message = $"Violation of safety rule: {rule.Name}",
                    Line = 0
                };
            }

            return null;
        }

        private async Task CheckHarmfulPatternsAsync(string content, SafetyValidationResult result)
        {
            var harmfulPatterns = new[]
            {
                @"\b(kill|murder|violence)\b",
                @"\b(hate|discrimination)\b",
                @"\b(illegal|crime)\b"
            };

            foreach (var pattern in harmfulPatterns)
            {
                try
                {
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    if (regex.IsMatch(content))
                    {
                        result.Issues.Add(new SafetyIssue
                        {
                            Type = SafetyIssueType.HarmfulContent,
                            Severity = SafetySeverity.High,
                            Message = $"Harmful pattern detected: {pattern}",
                            Line = 0
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking harmful pattern {Pattern}", pattern);
                }
            }
        }

        private async Task ValidateContextSafetyAsync(string content, string context, SafetyValidationResult result)
        {
            if (string.IsNullOrEmpty(context))
                return;

            try
            {
                // Context-specific validation logic
                if (context.Contains("medical") && content.Contains("diagnosis"))
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.ContextViolation,
                        Severity = SafetySeverity.Medium,
                        Message = "Medical content requires professional review",
                        Line = 0
                    });
                }

                if (context.Contains("financial") && content.Contains("investment"))
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.ContextViolation,
                        Severity = SafetySeverity.Medium,
                        Message = "Financial advice requires professional review",
                        Line = 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating context safety");
            }
        }
    }
}
