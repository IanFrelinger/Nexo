using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Safety
{
    /// <summary>
    /// Core functionality for EnhancedSafetyValidator.
    /// </summary>
    public partial class EnhancedSafetyValidator
    {
        /// <summary>
        /// Validates generated code for safety issues
        /// </summary>
        /// <param name="code">The code to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Safety validation result</returns>
        public async Task<SafetyValidationResult> ValidateGeneratedCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting enhanced safety validation");

                var result = new SafetyValidationResult
                {
                    IsValid = true,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>(),
                    Recommendations = new List<string>()
                };

                // Apply all safety rules
                foreach (var rule in _safetyRules)
                {
                    var ruleResult = await rule.ValidateAsync(code, cancellationToken);
                    result.Issues.AddRange(ruleResult.Issues);
                    result.Recommendations.AddRange(ruleResult.Recommendations);
                }

                // AI-powered safety analysis
                var aiResult = await PerformAISafetyAnalysisAsync(code, cancellationToken);
                result.Issues.AddRange(aiResult.Issues);
                result.Recommendations.AddRange(aiResult.Recommendations);

                // Determine overall validity
                result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical || i.Severity == SafetySeverity.High);
                result.RequiresHumanReview = result.Issues.Any(i => i.Severity == SafetySeverity.High);
                result.IsBlocked = result.Issues.Any(i => i.Severity == SafetySeverity.Critical);

                _logger.LogInformation("Safety validation completed. Valid: {IsValid}, Issues: {IssueCount}, Blocked: {IsBlocked}",
                    result.IsValid, result.Issues.Count, result.IsBlocked);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during safety validation");
                throw;
            }
        }

        /// <summary>
        /// Initializes safety rules
        /// </summary>
        private List<SafetyRule> InitializeSafetyRules()
        {
            return new List<SafetyRule>
            {
                new FileSystemSafetyRule(),
                new NetworkSafetyRule(),
                new SystemCommandSafetyRule(),
                new DataDestructionSafetyRule(),
                new SecurityVulnerabilitySafetyRule(),
                new ResourceExhaustionSafetyRule()
            };
        }

        /// <summary>
        /// Performs AI-powered safety analysis
        /// </summary>
        private async Task<SafetyValidationResult> PerformAISafetyAnalysisAsync(string code, CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Analyze the following C# code for safety issues and potential security vulnerabilities:

```csharp
{code}
```

Please identify:
1. File system operations that could be dangerous
2. Network operations that could be insecure
3. System command executions
4. Data destruction operations
5. Security vulnerabilities
6. Resource exhaustion risks

Rate each issue as Critical, High, Medium, or Low severity.
Provide specific recommendations for fixing each issue.
";

                var request = new ModelRequest
                {
                    Input = prompt,
                    Temperature = 0.1,
                    MaxTokens = 2000
                };

                var response = await _aiProvider.ExecuteAsync(request, cancellationToken);
                
                if (string.IsNullOrEmpty(response.Response))
                {
                    return new SafetyValidationResult
                    {
                        IsValid = true,
                        ValidationTime = DateTime.UtcNow,
                        Issues = new List<SafetyIssue>(),
                        Recommendations = new List<string>()
                    };
                }

                return ParseAISafetyResponse(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI safety analysis failed, continuing with static analysis only");
                return new SafetyValidationResult
                {
                    IsValid = true,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>(),
                    Recommendations = new List<string>()
                };
            }
        }

        /// <summary>
        /// Parses AI safety response
        /// </summary>
        private SafetyValidationResult ParseAISafetyResponse(string aiResponse)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Simple parsing of AI response - in production, this would be more sophisticated
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("-") || line.Trim().StartsWith("*"))
                {
                    var content = line.Trim().TrimStart('-', '*', ' ');
                    
                    // Try to extract severity
                    var severity = SafetySeverity.Low;
                    if (content.Contains("Critical", StringComparison.OrdinalIgnoreCase))
                        severity = SafetySeverity.Critical;
                    else if (content.Contains("High", StringComparison.OrdinalIgnoreCase))
                        severity = SafetySeverity.High;
                    else if (content.Contains("Medium", StringComparison.OrdinalIgnoreCase))
                        severity = SafetySeverity.Medium;

                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.Security,
                        Severity = severity,
                        Message = content,
                        Recommendation = "Review and address the identified issue"
                    });
                }
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical || i.Severity == SafetySeverity.High);
            return result;
        }
    }
}
