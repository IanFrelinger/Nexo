using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Models.Extensions;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    /// <summary>
    /// Basic implementation of IExtensionGenerator using IAIEngine
    /// </summary>
    public class BasicExtensionGenerator : IExtensionGenerator
    {
        private readonly ILogger<BasicExtensionGenerator> _logger;
        private readonly IAIRuntimeSelector _aiRuntimeSelector;

        public BasicExtensionGenerator(ILogger<BasicExtensionGenerator> logger, IAIRuntimeSelector aiRuntimeSelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiRuntimeSelector = aiRuntimeSelector ?? throw new ArgumentNullException(nameof(aiRuntimeSelector));
        }

        /// <summary>
        /// Generates extension code based on the request
        /// </summary>
        public async Task<GeneratedCode> GenerateAsync(ExtensionRequest request)
        {
            _logger.LogInformation("Generating extension: {ExtensionName}", request.Name);

            try
            {
                // Validate the request
                var validationResult = request.Validate();
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Extension request validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                    var result = new GeneratedCode
                    {
                        Code = string.Empty,
                        FileName = !string.IsNullOrEmpty(request.Name) ? $"{request.Name}.cs" : "Extension.cs",
                        FileExtension = ".cs"
                    };
                    result.GenerationMetadata["RequestId"] = request.Id;
                    result.GenerationMetadata["GeneratedAt"] = DateTime.UtcNow;
                    result.GenerationMetadata["ValidationErrors"] = validationResult.Errors;
                    return result;
                }

                // Generate the prompt
                var prompt = request.ToPrompt();
                _logger.LogDebug("Generated prompt for extension: {ExtensionName}", request.Name);

                // Create AI operation context
                var context = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeGeneration,
                    Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                    MaxTokens = 2000,
                    Temperature = 0.7
                };
                context.Parameters["Language"] = "C#";
                context.Parameters["Prompt"] = prompt;
                context.Parameters["TopP"] = 0.9;

                // Get the best AI engine
                var aiEngine = await _aiRuntimeSelector.SelectBestEngineAsync(context);
                if (aiEngine == null)
                {
                    _logger.LogError("No AI engine available for extension generation: {ExtensionName}", request.Name);
                    var result = new GeneratedCode
                    {
                        Code = string.Empty,
                        FileName = $"{request.Name}.cs",
                        FileExtension = ".cs"
                    };
                    result.GenerationMetadata["RequestId"] = request.Id;
                    result.GenerationMetadata["GeneratedAt"] = DateTime.UtcNow;
                    result.GenerationMetadata["Error"] = "No AI engine available";
                    return result;
                }

                // Create code generation request
                var codeRequest = new CodeGenerationRequest
                {
                    Prompt = prompt,
                    Language = "C#",
                    MaxTokens = 2000,
                    Temperature = 0.7,
                    Context = prompt
                };
                codeRequest.Options["TopP"] = 0.9;

                // Generate code using AI engine
                var codeResult = await aiEngine.GenerateCodeAsync(codeRequest);

                if (string.IsNullOrWhiteSpace(codeResult.GeneratedCode))
                {
                    _logger.LogWarning("AI engine returned empty or failed response for extension: {ExtensionName}", request.Name);
                    var result = new GeneratedCode
                    {
                        Code = string.Empty,
                        FileName = $"{request.Name}.cs",
                        FileExtension = ".cs"
                    };
                    result.GenerationMetadata["RequestId"] = request.Id;
                    result.GenerationMetadata["GeneratedAt"] = DateTime.UtcNow;
                    result.GenerationMetadata["Error"] = "Empty or failed response from AI engine";
                    return result;
                }

                // Extract code from the response (basic implementation)
                var code = ExtractCodeFromResponse(codeResult.GeneratedCode);

                _logger.LogInformation("Successfully generated extension: {ExtensionName}", request.Name);

                var successResult = new GeneratedCode
                {
                    Code = code,
                    FileName = $"{request.Name}.cs",
                    FileExtension = ".cs"
                };
                successResult.GenerationMetadata["RequestId"] = request.Id;
                successResult.GenerationMetadata["GeneratedAt"] = DateTime.UtcNow;
                successResult.GenerationMetadata["EngineType"] = aiEngine.EngineInfo.EngineType.ToString();
                successResult.GenerationMetadata["PromptLength"] = prompt.Length;
                successResult.GenerationMetadata["ResponseLength"] = codeResult.GeneratedCode.Length;
                successResult.GenerationMetadata["CodeLength"] = code.Length;

                return successResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating extension: {ExtensionName}", request.Name);
                var result = new GeneratedCode
                {
                    Code = string.Empty,
                    FileName = $"{request.Name}.cs",
                    FileExtension = ".cs"
                };
                result.GenerationMetadata["RequestId"] = request.Id;
                result.GenerationMetadata["GeneratedAt"] = DateTime.UtcNow;
                result.GenerationMetadata["Error"] = ex.Message;
                result.GenerationMetadata["ExceptionType"] = ex.GetType().Name;
                return result;
            }
        }

        /// <summary>
        /// Extracts C# code from the model response
        /// </summary>
        private string ExtractCodeFromResponse(string response)
        {
            // Basic implementation - look for code blocks
            var codeBlockStart = response.IndexOf("```csharp", StringComparison.OrdinalIgnoreCase);
            if (codeBlockStart == -1)
            {
                codeBlockStart = response.IndexOf("```", StringComparison.OrdinalIgnoreCase);
            }

            if (codeBlockStart != -1)
            {
                var codeStart = response.IndexOf('\n', codeBlockStart) + 1;
                var codeBlockEnd = response.IndexOf("```", codeStart);
                if (codeBlockEnd != -1)
                {
                    return response.Substring(codeStart, codeBlockEnd - codeStart).Trim();
                }
            }

            // If no code blocks found, return the entire response
            return response.Trim();
        }
    }
}
