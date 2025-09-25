using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Safety.Components
{
    public class ComplianceValidator
    {
        private readonly ILogger _logger;

        public ComplianceValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComplianceValidationResult> ValidateComplianceAsync(string content, string context = "")
        {
            try
            {
                _logger.LogDebug("Validating compliance for content");

                var result = new ComplianceValidationResult
                {
                    ComplianceScore = 0.0,
                    ComplianceIssues = new List<string>(),
                    ComplianceRecommendations = new List<string>(),
                    ValidationTime = DateTime.UtcNow
                };

                // Check GDPR compliance
                await CheckGDPRComplianceAsync(content, result);

                // Check HIPAA compliance
                await CheckHIPAAComplianceAsync(content, result);

                // Check COPPA compliance
                await CheckCOPPAComplianceAsync(content, result);

                // Check accessibility compliance
                await CheckAccessibilityComplianceAsync(content, result);

                // Check industry-specific compliance
                await CheckIndustryComplianceAsync(content, context, result);

                // Calculate overall compliance score
                result.ComplianceScore = CalculateComplianceScore(result.ComplianceIssues);

                _logger.LogDebug("Compliance validation completed. Score: {Score}, Issues: {IssueCount}", 
                    result.ComplianceScore, result.ComplianceIssues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compliance validation failed");
                return new ComplianceValidationResult
                {
                    ComplianceScore = 0.0,
                    ComplianceIssues = new List<string> { $"Validation error: {ex.Message}" },
                    ComplianceRecommendations = new List<string> { "Manual compliance review recommended" },
                    ValidationTime = DateTime.UtcNow
                };
            }
        }

        private async Task CheckGDPRComplianceAsync(string content, ComplianceValidationResult result)
        {
            var gdprPatterns = new[]
            {
                @"\b(personal data|personal information)\b",
                @"\b(email|phone|address)\b",
                @"\b(consent|opt-in|opt-out)\b",
                @"\b(data processing|data collection)\b"
            };

            foreach (var pattern in gdprPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"GDPR compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure GDPR compliance for personal data handling");
                }
            }
        }

        private async Task CheckHIPAAComplianceAsync(string content, ComplianceValidationResult result)
        {
            var hipaaPatterns = new[]
            {
                @"\b(medical|health|patient)\b",
                @"\b(diagnosis|treatment|medication)\b",
                @"\b(health record|medical record)\b",
                @"\b(phi|protected health information)\b"
            };

            foreach (var pattern in hipaaPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"HIPAA compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure HIPAA compliance for health information");
                }
            }
        }

        private async Task CheckCOPPAComplianceAsync(string content, ComplianceValidationResult result)
        {
            var coppaPatterns = new[]
            {
                @"\b(children|kids|minors)\b",
                @"\b(under 13|under 18)\b",
                @"\b(parental consent|parental permission)\b",
                @"\b(child data|children's data)\b"
            };

            foreach (var pattern in coppaPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"COPPA compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure COPPA compliance for children's data");
                }
            }
        }

        private async Task CheckAccessibilityComplianceAsync(string content, ComplianceValidationResult result)
        {
            var accessibilityPatterns = new[]
            {
                @"\b(accessibility|ada|wcag)\b",
                @"\b(screen reader|assistive technology)\b",
                @"\b(alt text|alternative text)\b",
                @"\b(contrast|color blind|visual impairment)\b"
            };

            foreach (var pattern in accessibilityPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"Accessibility compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure accessibility compliance (ADA/WCAG)");
                }
            }
        }

        private async Task CheckIndustryComplianceAsync(string content, string context, ComplianceValidationResult result)
        {
            if (string.IsNullOrEmpty(context))
                return;

            try
            {
                if (context.Contains("financial"))
                {
                    await CheckFinancialComplianceAsync(content, result);
                }

                if (context.Contains("healthcare"))
                {
                    await CheckHealthcareComplianceAsync(content, result);
                }

                if (context.Contains("education"))
                {
                    await CheckEducationComplianceAsync(content, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking industry compliance");
            }
        }

        private async Task CheckFinancialComplianceAsync(string content, ComplianceValidationResult result)
        {
            var financialPatterns = new[]
            {
                @"\b(investment advice|financial advice)\b",
                @"\b(securities|stocks|bonds)\b",
                @"\b(financial planning|wealth management)\b",
                @"\b(risk|return|portfolio)\b"
            };

            foreach (var pattern in financialPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"Financial compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure financial regulatory compliance");
                }
            }
        }

        private async Task CheckHealthcareComplianceAsync(string content, ComplianceValidationResult result)
        {
            var healthcarePatterns = new[]
            {
                @"\b(medical advice|health advice)\b",
                @"\b(diagnosis|treatment|medication)\b",
                @"\b(clinical|therapeutic|medical)\b",
                @"\b(patient care|healthcare)\b"
            };

            foreach (var pattern in healthcarePatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"Healthcare compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure healthcare regulatory compliance");
                }
            }
        }

        private async Task CheckEducationComplianceAsync(string content, ComplianceValidationResult result)
        {
            var educationPatterns = new[]
            {
                @"\b(educational content|learning)\b",
                @"\b(student data|academic records)\b",
                @"\b(ferpa|family educational rights)\b",
                @"\b(educational privacy|student privacy)\b"
            };

            foreach (var pattern in educationPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIssues.Add($"Education compliance issue: {pattern}");
                    result.ComplianceRecommendations.Add("Ensure education regulatory compliance (FERPA)");
                }
            }
        }

        private double CalculateComplianceScore(List<string> complianceIssues)
        {
            if (complianceIssues.Count == 0)
                return 100.0;

            // Simple scoring based on number of compliance issues
            var score = Math.Max(0.0, 100.0 - (complianceIssues.Count * 20.0));
            return score;
        }
    }

    public class ComplianceValidationResult
    {
        public double ComplianceScore { get; set; }
        public List<string> ComplianceIssues { get; set; } = new();
        public List<string> ComplianceRecommendations { get; set; } = new();
        public DateTime ValidationTime { get; set; }
    }
}
