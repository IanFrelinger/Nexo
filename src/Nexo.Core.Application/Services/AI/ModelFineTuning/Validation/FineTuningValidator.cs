using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.ModelFineTuning.Validation
{
    /// <summary>
    /// Validates fine-tuning data for quality and compatibility
    /// </summary>
    public class FineTuningValidator
    {
        private readonly ILogger _logger;

        public FineTuningValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FineTuningValidationResult> ValidateFineTuningDataAsync(FineTuningData data)
        {
            try
            {
                _logger.LogDebug("Validating fine-tuning data with {SampleCount} samples", data.Samples.Count);

                var result = new FineTuningValidationResult
                {
                    IsValid = true,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<ValidationIssue>(),
                    Recommendations = new List<string>()
                };

                // Validate data quality
                var qualityIssues = await ValidateDataQualityAsync(data);
                result.Issues.AddRange(qualityIssues);

                // Validate data format
                var formatIssues = await ValidateDataFormatAsync(data);
                result.Issues.AddRange(formatIssues);

                // Validate data diversity
                var diversityIssues = await ValidateDataDiversityAsync(data);
                result.Issues.AddRange(diversityIssues);

                // Generate recommendations
                result.Recommendations = await GenerateValidationRecommendationsAsync(data, result.Issues);

                // Determine overall validity
                result.IsValid = !result.Issues.Any(issue => issue.Severity == ValidationSeverity.Critical);

                _logger.LogInformation("Fine-tuning data validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate fine-tuning data");
                return new FineTuningValidationResult
                {
                    IsValid = false,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = ValidationIssueType.ValidationError,
                            Severity = ValidationSeverity.Critical,
                            Message = $"Validation failed: {ex.Message}",
                            Line = 0
                        }
                    },
                    Recommendations = new List<string> { "Review data manually for quality and format" }
                };
            }
        }

        private async Task<List<ValidationIssue>> ValidateDataQualityAsync(FineTuningData data)
        {
            var issues = new List<ValidationIssue>();

            // Check for empty samples
            if (data.Samples.Count == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataQuality,
                    Severity = ValidationSeverity.Critical,
                    Message = "No training samples provided",
                    Line = 0
                });
            }

            // Check for minimum sample count
            if (data.Samples.Count < 10)
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataQuality,
                    Severity = ValidationSeverity.High,
                    Message = "Insufficient training samples (minimum 10 recommended)",
                    Line = 0
                });
            }

            // Check for sample quality
            var emptySamples = data.Samples.Count(s => string.IsNullOrWhiteSpace(s.Input) || string.IsNullOrWhiteSpace(s.Output));
            if (emptySamples > 0)
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataQuality,
                    Severity = ValidationSeverity.Medium,
                    Message = $"{emptySamples} samples have empty input or output",
                    Line = 0
                });
            }

            await Task.Delay(50);
            return issues;
        }

        private async Task<List<ValidationIssue>> ValidateDataFormatAsync(FineTuningData data)
        {
            var issues = new List<ValidationIssue>();

            // Check for consistent format
            var inputLengths = data.Samples.Select(s => s.Input.Length).ToList();
            var outputLengths = data.Samples.Select(s => s.Output.Length).ToList();

            var avgInputLength = inputLengths.Average();
            var avgOutputLength = outputLengths.Average();

            // Check for extreme length variations
            if (inputLengths.Any(l => l > avgInputLength * 10))
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataFormat,
                    Severity = ValidationSeverity.Medium,
                    Message = "Some input samples are significantly longer than average",
                    Line = 0
                });
            }

            if (outputLengths.Any(l => l > avgOutputLength * 10))
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataFormat,
                    Severity = ValidationSeverity.Medium,
                    Message = "Some output samples are significantly longer than average",
                    Line = 0
                });
            }

            await Task.Delay(50);
            return issues;
        }

        private async Task<List<ValidationIssue>> ValidateDataDiversityAsync(FineTuningData data)
        {
            var issues = new List<ValidationIssue>();

            // Check for duplicate samples
            var uniqueSamples = data.Samples.Select(s => $"{s.Input}|{s.Output}").Distinct().Count();
            var duplicateCount = data.Samples.Count - uniqueSamples;

            if (duplicateCount > data.Samples.Count * 0.1) // More than 10% duplicates
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataDiversity,
                    Severity = ValidationSeverity.Medium,
                    Message = $"{duplicateCount} duplicate samples found (may reduce training effectiveness)",
                    Line = 0
                });
            }

            // Check for input diversity
            var uniqueInputs = data.Samples.Select(s => s.Input).Distinct().Count();
            if (uniqueInputs < data.Samples.Count * 0.8) // Less than 80% unique inputs
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DataDiversity,
                    Severity = ValidationSeverity.Low,
                    Message = "Low input diversity may limit model generalization",
                    Line = 0
                });
            }

            await Task.Delay(50);
            return issues;
        }

        private async Task<List<string>> GenerateValidationRecommendationsAsync(FineTuningData data, List<ValidationIssue> issues)
        {
            var recommendations = new List<string>();

            if (data.Samples.Count < 100)
            {
                recommendations.Add("Consider adding more training samples for better model performance");
            }

            if (issues.Any(i => i.Type == ValidationIssueType.DataDiversity))
            {
                recommendations.Add("Increase data diversity by adding more varied samples");
            }

            if (issues.Any(i => i.Type == ValidationIssueType.DataFormat))
            {
                recommendations.Add("Normalize data format for consistent training");
            }

            await Task.Delay(10);
            return recommendations;
        }
    }
}
