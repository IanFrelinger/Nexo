using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using CodeOptimizationResult = Nexo.Core.Domain.Entities.AI.CodeOptimizationResult;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered code optimization pipeline step for performance and quality improvements
    /// </summary>
    public class AIOptimizationStep : IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AIOptimizationStep> _logger;

        public AIOptimizationStep(IAIRuntimeSelector runtimeSelector, ILogger<AIOptimizationStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Code Optimization";
        public int Order => 4;

        public async Task<Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest> ExecuteAsync(Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest input, PipelineContext context)
        {
            try
            {
                _logger.LogInformation("Starting AI code optimization for {Language} code", input.Language);

                // Validate input
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogWarning("Empty code provided for optimization");
                    input.Result = new CodeOptimizationResult
                    {
                        OptimizedCode = input.Code,
                        OptimizationScore = 0,
                        Improvements = new List<string> { "No code provided for optimization" },
                        PerformanceGain = 0,
                        OptimizationTime = DateTime.UtcNow,
                        EngineType = AIEngineType.Mock
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeOptimization,
                    TargetPlatform = context.EnvironmentProfile?.CurrentPlatform ?? PlatformType.Unknown,
                    MaxTokens = 4096,
                    Temperature = 0.2, // Lower temperature for more consistent optimizations
                    Priority = AIPriority.Performance,
                    Requirements = new AIRequirements
                    {
                        QualityThreshold = 90,
                        SafetyLevel = SafetyLevel.High,
                        PerformanceTarget = PerformanceTarget.Maximum
                    }
                };

                // Select optimal AI engine
                var selection = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (selection == null)
                {
                    _logger.LogError("No suitable AI provider found for code optimization");
                    throw new InvalidOperationException("No AI provider available for code optimization");
                }

                // Create AI engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = selection.EngineType,
                    ModelPath = GetModelPathForOptimization(selection.EngineType),
                    MaxTokens = aiContext.MaxTokens,
                    Temperature = aiContext.Temperature
                };

                var engine = await selection.Provider.CreateEngineAsync(engineInfo);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for code optimization");
                    throw new InvalidOperationException("Failed to create AI engine for code optimization");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    await aiEngine.InitializeAsync();
                }

                // Perform code optimization
                var optimizationResult = await aiEngine.OptimizeCodeAsync(input);

                // Enhance optimization result with additional analysis
                var enhancedResult = await EnhanceOptimizationResultAsync(optimizationResult, input, context);

                // Apply safety validation
                var validatedResult = await ApplySafetyValidationAsync(enhancedResult, input, context);

                // Update input with results
                input.Result = validatedResult;
                input.OptimizationCompleted = true;
                input.OptimizationTime = DateTime.UtcNow;

                _logger.LogInformation("AI code optimization completed with score {Score} and {Gain}% performance gain", 
                    validatedResult.OptimizationScore, validatedResult.PerformanceGain);

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI code optimization");
                
                // Create fallback result
                input.Result = new CodeOptimizationResult
                {
                    OptimizedCode = input.Code,
                    OptimizationScore = 0,
                    Improvements = new List<string> { $"Optimization failed: {ex.Message}" },
                    PerformanceGain = 0,
                    OptimizationTime = DateTime.UtcNow,
                    EngineType = AIEngineType.Mock
                };
                input.OptimizationCompleted = false;
                
                return input;
            }
        }

        private string GetModelPathForOptimization(AIEngineType engineType)
        {
            return engineType switch
            {
                AIEngineType.LlamaWebAssembly => "models/codellama-7b-instruct.gguf",
                AIEngineType.LlamaNative => "models/codellama-13b-instruct.gguf",
                _ => "models/codellama-7b-instruct.gguf"
            };
        }

        private async Task<CodeOptimizationResult> EnhanceOptimizationResultAsync(CodeOptimizationResult result, Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest request, PipelineContext context)
        {
            _logger.LogDebug("Enhancing code optimization result with additional analysis");

            // Add performance analysis
            var performanceImprovements = await AnalyzePerformanceImprovementsAsync(request.Code, result.OptimizedCode, request.Language);
            result.Improvements.AddRange(performanceImprovements);

            // Add memory optimization analysis
            var memoryImprovements = await AnalyzeMemoryOptimizationsAsync(request.Code, result.OptimizedCode, request.Language);
            result.Improvements.AddRange(memoryImprovements);

            // Add readability improvements
            var readabilityImprovements = await AnalyzeReadabilityImprovementsAsync(request.Code, result.OptimizedCode, request.Language);
            result.Improvements.AddRange(readabilityImprovements);

            // Recalculate optimization score
            result.OptimizationScore = CalculateEnhancedOptimizationScore(result);

            // Calculate actual performance gain
            result.PerformanceGain = await CalculateActualPerformanceGainAsync(request.Code, result.OptimizedCode, request.Language);

            // Add context-specific optimizations
            var contextOptimizations = await GenerateContextOptimizationsAsync(request, context);
            result.Improvements.AddRange(contextOptimizations);

            return result;
        }

        private async Task<CodeOptimizationResult> ApplySafetyValidationAsync(CodeOptimizationResult result, Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest request, PipelineContext context)
        {
            _logger.LogDebug("Applying safety validation to code optimization result");

            // Validate optimized code for safety
            var safetyIssues = await ValidateOptimizedCodeSafetyAsync(result.OptimizedCode, request.Language);
            if (safetyIssues.Any())
            {
                _logger.LogWarning("Safety issues detected in optimized code, reverting to original");
                result.OptimizedCode = request.Code;
                result.Improvements.Add("Optimization reverted due to safety concerns");
            }

            // Filter improvements for safety
            result.Improvements = await FilterImprovementsForSafetyAsync(result.Improvements, request, context);

            return result;
        }

        private async Task<List<string>> AnalyzePerformanceImprovementsAsync(string originalCode, string optimizedCode, CodeLanguage language)
        {
            // In a real implementation, this would analyze performance improvements
            await Task.Delay(100);

            var improvements = new List<string>();

            // Check for common performance improvements
            if (originalCode.Contains("for (int i = 0; i < items.Count; i++)") && 
                optimizedCode.Contains("foreach"))
            {
                improvements.Add("Replaced for loop with foreach for better performance");
            }

            if (originalCode.Contains("string +") && optimizedCode.Contains("StringBuilder"))
            {
                improvements.Add("Replaced string concatenation with StringBuilder");
            }

            if (originalCode.Contains("LINQ") && optimizedCode.Contains("for loop"))
            {
                improvements.Add("Replaced LINQ with for loop for better performance");
            }

            return improvements;
        }

        private async Task<List<string>> AnalyzeMemoryOptimizationsAsync(string originalCode, string optimizedCode, CodeLanguage language)
        {
            // In a real implementation, this would analyze memory optimizations
            await Task.Delay(100);

            var improvements = new List<string>();

            // Check for memory optimizations
            if (originalCode.Contains("new List") && optimizedCode.Contains("Array"))
            {
                improvements.Add("Replaced List with Array to reduce memory allocation");
            }

            if (originalCode.Contains("boxing") && optimizedCode.Contains("generic"))
            {
                improvements.Add("Eliminated boxing by using generics");
            }

            if (originalCode.Contains("dispose") && optimizedCode.Contains("using"))
            {
                improvements.Add("Added proper disposal pattern with using statements");
            }

            return improvements;
        }

        private async Task<List<string>> AnalyzeReadabilityImprovementsAsync(string originalCode, string optimizedCode, CodeLanguage language)
        {
            // In a real implementation, this would analyze readability improvements
            await Task.Delay(100);

            var improvements = new List<string>();

            // Check for readability improvements
            if (originalCode.Contains("magic number") && optimizedCode.Contains("const"))
            {
                improvements.Add("Replaced magic numbers with named constants");
            }

            if (originalCode.Contains("long method") && optimizedCode.Contains("smaller methods"))
            {
                improvements.Add("Broke down large method into smaller, focused methods");
            }

            if (originalCode.Contains("complex condition") && optimizedCode.Contains("extracted method"))
            {
                improvements.Add("Extracted complex conditions into well-named methods");
            }

            return improvements;
        }

        private int CalculateEnhancedOptimizationScore(CodeOptimizationResult result)
        {
            var baseScore = result.OptimizationScore;
            var improvementBonus = result.Improvements.Count * 2;
            var performanceBonus = (int)(result.PerformanceGain * 0.5);

            return Math.Min(100, baseScore + improvementBonus + performanceBonus);
        }

        private async Task<double> CalculateActualPerformanceGainAsync(string originalCode, string optimizedCode, CodeLanguage language)
        {
            // In a real implementation, this would calculate actual performance gain
            await Task.Delay(100);

            // Simulate performance analysis
            var improvements = 0;
            
            if (originalCode.Contains("for (int i = 0; i < items.Count; i++)") && optimizedCode.Contains("foreach"))
                improvements += 15;
            
            if (originalCode.Contains("string +") && optimizedCode.Contains("StringBuilder"))
                improvements += 25;
            
            if (originalCode.Contains("LINQ") && optimizedCode.Contains("for loop"))
                improvements += 20;

            return Math.Min(100, improvements);
        }

        private async Task<List<string>> GenerateContextOptimizationsAsync(Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest request, PipelineContext context)
        {
            // In a real implementation, this would generate context-specific optimizations
            await Task.Delay(50);

            var optimizations = new List<string>();

            // Add context-specific optimizations
            if (context.EnvironmentProfile?.CurrentPlatform == PlatformType.WebAssembly)
            {
                optimizations.Add("Applied WebAssembly-specific optimizations for better browser performance");
            }

            if (context.EnvironmentProfile?.CurrentPlatform == PlatformType.Windows)
            {
                optimizations.Add("Applied Windows-specific optimizations for better native performance");
            }

            if (request.OptimizationType == OptimizationType.Performance)
            {
                optimizations.Add("Applied performance-focused optimizations");
            }

            if (request.OptimizationType == OptimizationType.Memory)
            {
                optimizations.Add("Applied memory-focused optimizations");
            }

            return optimizations;
        }

        private async Task<List<string>> ValidateOptimizedCodeSafetyAsync(string optimizedCode, CodeLanguage language)
        {
            // In a real implementation, this would validate optimized code for safety
            await Task.Delay(50);

            var issues = new List<string>();

            // Check for safety issues
            if (optimizedCode.Contains("unsafe"))
            {
                issues.Add("Unsafe code detected in optimization");
            }

            if (optimizedCode.Contains("eval") || optimizedCode.Contains("exec"))
            {
                issues.Add("Dynamic code execution detected in optimization");
            }

            if (optimizedCode.Contains("reflection") && optimizedCode.Contains("private"))
            {
                issues.Add("Private member access via reflection detected");
            }

            return issues;
        }

        private async Task<List<string>> FilterImprovementsForSafetyAsync(List<string> improvements, Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest request, PipelineContext context)
        {
            // In a real implementation, this would filter improvements for safety
            await Task.Delay(50);

            var filteredImprovements = new List<string>();

            foreach (var improvement in improvements)
            {
                // Filter out potentially unsafe improvements
                if (!improvement.Contains("unsafe") && 
                    !improvement.Contains("reflection") && 
                    !improvement.Contains("eval"))
                {
                    filteredImprovements.Add(improvement);
                }
            }

            return filteredImprovements;
        }

        public async Task<bool> CanExecuteAsync(Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogDebug("Cannot execute code optimization step: empty code provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute code optimization step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute code optimization step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if code optimization step can execute");
                return false;
            }
        }
    }


    /// <summary>
    /// Types of code optimization
    /// </summary>
    public enum OptimizationType
    {
        Performance,
        Memory,
        Readability,
        Balanced,
        Maximum
    }
}
