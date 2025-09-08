using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// A configurable coding standards analyzer that can enforce specific coding standards
    /// on code generation agents. This service integrates with the existing framework architecture.
    /// </summary>
    public class ConfigurableCodingStandardAnalyzer : ICodingStandardAnalyzer
    {
        private readonly ILogger<ConfigurableCodingStandardAnalyzer> _logger;
        private readonly ICodingStandardConfigurationService _configurationService;
        private CodingStandardConfiguration _configuration;
        private readonly CodingStandardAnalyzerStatistics _statistics;

        public ConfigurableCodingStandardAnalyzer(
            ILogger<ConfigurableCodingStandardAnalyzer> logger,
            ICodingStandardConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _configuration = _configurationService.GetDefaultConfiguration();
            _statistics = new CodingStandardAnalyzerStatistics();
        }

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

        public CodingStandardConfiguration GetConfiguration()
        {
            return _configuration;
        }

        public async Task UpdateConfigurationAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating coding standards configuration");

            // Validate the configuration
            var validationResult = _configurationService.ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors)}");
            }

            _configuration = configuration;
            _statistics.LastConfigurationUpdate = DateTime.UtcNow;
            _statistics.TotalStandards = configuration.Standards.Count;
            _statistics.TotalRules = configuration.Standards.Sum(s => s.Rules.Count);
            _statistics.EnabledStandards = configuration.Standards.Count(s => s.IsEnabled);
            _statistics.EnabledRules = configuration.Standards.Sum(s => s.Rules.Count(r => r.IsEnabled));
            _statistics.ConfiguredAgents = configuration.AgentSettings.Count;
            _statistics.ConfiguredFileTypes = configuration.FileTypeSettings.Count;

            await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);
            _logger.LogInformation("Coding standards configuration updated successfully");
        }

        public async Task LoadConfigurationAsync(string source, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading coding standards configuration from {Source}", source);

            CodingStandardConfiguration configuration;

            if (File.Exists(source))
            {
                configuration = await _configurationService.LoadFromFileAsync(source, cancellationToken);
            }
            else
            {
                configuration = await _configurationService.LoadFromJsonAsync(source, cancellationToken);
            }

            await UpdateConfigurationAsync(configuration, cancellationToken);
            _logger.LogInformation("Coding standards configuration loaded successfully");
        }

        public async Task<List<CodingStandard>> GetAvailableStandardsAsync()
        {
            return await Task.FromResult(_configuration.Standards.ToList());
        }

        public async Task<List<CodingStandard>> GetStandardsForAgentAsync(string agentId)
        {
            var agentSettings = _configuration.AgentSettings.GetValueOrDefault(agentId);
            if (agentSettings == null || !agentSettings.IsEnabled)
            {
                return new List<CodingStandard>();
            }

            var applicableStandards = _configuration.Standards
                .Where(s => s.IsEnabled && (agentSettings.AppliedStandards.Count == 0 || agentSettings.AppliedStandards.Contains(s.Id)))
                .Where(s => !agentSettings.ExcludedRules.Any(er => s.Rules.Any(r => r.Id == er)))
                .ToList();

            return await Task.FromResult(applicableStandards);
        }

        public async Task<List<CodingStandard>> GetStandardsForFileTypeAsync(string fileExtension)
        {
            var fileTypeSettings = _configuration.FileTypeSettings.GetValueOrDefault(fileExtension);
            if (fileTypeSettings == null || !fileTypeSettings.IsEnabled)
            {
                return new List<CodingStandard>();
            }

            var applicableStandards = _configuration.Standards
                .Where(s => s.IsEnabled && (fileTypeSettings.AppliedStandards.Count == 0 || fileTypeSettings.AppliedStandards.Contains(s.Id)))
                .Where(s => !fileTypeSettings.ExcludedRules.Any(er => s.Rules.Any(r => r.Id == er)))
                .ToList();

            return await Task.FromResult(applicableStandards);
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

        public async Task<CodingStandardAnalyzerStatistics> GetStatisticsAsync()
        {
            return await Task.FromResult(_statistics);
        }

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

        private async Task<CodingStandardValidationResult> ValidateAgainstStandardAsync(
            string code, 
            CodingStandard standard, 
            string? filePath, 
            CancellationToken cancellationToken)
        {
            var result = new CodingStandardValidationResult();

            foreach (var rule in standard.Rules.Where(r => r.IsEnabled))
            {
                if (IsRuleApplicable(rule, filePath))
                {
                    var violations = await ValidateRuleAsync(code, rule, filePath, cancellationToken);
                    result.Violations.AddRange(violations);
                }
            }

            return result;
        }

        private async Task<List<CodingStandardViolation>> ValidateRuleAsync(
            string code, 
            CodingStandardRule rule, 
            string? filePath, 
            CancellationToken cancellationToken)
        {
            var violations = new List<CodingStandardViolation>();

            try
            {
                switch (rule.Type)
                {
                    case CodingStandardRuleType.Pattern:
                        violations.AddRange(ValidatePatternRule(code, rule, filePath));
                        break;
                    case CodingStandardRuleType.Naming:
                        violations.AddRange(ValidateNamingRule(code, rule, filePath));
                        break;
                    case CodingStandardRuleType.Formatting:
                        violations.AddRange(ValidateFormattingRule(code, rule, filePath));
                        break;
                    case CodingStandardRuleType.Structure:
                        violations.AddRange(ValidateStructureRule(code, rule, filePath));
                        break;
                    case CodingStandardRuleType.Security:
                        violations.AddRange(ValidateSecurityRule(code, rule, filePath));
                        break;
                    case CodingStandardRuleType.Performance:
                        violations.AddRange(ValidatePerformanceRule(code, rule, filePath));
                        break;
                    case CodingStandardRuleType.Custom:
                        violations.AddRange(await ValidateCustomRuleAsync(code, rule, filePath, cancellationToken));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating rule {RuleName}", rule.Name);
            }

            return violations;
        }

        private List<CodingStandardViolation> ValidatePatternRule(string code, CodingStandardRule rule, string? filePath)
        {
            var violations = new List<CodingStandardViolation>();

            try
            {
                var regex = new Regex(rule.Pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var matches = regex.Matches(code);

                foreach (Match match in matches)
                {
                    var violation = new CodingStandardViolation
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        Severity = rule.Severity,
                        Message = rule.ErrorMessage,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(code, match.Index),
                        ColumnNumber = GetColumnNumber(code, match.Index),
                        CodeSnippet = match.Value,
                        SuggestedFix = rule.SuggestedFix,
                        Category = rule.Category
                    };

                    violations.Add(violation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pattern validation for rule {RuleName}", rule.Name);
            }

            return violations;
        }

        private List<CodingStandardViolation> ValidateNamingRule(string code, CodingStandardRule rule, string? filePath)
        {
            var violations = new List<CodingStandardViolation>();

            // Basic naming convention validation
            var namingPatterns = new Dictionary<string, string>
            {
                { "class", @"class\s+([a-zA-Z_][a-zA-Z0-9_]*)" },
                { "method", @"(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:[a-zA-Z_][a-zA-Z0-9_<>,\s]*\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\s*\(" },
                { "property", @"(?:public|private|protected|internal)\s+(?:static\s+)?(?:[a-zA-Z_][a-zA-Z0-9_<>,\s]*\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\s*\{\s*get" },
                { "variable", @"(?:var|let|const)\s+([a-zA-Z_][a-zA-Z0-9_]*)" }
            };

            foreach (var pattern in namingPatterns)
            {
                var regex = new Regex(pattern.Value, RegexOptions.Multiline);
                var matches = regex.Matches(code);

                foreach (Match match in matches)
                {
                    var name = match.Groups[1].Value;
                    if (!IsValidName(name, rule.Pattern))
                    {
                        var violation = new CodingStandardViolation
                        {
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Message = $"{rule.ErrorMessage} - {pattern.Key} '{name}' does not follow naming convention",
                            FilePath = filePath,
                            LineNumber = GetLineNumber(code, match.Index),
                            ColumnNumber = GetColumnNumber(code, match.Index),
                            CodeSnippet = match.Value,
                            SuggestedFix = rule.SuggestedFix,
                            Category = rule.Category
                        };

                        violations.Add(violation);
                    }
                }
            }

            return violations;
        }

        private List<CodingStandardViolation> ValidateFormattingRule(string code, CodingStandardRule rule, string? filePath)
        {
            var violations = new List<CodingStandardViolation>();

            // Basic formatting validation
            var lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // Check for trailing whitespace
                if (rule.Pattern.Contains("no-trailing-whitespace") && line.EndsWith(" ") || line.EndsWith("\t"))
                {
                    violations.Add(new CodingStandardViolation
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        Severity = rule.Severity,
                        Message = "Line contains trailing whitespace",
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        CodeSnippet = line,
                        SuggestedFix = "Remove trailing whitespace",
                        Category = rule.Category
                    });
                }

                // Check line length
                if (rule.Pattern.Contains("max-line-length") && line.Length > 120)
                {
                    violations.Add(new CodingStandardViolation
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        Severity = rule.Severity,
                        Message = $"Line length ({line.Length}) exceeds maximum allowed length",
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        CodeSnippet = line,
                        SuggestedFix = "Break line into multiple lines",
                        Category = rule.Category
                    });
                }
            }

            return violations;
        }

        private List<CodingStandardViolation> ValidateStructureRule(string code, CodingStandardRule rule, string? filePath)
        {
            var violations = new List<CodingStandardViolation>();

            // Basic structure validation
            if (rule.Pattern.Contains("require-using-statements") && !code.Contains("using "))
            {
                violations.Add(new CodingStandardViolation
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Severity = rule.Severity,
                    Message = "File should contain using statements",
                    FilePath = filePath,
                    SuggestedFix = "Add appropriate using statements",
                    Category = rule.Category
                });
            }

            return violations;
        }

        private List<CodingStandardViolation> ValidateSecurityRule(string code, CodingStandardRule rule, string? filePath)
        {
            var violations = new List<CodingStandardViolation>();

            // Basic security validation
            var securityPatterns = new Dictionary<string, string>
            {
                { "sql-injection", @"(?:SqlCommand|ExecuteReader|ExecuteScalar).*\+.*" },
                { "hardcoded-password", @"password\s*=\s*[""'][^""']*[""']" },
                { "eval-usage", @"eval\s*\(" }
            };

            foreach (var pattern in securityPatterns)
            {
                if (rule.Pattern.Contains(pattern.Key))
                {
                    var regex = new Regex(pattern.Value, RegexOptions.IgnoreCase);
                    var matches = regex.Matches(code);

                    foreach (Match match in matches)
                    {
                        violations.Add(new CodingStandardViolation
                        {
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Message = $"Potential security issue: {pattern.Key}",
                            FilePath = filePath,
                            LineNumber = GetLineNumber(code, match.Index),
                            ColumnNumber = GetColumnNumber(code, match.Index),
                            CodeSnippet = match.Value,
                            SuggestedFix = rule.SuggestedFix,
                            Category = rule.Category
                        });
                    }
                }
            }

            return violations;
        }

        private List<CodingStandardViolation> ValidatePerformanceRule(string code, CodingStandardRule rule, string? filePath)
        {
            var violations = new List<CodingStandardViolation>();

            // Basic performance validation
            var performancePatterns = new Dictionary<string, string>
            {
                { "string-concatenation", @"string\s+\w+\s*=\s*[""'][^""']*[""']\s*\+" },
                { "boxing", @"object\s+\w+\s*=\s*\d+" },
                { "unnecessary-linq", @"\.Where\([^)]*\)\.First\(\)" }
            };

            foreach (var pattern in performancePatterns)
            {
                if (rule.Pattern.Contains(pattern.Key))
                {
                    var regex = new Regex(pattern.Value, RegexOptions.IgnoreCase);
                    var matches = regex.Matches(code);

                    foreach (Match match in matches)
                    {
                        violations.Add(new CodingStandardViolation
                        {
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Message = $"Potential performance issue: {pattern.Key}",
                            FilePath = filePath,
                            LineNumber = GetLineNumber(code, match.Index),
                            ColumnNumber = GetColumnNumber(code, match.Index),
                            CodeSnippet = match.Value,
                            SuggestedFix = rule.SuggestedFix,
                            Category = rule.Category
                        });
                    }
                }
            }

            return violations;
        }

        private async Task<List<CodingStandardViolation>> ValidateCustomRuleAsync(
            string code, 
            CodingStandardRule rule, 
            string? filePath, 
            CancellationToken cancellationToken)
        {
            var violations = new List<CodingStandardViolation>();

            // Custom validation logic would be implemented here
            // This could involve calling external validation services or custom validation functions
            await Task.CompletedTask;

            return violations;
        }

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
