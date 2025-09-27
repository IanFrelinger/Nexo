using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Utility methods for ConfigurableCodingStandardAnalyzer.
    /// Contains helper methods for standards filtering, auto-fix, validation, and statistics.
    /// </summary>
    public partial class ConfigurableCodingStandardAnalyzer
    {
        /// <summary>
        /// Gets applicable standards for a file path and agent.
        /// </summary>
        private async Task<List<CodingStandard>> GetApplicableStandardsAsync(string? filePath, string? agentId)
        {
            var applicableStandards = new List<CodingStandard>();

            // Get standards for agent
            if (!string.IsNullOrEmpty(agentId))
            {
                var agentStandards = await GetStandardsForAgentAsync(agentId);
                applicableStandards.AddRange(agentStandards);
            }

            // Get standards for file type
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileExtension = Path.GetExtension(filePath);
                var fileTypeStandards = await GetStandardsForFileTypeAsync(fileExtension);
                applicableStandards.AddRange(fileTypeStandards);
            }

            // If no specific standards found, use global standards
            if (!applicableStandards.Any())
            {
                applicableStandards = _configuration.Standards.Where(s => s.IsEnabled).ToList();
            }

            // Remove duplicates and sort by priority
            return applicableStandards
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .OrderByDescending(s => s.Priority)
                .ToList();
        }

        /// <summary>
        /// Checks if a rule is applicable to a file.
        /// </summary>
        private bool IsRuleApplicable(CodingStandardRule rule, string? filePath)
        {
            if (!rule.IsEnabled)
                return false;

            if (string.IsNullOrEmpty(filePath))
                return true;

            var fileExtension = Path.GetExtension(filePath);
            return rule.FilePatterns.Count == 0 || rule.FilePatterns.Any(pattern => 
                pattern == fileExtension || 
                pattern == "*" || 
                (pattern.StartsWith("*") && fileExtension.EndsWith(pattern.Substring(1))));
        }

        /// <summary>
        /// Checks if a rule can be auto-fixed.
        /// </summary>
        private bool CanAutoFix(CodingStandardRule rule)
        {
            return !string.IsNullOrEmpty(rule.SuggestedFix) && 
                   _configuration.GlobalSettings.AutoFixEnabled;
        }

        /// <summary>
        /// Applies auto-fix to code based on a rule.
        /// </summary>
        private (string FixedCode, bool FixApplied) ApplyAutoFix(string code, CodingStandardRule rule)
        {
            // Basic auto-fix implementation
            if (rule.SuggestedFix?.Contains("remove-trailing-whitespace") == true)
            {
                var lines = code.Split('\n');
                var fixedLines = lines.Select(line => line.TrimEnd()).ToArray();
                return (string.Join("\n", fixedLines), true);
            }

            return (code, false);
        }

        /// <summary>
        /// Validates if a name follows the specified pattern.
        /// </summary>
        private bool IsValidName(string name, string pattern)
        {
            try
            {
                var regex = new Regex(pattern);
                return regex.IsMatch(name);
            }
            catch
            {
                return true; // If pattern is invalid, assume name is valid
            }
        }

        /// <summary>
        /// Gets the line number for a character index in code.
        /// </summary>
        private int GetLineNumber(string code, int index)
        {
            return code.Substring(0, index).Split('\n').Length;
        }

        /// <summary>
        /// Gets the column number for a character index in code.
        /// </summary>
        private int GetColumnNumber(string code, int index)
        {
            var lines = code.Substring(0, index).Split('\n');
            return lines.Last().Length + 1;
        }

        /// <summary>
        /// Calculates quality score based on violations.
        /// </summary>
        private int CalculateQualityScore(List<CodingStandardViolation> violations)
        {
            if (!violations.Any())
                return 100;

            var totalPenalty = violations.Sum(v => (int)v.Severity * 10);
            var score = Math.Max(0, 100 - totalPenalty);
            return score;
        }

        /// <summary>
        /// Determines if code is valid based on configuration settings.
        /// </summary>
        private bool DetermineIfValid(CodingStandardValidationResult result, List<CodingStandard> applicableStandards)
        {
            var globalSettings = _configuration.GlobalSettings;

            // Check if score meets minimum requirement
            if (result.Score < globalSettings.MinimumQualityScore)
                return false;

            // Check violation counts
            if (result.Violations.Count > globalSettings.MaxViolationsAllowed)
                return false;

            // Check critical violations
            if (globalSettings.FailOnCriticalViolations && 
                result.Violations.Any(v => v.Severity == CodingStandardSeverity.Critical))
                return false;

            // Check error violations
            if (globalSettings.FailOnErrorViolations && 
                result.Violations.Any(v => v.Severity == CodingStandardSeverity.Error))
                return false;

            return true;
        }

        /// <summary>
        /// Generates a summary of validation results.
        /// </summary>
        private string GenerateSummary(CodingStandardValidationResult result)
        {
            var violationCounts = result.ViolationCounts;
            var totalViolations = result.Violations.Count;
            var criticalCount = violationCounts[CodingStandardSeverity.Critical];
            var errorCount = violationCounts[CodingStandardSeverity.Error];
            var warningCount = violationCounts[CodingStandardSeverity.Warning];
            var infoCount = violationCounts[CodingStandardSeverity.Info];

            if (totalViolations == 0)
            {
                return $"Code quality score: {result.Score}/100. No violations found.";
            }

            var summary = $"Code quality score: {result.Score}/100. ";
            summary += $"Violations: {totalViolations} total";
            
            if (criticalCount > 0) summary += $", {criticalCount} critical";
            if (errorCount > 0) summary += $", {errorCount} errors";
            if (warningCount > 0) summary += $", {warningCount} warnings";
            if (infoCount > 0) summary += $", {infoCount} info";

            return summary;
        }

        /// <summary>
        /// Calculates average validation time.
        /// </summary>
        private double CalculateAverageValidationTime(DateTime startTime)
        {
            var elapsed = DateTime.UtcNow - startTime;
            var totalTime = _statistics.TotalValidations * _statistics.AverageValidationTimeMs + elapsed.TotalMilliseconds;
            return totalTime / (_statistics.TotalValidations + 1);
        }
    }
}
