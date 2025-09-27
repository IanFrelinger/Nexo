using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    public partial class ConfigurableCodingStandardAnalyzer
    {
        public async Task<CodingStandardValidationResult> ValidateCodeAsync(
            string code, 
            string? filePath = null, 
            string? agentId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting code validation for {FilePath} by agent {AgentId}", filePath, agentId);

            var startTime = DateTime.UtcNow;
            var result = new CodingStandardValidationResult
            {
                IsValid = true,
                Score = 100
            };

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    result.IsValid = false;
                    result.Score = 0;
                    result.Summary = "Code is null or empty";
                    return result;
                }

                // Get applicable standards
                var applicableStandards = await GetApplicableStandardsAsync(filePath, agentId);
                result.AppliedStandards = applicableStandards.Select(s => s.Name).ToList();

                // Validate against each applicable standard
                foreach (var standard in applicableStandards)
                {
                    var standardResult = await ValidateAgainstStandardAsync(code, standard, filePath, cancellationToken);
                    result.Violations.AddRange(standardResult.Violations);
                    result.Suggestions.AddRange(standardResult.Suggestions);
                }

                // Calculate overall score
                result.Score = CalculateQualityScore(result.Violations);
                result.IsValid = DetermineIfValid(result, applicableStandards);

                // Generate summary
                result.Summary = GenerateSummary(result);

                // Update statistics
                _statistics.TotalValidations++;
                _statistics.TotalViolations += result.Violations.Count;
                _statistics.LastValidationTime = DateTime.UtcNow;
                _statistics.AverageValidationTimeMs = CalculateAverageValidationTime(startTime);

                _logger.LogInformation("Code validation completed. Score: {Score}, Violations: {ViolationCount}", 
                    result.Score, result.Violations.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during code validation");
                result.IsValid = false;
                result.Score = 0;
                result.Summary = $"Validation failed: {ex.Message}";
                return result;
            }
        }

        public async Task<Dictionary<string, CodingStandardValidationResult>> ValidateCodeFilesAsync(
            Dictionary<string, string> codeFiles, 
            string? agentId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting validation of {FileCount} code files", codeFiles.Count);

            var results = new Dictionary<string, CodingStandardValidationResult>();

            foreach (var file in codeFiles)
            {
                try
                {
                    var result = await ValidateCodeAsync(file.Value, file.Key, agentId, cancellationToken);
                    results[file.Key] = result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating file {FilePath}", file.Key);
                    results[file.Key] = new CodingStandardValidationResult
                    {
                        IsValid = false,
                        Score = 0,
                        Summary = $"Validation failed: {ex.Message}"
                    };
                }
            }

            _logger.LogInformation("Completed validation of {FileCount} code files", codeFiles.Count);
            return results;
        }

        public async Task<(string FixedCode, List<string> AppliedFixes)> AutoFixCodeAsync(
            string code, 
            string? filePath = null, 
            string? agentId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting auto-fix for code from {FilePath} by agent {AgentId}", filePath, agentId);

            var fixedCode = code;
            var appliedFixes = new List<string>();

            try
            {
                // Get applicable standards
                var applicableStandards = await GetApplicableStandardsAsync(filePath, agentId);

                foreach (var standard in applicableStandards)
                {
                    foreach (var rule in standard.Rules.Where(r => r.IsEnabled))
                    {
                        if (CanAutoFix(rule))
                        {
                            var (newCode, fixApplied) = ApplyAutoFix(fixedCode, rule);
                            if (fixApplied)
                            {
                                fixedCode = newCode;
                                appliedFixes.Add($"Applied fix for rule '{rule.Name}': {rule.SuggestedFix}");
                            }
                        }
                    }
                }

                _statistics.TotalAutoFixes += appliedFixes.Count;
                _logger.LogInformation("Auto-fix completed. Applied {FixCount} fixes", appliedFixes.Count);

                return (fixedCode, appliedFixes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-fix");
                return (code, new List<string> { $"Auto-fix failed: {ex.Message}" });
            }
        }

        public bool IsConfigured()
        {
            return _configuration != null && _configuration.IsEnabled && _configuration.Standards.Any();
        }

        public Task<CodingStandardAnalyzerStatistics> GetStatisticsAsync()
        {
            return Task.FromResult(_statistics);
        }
    }
}
