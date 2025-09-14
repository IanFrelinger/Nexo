using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Quality
{
    /// <summary>
    /// Analyzes code quality using multiple techniques including AI-powered analysis
    /// </summary>
    public class CodeQualityAnalyzer : ICodeQualityAnalyzer
    {
        private readonly IModelProvider _aiProvider;
        private readonly ILogger<CodeQualityAnalyzer> _logger;

        public CodeQualityAnalyzer(IModelProvider aiProvider, ILogger<CodeQualityAnalyzer> logger)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<QualityScore> AnalyzeCodeAsync(string code, string toolName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting comprehensive code quality analysis for tool: {ToolName}", toolName);

                var score = new QualityScore
                {
                    ToolName = toolName,
                    AnalyzedAt = DateTime.UtcNow
                };

                // Basic metrics
                score.LinesOfCode = CountLinesOfCode(code);
                score.CyclomaticComplexity = CalculateCyclomaticComplexity(code);
                score.NumberOfMethods = CountMethods(code);
                score.NumberOfClasses = CountClasses(code);

                // Individual component analysis
                var securityTask = AnalyzeSecurityAsync(code, cancellationToken);
                var performanceTask = AnalyzePerformanceAsync(code, cancellationToken);
                var maintainabilityTask = AnalyzeMaintainabilityAsync(code, cancellationToken);
                var testabilityTask = AnalyzeTestabilityAsync(code, cancellationToken);

                await Task.WhenAll(securityTask, performanceTask, maintainabilityTask, testabilityTask);

                score.SecurityScore = securityTask.Result.OverallScore;
                score.PerformanceScore = performanceTask.Result.OverallScore;
                score.MaintainabilityScore = maintainabilityTask.Result.OverallScore;
                score.TestabilityScore = testabilityTask.Result.OverallScore;

                // Additional analysis
                score.ReadabilityScore = await AnalyzeReadabilityAsync(code, cancellationToken);
                score.DocumentationScore = await AnalyzeDocumentationAsync(code, cancellationToken);

                // Calculate overall score (weighted average)
                score.OverallScore = CalculateOverallScore(score);

                // Determine quality level
                score.QualityLevel = DetermineQualityLevel(score.OverallScore);

                // Quality gates
                score.PassesQualityGate = PassesQualityGate(score);
                score.RequiresHumanReview = RequiresHumanReview(score);
                score.IsProductionReady = IsProductionReady(score);

                // AI-powered analysis for additional insights
                await PerformAIAnalysisAsync(code, score, cancellationToken);

                _logger.LogInformation("Code quality analysis completed for {ToolName}. Overall Score: {Score}, Quality Level: {Level}",
                    toolName, score.OverallScore, score.QualityLevel);

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during code quality analysis for tool: {ToolName}", toolName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<QualityScore> AnalyzeSecurityAsync(string code, CancellationToken cancellationToken = default)
        {
            var score = new QualityScore { AnalyzedAt = DateTime.UtcNow };
            var issues = new List<QualityIssue>();

            // Check for common security vulnerabilities
            issues.AddRange(CheckForSQLInjection(code));
            issues.AddRange(CheckForXSS(code));
            issues.AddRange(CheckForPathTraversal(code));
            issues.AddRange(CheckForInsecureRandom(code));
            issues.AddRange(CheckForHardcodedSecrets(code));
            issues.AddRange(CheckForInsecureFileOperations(code));
            issues.AddRange(CheckForInsecureNetworkOperations(code));

            score.Issues = issues;
            score.SecurityScore = CalculateSecurityScore(issues);
            score.OverallScore = score.SecurityScore;

            return await Task.FromResult(score);
        }

        /// <inheritdoc />
        public async Task<QualityScore> AnalyzePerformanceAsync(string code, CancellationToken cancellationToken = default)
        {
            var score = new QualityScore { AnalyzedAt = DateTime.UtcNow };
            var issues = new List<QualityIssue>();

            // Check for performance issues
            issues.AddRange(CheckForNPlusOneQueries(code));
            issues.AddRange(CheckForInefficientLoops(code));
            issues.AddRange(CheckForMemoryLeaks(code));
            issues.AddRange(CheckForBlockingOperations(code));
            issues.AddRange(CheckForInefficientStringOperations(code));

            score.Issues = issues;
            score.PerformanceScore = CalculatePerformanceScore(issues);
            score.OverallScore = score.PerformanceScore;

            return await Task.FromResult(score);
        }

        /// <inheritdoc />
        public async Task<QualityScore> AnalyzeMaintainabilityAsync(string code, CancellationToken cancellationToken = default)
        {
            var score = new QualityScore { AnalyzedAt = DateTime.UtcNow };
            var issues = new List<QualityIssue>();

            // Check for maintainability issues
            issues.AddRange(CheckForLongMethods(code));
            issues.AddRange(CheckForLargeClasses(code));
            issues.AddRange(CheckForDuplicateCode(code));
            issues.AddRange(CheckForComplexConditionals(code));
            issues.AddRange(CheckForMagicNumbers(code));

            score.Issues = issues;
            score.MaintainabilityScore = CalculateMaintainabilityScore(issues);
            score.OverallScore = score.MaintainabilityScore;

            return await Task.FromResult(score);
        }

        /// <inheritdoc />
        public async Task<QualityScore> AnalyzeTestabilityAsync(string code, CancellationToken cancellationToken = default)
        {
            var score = new QualityScore { AnalyzedAt = DateTime.UtcNow };
            var issues = new List<QualityIssue>();

            // Check for testability issues
            issues.AddRange(CheckForStaticDependencies(code));
            issues.AddRange(CheckForTightCoupling(code));
            issues.AddRange(CheckForPrivateMethods(code));
            issues.AddRange(CheckForExceptionHandling(code));

            score.Issues = issues;
            score.TestabilityScore = CalculateTestabilityScore(issues);
            score.OverallScore = score.TestabilityScore;

            return await Task.FromResult(score);
        }

        /// <inheritdoc />
        public bool PassesQualityGate(QualityScore qualityScore)
        {
            return qualityScore.OverallScore >= 6.0 && 
                   qualityScore.SecurityScore >= 7.0 && 
                   qualityScore.QualityLevel >= QualityLevel.Acceptable;
        }

        /// <inheritdoc />
        public bool RequiresHumanReview(QualityScore qualityScore)
        {
            return qualityScore.OverallScore < 7.0 || 
                   qualityScore.SecurityScore < 8.0 || 
                   qualityScore.Issues.Any(i => i.Severity == IssueSeverity.Critical);
        }

        /// <inheritdoc />
        public bool IsProductionReady(QualityScore qualityScore)
        {
            return qualityScore.OverallScore >= 8.0 && 
                   qualityScore.SecurityScore >= 9.0 && 
                   qualityScore.QualityLevel >= QualityLevel.Good &&
                   !qualityScore.Issues.Any(i => i.Severity == IssueSeverity.Critical || i.Severity == IssueSeverity.High);
        }

        #region Private Methods

        private int CountLinesOfCode(string code)
        {
            return code.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//"));
        }

        private int CalculateCyclomaticComplexity(string code)
        {
            var complexity = 1; // Base complexity
            var keywords = new[] { "if", "else", "while", "for", "foreach", "switch", "case", "catch", "&&", "||", "?" };
            
            foreach (var keyword in keywords)
            {
                complexity += Regex.Matches(code, $@"\b{keyword}\b", RegexOptions.IgnoreCase).Count;
            }
            
            return complexity;
        }

        private int CountMethods(string code)
        {
            return Regex.Matches(code, @"\b(public|private|protected|internal)\s+\w+\s+\w+\s*\(", RegexOptions.IgnoreCase).Count;
        }

        private int CountClasses(string code)
        {
            return Regex.Matches(code, @"\bclass\s+\w+", RegexOptions.IgnoreCase).Count;
        }

        private async Task<double> AnalyzeReadabilityAsync(string code, CancellationToken cancellationToken)
        {
            // Simple readability analysis based on code structure
            var score = 10.0;
            
            // Penalize very long lines
            var lines = code.Split('\n');
            var longLines = lines.Count(line => line.Length > 120);
            score -= longLines * 0.1;
            
            // Penalize very long methods
            var methodLength = CalculateAverageMethodLength(code);
            if (methodLength > 50) score -= 2.0;
            else if (methodLength > 30) score -= 1.0;
            
            // Penalize high cyclomatic complexity
            var complexity = CalculateCyclomaticComplexity(code);
            if (complexity > 20) score -= 3.0;
            else if (complexity > 10) score -= 1.5;
            
            return Math.Max(0, Math.Min(10, score));
        }

        private async Task<double> AnalyzeDocumentationAsync(string code, CancellationToken cancellationToken)
        {
            var score = 0.0;
            var lines = code.Split('\n');
            var totalLines = lines.Length;
            var documentedLines = 0;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("///") || trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                {
                    documentedLines++;
                }
            }
            
            if (totalLines > 0)
            {
                score = (double)documentedLines / totalLines * 10;
            }
            
            return Math.Min(10, score);
        }

        private double CalculateOverallScore(QualityScore score)
        {
            // Weighted average with security being most important
            return (score.SecurityScore * 0.3) +
                   (score.PerformanceScore * 0.2) +
                   (score.MaintainabilityScore * 0.2) +
                   (score.TestabilityScore * 0.15) +
                   (score.ReadabilityScore * 0.1) +
                   (score.DocumentationScore * 0.05);
        }

        private QualityLevel DetermineQualityLevel(double overallScore)
        {
            return overallScore switch
            {
                >= 9.0 => QualityLevel.Excellent,
                >= 7.0 => QualityLevel.Good,
                >= 5.0 => QualityLevel.Acceptable,
                >= 3.0 => QualityLevel.Poor,
                _ => QualityLevel.Unacceptable
            };
        }

        private async Task PerformAIAnalysisAsync(string code, QualityScore score, CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Analyze the following C# code for quality issues and provide recommendations:

```csharp
{code}
```

Please identify:
1. Security vulnerabilities
2. Performance issues
3. Maintainability problems
4. Testability concerns
5. Code style issues

Provide specific recommendations for improvement.
";

                var request = new ModelRequest
                {
                    Input = prompt,
                    Temperature = 0.1,
                    MaxTokens = 2000
                };

                var response = await _aiProvider.ExecuteAsync(request, cancellationToken);
                
                if (!string.IsNullOrEmpty(response.Response))
                {
                    // Parse AI response and add to recommendations
                    var aiRecommendations = ParseAIRecommendations(response.Response);
                    score.Recommendations.AddRange(aiRecommendations);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI analysis failed, continuing with static analysis only");
            }
        }

        private List<QualityRecommendation> ParseAIRecommendations(string aiResponse)
        {
            var recommendations = new List<QualityRecommendation>();
            
            // Simple parsing of AI response - in production, this would be more sophisticated
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("-") || line.Trim().StartsWith("*"))
                {
                    recommendations.Add(new QualityRecommendation
                    {
                        Type = RecommendationType.Maintainability,
                        Title = "AI Recommendation",
                        Description = line.Trim().TrimStart('-', '*', ' '),
                        Priority = 3,
                        ImpactScore = 5.0
                    });
                }
            }
            
            return recommendations;
        }

        #endregion

        #region Security Checks

        private List<QualityIssue> CheckForSQLInjection(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"string\.Format.*SELECT.*\{.*\}", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Critical,
                    Title = "Potential SQL Injection",
                    Description = "String concatenation in SQL queries can lead to SQL injection",
                    FixSuggestion = "Use parameterized queries or Entity Framework"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForXSS(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"Response\.Write.*Request\.", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.High,
                    Title = "Potential XSS Vulnerability",
                    Description = "Direct output of user input without encoding",
                    FixSuggestion = "Use Html.Encode() or similar encoding methods"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForPathTraversal(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"File\.ReadAllText.*Request\.", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.High,
                    Title = "Potential Path Traversal",
                    Description = "Using user input directly in file operations",
                    FixSuggestion = "Validate and sanitize file paths"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForInsecureRandom(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"new Random\(\)", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Medium,
                    Title = "Insecure Random Number Generation",
                    Description = "Using default Random constructor for security-sensitive operations",
                    FixSuggestion = "Use RandomNumberGenerator for cryptographic purposes"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForHardcodedSecrets(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"password\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Critical,
                    Title = "Hardcoded Password",
                    Description = "Password appears to be hardcoded in source code",
                    FixSuggestion = "Use configuration or secure credential storage"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForInsecureFileOperations(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"File\.Delete.*Request\.", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Critical,
                    Title = "Dangerous File Operation",
                    Description = "File deletion using user input without validation",
                    FixSuggestion = "Validate file paths and implement proper authorization"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForInsecureNetworkOperations(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"HttpClient.*http://", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Medium,
                    Title = "Insecure HTTP Connection",
                    Description = "Using HTTP instead of HTTPS for network requests",
                    FixSuggestion = "Use HTTPS for secure communications"
                });
            }
            
            return issues;
        }

        #endregion

        #region Performance Checks

        private List<QualityIssue> CheckForNPlusOneQueries(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"foreach.*\.Where.*\.FirstOrDefault", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Performance,
                    Severity = IssueSeverity.Medium,
                    Title = "Potential N+1 Query Problem",
                    Description = "Database query inside a loop can cause performance issues",
                    FixSuggestion = "Use Include() or load related data in a single query"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForInefficientLoops(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"for.*\.Count", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Performance,
                    Severity = IssueSeverity.Low,
                    Title = "Inefficient Loop",
                    Description = "Calling Count() in loop condition",
                    FixSuggestion = "Cache the count before the loop"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForMemoryLeaks(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"new.*Timer.*Start", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Performance,
                    Severity = IssueSeverity.Medium,
                    Title = "Potential Memory Leak",
                    Description = "Timer may not be properly disposed",
                    FixSuggestion = "Implement IDisposable and dispose the timer"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForBlockingOperations(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"\.Result\b", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Performance,
                    Severity = IssueSeverity.Medium,
                    Title = "Blocking Async Operation",
                    Description = "Using .Result can cause deadlocks",
                    FixSuggestion = "Use await instead of .Result"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForInefficientStringOperations(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"string\s+\w+\s*=\s*""""", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Performance,
                    Severity = IssueSeverity.Low,
                    Title = "Inefficient String Concatenation",
                    Description = "String concatenation in loops can be inefficient",
                    FixSuggestion = "Use StringBuilder for multiple concatenations"
                });
            }
            
            return issues;
        }

        #endregion

        #region Maintainability Checks

        private List<QualityIssue> CheckForLongMethods(string code)
        {
            var issues = new List<QualityIssue>();
            var methods = ExtractMethods(code);
            
            foreach (var method in methods)
            {
                var lines = method.Split('\n').Length;
                if (lines > 50)
                {
                    issues.Add(new QualityIssue
                    {
                        Type = IssueType.Maintainability,
                        Severity = IssueSeverity.Medium,
                        Title = "Long Method",
                        Description = $"Method has {lines} lines, consider breaking it down",
                        FixSuggestion = "Extract smaller methods with single responsibilities"
                    });
                }
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForLargeClasses(string code)
        {
            var issues = new List<QualityIssue>();
            var classes = ExtractClasses(code);
            
            foreach (var classCode in classes)
            {
                var lines = classCode.Split('\n').Length;
                if (lines > 200)
                {
                    issues.Add(new QualityIssue
                    {
                        Type = IssueType.Maintainability,
                        Severity = IssueSeverity.Medium,
                        Title = "Large Class",
                        Description = $"Class has {lines} lines, consider splitting",
                        FixSuggestion = "Apply Single Responsibility Principle"
                    });
                }
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForDuplicateCode(string code)
        {
            var issues = new List<QualityIssue>();
            
            // Simple duplicate detection - in production, use more sophisticated algorithms
            var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var lineGroups = lines.GroupBy(line => line.Trim())
                                 .Where(group => group.Count() > 3)
                                 .ToList();
            
            foreach (var group in lineGroups)
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Maintainability,
                    Severity = IssueSeverity.Low,
                    Title = "Duplicate Code",
                    Description = $"Line appears {group.Count()} times",
                    FixSuggestion = "Extract common code into a method"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForComplexConditionals(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"if.*&&.*&&.*&&", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Maintainability,
                    Severity = IssueSeverity.Low,
                    Title = "Complex Conditional",
                    Description = "Complex if statement with multiple conditions",
                    FixSuggestion = "Extract conditions into well-named boolean methods"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForMagicNumbers(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"\b(if|while|for).*\b\d{3,}\b", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Maintainability,
                    Severity = IssueSeverity.Low,
                    Title = "Magic Number",
                    Description = "Large number used directly in code",
                    FixSuggestion = "Define named constants for magic numbers"
                });
            }
            
            return issues;
        }

        #endregion

        #region Testability Checks

        private List<QualityIssue> CheckForStaticDependencies(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"DateTime\.Now|Console\.WriteLine|File\.", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Testability,
                    Severity = IssueSeverity.Medium,
                    Title = "Static Dependencies",
                    Description = "Static method calls make testing difficult",
                    FixSuggestion = "Use dependency injection for better testability"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForTightCoupling(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"new\s+\w+\(\)", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Testability,
                    Severity = IssueSeverity.Low,
                    Title = "Tight Coupling",
                    Description = "Direct instantiation makes testing difficult",
                    FixSuggestion = "Use dependency injection and interfaces"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForPrivateMethods(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"private.*async.*Task", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Testability,
                    Severity = IssueSeverity.Low,
                    Title = "Private Async Methods",
                    Description = "Private async methods are hard to test",
                    FixSuggestion = "Make methods internal or extract to testable classes"
                });
            }
            
            return issues;
        }

        private List<QualityIssue> CheckForExceptionHandling(string code)
        {
            var issues = new List<QualityIssue>();
            
            if (Regex.IsMatch(code, @"catch\s*\(\s*Exception\s*\)", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Type = IssueType.Testability,
                    Severity = IssueSeverity.Low,
                    Title = "Generic Exception Handling",
                    Description = "Catching generic Exception makes testing difficult",
                    FixSuggestion = "Catch specific exceptions and handle appropriately"
                });
            }
            
            return issues;
        }

        #endregion

        #region Helper Methods

        private double CalculateSecurityScore(List<QualityIssue> issues)
        {
            var securityIssues = issues.Where(i => i.Type == IssueType.Security).ToList();
            if (!securityIssues.Any()) return 10.0;
            
            var score = 10.0;
            foreach (var issue in securityIssues)
            {
                score -= (int)issue.Severity * 2.0;
            }
            
            return Math.Max(0, score);
        }

        private double CalculatePerformanceScore(List<QualityIssue> issues)
        {
            var performanceIssues = issues.Where(i => i.Type == IssueType.Performance).ToList();
            if (!performanceIssues.Any()) return 10.0;
            
            var score = 10.0;
            foreach (var issue in performanceIssues)
            {
                score -= (int)issue.Severity * 1.5;
            }
            
            return Math.Max(0, score);
        }

        private double CalculateMaintainabilityScore(List<QualityIssue> issues)
        {
            var maintainabilityIssues = issues.Where(i => i.Type == IssueType.Maintainability).ToList();
            if (!maintainabilityIssues.Any()) return 10.0;
            
            var score = 10.0;
            foreach (var issue in maintainabilityIssues)
            {
                score -= (int)issue.Severity * 1.0;
            }
            
            return Math.Max(0, score);
        }

        private double CalculateTestabilityScore(List<QualityIssue> issues)
        {
            var testabilityIssues = issues.Where(i => i.Type == IssueType.Testability).ToList();
            if (!testabilityIssues.Any()) return 10.0;
            
            var score = 10.0;
            foreach (var issue in testabilityIssues)
            {
                score -= (int)issue.Severity * 1.0;
            }
            
            return Math.Max(0, score);
        }

        private int CalculateAverageMethodLength(string code)
        {
            var methods = ExtractMethods(code);
            if (!methods.Any()) return 0;
            
            return (int)methods.Average(method => method.Split('\n').Length);
        }

        private List<string> ExtractMethods(string code)
        {
            var methods = new List<string>();
            var lines = code.Split('\n');
            var currentMethod = new List<string>();
            var braceCount = 0;
            var inMethod = false;
            
            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"\b(public|private|protected|internal)\s+\w+\s+\w+\s*\(", RegexOptions.IgnoreCase))
                {
                    if (inMethod && currentMethod.Any())
                    {
                        methods.Add(string.Join("\n", currentMethod));
                    }
                    currentMethod.Clear();
                    inMethod = true;
                }
                
                if (inMethod)
                {
                    currentMethod.Add(line);
                    braceCount += line.Count(c => c == '{');
                    braceCount -= line.Count(c => c == '}');
                    
                    if (braceCount == 0 && currentMethod.Count > 1)
                    {
                        methods.Add(string.Join("\n", currentMethod));
                        currentMethod.Clear();
                        inMethod = false;
                    }
                }
            }
            
            return methods;
        }

        private List<string> ExtractClasses(string code)
        {
            var classes = new List<string>();
            var lines = code.Split('\n');
            var currentClass = new List<string>();
            var braceCount = 0;
            var inClass = false;
            
            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"\bclass\s+\w+", RegexOptions.IgnoreCase))
                {
                    if (inClass && currentClass.Any())
                    {
                        classes.Add(string.Join("\n", currentClass));
                    }
                    currentClass.Clear();
                    inClass = true;
                }
                
                if (inClass)
                {
                    currentClass.Add(line);
                    braceCount += line.Count(c => c == '{');
                    braceCount -= line.Count(c => c == '}');
                    
                    if (braceCount == 0 && currentClass.Count > 1)
                    {
                        classes.Add(string.Join("\n", currentClass));
                        currentClass.Clear();
                        inClass = false;
                    }
                }
            }
            
            return classes;
        }

        #endregion
    }
}
