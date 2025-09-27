using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    public partial class ConfigurableCodingStandardAnalyzer
    {
        private bool CanAutoFix(CodingStandardRule rule)
        {
            return !string.IsNullOrEmpty(rule.SuggestedFix) && 
                   _configuration.GlobalSettings.AutoFixEnabled;
        }

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

        private int GetLineNumber(string code, int index)
        {
            return code.Substring(0, index).Split('\n').Length;
        }

        private int GetColumnNumber(string code, int index)
        {
            var lines = code.Substring(0, index).Split('\n');
            return lines.Last().Length + 1;
        }

        private int CalculateQualityScore(List<CodingStandardViolation> violations)
        {
            if (!violations.Any())
                return 100;

            var totalPenalty = violations.Sum(v => (int)v.Severity * 10);
            var score = Math.Max(0, 100 - totalPenalty);
            return score;
        }

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

        private double CalculateAverageValidationTime(DateTime startTime)
        {
            var elapsed = DateTime.UtcNow - startTime;
            var totalTime = _statistics.TotalValidations * _statistics.AverageValidationTimeMs + elapsed.TotalMilliseconds;
            return totalTime / (_statistics.TotalValidations + 1);
        }
    }
}
