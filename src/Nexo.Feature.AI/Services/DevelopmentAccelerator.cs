using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Intelligent development accelerator that provides code suggestions, refactoring, and test generation.
    /// </summary>
    public class DevelopmentAccelerator : IDevelopmentAccelerator
    {
        private readonly ILogger<DevelopmentAccelerator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public DevelopmentAccelerator(
            ILogger<DevelopmentAccelerator> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        public async Task<IList<string>> SuggestCodeAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting code suggestions for source code of length: {Length}", sourceCode?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                {
                    return new List<string> { "No source code provided for suggestions" };
                }

                var prompt = CreateCodeSuggestionPrompt(sourceCode, context);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.4,
                    Metadata = context != null ? new Dictionary<string, object>(context) : new Dictionary<string, object>()
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("code generation", ModelType.TextGeneration, cancellationToken);
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting code suggestions");
                return new List<string> { $"Error during code suggestion: {ex.Message}" };
            }
        }

        public async Task<IList<string>> SuggestRefactoringsAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting refactoring suggestions for source code of length: {Length}", sourceCode?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                {
                    return new List<string> { "No source code provided for refactoring suggestions" };
                }

                var prompt = CreateRefactoringPrompt(sourceCode, context);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2500,
                    Temperature = 0.3
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("code refactoring", ModelType.TextGeneration, cancellationToken);
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refactoring suggestions");
                return new List<string> { $"Error during refactoring suggestion: {ex.Message}" };
            }
        }

        public async Task<IList<string>> GenerateTestsAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating tests for source code of length: {Length}", sourceCode?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                {
                    return new List<string> { "No source code provided for test generation" };
                }

                var prompt = CreateTestGenerationPrompt(sourceCode, context);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 3000,
                    Temperature = 0.3
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("test generation", ModelType.TextGeneration, cancellationToken);
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseTestSuggestions(response.Content);
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions to allow proper cancellation handling
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tests");
                return new List<string> { $"Error during test generation: {ex.Message}" };
            }
        }

        public async Task<IList<string>> SuggestOptimizationsAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting optimization suggestions for source code of length: {Length}", sourceCode?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                {
                    return new List<string> { "No source code provided for optimization suggestions" };
                }

                var prompt = CreateOptimizationPrompt(sourceCode, context);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.3
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("code optimization", ModelType.TextGeneration, cancellationToken);
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization suggestions");
                return new List<string> { $"Error during optimization suggestion: {ex.Message}" };
            }
        }

        public async Task<IList<string>> SuggestDocumentationAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting documentation suggestions for source code of length: {Length}", sourceCode?.Length ?? 0);

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                {
                    return new List<string> { "No source code provided for documentation suggestions" };
                }

                var prompt = CreateDocumentationPrompt(sourceCode, context);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 2000,
                    Temperature = 0.3
                };

                // Get the best model for the task
                var model = await _modelOrchestrator.GetBestModelForTaskAsync("documentation generation", ModelType.TextGeneration, cancellationToken);
                var response = await model.ExecuteAsync(request, cancellationToken);
                return ParseSuggestions(response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documentation suggestions");
                return new List<string> { $"Error during documentation suggestion: {ex.Message}" };
            }
        }

        private string CreateCodeSuggestionPrompt(string sourceCode, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following C# code and provide intelligent code suggestions for improvement:

{sourceCode}

{contextInfo}

Please provide suggestions for:
1. Code completion and improvements
2. Better variable naming
3. Method extraction opportunities
4. Code organization improvements
5. Best practice implementations
6. Design pattern applications
7. Error handling improvements
8. Performance optimizations

Format your response as a numbered list of specific, actionable code suggestions. Include code examples where appropriate.";
        }

        private string CreateRefactoringPrompt(string sourceCode, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following C# code and provide refactoring suggestions:

{sourceCode}

{contextInfo}

Please provide refactoring suggestions for:
1. Extract methods for better readability
2. Extract classes for single responsibility
3. Replace magic numbers with constants
4. Improve variable naming
5. Reduce method complexity
6. Remove code duplication
7. Improve error handling
8. Apply design patterns
9. Improve code organization
10. Enhance maintainability

For each suggestion, provide:
- The specific refactoring action
- The reasoning behind it
- Expected benefits
- Potential risks

Format your response as a numbered list with detailed explanations.";
        }

        private string CreateTestGenerationPrompt(string sourceCode, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Generate comprehensive unit tests for the following C# code:

{sourceCode}

{contextInfo}

Please generate tests that cover:
1. Happy path scenarios
2. Edge cases and boundary conditions
3. Error conditions and exceptions
4. Null input handling
5. Invalid input validation
6. Performance edge cases
7. Integration scenarios
8. Mocking strategies

For each test, provide:
- Test method name and description
- Test data setup
- Expected behavior
- Assertions
- Any necessary mocking

Use xUnit framework and follow AAA (Arrange-Act-Assert) pattern.
Format your response as complete, compilable test code.";
        }

        private string CreateOptimizationPrompt(string sourceCode, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following C# code for optimization opportunities:

{sourceCode}

{contextInfo}

Please provide optimization suggestions for:
1. Algorithm efficiency improvements
2. Memory usage optimization
3. Database query optimization
4. Caching strategies
5. Async/await usage
6. LINQ optimization
7. String handling optimization
8. Collection usage optimization
9. Resource disposal
10. Performance bottlenecks

For each suggestion, provide:
- The specific optimization
- Expected performance improvement
- Implementation complexity
- Potential trade-offs

Format your response as a numbered list with detailed explanations.";
        }

        private string CreateDocumentationPrompt(string sourceCode, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following C# code and provide documentation suggestions:

{sourceCode}

{contextInfo}

Please provide documentation suggestions for:
1. XML documentation comments
2. README file content
3. API documentation
4. Code examples
5. Usage scenarios
6. Configuration documentation
7. Troubleshooting guides
8. Performance considerations
9. Security considerations
10. Deployment instructions

For each suggestion, provide:
- The specific documentation need
- Content recommendations
- Target audience
- Format suggestions

Format your response as a numbered list with detailed recommendations.";
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

        private IList<string> ParseTestSuggestions(string aiResponse)
        {
            var suggestions = new List<string>();
            
            if (string.IsNullOrEmpty(aiResponse))
            {
                return suggestions;
            }

            // For test generation, we want to preserve the code structure
            // Split by test method patterns
            var lines = aiResponse.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var currentTest = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Check if this is the start of a new test method
                if (trimmedLine.StartsWith("[Fact]") || 
                    trimmedLine.StartsWith("[Theory]") ||
                    trimmedLine.StartsWith("public async Task") ||
                    trimmedLine.StartsWith("public void"))
                {
                    // Save the previous test if we have one
                    if (currentTest.Count > 0)
                    {
                        suggestions.Add(string.Join("\n", currentTest));
                        currentTest.Clear();
                    }
                }
                
                currentTest.Add(trimmedLine);
            }
            
            // Add the last test
            if (currentTest.Count > 0)
            {
                suggestions.Add(string.Join("\n", currentTest));
            }

            return suggestions;
        }
    }
} 