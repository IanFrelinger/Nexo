using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// Pipeline step for AI-powered code generation
    /// </summary>
    public class AICodeGenerationStep : IPipelineStep<CodeGenerationRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AICodeGenerationStep> _logger;

        public AICodeGenerationStep(
            IAIRuntimeSelector runtimeSelector,
            ILogger<AICodeGenerationStep> logger)
        {
            _runtimeSelector = runtimeSelector;
            _logger = logger;
        }

        public string Name => "AI Code Generation";
        public int Order => 1;

        public async Task<CodeGenerationRequest> ExecuteAsync(
            CodeGenerationRequest input, 
            Nexo.Core.Domain.Entities.Pipeline.PipelineContext context)
        {
            _logger.LogDebug("Executing AI code generation for prompt: {Prompt}", input.Prompt);

            try
            {
                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeGeneration,
                    Platform = context.EnvironmentProfile.CurrentPlatform,
                    Requirements = input.Requirements,
                    Resources = new AIResources
                    {
                        AvailableMemory = context.EnvironmentProfile.AvailableMemory,
                        CpuCores = context.EnvironmentProfile.CpuCores,
                        HasInternetConnection = true // TODO: Detect actual connectivity
                    },
                    Parameters = input.Options
                };

                // Select best AI engine
                var aiEngine = await _runtimeSelector.SelectBestEngineAsync(aiContext);
                
                _logger.LogInformation("Selected AI engine: {EngineType} for code generation", 
                    aiEngine.EngineInfo.Type);

                // Generate code using AI
                var result = await aiEngine.GenerateCodeAsync(input);

                // Update input with generated code
                input.GeneratedCode = result.GeneratedCode;
                input.Explanation = result.Explanation;
                input.Confidence = result.Confidence;
                input.ConfidenceScore = result.ConfidenceScore;
                input.Suggestions = result.Suggestions;
                input.Warnings = result.Warnings;
                input.Metadata = result.Metadata;

                _logger.LogInformation("AI code generation completed successfully. Generated {Length} characters", 
                    result.GeneratedCode.Length);

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute AI code generation for prompt: {Prompt}", input.Prompt);
                
                // Add error information to input
                input.Error = ex.Message;
                input.Metadata["Error"] = ex.Message;
                input.Metadata["ErrorType"] = ex.GetType().Name;
                
                throw;
            }
        }

        public async Task<bool> CanExecuteAsync(CodeGenerationRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Prompt))
                {
                    _logger.LogDebug("Cannot execute code generation step: empty prompt provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute code generation step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute code generation step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if code generation step can execute");
                return false;
            }
        }
    }

    /// <summary>
    /// Extended code generation request with AI-specific properties
    /// </summary>
    public class AICodeGenerationRequest : CodeGenerationRequest
    {
        public string? GeneratedCode { get; set; }
        public string? Explanation { get; set; }
        public AIConfidenceLevel Confidence { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string? Error { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
