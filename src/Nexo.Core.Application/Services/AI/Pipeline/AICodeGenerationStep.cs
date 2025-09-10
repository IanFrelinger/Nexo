using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;

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

        public async Task<CodeGenerationRequest> ExecuteAsync(
            CodeGenerationRequest input, 
            PipelineContext context)
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
