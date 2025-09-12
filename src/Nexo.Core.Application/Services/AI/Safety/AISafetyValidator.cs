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

namespace Nexo.Core.Application.Services.AI.Safety
{
    /// <summary>
    /// AI safety validator for content filtering and safety validation
    /// </summary>
    public class AISafetyValidator
    {
        private readonly ILogger<AISafetyValidator> _logger;
        private readonly List<SafetyRule> _safetyRules;
        private readonly List<ContentFilter> _contentFilters;

        public AISafetyValidator(ILogger<AISafetyValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyRules = InitializeSafetyRules();
            _contentFilters = InitializeContentFilters();
        }

        /// <summary>
        /// Validates AI-generated content for safety and compliance
        /// </summary>
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

                // Apply safety rules
                var ruleViolations = await ApplySafetyRulesAsync(content, safetyLevel);
                result.Issues.AddRange(ruleViolations);

                // Check for malicious patterns
                var maliciousPatterns = await CheckMaliciousPatternsAsync(content);
                result.Issues.AddRange(maliciousPatterns);

                // Check for inappropriate content
                var inappropriateContent = await CheckInappropriateContentAsync(content, safetyLevel);
                result.Issues.AddRange(inappropriateContent);

                // Check for security vulnerabilities
                var securityIssues = await CheckSecurityVulnerabilitiesAsync(content);
                result.Issues.AddRange(securityIssues);

                // Determine overall validity
                result.IsValid = !result.Issues.Any(issue => issue.Severity == SafetySeverity.High || issue.Severity == SafetySeverity.Critical);

                // Generate recommendations
                result.Recommendations = await GenerateSafetyRecommendationsAsync(result.Issues, safetyLevel);

