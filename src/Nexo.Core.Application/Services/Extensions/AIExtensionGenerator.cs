using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using Nexo.Core.Domain.Composition;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    /// <summary>
    /// AI-powered extension generator that creates plugins using local Ollama.
    /// </summary>
    public class AIExtensionGenerator : IExtensionGenerator
    {
        private readonly ILogger<AIExtensionGenerator> _logger;
        private readonly IAIEngine _aiEngine;
        private readonly ICSharpSyntaxValidator _syntaxValidator;
        private readonly IExtensionCompiler _compiler;

        public AIExtensionGenerator(
            ILogger<AIExtensionGenerator> logger,
            IAIEngine aiEngine,
            ICSharpSyntaxValidator syntaxValidator,
            IExtensionCompiler compiler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiEngine = aiEngine ?? throw new ArgumentNullException(nameof(aiEngine));
            _syntaxValidator = syntaxValidator ?? throw new ArgumentNullException(nameof(syntaxValidator));
            _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
        }

        public async Task<ExtensionGenerationResult> GenerateExtensionAsync(ExtensionRequest request)
        {
            var result = new ExtensionGenerationResult
            {
                RequestId = request.Id,
                OriginalRequest = request
            };

            var totalStopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting extension generation for: {PluginName}", request.Name);

                // Step 1: Generate code using AI
                var generationStopwatch = Stopwatch.StartNew();
                result.GeneratedCode = await GenerateCodeAsync(request);
                generationStopwatch.Stop();
                result.GenerationTime = generationStopwatch.Elapsed;

                _logger.LogInformation("Code generation completed in {ElapsedMs}ms", result.GenerationTime.TotalMilliseconds);

                // Step 2: Validate syntax
                var validationResult = await ValidateCodeAsync(result.GeneratedCode);
                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        result.AddCompilationError(error.Message, 0, 0, error.Code ?? "SYNTAX_ERROR");
                    }
                    return result;
                }

                // Step 3: Compile the generated code
                var compilationStopwatch = Stopwatch.StartNew();
                var compilationResult = await CompileCodeAsync(result.GeneratedCode, request);
                compilationStopwatch.Stop();
                result.CompilationTime = compilationStopwatch.Elapsed;

                // Merge compilation results
                result.CompiledAssembly = compilationResult.CompiledAssembly;
                result.AssemblyPath = compilationResult.AssemblyPath;
                result.CompilationErrors.AddRange(compilationResult.CompilationErrors);
                result.SyntaxWarnings.AddRange(compilationResult.SyntaxWarnings);

                if (result.IsCompiled)
                {
                    result.IsSuccess = true;
                    _logger.LogInformation("Extension generated and compiled successfully: {PluginName} in {TotalMs}ms", 
                        request.Name, result.TotalTime.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Extension generation failed for: {PluginName} - {ErrorCount} compilation errors", 
                        request.Name, result.CompilationErrors.Count);
                }
            }
            catch (Exception ex)
            {
                result.AddCompilationError($"Unexpected error during extension generation: {ex.Message}", 0, 0, "GENERATION_ERROR");
                _logger.LogError(ex, "Unexpected error generating extension: {PluginName}", request.Name);
            }
            finally
            {
                totalStopwatch.Stop();
            }

            return result;
        }

        public async Task<string> GenerateCodeAsync(ExtensionRequest request)
        {
            try
            {
                _logger.LogInformation("Generating code for extension: {PluginName}", request.Name);

                // Prepare the AI request
                var prompt = request.ToPrompt();
                
                var context = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeGeneration,
                    Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                    MaxTokens = 4000,
                    Temperature = 0.2
                };
                context.Parameters["Language"] = "C#";
                context.Parameters["Prompt"] = prompt;
                context.Parameters["TopP"] = 0.9;
                context.Parameters["SystemPrompt"] = "You are an expert C# developer. Generate only valid, compilable C# code for a plugin that implements the IPlugin interface. Include all necessary using directives and a namespace. Do not include any extra text or markdown formatting, just the raw C# code.";

                var codeRequest = new CodeGenerationRequest
                {
                    Prompt = prompt,
                    Language = "C#",
                    MaxTokens = 4000,
                    Temperature = 0.2,
                    Context = prompt
                };
                codeRequest.Options["TopP"] = 0.9;

                var codeResult = await _aiEngine.GenerateCodeAsync(codeRequest);

                if (codeResult == null || string.IsNullOrWhiteSpace(codeResult.GeneratedCode))
                {
                    throw new InvalidOperationException("AI engine returned empty response");
                }

                // Extract code from the response (remove markdown formatting if present)
                var code = ExtractCodeFromResponse(codeResult.GeneratedCode);
                
                _logger.LogInformation("Code generation completed for: {PluginName}", request.Name);
                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code for extension: {PluginName}", request.Name);
                throw;
            }
        }

        public async Task<ValidationResult> ValidateCodeAsync(string code)
        {
            var result = await _syntaxValidator.ValidateSyntaxAsync(code, "temp");
            var validationResult = new ValidationResult();
            
            if (!result.IsSuccess)
            {
                foreach (var error in result.CompilationErrors)
                {
                    validationResult.AddError(error.Message, "syntax", error.Id);
                }
            }
            
            return validationResult;
        }

        public async Task<ExtensionGenerationResult> CompileCodeAsync(string code, ExtensionRequest request)
        {
            return await _compiler.CompileExtensionAsync(code, request.Name);
        }

        /// <summary>
        /// Extracts C# code from AI response, removing markdown formatting.
        /// </summary>
        private string ExtractCodeFromResponse(string response)
        {
            // Remove markdown code blocks
            var code = response;
            
            // Remove ```csharp and ``` markers
            if (code.Contains("```csharp"))
            {
                var startIndex = code.IndexOf("```csharp") + 9;
                var endIndex = code.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    code = code.Substring(startIndex, endIndex - startIndex);
                }
            }
            else if (code.Contains("```"))
            {
                var startIndex = code.IndexOf("```") + 3;
                var endIndex = code.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    code = code.Substring(startIndex, endIndex - startIndex);
                }
            }

            return code.Trim();
        }
    }
}
