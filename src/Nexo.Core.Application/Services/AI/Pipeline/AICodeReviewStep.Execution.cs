using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Results;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// Main execution functionality
    /// </summary>
    public partial class AICodeReviewStep
    {
        public async Task<Nexo.Core.Domain.Entities.AI.CodeReviewRequest> ExecuteAsync(Nexo.Core.Domain.Entities.AI.CodeReviewRequest input, PipelineContext context)
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
                                Type = CodeIssueType.Error.ToString(),
                                Message = "No code provided for review",
                                Line = 0,
                                Severity = "High"
                            }
                        },
                        Suggestions = new List<string> { "Provide valid code for review" },
                        ReviewTime = DateTime.UtcNow,
                        EngineType = AIEngineType.Mock.ToString().ToString()
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeReview,
                    TargetPlatform = ConvertToEnumsPlatformType(context.EnvironmentProfile?.CurrentPlatform ?? Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown),
                    MaxTokens = 2048,
                    Temperature = 0.3, // Lower temperature for more consistent reviews
                    Priority = AIPriority.Quality.ToString(),
                    Requirements = new Nexo.Core.Domain.Entities.AI.AIRequirements
                    {
                        Priority = AIPriority.Quality,
                        SafetyLevel = Nexo.Core.Domain.Enums.Safety.SafetyLevel.High,
                        RequiresHighQuality = true,
                        MaxTokens = 2048,
                        Temperature = 0.3
                    }
                };

                // Select optimal AI engine
                var provider = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (provider == null)
                {
                    _logger.LogError("No suitable AI provider found for code review");
                    throw new InvalidOperationException("No AI provider available for code review");
                }

                // Create AI engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = provider.EngineType,
                    ModelPath = GetModelPathForReview(provider.EngineType),
                    MaxTokens = aiContext.MaxTokens,
                    Temperature = aiContext.Temperature
                };

                var model = new ModelInfo
                {
                    Id = "review-model",
                    Name = "Code Review Model",
                    EngineType = provider.EngineType
                };

                var reviewContext = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeReview,
                    Platform = Nexo.Core.Domain.Enums.PlatformType.Unknown,
                    MaxTokens = 1000,
                    Temperature = 0.7,
                    Priority = AIPriority.Balanced.ToString()
                };
                var engine = await provider.CreateEngineAsync(reviewContext);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for code review");
                    throw new InvalidOperationException("Failed to create AI engine for code review");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    await aiEngine.InitializeAsync(model, reviewContext);
                }

                // Perform code review
                var reviewResult = await aiEngine.ReviewCodeAsync(input.Code, reviewContext);

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
                            Type = CodeIssueType.Error.ToString(),
                            Message = $"Code review failed: {ex.Message}",
                            Line = 0,
                            Severity = "High"
                        }
                    },
                    Suggestions = new List<string> { "Review failed due to technical error. Please try again." },
                    ReviewTime = DateTime.UtcNow,
                    EngineType = AIEngineType.Mock.ToString()
                };
                input.ReviewCompleted = false;
                
                return input;
            }
        }

        public async Task<bool> CanExecuteAsync(Nexo.Core.Domain.Entities.AI.CodeReviewRequest input, PipelineContext context)
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
}
