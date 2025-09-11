using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered code review pipeline step for comprehensive code analysis
    /// </summary>
    public class AICodeReviewStep : IPipelineStep<CodeReviewRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AICodeReviewStep> _logger;

        public AICodeReviewStep(IAIRuntimeSelector runtimeSelector, ILogger<AICodeReviewStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Code Review";
        public int Order => 2;

        public async Task<CodeReviewRequest> ExecuteAsync(CodeReviewRequest input, PipelineContext context)
        {
            try
            {
                _logger.LogInformation("Starting AI code review for {Language} code", input.Language);

                // Validate input
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogWarning("Empty code provided for review");
                    input.Result = new Nexo.Core.Domain.Results.CodeReviewResult
                    {
                        QualityScore = 0,
                        Issues = new List<CodeIssue>
                        {
                            new CodeIssue
                            {
                                Type = CodeIssueType.Error,
                                Message = "No code provided for review",
                                Line = 0,
                                Severity = "High"
                            }
                        },
                        Suggestions = new List<string> { "Provide valid code for review" },
                        ReviewTime = DateTime.UtcNow,
                        EngineType = AIEngineType.Mock
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeReview,
                    TargetPlatform = context.EnvironmentProfile?.CurrentPlatform ?? PlatformType.Unknown,
                    MaxTokens = 2048,
                    Temperature = 0.3, // Lower temperature for more consistent reviews
                    Priority = AIPriority.Quality,
                    Requirements = new AIRequirements
                    {
                        QualityThreshold = 80,
                        SafetyLevel = SafetyLevel.High,
                        PerformanceTarget = PerformanceTarget.Balanced
                    }
                };

                // Select optimal AI engine
                var selection = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (selection == null)
                {
                    _logger.LogError("No suitable AI provider found for code review");
                    throw new InvalidOperationException("No AI provider available for code review");
                }

                // Create AI engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = selection.EngineType,
                    ModelPath = GetModelPathForReview(selection.EngineType),
                    MaxTokens = aiContext.MaxTokens,
                    Temperature = aiContext.Temperature
                };

                var engine = await selection.Provider.CreateEngineAsync(engineInfo);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for code review");
                    throw new InvalidOperationException("Failed to create AI engine for code review");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    await aiEngine.InitializeAsync();
                }

                // Perform code review
                var reviewResult = await aiEngine.ReviewCodeAsync(input);

                // Enhance review result with additional analysis
                var enhancedResult = await EnhanceReviewResultAsync(reviewResult, input, context);

                // Apply safety validation
                var validatedResult = await ApplySafetyValidationAsync(enhancedResult, input, context);

                // Update input with results
                input.Result = validatedResult;
                input.ReviewCompleted = true;
                input.ReviewTime = DateTime.UtcNow;

                _logger.LogInformation("AI code review completed with quality score {Score}", validatedResult.QualityScore);

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI code review");
                
                // Create fallback result
                input.Result = new Nexo.Core.Domain.Results.CodeReviewResult
                {
                    QualityScore = 0,
                    Issues = new List<CodeIssue>
                    {
                        new CodeIssue
                        {
                            Type = CodeIssueType.Error,
                            Message = $"Code review failed: {ex.Message}",
                            Line = 0,
                            Severity = "High"
                        }
                    },
                    Suggestions = new List<string> { "Review failed due to technical error. Please try again." },
                    ReviewTime = DateTime.UtcNow,
                    EngineType = AIEngineType.Mock
                };
                input.ReviewCompleted = false;
                
                return input;
            }
        }

        private string GetModelPathForReview(AIEngineType engineType)
        {
            return engineType switch
            {
                AIEngineType.LlamaWebAssembly => "models/codellama-7b-instruct.gguf",
                AIEngineType.LlamaNative => "models/codellama-13b-instruct.gguf",
                _ => "models/codellama-7b-instruct.gguf"
            };
        }

        private async Task<Nexo.Core.Domain.Results.CodeReviewResult> EnhanceReviewResultAsync(Nexo.Core.Domain.Results.CodeReviewResult result, Nexo.Core.Domain.Entities.AI.CodeReviewRequest request, Nexo.Core.Domain.Entities.Pipeline.PipelineContext context)
        {
            _logger.LogDebug("Enhancing code review result with additional analysis");

            // Add performance analysis
            var performanceIssues = await AnalyzePerformanceAsync(request.Code, request.Language);
            result.Issues.AddRange(performanceIssues);

            // Add security analysis
            var securityIssues = await AnalyzeSecurityAsync(request.Code, request.Language);
            result.Issues.AddRange(securityIssues);

            // Add maintainability analysis
            var maintainabilityIssues = await AnalyzeMaintainabilityAsync(request.Code, request.Language);
            result.Issues.AddRange(maintainabilityIssues);

            // Recalculate quality score
            result.QualityScore = CalculateEnhancedQualityScore(result);

            // Add context-specific suggestions
            var contextSuggestions = await GenerateContextSuggestionsAsync(request, context);
            result.Suggestions.AddRange(contextSuggestions);

            return result;
        }

        private async Task<Nexo.Core.Domain.Results.CodeReviewResult> ApplySafetyValidationAsync(Nexo.Core.Domain.Results.CodeReviewResult result, Nexo.Core.Domain.Entities.AI.CodeReviewRequest request, Nexo.Core.Domain.Entities.Pipeline.PipelineContext context)
        {
            _logger.LogDebug("Applying safety validation to code review result");

            // Filter out any potentially harmful suggestions
            result.Suggestions = await FilterSuggestionsAsync(result.Suggestions, request, context);

            // Validate issues for safety
            result.Issues = await ValidateIssuesAsync(result.Issues, request, context);

            // Apply content filtering
            result = await ApplyContentFilteringAsync(result, request, context);

            return result;
        }

        private async Task<List<CodeIssue>> AnalyzePerformanceAsync(string code, CodeLanguage language)
        {
            // In a real implementation, this would analyze code for performance issues
            await Task.Delay(100); // Simulate analysis time

            var issues = new List<CodeIssue>();

            // Check for common performance issues
            if (code.Contains("for (int i = 0; i < items.Count; i++)"))
            {
                issues.Add(new CodeIssue
                {
                    Type = CodeIssueType.Warning,
                    Message = "Consider using foreach loop for better performance",
                    Line = 1,
                    Severity = "Medium"
                });
            }

            if (code.Contains("string concatenation") && !code.Contains("StringBuilder"))
            {
                issues.Add(new CodeIssue
                {
                    Type = CodeIssueType.Warning,
                    Message = "Consider using StringBuilder for multiple string concatenations",
                    Line = 1,
                    Severity = "Low"
                });
            }

            return issues;
        }

        private async Task<List<CodeIssue>> AnalyzeSecurityAsync(string code, CodeLanguage language)
        {
            // In a real implementation, this would analyze code for security issues
            await Task.Delay(100); // Simulate analysis time

            var issues = new List<CodeIssue>();

            // Check for common security issues
            if (code.Contains("password") && code.Contains("plain text"))
            {
                issues.Add(new CodeIssue
                {
                    Type = CodeIssueType.Error,
                    Message = "Never store passwords in plain text",
                    Line = 1,
                    Severity = "High"
                });
            }

            if (code.Contains("SQL") && code.Contains("string concatenation"))
            {
                issues.Add(new CodeIssue
                {
                    Type = CodeIssueType.Error,
                    Message = "Use parameterized queries to prevent SQL injection",
                    Line = 1,
                    Severity = "High"
                });
            }

            return issues;
        }

        private async Task<List<CodeIssue>> AnalyzeMaintainabilityAsync(string code, CodeLanguage language)
        {
            // In a real implementation, this would analyze code for maintainability issues
            await Task.Delay(100); // Simulate analysis time

            var issues = new List<CodeIssue>();

            // Check for maintainability issues
            if (code.Length > 1000)
            {
                issues.Add(new CodeIssue
                {
                    Type = CodeIssueType.Info,
                    Message = "Consider breaking down large methods into smaller ones",
                    Line = 1,
                    Severity = "Low"
                });
            }

            if (code.Contains("magic numbers") && !code.Contains("const"))
            {
                issues.Add(new CodeIssue
                {
                    Type = CodeIssueType.Warning,
                    Message = "Replace magic numbers with named constants",
                    Line = 1,
                    Severity = "Medium"
                });
            }

            return issues;
        }

        private int CalculateEnhancedQualityScore(Nexo.Core.Domain.Results.CodeReviewResult result)
        {
            var baseScore = result.QualityScore;
            var issuePenalty = result.Issues.Sum(issue => issue.Severity switch
            {
                "High" => 20,
                "Medium" => 10,
                "Low" => 5,
                _ => 0
            });

            return Math.Max(0, baseScore - issuePenalty);
        }

        private async Task<List<string>> GenerateContextSuggestionsAsync(CodeReviewRequest request, PipelineContext context)
        {
            // In a real implementation, this would generate context-specific suggestions
            await Task.Delay(50);

            var suggestions = new List<string>();

            // Add context-specific suggestions based on the pipeline context
            if (context.EnvironmentProfile?.CurrentPlatform == PlatformType.WebAssembly)
            {
                suggestions.Add("Consider WebAssembly-specific optimizations for better performance");
            }

            if (context.EnvironmentProfile?.CurrentPlatform == PlatformType.Windows)
            {
                suggestions.Add("Consider Windows-specific APIs for enhanced functionality");
            }

            return suggestions;
        }

        private async Task<List<string>> FilterSuggestionsAsync(List<string> suggestions, CodeReviewRequest request, PipelineContext context)
        {
            // In a real implementation, this would filter out potentially harmful suggestions
            await Task.Delay(50);

            var filteredSuggestions = new List<string>();

            foreach (var suggestion in suggestions)
            {
                // Filter out potentially harmful suggestions
                if (!suggestion.Contains("delete") && 
                    !suggestion.Contains("remove") && 
                    !suggestion.Contains("disable"))
                {
                    filteredSuggestions.Add(suggestion);
                }
            }

            return filteredSuggestions;
        }

        private async Task<List<CodeIssue>> ValidateIssuesAsync(List<CodeIssue> issues, CodeReviewRequest request, PipelineContext context)
        {
            // In a real implementation, this would validate issues for safety and accuracy
            await Task.Delay(50);

            var validatedIssues = new List<CodeIssue>();

            foreach (var issue in issues)
            {
                // Validate issue severity and content
                if (issue.Severity != "High" || !issue.Message.Contains("dangerous"))
                {
                    validatedIssues.Add(issue);
                }
            }

            return validatedIssues;
        }

        private async Task<Nexo.Core.Domain.Results.CodeReviewResult> ApplyContentFilteringAsync(Nexo.Core.Domain.Results.CodeReviewResult result, Nexo.Core.Domain.Entities.AI.CodeReviewRequest request, Nexo.Core.Domain.Entities.Pipeline.PipelineContext context)
        {
            // In a real implementation, this would apply content filtering
            await Task.Delay(50);

            // Ensure all content is appropriate and safe
            result.Suggestions = result.Suggestions.Where(s => !s.Contains("inappropriate")).ToList();
            result.Issues = result.Issues.Where(i => !i.Message.Contains("inappropriate")).ToList();

            return result;
        }

        public async Task<bool> CanExecuteAsync(CodeReviewRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogDebug("Cannot execute code review step: empty code provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute code review step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute code review step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if code review step can execute");
                return false;
            }
        }
    }

    /// <summary>
    /// Code review request for AI pipeline processing
    /// </summary>
    public class CodeReviewRequest
    {
        public string Code { get; set; } = string.Empty;
        public CodeLanguage Language { get; set; }
        public string Context { get; set; } = string.Empty;
        public Nexo.Core.Domain.Results.CodeReviewResult? Result { get; set; }
        public bool ReviewCompleted { get; set; }
        public DateTime? ReviewTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// AI requirements for code review operations
    /// </summary>
    public class AIRequirements
    {
        public int QualityThreshold { get; set; } = 80;
        public SafetyLevel SafetyLevel { get; set; } = SafetyLevel.Medium;
        public PerformanceTarget PerformanceTarget { get; set; } = PerformanceTarget.Balanced;
        public bool RequireOffline { get; set; } = false;
        public Dictionary<string, object> CustomRequirements { get; set; } = new();
    }

    /// <summary>
    /// Safety levels for AI operations
    /// </summary>
    public enum SafetyLevel
    {
        Low,
        Medium,
        High,
        Maximum
    }

    /// <summary>
    /// Performance targets for AI operations
    /// </summary>
    public enum PerformanceTarget
    {
        Speed,
        Quality,
        Balanced,
        Maximum
    }
}
