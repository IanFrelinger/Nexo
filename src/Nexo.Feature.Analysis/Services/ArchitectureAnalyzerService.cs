using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Enums;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Service for analyzing code architecture and design patterns within the pipeline architecture.
    /// </summary>
    public class ArchitectureAnalyzerService : IArchitectureAnalyzer
    {
        private readonly ILogger<ArchitectureAnalyzerService> _logger;

        public ArchitectureAnalyzerService(ILogger<ArchitectureAnalyzerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes code architecture and design patterns.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Architecture analysis result as a string</returns>
        public string AnalyzeArchitecture(string code)
        {
            _logger.LogInformation("Starting architecture analysis");
            
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Empty or null code provided for architecture analysis");
                return "No code provided for architecture analysis";
            }

            try
            {
                var analysisResult = new AnalysisResult
                {
                    Success = true,
                    Message = "Architecture analysis completed successfully",
                    Issues = new List<AnalysisIssue>(),
                    Metrics = new Dictionary<string, double>(),
                    Summary = "Architecture assessment completed"
                };

                // Architecture analysis metrics
                analysisResult.Metrics["classCount"] = CountClasses(code);
                analysisResult.Metrics["interfaceCount"] = CountInterfaces(code);
                analysisResult.Metrics["methodCount"] = CountMethods(code);
                analysisResult.Metrics["dependencyCount"] = CountDependencies(code);
                analysisResult.Metrics["couplingScore"] = CalculateCouplingScore(code);
                analysisResult.Metrics["cohesionScore"] = CalculateCohesionScore(code);

                // Check for architectural issues
                var issues = DetectArchitecturalIssues(code);
                analysisResult.Issues.AddRange(issues);

                // Check for design patterns
                var patterns = DetectDesignPatterns(code);
                analysisResult.Metrics["patternCount"] = patterns.Count;

                _logger.LogInformation("Architecture analysis completed with {IssueCount} issues and {PatternCount} patterns", 
                    issues.Count, patterns.Count);
                
                return $"Architecture analysis completed: {analysisResult.Metrics["classCount"]} classes, {analysisResult.Metrics["interfaceCount"]} interfaces, {issues.Count} issues found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during architecture analysis");
                return $"Architecture analysis failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Analyzes code using the base IAnalyzer interface.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        public void Analyze(string code)
        {
            AnalyzeArchitecture(code);
        }

        /// <summary>
        /// Counts the number of classes in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Number of classes</returns>
        private int CountClasses(string code)
        {
            var classKeywords = new[] { "class ", "public class ", "private class ", "internal class ", "protected class " };
            return classKeywords.Sum(keyword => CountOccurrences(code, keyword));
        }

        /// <summary>
        /// Counts the number of interfaces in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Number of interfaces</returns>
        private int CountInterfaces(string code)
        {
            var interfaceKeywords = new[] { "interface ", "public interface ", "private interface ", "internal interface " };
            return interfaceKeywords.Sum(keyword => CountOccurrences(code, keyword));
        }

        /// <summary>
        /// Counts the number of methods in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Number of methods</returns>
        private int CountMethods(string code)
        {
            var methodPatterns = new[] { "public ", "private ", "internal ", "protected " };
            var methodCount = 0;
            
            foreach (var pattern in methodPatterns)
            {
                var lines = code.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith(pattern) && 
                        (trimmedLine.Contains("(") && trimmedLine.Contains(")") && !trimmedLine.Contains("class") && !trimmedLine.Contains("interface")))
                    {
                        methodCount++;
                    }
                }
            }

            return methodCount;
        }

        /// <summary>
        /// Counts dependencies in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Number of dependencies</returns>
        private int CountDependencies(string code)
        {
            var dependencyKeywords = new[] { "using ", "import ", "new ", "var " };
            return dependencyKeywords.Sum(keyword => CountOccurrences(code, keyword));
        }

        /// <summary>
        /// Calculates coupling score based on dependencies.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Coupling score (0-10)</returns>
        private double CalculateCouplingScore(string code)
        {
            var dependencies = CountDependencies(code);
            var classes = CountClasses(code);
            
            if (classes == 0) return 0;
            
            var couplingRatio = (double)dependencies / classes;
            return Math.Min(couplingRatio, 10.0);
        }

        /// <summary>
        /// Calculates cohesion score based on method distribution.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>Cohesion score (0-10)</returns>
        private double CalculateCohesionScore(string code)
        {
            var methods = CountMethods(code);
            var classes = CountClasses(code);
            
            if (classes == 0) return 0;
            
            var methodsPerClass = (double)methods / classes;
            
            // Higher cohesion when methods are well distributed (not too few, not too many per class)
            if (methodsPerClass >= 2 && methodsPerClass <= 10)
            {
                return 8.0;
            }
            else if (methodsPerClass > 10)
            {
                return Math.Max(10.0 - (methodsPerClass - 10) * 0.5, 0);
            }
            else
            {
                return methodsPerClass * 4.0; // Low cohesion for classes with few methods
            }
        }

        /// <summary>
        /// Detects architectural issues in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>List of architectural issues</returns>
        private List<AnalysisIssue> DetectArchitecturalIssues(string code)
        {
            var issues = new List<AnalysisIssue>();

            // Check for large classes
            var lines = code.Split('\n');
            var classLines = 0;
            var inClass = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Contains("class ") && !trimmedLine.Contains("interface"))
                {
                    if (inClass && classLines > 200)
                    {
                        issues.Add(new AnalysisIssue
                        {
                            Severity = "Warning",
                            Message = "Class is too large. Consider breaking it into smaller classes.",
                            Location = "Class",
                            Category = "Architecture"
                        });
                    }
                    
                    inClass = true;
                    classLines = 0;
                }
                else if (inClass)
                {
                    classLines++;
                }
            }

            // Check for tight coupling
            var couplingScore = CalculateCouplingScore(code);
            if (couplingScore > 8.0)
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = "Warning",
                    Message = "High coupling detected. Consider reducing dependencies between classes.",
                    Location = "Architecture",
                    Category = "Coupling"
                });
            }

            // Check for low cohesion
            var cohesionScore = CalculateCohesionScore(code);
            if (cohesionScore < 5.0)
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = "Info",
                    Message = "Low cohesion detected. Consider grouping related functionality together.",
                    Location = "Architecture",
                    Category = "Cohesion"
                });
            }

            return issues;
        }

        /// <summary>
        /// Detects common design patterns in the code.
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>List of detected patterns</returns>
        private List<string> DetectDesignPatterns(string code)
        {
            var patterns = new List<string>();

            // Singleton pattern
            if (code.Contains("private static") && code.Contains("GetInstance") && code.Contains("lock"))
            {
                patterns.Add("Singleton");
            }

            // Factory pattern
            if (code.Contains("Create") && code.Contains("Factory") && code.Contains("new"))
            {
                patterns.Add("Factory");
            }

            // Observer pattern
            if (code.Contains("event") && code.Contains("EventHandler"))
            {
                patterns.Add("Observer");
            }

            // Strategy pattern
            if (code.Contains("interface") && code.Contains("Strategy") && code.Contains("Context"))
            {
                patterns.Add("Strategy");
            }

            // Command pattern
            if (code.Contains("Execute") && code.Contains("Command") && code.Contains("Invoker"))
            {
                patterns.Add("Command");
            }

            return patterns;
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
    }
} 