using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Results;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// Core AI testing step functionality
    /// </summary>
    public partial class AITestingStep : IPipelineStep<TestingRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AITestingStep> _logger;

        public AITestingStep(IAIRuntimeSelector runtimeSelector, ILogger<AITestingStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Test Generation";
        public int Order => 5;

        public async Task<TestingRequest> ExecuteAsync(TestingRequest input, PipelineContext context)
        {
            try
            {
                _logger.LogInformation("Starting AI test generation for {Language} code", input.Language);

                // Validate input
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogWarning("Empty code provided for test generation");
                    input.Result = new Nexo.Core.Domain.Results.TestingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No code provided for test generation.",
                        Score = 0,
                        CompletedAt = DateTime.UtcNow
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.Testing,
                    TargetPlatform = ConvertToEnumsPlatformType(context.EnvironmentProfile?.CurrentPlatform ?? Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown),
                    MaxTokens = 4096,
                    Temperature = 0.3, // Lower temperature for more consistent test generation
                    Priority = AIPriority.Quality.ToString(),
                    Requirements = new Nexo.Core.Domain.Entities.AI.AIRequirements
                    {
                        Priority = AIPriority.Quality,
                        SafetyLevel = Nexo.Core.Domain.Enums.Safety.SafetyLevel.High,
                        RequiresHighQuality = true,
                        MaxTokens = 4096,
                        Temperature = 0.3
                    }
                };

                // Select optimal AI engine
                var provider = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (provider == null)
                {
                    _logger.LogError("No suitable AI provider found for test generation");
                    throw new InvalidOperationException("No AI provider available for test generation");
                }

                // Create AI engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = provider.EngineType,
                    ModelPath = GetModelPathForTesting(provider.EngineType),
                    MaxTokens = aiContext.MaxTokens,
                    Temperature = aiContext.Temperature
                };

                var engine = await provider.CreateEngineAsync(aiContext);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for test generation");
                    throw new InvalidOperationException("Failed to create AI engine for test generation");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    var modelInfo = new ModelInfo
                    {
                        Id = "test-model",
                        Name = "Test Model",
                        Version = "1.0",
                        Size = 1000000,
                        Format = "GGUF"
                    };
                    await aiEngine.InitializeAsync(modelInfo, aiContext);
                }

                // Generate tests
                var testCode = await GenerateTestCodeAsync(aiEngine, input, context);

                // Enhance test code with additional analysis
                var enhancedTests = await EnhanceTestCodeAsync(testCode, input, context);

                // Apply safety validation
                var validatedTests = await ApplySafetyValidationAsync(enhancedTests, input, context);

                // Create testing result
                var result = new Nexo.Core.Domain.Results.TestingResult
                {
                    IsSuccess = true,
                    Score = (int)CalculateTestQuality(validatedTests, input),
                    SuccessMessage = $"Generated tests with {CalculateTestCoverage(validatedTests, input.Code)}% coverage",
                    CompletedAt = DateTime.UtcNow
                };

                // Update input with results
                input.Result = result;
                input.TestGenerationCompleted = true;
                input.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("AI test generation completed with quality score {Score} and {Coverage}% coverage", 
                    result.QualityScore, result.Coverage);

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI test generation");
                
                // Create fallback result
                input.Result = new Nexo.Core.Domain.Results.TestingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Test generation failed: {ex.Message}",
                    Exception = ex,
                    Score = 0,
                    CompletedAt = DateTime.UtcNow
                };
                input.TestGenerationCompleted = false;
                
                return input;
            }
        }

        public async Task<bool> CanExecuteAsync(TestingRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogDebug("Cannot execute test generation step: empty code provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute test generation step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute test generation step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if test generation step can execute");
                return false;
            }
        }
    }
}
