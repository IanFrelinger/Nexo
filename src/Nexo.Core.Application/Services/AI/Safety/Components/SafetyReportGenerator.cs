using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Safety;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Safety.Components
{
    public class SafetyReportGenerator
    {
        private readonly ILogger _logger;

        public SafetyReportGenerator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SafetyReport> GenerateReportAsync(string content, SafetyLevel safetyLevel, string context = "")
        {
            try
            {
                _logger.LogInformation("Generating comprehensive safety report");

                var report = new SafetyReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    GeneratedAt = DateTime.UtcNow,
                    ContentLength = content.Length,
                    SafetyLevel = safetyLevel,
                    Context = context,
                    Summary = await GenerateReportSummaryAsync(content, safetyLevel),
                    Details = await GenerateReportDetailsAsync(content, safetyLevel),
                    Recommendations = await GenerateReportRecommendationsAsync(content, safetyLevel),
                    Metadata = await GenerateReportMetadataAsync(content, safetyLevel)
                };

                _logger.LogInformation("Safety report generated successfully. Report ID: {ReportId}", report.ReportId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate safety report");
                return new SafetyReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    GeneratedAt = DateTime.UtcNow,
                    ContentLength = content.Length,
                    SafetyLevel = safetyLevel,
                    Context = context,
                    Summary = $"Report generation failed: {ex.Message}",
                    Details = new Dictionary<string, object>(),
                    Recommendations = new List<string> { "Manual safety review recommended" },
                    Metadata = new Dictionary<string, object>()
                };
            }
        }

        private async Task<string> GenerateReportSummaryAsync(string content, SafetyLevel safetyLevel)
        {
            try
            {
                var summary = $"Safety Report Summary\n";
                summary += $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n";
                summary += $"Content Length: {content.Length} characters\n";
                summary += $"Safety Level: {safetyLevel}\n";
                summary += $"Content Analysis: {await AnalyzeContentAsync(content)}\n";
                summary += $"Safety Assessment: {await AssessSafetyAsync(content, safetyLevel)}\n";

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report summary");
                return $"Summary generation failed: {ex.Message}";
            }
        }

        private async Task<Dictionary<string, object>> GenerateReportDetailsAsync(string content, SafetyLevel safetyLevel)
        {
            var details = new Dictionary<string, object>();

            try
            {
                details["content_analysis"] = await AnalyzeContentAsync(content);
                details["safety_assessment"] = await AssessSafetyAsync(content, safetyLevel);
                details["risk_factors"] = await IdentifyRiskFactorsAsync(content);
                details["safety_indicators"] = await IdentifySafetyIndicatorsAsync(content);
                details["compliance_status"] = await CheckComplianceStatusAsync(content);
                details["recommendations"] = await GenerateRecommendationsAsync(content, safetyLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report details");
                details["error"] = ex.Message;
            }

            return details;
        }

        private async Task<List<string>> GenerateReportRecommendationsAsync(string content, SafetyLevel safetyLevel)
        {
            var recommendations = new List<string>();

            try
            {
                // Content-specific recommendations
                if (content.Length > 1000)
                {
                    recommendations.Add("Consider breaking down long content into smaller sections");
                }

                if (content.Contains("personal information"))
                {
                    recommendations.Add("Review content for personal information disclosure");
                }

                if (content.Contains("medical advice"))
                {
                    recommendations.Add("Ensure medical content is reviewed by qualified professionals");
                }

                if (content.Contains("financial advice"))
                {
                    recommendations.Add("Ensure financial content is reviewed by qualified professionals");
                }

                // Safety level specific recommendations
                switch (safetyLevel)
                {
                    case SafetyLevel.Low:
                        recommendations.Add("Content appears safe for general use");
                        break;
                    case SafetyLevel.Medium:
                        recommendations.Add("Content requires additional review before publication");
                        break;
                    case SafetyLevel.High:
                        recommendations.Add("Content requires expert review before publication");
                        break;
                    case SafetyLevel.Critical:
                        recommendations.Add("Content should not be published without significant modification");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report recommendations");
                recommendations.Add($"Recommendation generation failed: {ex.Message}");
            }

            return recommendations;
        }

        private async Task<Dictionary<string, object>> GenerateReportMetadataAsync(string content, SafetyLevel safetyLevel)
        {
            var metadata = new Dictionary<string, object>();

            try
            {
                metadata["report_version"] = "1.0";
                metadata["generator_version"] = "1.0";
                metadata["content_hash"] = content.GetHashCode();
                metadata["safety_level"] = safetyLevel.ToString();
                metadata["generation_time"] = DateTime.UtcNow;
                metadata["content_metrics"] = await GenerateContentMetricsAsync(content);
                metadata["safety_metrics"] = await GenerateSafetyMetricsAsync(content, safetyLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report metadata");
                metadata["error"] = ex.Message;
            }

            return metadata;
        }

        private async Task<string> AnalyzeContentAsync(string content)
        {
            // Simple content analysis
            var wordCount = content.Split(' ').Length;
            var sentenceCount = content.Split('.').Length;
            var paragraphCount = content.Split('\n').Length;

            return $"Content analysis: {wordCount} words, {sentenceCount} sentences, {paragraphCount} paragraphs";
        }

        private async Task<string> AssessSafetyAsync(string content, SafetyLevel safetyLevel)
        {
            // Simple safety assessment
            var riskFactors = await IdentifyRiskFactorsAsync(content);
            var safetyIndicators = await IdentifySafetyIndicatorsAsync(content);

            if (riskFactors.Count > 3)
                return "High risk content detected";
            else if (riskFactors.Count > 1)
                return "Medium risk content detected";
            else
                return "Low risk content detected";
        }

        private async Task<List<string>> IdentifyRiskFactorsAsync(string content)
        {
            var riskFactors = new List<string>();

            // Check for common risk factors
            if (content.Contains("violence"))
                riskFactors.Add("Violence content");
            if (content.Contains("hate"))
                riskFactors.Add("Hate speech content");
            if (content.Contains("discrimination"))
                riskFactors.Add("Discriminatory content");
            if (content.Contains("illegal"))
                riskFactors.Add("Illegal activity content");

            return riskFactors;
        }

        private async Task<List<string>> IdentifySafetyIndicatorsAsync(string content)
        {
            var safetyIndicators = new List<string>();

            // Check for safety indicators
            if (content.Contains("safety"))
                safetyIndicators.Add("Safety awareness");
            if (content.Contains("responsible"))
                safetyIndicators.Add("Responsible content");
            if (content.Contains("ethical"))
                safetyIndicators.Add("Ethical considerations");

            return safetyIndicators;
        }

        private async Task<string> CheckComplianceStatusAsync(string content)
        {
            // Simple compliance check
            if (content.Contains("personal data"))
                return "GDPR compliance required";
            if (content.Contains("medical"))
                return "HIPAA compliance required";
            if (content.Contains("children"))
                return "COPPA compliance required";
            
            return "Standard compliance";
        }

        private async Task<List<string>> GenerateRecommendationsAsync(string content, SafetyLevel safetyLevel)
        {
            var recommendations = new List<string>();

            // Generate recommendations based on content and safety level
            if (safetyLevel == SafetyLevel.Critical)
            {
                recommendations.Add("Content requires immediate review and modification");
                recommendations.Add("Consider removing or significantly rewriting content");
            }
            else if (safetyLevel == SafetyLevel.High)
            {
                recommendations.Add("Content requires expert review before publication");
                recommendations.Add("Consider additional safety measures");
            }
            else if (safetyLevel == SafetyLevel.Medium)
            {
                recommendations.Add("Content requires additional review");
                recommendations.Add("Consider minor modifications for safety");
            }
            else
            {
                recommendations.Add("Content appears safe for publication");
            }

            return recommendations;
        }

        private async Task<Dictionary<string, object>> GenerateContentMetricsAsync(string content)
        {
            var metrics = new Dictionary<string, object>();

            try
            {
                metrics["word_count"] = content.Split(' ').Length;
                metrics["character_count"] = content.Length;
                metrics["sentence_count"] = content.Split('.').Length;
                metrics["paragraph_count"] = content.Split('\n').Length;
                metrics["average_word_length"] = content.Split(' ').Average(w => w.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate content metrics");
                metrics["error"] = ex.Message;
            }

            return metrics;
        }

        private async Task<Dictionary<string, object>> GenerateSafetyMetricsAsync(string content, SafetyLevel safetyLevel)
        {
            var metrics = new Dictionary<string, object>();

            try
            {
                metrics["safety_level"] = safetyLevel.ToString();
                metrics["risk_score"] = CalculateRiskScore(content);
                metrics["safety_score"] = CalculateSafetyScore(content);
                metrics["compliance_score"] = CalculateComplianceScore(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate safety metrics");
                metrics["error"] = ex.Message;
            }

            return metrics;
        }

        private double CalculateRiskScore(string content)
        {
            // Simple risk score calculation
            var riskWords = new[] { "violence", "hate", "illegal", "dangerous" };
            var riskCount = riskWords.Count(word => content.ToLower().Contains(word));
            return Math.Min(100.0, riskCount * 25.0);
        }

        private double CalculateSafetyScore(string content)
        {
            // Simple safety score calculation
            var safetyWords = new[] { "safe", "responsible", "ethical", "helpful" };
            var safetyCount = safetyWords.Count(word => content.ToLower().Contains(word));
            return Math.Min(100.0, safetyCount * 25.0);
        }

        private double CalculateComplianceScore(string content)
        {
            // Simple compliance score calculation
            var complianceWords = new[] { "compliant", "legal", "approved", "certified" };
            var complianceCount = complianceWords.Count(word => content.ToLower().Contains(word));
            return Math.Min(100.0, complianceCount * 25.0);
        }
    }

    public class SafetyReport
    {
        public string ReportId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int ContentLength { get; set; }
        public SafetyLevel SafetyLevel { get; set; }
        public string Context { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
