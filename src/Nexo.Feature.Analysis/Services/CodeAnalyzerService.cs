using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Enums;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Service for analyzing code quality and structure within the pipeline architecture.
    /// </summary>
    public class CodeAnalyzerService : ICodeAnalyzer
    {
        private readonly ILogger<CodeAnalyzerService> _logger;

        public CodeAnalyzerService(ILogger<CodeAnalyzerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes code for quality issues and returns analysis results.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Analysis result as a string</returns>
        public string AnalyzeCode(string code)
        {
            _logger.LogInformation("Starting code analysis");
            
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Empty or null code provided for analysis");
                return "No code provided for analysis";
            }

            try
            {
                var analysisResult = new AnalysisResult
                {
                    Success = true,
                    Message = "Code analysis completed successfully",
                    Issues = new List<AnalysisIssue>(),
                    Metrics = new Dictionary<string, double>(),
                    Summary = "Code quality assessment completed"
                };

                // Basic code analysis metrics
                analysisResult.Metrics["linesOfCode"] = code.Split('\n').Length;
                analysisResult.Metrics["characterCount"] = code.Length;
                analysisResult.Metrics["complexity"] = CalculateComplexity(code);

                // Check for common issues
                var issues = DetectCodeIssues(code);
                analysisResult.Issues.AddRange(issues);

                _logger.LogInformation("Code analysis completed with {IssueCount} issues", issues.Count);
                
                return $"Analysis completed: {analysisResult.Metrics["linesOfCode"]} lines, {issues.Count} issues found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during code analysis");
                return $"Analysis failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Analyzes code using the base IAnalyzer interface.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        public void Analyze(string code)
        {
            AnalyzeCode(code);
        }

        /// <summary>
        /// Calculates code complexity based on various metrics.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Complexity score</returns>
        private double CalculateComplexity(string code)
        {
            var complexity = 1.0;
            
            // Count control flow statements
            var controlFlowKeywords = new[] { "if", "else", "for", "while", "switch", "case", "catch", "try" };
            foreach (var keyword in controlFlowKeywords)
            {
                complexity += CountOccurrences(code, keyword) * 0.5;
            }

            // Count nested structures
            complexity += CountNestedStructures(code) * 0.3;

            return Math.Min(complexity, 10.0); // Cap at 10
        }

        /// <summary>
        /// Detects common code quality issues.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>List of detected issues</returns>
        private List<AnalysisIssue> DetectCodeIssues(string code)
        {
            var issues = new List<AnalysisIssue>();

            // Check for long methods
            var lines = code.Split('\n');
            if (lines.Length > 50)
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = "Warning",
                    Message = "Method is too long. Consider breaking it into smaller methods.",
                    Location = "Method",
                    Category = "Maintainability"
                });
            }

            // Check for magic numbers
            if (ContainsMagicNumbers(code))
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = "Info",
                    Message = "Consider extracting magic numbers into named constants.",
                    Location = "Code",
                    Category = "Readability"
                });
            }

            // Check for potential null reference issues
            if (ContainsPotentialNullReferences(code))
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = "Warning",
                    Message = "Potential null reference detected. Consider adding null checks.",
                    Location = "Code",
                    Category = "Safety"
                });
            }

            return issues;
        }

        /// <summary>
        /// Counts occurrences of a keyword in the code.
        /// </summary>
        /// <param name="code">The code to search</param>
        /// <param name="keyword">The keyword to count</param>
        /// <returns>Number of occurrences</returns>
        private int CountOccurrences(string code, string keyword)
        {
            return code.Split(new[] { keyword }, StringSplitOptions.None).Length - 1;
        }

        /// <summary>
        /// Counts nested structures in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Number of nested structures</returns>
        private int CountNestedStructures(string code)
        {
            var nestingLevel = 0;
            var maxNesting = 0;
            
            foreach (var line in code.Split('\n'))
            {
                var trimmedLine = line.TrimStart();
                var indentLevel = line.Length - trimmedLine.Length;
                
                if (indentLevel > nestingLevel)
                {
                    nestingLevel = indentLevel;
                    maxNesting = Math.Max(maxNesting, nestingLevel);
                }
                else if (indentLevel < nestingLevel)
                {
                    nestingLevel = indentLevel;
                }
            }

            return maxNesting;
        }

        /// <summary>
        /// Checks if the code contains magic numbers.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>True if magic numbers are found</returns>
        private bool ContainsMagicNumbers(string code)
        {
            // Simple check for numbers that might be magic numbers
            var words = code.Split(new[] { ' ', '\t', '\n', '\r', '(', ')', '{', '}', '[', ']', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                if (int.TryParse(word, out var number))
                {
                    // Consider numbers other than 0, 1, -1 as potential magic numbers
                    if (number != 0 && number != 1 && number != -1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks for potential null reference issues.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>True if potential null references are found</returns>
        private bool ContainsPotentialNullReferences(string code)
        {
            // Simple check for potential null reference patterns
            var patterns = new[] { ".Length", ".Count", ".ToString()", ".Equals" };
            
            foreach (var pattern in patterns)
            {
                if (code.Contains(pattern) && !code.Contains("?.Length") && !code.Contains("?.Count"))
                {
                    return true;
                }
            }

            return false;
        }
    }
} 