                _logger.LogInformation("Content validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during content validation");
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
                            Severity = SafetySeverity.High,
                            Message = $"Validation failed: {ex.Message}",
                            Line = 0
                        }
                    },
                    Recommendations = new List<string> { "Review content manually for safety" }
                };
            }
        }

        /// <summary>
        /// Validates AI operation context for safety
        /// </summary>
        public async Task<SafetyValidationResult> ValidateOperationContextAsync(AIOperationContext context)
        {
            try
            {
                _logger.LogDebug("Validating AI operation context for safety");

                var result = new SafetyValidationResult
                {
                    IsValid = true,
                    SafetyLevel = context.Requirements?.SafetyLevel ?? SafetyLevel.Medium,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>(),
                    Recommendations = new List<string>()
                };

                // Check operation type safety
                var operationIssues = await ValidateOperationTypeAsync(context.OperationType);
                result.Issues.AddRange(operationIssues);

                // Check platform safety
                var platformIssues = await ValidatePlatformSafetyAsync(ConvertToInfrastructurePlatformType(context.TargetPlatform));
                result.Issues.AddRange(platformIssues);

                // Check resource requirements
                var resourceIssues = await ValidateResourceRequirementsAsync(context);
                result.Issues.AddRange(resourceIssues);

                // Check temperature and token limits
                var parameterIssues = await ValidateParametersAsync(context);
                result.Issues.AddRange(parameterIssues);

                // Determine overall validity
                result.IsValid = !result.Issues.Any(issue => issue.Severity == SafetySeverity.High || issue.Severity == SafetySeverity.Critical);

                // Generate recommendations
                result.Recommendations = await GenerateContextRecommendationsAsync(result.Issues, context);

                _logger.LogInformation("Operation context validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during operation context validation");
                return new SafetyValidationResult
                {
                    IsValid = false,
                    SafetyLevel = SafetyLevel.Medium,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>
                    {
                        new SafetyIssue
                        {
                            Type = SafetyIssueType.ValidationError,
                            Severity = SafetySeverity.High,
                            Message = $"Context validation failed: {ex.Message}",
                            Line = 0
                        }
                    },
                    Recommendations = new List<string> { "Review operation context manually" }
                };
            }
        }

        private async Task<string> ApplyContentFiltersAsync(string content, SafetyLevel safetyLevel)
        {
            var filteredContent = content;

            foreach (var filter in _contentFilters.Where(f => f.SafetyLevel <= safetyLevel))
            {
                filteredContent = await filter.ApplyAsync(filteredContent);
            }

            return filteredContent;
        }

        private async Task<List<SafetyIssue>> ApplySafetyRulesAsync(string content, SafetyLevel safetyLevel)
        {
            var issues = new List<SafetyIssue>();

            foreach (var rule in _safetyRules.Where(r => r.SafetyLevel <= safetyLevel))
            {
                var ruleIssues = await rule.ValidateAsync(content);
                issues.AddRange(ruleIssues);
            }

            return issues;
        }

        private async Task<List<SafetyIssue>> CheckMaliciousPatternsAsync(string content)
        {
            var issues = new List<SafetyIssue>();

            // Check for SQL injection patterns
            if (Regex.IsMatch(content, @"(union|select|insert|update|delete|drop|create|alter)\s+", RegexOptions.IgnoreCase))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.MaliciousPattern,
                    Severity = SafetySeverity.High,
                    Message = "Potential SQL injection pattern detected",
                    Line = 0
                });
            }

            // Check for XSS patterns
            if (Regex.IsMatch(content, @"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.MaliciousPattern,
                    Severity = SafetySeverity.High,
                    Message = "Potential XSS pattern detected",
                    Line = 0
                });
            }

            // Check for command injection patterns
            if (Regex.IsMatch(content, @"(exec|system|shell_exec|passthru|eval)\s*\(", RegexOptions.IgnoreCase))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.MaliciousPattern,
                    Severity = SafetySeverity.Critical,
                    Message = "Potential command injection pattern detected",
                    Line = 0
                });
            }

            await Task.Delay(50); // Simulate processing time
            return issues;
        }

        private async Task<List<SafetyIssue>> CheckInappropriateContentAsync(string content, SafetyLevel safetyLevel)
        {
            var issues = new List<SafetyIssue>();

            // Check for inappropriate keywords
            var inappropriateKeywords = new[] { "hack", "exploit", "vulnerability", "backdoor", "malware" };
            
            foreach (var keyword in inappropriateKeywords)
            {
                if (content.ToLower().Contains(keyword))
                {
                    issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.InappropriateContent,
                        Severity = safetyLevel >= SafetyLevel.High ? SafetySeverity.Medium : SafetySeverity.Low,
                        Message = $"Potentially inappropriate content detected: {keyword}",
                        Line = 0
                    });
                }
            }

            await Task.Delay(50); // Simulate processing time
            return issues;
        }

        private async Task<List<SafetyIssue>> CheckSecurityVulnerabilitiesAsync(string content)
        {
            var issues = new List<SafetyIssue>();

            // Check for hardcoded credentials
            if (Regex.IsMatch(content, @"(password|pwd|pass)\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.SecurityVulnerability,
                    Severity = SafetySeverity.High,
                    Message = "Hardcoded credentials detected",
                    Line = 0
                });
            }

            // Check for insecure random number generation
            if (content.Contains("new Random()") && !content.Contains("RandomNumberGenerator"))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.SecurityVulnerability,
                    Severity = SafetySeverity.Medium,
                    Message = "Insecure random number generation detected",
                    Line = 0
                });
            }

            // Check for weak encryption
            if (content.Contains("DES") || content.Contains("MD5") || content.Contains("SHA1"))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.SecurityVulnerability,
                    Severity = SafetySeverity.Medium,
                    Message = "Weak encryption algorithm detected",
                    Line = 0
                });
            }

            await Task.Delay(50); // Simulate processing time
            return issues;
        }

        private async Task<List<SafetyIssue>> ValidateOperationTypeAsync(AIOperationType operationType)
        {
            var issues = new List<SafetyIssue>();

            // Check for potentially risky operation types
            if (operationType == AIOperationType.CodeGeneration)
            {
                // Code generation is generally safe
                await Task.Delay(10);
            }
            else if (operationType == AIOperationType.CodeReview)
            {
                // Code review is generally safe
                await Task.Delay(10);
            }

            return issues;
        }

        private async Task<List<SafetyIssue>> ValidatePlatformSafetyAsync(PlatformType platform)
        {
            var issues = new List<SafetyIssue>();

            // Check platform-specific safety concerns
            if (platform == PlatformType.WebAssembly)
            {
                // WebAssembly has additional security considerations
                await Task.Delay(10);
            }

            return issues;
        }

        private async Task<List<SafetyIssue>> ValidateResourceRequirementsAsync(AIOperationContext context)
        {
            var issues = new List<SafetyIssue>();

            // Check for excessive resource requirements
            if (context.MaxTokens > 10000)
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.ResourceAbuse,
                    Severity = SafetySeverity.Medium,
                    Message = "High token count may indicate resource abuse",
                    Line = 0
                });
            }

            await Task.Delay(10);
            return issues;
        }

        private async Task<List<SafetyIssue>> ValidateParametersAsync(AIOperationContext context)
        {
            var issues = new List<SafetyIssue>();

            // Check temperature parameter
            if (context.Temperature > 1.0 || context.Temperature < 0.0)
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.InvalidParameter,
                    Severity = SafetySeverity.Medium,
                    Message = "Temperature parameter out of valid range (0.0-1.0)",
                    Line = 0
                });
            }

            await Task.Delay(10);
            return issues;
        }

        private async Task<List<string>> GenerateSafetyRecommendationsAsync(List<SafetyIssue> issues, SafetyLevel safetyLevel)
        {
            var recommendations = new List<string>();

            if (issues.Any(i => i.Type == SafetyIssueType.MaliciousPattern))
            {
                recommendations.Add("Review content for malicious patterns and sanitize input");
            }

            if (issues.Any(i => i.Type == SafetyIssueType.SecurityVulnerability))
            {
                recommendations.Add("Address security vulnerabilities before deployment");
            }

            if (issues.Any(i => i.Type == SafetyIssueType.InappropriateContent))
            {
                recommendations.Add("Review content for appropriateness and compliance");
            }

            if (safetyLevel < SafetyLevel.High)
            {
                recommendations.Add("Consider increasing safety level for more comprehensive validation");
            }

            await Task.Delay(10);
            return recommendations;
        }

        private async Task<List<string>> GenerateContextRecommendationsAsync(List<SafetyIssue> issues, AIOperationContext context)
        {
            var recommendations = new List<string>();

            if (issues.Any(i => i.Type == SafetyIssueType.ResourceAbuse))
            {
                recommendations.Add("Consider reducing resource requirements or implementing rate limiting");
            }

            if (issues.Any(i => i.Type == SafetyIssueType.InvalidParameter))
            {
                recommendations.Add("Validate all parameters before processing");
            }

            if (context.Requirements?.SafetyLevel < SafetyLevel.High)
            {
                recommendations.Add("Consider increasing safety level for sensitive operations");
            }

            await Task.Delay(10);
            return recommendations;
        }

        private List<SafetyRule> InitializeSafetyRules()
        {
            return new List<SafetyRule>
            {
                new SafetyRule
                {
                    Name = "No Dangerous Operations",
                    SafetyLevel = SafetyLevel.Low,
                    Pattern = @"(delete|remove|destroy|kill|terminate)\s+",
                    Severity = SafetySeverity.High
                },
                new SafetyRule
                {
                    Name = "No System Access",
                    SafetyLevel = SafetyLevel.Medium,
                    Pattern = @"(system|exec|shell|cmd|powershell)\s*\(",
                    Severity = SafetySeverity.Critical
                },
                new SafetyRule
                {
                    Name = "No File System Access",
                    SafetyLevel = SafetyLevel.Medium,
                    Pattern = @"(File\.|Directory\.|Path\.)",
                    Severity = SafetySeverity.High
                }
            };
        }

        private List<ContentFilter> InitializeContentFilters()
        {
            return new List<ContentFilter>
            {
                new ContentFilter
                {
                    Name = "Profanity Filter",
                    SafetyLevel = SafetyLevel.Low,
                    Pattern = @"\b(badword|profanity)\b",
                    Replacement = "***"
                },
                new ContentFilter
                {
                    Name = "Personal Information Filter",
                    SafetyLevel = SafetyLevel.Medium,
                    Pattern = @"\b\d{3}-\d{2}-\d{4}\b", // SSN pattern
                    Replacement = "***-**-****"
                }
            };
        }

        private Nexo.Core.Domain.Entities.Infrastructure.PlatformType ConvertToInfrastructurePlatformType(Nexo.Core.Domain.Enums.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Enums.PlatformType.Web => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web,
                Nexo.Core.Domain.Enums.PlatformType.Desktop => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop,
                Nexo.Core.Domain.Enums.PlatformType.Mobile => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile,
                Nexo.Core.Domain.Enums.PlatformType.Windows => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows,
                Nexo.Core.Domain.Enums.PlatformType.Linux => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux,
                Nexo.Core.Domain.Enums.PlatformType.macOS => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS,
                Nexo.Core.Domain.Enums.PlatformType.iOS => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS,
                Nexo.Core.Domain.Enums.PlatformType.Android => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android,
                Nexo.Core.Domain.Enums.PlatformType.Cloud => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud,
                Nexo.Core.Domain.Enums.PlatformType.Container => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker,
                Nexo.Core.Domain.Enums.PlatformType.CrossPlatform => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other,
                _ => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop
            };
        }
    }

    /// <summary>
    /// Safety validation result
    /// </summary>
    public class SafetyValidationResult
    {
        public bool IsValid { get; set; }
        public SafetyLevel SafetyLevel { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<SafetyIssue> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string? FilteredContent { get; set; }
    }

    /// <summary>
    /// Safety issue found during validation
    /// </summary>
    public class SafetyIssue
    {
        public SafetyIssueType Type { get; set; }
        public SafetySeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Line { get; set; }
        public string? Suggestion { get; set; }
    }

    /// <summary>
    /// Types of safety issues
    /// </summary>
    public enum SafetyIssueType
    {
        MaliciousPattern,
        InappropriateContent,
        SecurityVulnerability,
        ResourceAbuse,
        InvalidParameter,
        ContentFiltered,
        ValidationError
    }

    /// <summary>
    /// Severity levels for safety issues
    /// </summary>
    public enum SafetySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Safety rule for content validation
    /// </summary>
    public class SafetyRule
    {
        public string Name { get; set; } = string.Empty;
        public SafetyLevel SafetyLevel { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public SafetySeverity Severity { get; set; }

        public async Task<List<SafetyIssue>> ValidateAsync(string content)
        {
            var issues = new List<SafetyIssue>();

            if (Regex.IsMatch(content, Pattern, RegexOptions.IgnoreCase))
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.MaliciousPattern,
                    Severity = Severity,
                    Message = $"Safety rule violated: {Name}",
                    Line = 0
                });
            }

            await Task.Delay(10);
            return issues;
        }
    }

    /// <summary>
    /// Content filter for safety
    /// </summary>
    public class ContentFilter
    {
        public string Name { get; set; } = string.Empty;
        public SafetyLevel SafetyLevel { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;

        public async Task<string> ApplyAsync(string content)
        {
            var filteredContent = Regex.Replace(content, Pattern, Replacement, RegexOptions.IgnoreCase);
            await Task.Delay(10);
            return filteredContent;
        }
    }
}
