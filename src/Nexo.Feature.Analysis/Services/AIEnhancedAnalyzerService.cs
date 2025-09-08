using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// AI-enhanced analyzer service that provides intelligent code analysis and suggestions.
    /// </summary>
    public class AIEnhancedAnalyzerService : IAIEnhancedAnalyzerService
    {
        private readonly ILogger<AIEnhancedAnalyzerService> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public AIEnhancedAnalyzerService(
            ILogger<AIEnhancedAnalyzerService> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        public void Analyze(string code)
        {
            _logger.LogInformation("Performing basic code analysis for code of length: {Length}", code?.Length ?? 0);
            
            // Basic analysis implementation
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Code is null or empty");
                return;
            }

            // Perform basic static analysis
            PerformBasicAnalysis(code);
        }

        public async Task<IList<string>> GetAISuggestionsAsync(string code, IDictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting AI suggestions for code of length: {Length}", code?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return new List<string> { "No code provided for analysis" };
                }

                var prompt = CreateAnalysisPrompt(code, context ?? new Dictionary<string, object>());
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 3000,
                    Temperature = 0.3
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("code analysis", ModelType.TextGeneration, cancellationToken);
                if (model == null)
                {
                    return new List<string> { "No suitable model available for code analysis" };
                }
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Response);
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions to allow proper cancellation handling
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI suggestions");
                return new List<string> { $"Error during AI analysis: {ex.Message}" };
            }
        }

        public async Task<IList<string>> GetArchitecturalComplianceSuggestionsAsync(string code, string architectureGuidelines, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting architectural compliance suggestions");

            try
            {
                var prompt = CreateArchitecturalCompliancePrompt(code, architectureGuidelines);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2500,
                    Temperature = 0.2
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("architectural analysis", ModelType.TextGeneration, cancellationToken);
                if (model == null)
                {
                    return new List<string> { "No suitable model available for architectural analysis" };
                }
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting architectural compliance suggestions");
                return new List<string> { $"Error during architectural analysis: {ex.Message}" };
            }
        }

        public async Task<IList<string>> GetPerformanceOptimizationSuggestionsAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting performance optimization suggestions");

            try
            {
                var prompt = CreatePerformanceAnalysisPrompt(code);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.3
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("performance analysis", ModelType.TextGeneration, cancellationToken);
                if (model == null)
                {
                    return new List<string> { "No suitable model available for performance analysis" };
                }
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance optimization suggestions");
                return new List<string> { $"Error during performance analysis: {ex.Message}" };
            }
        }

        public async Task<IList<string>> GetSecurityAnalysisSuggestionsAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting security analysis suggestions");

            try
            {
                var prompt = CreateSecurityAnalysisPrompt(code);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.2
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("security analysis", ModelType.TextGeneration, cancellationToken);
                if (model == null)
                {
                    return new List<string> { "No suitable model available for security analysis" };
                }
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security analysis suggestions");
                return new List<string> { $"Error during security analysis: {ex.Message}" };
            }
        }

        private void PerformBasicAnalysis(string code)
        {
            // Basic static analysis checks
            var issues = new List<string>();

            // Check for common issues
            if (code.Contains("TODO"))
            {
                issues.Add("Found TODO comments that should be addressed");
            }

            if (code.Contains("FIXME"))
            {
                issues.Add("Found FIXME comments that need attention");
            }

            if (code.Contains("Console.WriteLine"))
            {
                issues.Add("Found Console.WriteLine - consider using proper logging");
            }

            if (code.Contains("catch (Exception"))
            {
                issues.Add("Found generic exception catching - consider catching specific exceptions");
            }

            foreach (var issue in issues)
            {
                _logger.LogInformation("Analysis finding: {Issue}", issue);
            }
        }

        private string CreateAnalysisPrompt(string code, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following C# code and provide intelligent suggestions for improvement:

{code}

{contextInfo}

Please provide suggestions in the following areas:
1. Code quality and best practices
2. Performance optimizations
3. Security considerations
4. Maintainability improvements
5. Error handling
6. Documentation needs
7. Testing recommendations
8. Architectural improvements

Format your response as a numbered list of specific, actionable suggestions. Focus on practical improvements that can be implemented immediately.";
        }

        private string CreateArchitecturalCompliancePrompt(string code, string architectureGuidelines)
        {
            return $@"Analyze the following C# code for architectural compliance:

Code:
{code}

Architecture Guidelines:
{architectureGuidelines}

Please provide suggestions for:
1. Architectural pattern compliance
2. Separation of concerns
3. Dependency management
4. Interface design
5. SOLID principles adherence
6. Clean architecture principles
7. Design pattern usage
8. Component coupling and cohesion

Format your response as a numbered list of specific architectural improvements.";
        }

        private string CreatePerformanceAnalysisPrompt(string code)
        {
            return $@"Analyze the following C# code for performance optimization opportunities:

{code}

Please provide suggestions for:
1. Algorithm optimization
2. Memory usage optimization
3. Database query optimization
4. Caching strategies
5. Async/await usage
6. Resource disposal
7. Collection usage optimization
8. LINQ optimization
9. String handling optimization
10. Exception handling performance

Format your response as a numbered list of specific performance improvements with estimated impact.";
        }

        private string CreateSecurityAnalysisPrompt(string code)
        {
            return $@"Analyze the following C# code for security vulnerabilities and best practices:

{code}

Please provide suggestions for:
1. Input validation
2. Authentication and authorization
3. Data encryption
4. SQL injection prevention
5. XSS prevention
6. CSRF protection
7. Secure configuration
8. Error handling security
9. Logging security
10. Dependency security

Format your response as a numbered list of specific security improvements with risk levels.";
        }

        private IList<string> ParseSuggestions(string aiResponse)
        {
            var suggestions = new List<string>();
            
            if (string.IsNullOrEmpty(aiResponse))
            {
                return suggestions;
            }

            // Split by numbered lines or bullet points
            var lines = aiResponse.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && 
                    (trimmedLine.StartsWith("1.") || 
                     trimmedLine.StartsWith("2.") || 
                     trimmedLine.StartsWith("3.") || 
                     trimmedLine.StartsWith("4.") || 
                     trimmedLine.StartsWith("5.") || 
                     trimmedLine.StartsWith("6.") || 
                     trimmedLine.StartsWith("7.") || 
                     trimmedLine.StartsWith("8.") || 
                     trimmedLine.StartsWith("9.") || 
                     trimmedLine.StartsWith("10.") ||
                     trimmedLine.StartsWith("-") ||
                     trimmedLine.StartsWith("•")))
                {
                    // Remove the number/bullet and clean up
                    var suggestion = trimmedLine;
                    if (suggestion.Contains("."))
                    {
                        suggestion = suggestion.Substring(suggestion.IndexOf(".") + 1).Trim();
                    }
                    else if (suggestion.StartsWith("-") || suggestion.StartsWith("•"))
                    {
                        suggestion = suggestion.Substring(1).Trim();
                    }
                    
                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        suggestions.Add(suggestion);
                    }
                }
            }

            return suggestions;
        }
    }
} 