using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Results;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered documentation generation pipeline step for automatic documentation
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AIDocumentationStep : IPipelineStep<DocumentationRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AIDocumentationStep> _logger;

        public AIDocumentationStep(IAIRuntimeSelector runtimeSelector, ILogger<AIDocumentationStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Documentation Generation";
        public int Order => 3;

        public async Task<DocumentationRequest> ExecuteAsync(DocumentationRequest input, PipelineContext context)
        {
            try
            {
                _logger.LogInformation("Starting AI documentation generation for {Language} code", input.Language);

                // Validate input
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogWarning("Empty code provided for documentation generation");
                    input.Result = new Nexo.Core.Domain.Results.DocumentationResult
                    {
                        IsSuccess = true
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.Documentation,
                    TargetPlatform = ConvertToEnumsPlatformType(context.EnvironmentProfile?.CurrentPlatform ?? Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown),
                    MaxTokens = 3072,
                    Temperature = 0.4, // Moderate temperature for balanced creativity and consistency
                    Priority = AIPriority.Quality.ToString(),
                    Requirements = new Nexo.Core.Domain.Entities.AI.AIRequirements
                    {
                        Priority = AIPriority.Quality,
                        SafetyLevel = Nexo.Core.Domain.Enums.Safety.SafetyLevel.Medium,
                        RequiresHighQuality = true,
                        MaxTokens = 3072,
                        Temperature = 0.4
                    }
                };

                // Select optimal AI engine
                var selection = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (selection == null)
                {
                    _logger.LogError("No suitable AI provider found for documentation generation");
                    throw new InvalidOperationException("No AI provider available for documentation generation");
                }

                // Create AI engine
                var engine = await selection.CreateEngineAsync(aiContext);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for documentation generation");
                    throw new InvalidOperationException("Failed to create AI engine for documentation generation");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    var model = new ModelInfo { Id = "mock-model", Name = "Mock Model" };
                    await aiEngine.InitializeAsync(model, aiContext);
                }

                // Generate documentation
                var documentation = await aiEngine.GenerateDocumentationAsync(input.Code, aiContext);

                // Enhance documentation with additional analysis
                var enhancedDocumentation = await EnhanceDocumentationAsync(documentation, input, context);

                // Apply safety validation
                var validatedDocumentation = await ApplySafetyValidationAsync(enhancedDocumentation, input, context);

                // Create documentation result
                var result = new Nexo.Core.Domain.Results.DocumentationResult
                {
                    IsSuccess = true
                };

                // Update input with results
                input.Result = result;
                input.DocumentationCompleted = true;
                input.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("AI documentation generation completed successfully");

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI documentation generation");
                
                // Create fallback result
                input.Result = new Nexo.Core.Domain.Results.DocumentationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
                input.DocumentationCompleted = false;
                
                return input;
            }
        }

        public async Task<bool> CanExecuteAsync(DocumentationRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogDebug("Cannot execute documentation step: empty code provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute documentation step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute documentation step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if documentation step can execute");
                return false;
            }
        }
        // This class acts as an orchestrator for various AI documentation functionalities,
        // with specific categories defined in partial classes.
    }

    /// <summary>
    /// Documentation result from AI pipeline processing
    /// </summary>
    public class DocumentationResult
    {
        public string GeneratedDocumentation { get; set; } = string.Empty;
        public DocumentationType DocumentationType { get; set; }
        public int QualityScore { get; set; }
        public int Coverage { get; set; }
        public DateTime GenerationTime { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public AIEngineType EngineType { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of documentation
    /// </summary>
    public enum DocumentationType
    {
        API,
        Internal,
        User,
        Technical,
        Tutorial
    }
}