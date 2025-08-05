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
    /// Provides advanced development support capabilities such as code suggestions, refactoring recommendations, test generation, optimizations, and documentation suggestions.
    /// </summary>
    public class DevelopmentAccelerator : IDevelopmentAccelerator
    {
        /// <summary>
        /// Logger instance used to log informational messages, errors, and trace the execution flow
        /// for the <see cref="DevelopmentAccelerator" /> class.
        /// </summary>
        /// <remarks>
        /// This logger facilitates tracking and debugging by outputting log entries for events such
        /// as processing code suggestions, handling exceptions, and other key activities.
        /// </remarks>
        private readonly ILogger<DevelopmentAccelerator> _logger;

        /// <summary>
        /// Represents the orchestrator responsible for managing and selecting the most appropriate AI models
        /// based on the specific task requirements. Utilized for operations such as code generation, refactoring,
        /// optimization, and test generation within the DevelopmentAccelerator service.
        /// </summary>
        private readonly IModelOrchestrator _modelOrchestrator;

        /// <summary>
        /// Provides functionalities for accelerating software development through intelligent code suggestions,
        /// refactoring assistance, test case generation, optimization recommendations, and documentation suggestions.
        /// </summary>
        public DevelopmentAccelerator(
            ILogger<DevelopmentAccelerator> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Suggests code snippets or modifications based on the provided source code and optional context.
        /// </summary>
        /// <param name="sourceCode">
        /// The source code for which suggestions are requested. If null or empty, a default suggestion is returned.
        /// </param>
        /// <param name="context">
        /// Optional additional metadata that may influence the code suggestion generation logic (e.g., programming language, frameworks).
        /// Pass null if additional context is not required.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests while the task is being executed.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of suggested code snippets
        /// or error messages if the operation encounters issues.
        /// </returns>
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

        /// <summary>
        /// Asynchronously generates a list of code refactoring suggestions based on the provided source code and optional context.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze and suggest refactorings for. Must be a non-null, non-empty string.</param>
        /// <param name="context">
        /// Optional context data to guide the refactoring process. This is a dictionary of key-value pairs where the keys are string identifiers
        /// and the values provide additional information for customization.
        /// </param>
        /// <param name="cancellationToken">
        /// A CancellationToken to observe while waiting for the task to complete. Allows the operation to be cancelled if needed.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of strings, where each string is a suggested refactoring,
        /// or error messages if the operation fails.
        /// </returns>
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

        /// <summary>
        /// Asynchronously generates test case suggestions based on the provided source code.
        /// </summary>
        /// <param name="sourceCode">The source code for which the tests should be generated.</param>
        /// <param name="context">An optional dictionary containing additional context to aid in test generation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of test case suggestions as strings.</returns>
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

        /// <summary>
        /// Provides suggestions to optimize the provided source code, leveraging AI models to analyze and recommend improvements.
        /// </summary>
        /// <param name="sourceCode">
        /// The source code to be analyzed for optimization suggestions. Must be a non-null/non-empty string.
        /// </param>
        /// <param name="context">
        /// An optional dictionary containing additional context or metadata to tailor the optimization suggestions.
        /// </param>
        /// <param name="cancellationToken">
        /// A token for cancelling the asynchronous operation, if necessary.
        /// </param>
        /// <returns>
        /// A list of string suggestions representing potential optimizations for the given source code. If an error occurs or if the input is invalid, appropriate error messages are returned within the list.
        /// </returns>
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

        /// <summary>
        /// Provides documentation suggestions for the provided source code.
        /// </summary>
        /// <param name="sourceCode">The source code for which documentation suggestions are needed.</param>
        /// <param name="context">A dictionary containing additional context or metadata that can influence the suggestion process.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A list of suggested documentation snippets based on the provided source code.</returns>
        /// <remarks>
        /// This method leverages an AI model to analyze the source code and generate relevant documentation suggestions.
        /// If the input code is null or empty, a default message is returned. In case of errors, an error message is included in the response.
        /// </remarks>
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

        /// <summary>
        /// Creates a prompt for generating intelligent code suggestions based on provided source code and context.
        /// The prompt includes details about code improvement areas such as naming conventions, performance optimizations,
        /// best practices, and potential design pattern applications.
        /// </summary>
        /// <param name="sourceCode">The source code for which suggestions are to be generated.</param>
        /// <param name="context">Additional context or metadata to provide to the suggestion model, if available.</param>
        /// <returns>A string containing the formatted prompt to be used for generating code suggestions.</returns>
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

        /// <summary>
        /// Creates a prompt for generating refactoring suggestions for the provided source code.
        /// The prompt includes predefined guidelines and context information for the refactoring analysis.
        /// </summary>
        /// <param name="sourceCode">The C# source code to be analyzed for refactoring suggestions.</param>
        /// <param name="context">Optional context information to tailor the refactoring suggestions. It can include key-value pairs providing additional details or constraints for the analysis.</param>
        /// <returns>A formatted string containing the refactoring prompt with guidelines to analyze the given source code and provide actionable recommendations.</returns>
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

        /// <summary>
        /// Constructs a prompt for test generation based on the provided C# source code and optional context.
        /// </summary>
        /// <param name="sourceCode">
        /// The source code for which to generate test cases. This must be a valid C# code segment.
        /// </param>
        /// <param name="context">
        /// Optional additional information that can assist in refining the generated test cases.
        /// Contains key-value pairs relevant to the context of the source code.
        /// </param>
        /// <returns>
        /// A structured string prompt to instruct the underlying model to generate comprehensive and diverse test cases.
        /// </returns>
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

        /// <summary>
        /// Generates a detailed prompt for optimizing given source code with specific recommendations
        /// regarding efficiency, performance, and resource management.
        /// </summary>
        /// <param name="sourceCode">The C# source code to be analyzed for optimization opportunities.</param>
        /// <param name="context">
        /// An optional dictionary containing contextual information that may influence the optimization suggestions.
        /// This can include project-specific or environmental details.
        /// </param>
        /// <returns>
        /// A string containing a formatted prompt that outlines areas for optimization,
        /// including instructions on what suggestions should be generated.
        /// </returns>
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

        /// <summary>
        /// Constructs a documentation prompt for analyzing and generating documentation suggestions for the provided source code.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze and generate documentation suggestions for.</param>
        /// <param name="context">Optional additional context to be included in the documentation prompt. This could include project-specific details or metadata.</param>
        /// <returns>A formatted string containing the documentation prompt that incorporates the source code and context information.</returns>
        private static string CreateDocumentationPrompt(string sourceCode, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $"""
                    Analyze the following C# code and provide documentation suggestions:

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

                    Format your response as a numbered list with detailed recommendations.
                    """;
        }

        /// <summary>
        /// Parses the AI-generated response into a list of code suggestions.
        /// The method splits the response by lines and extracts items prefixed with numbers or bullet points.
        /// </summary>
        /// <param name="aiResponse">The AI-generated response as a string containing code suggestions.</param>
        /// <returns>A list of parsed code suggestion strings extracted from the AI response.</returns>
        private IList<string> ParseSuggestions(string aiResponse)
        {
            var suggestions = new List<string>();
            
            if (string.IsNullOrEmpty(aiResponse))
            {
                return suggestions;
            }

            // Split by numbered lines or bullet points
            var lines = aiResponse.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) ||
                    (!trimmedLine.StartsWith("1.") &&
                     !trimmedLine.StartsWith("2.") &&
                     !trimmedLine.StartsWith("3.") &&
                     !trimmedLine.StartsWith("4.") &&
                     !trimmedLine.StartsWith("5.") &&
                     !trimmedLine.StartsWith("6.") &&
                     !trimmedLine.StartsWith("7.") &&
                     !trimmedLine.StartsWith("8.") &&
                     !trimmedLine.StartsWith("9.") &&
                     !trimmedLine.StartsWith("10.") &&
                     !trimmedLine.StartsWith("-") &&
                     !trimmedLine.StartsWith("•"))) continue;
                // Remove the number/bullet and clean up
                var suggestion = trimmedLine;
                if (suggestion.Contains("."))
                {
                    suggestion = suggestion.Substring(suggestion.IndexOf(".", StringComparison.Ordinal) + 1).Trim();
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

            return suggestions;
        }

        /// <summary>
        /// Parses the AI-generated response for test method suggestions and organizes them into individual test cases.
        /// </summary>
        /// <param name="aiResponse">The raw response generated by the AI containing potential test methods.</param>
        /// <returns>A list of parsed test method suggestions, where each entry represents a complete test case.</returns>
        private IList<string> ParseTestSuggestions(string aiResponse)
        {
            var suggestions = new List<string>();
            
            if (string.IsNullOrEmpty(aiResponse))
            {
                return suggestions;
            }

            // For test generation, we want to preserve the code structure
            // Split by test method patterns
            var lines = aiResponse.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
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