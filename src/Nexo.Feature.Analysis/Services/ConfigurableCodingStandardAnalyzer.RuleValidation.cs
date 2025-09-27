using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Rule validation functionality for ConfigurableCodingStandardAnalyzer.
    /// Handles different types of rule validation including pattern, naming, formatting, structure, security, and performance rules.
    /// </summary>
    public partial class ConfigurableCodingStandardAnalyzer
    {
        /// <summary>
        /// Validates code against a specific standard.
        /// </summary>
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

        /// <summary>
        /// Validates code against a specific rule.
        /// </summary>
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

        /// <summary>
        /// Validates pattern-based rules.
        /// </summary>
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

        /// <summary>
        /// Validates naming convention rules.
        /// </summary>
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

        /// <summary>
        /// Validates formatting rules.
        /// </summary>
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

        /// <summary>
        /// Validates structure rules.
        /// </summary>
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

        /// <summary>
        /// Validates security rules.
        /// </summary>
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

        /// <summary>
        /// Validates performance rules.
        /// </summary>
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

        /// <summary>
        /// Validates custom rules.
        /// </summary>
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
    }
}
