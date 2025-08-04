using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.UseCases
{
    /// <summary>
    /// Use case for generating web code with React/Vue support and WebAssembly optimization.
    /// </summary>
    public class GenerateWebCodeUseCase
    {
        private readonly ILogger<GenerateWebCodeUseCase> _logger;
        private readonly IWebCodeGenerator _codeGenerator;
        private readonly IWebAssemblyOptimizer _wasmOptimizer;

        public GenerateWebCodeUseCase(
            ILogger<GenerateWebCodeUseCase> logger,
            IWebCodeGenerator codeGenerator,
            IWebAssemblyOptimizer wasmOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _wasmOptimizer = wasmOptimizer ?? throw new ArgumentNullException(nameof(wasmOptimizer));
        }

        /// <summary>
        /// Executes the web code generation use case.
        /// </summary>
        /// <param name="request">The code generation request.</param>
        /// <returns>The code generation result.</returns>
        public async Task<WebCodeGenerationResult> ExecuteAsync(WebCodeGenerationRequest request)
        {
            _logger.LogInformation("Starting web code generation use case for {ComponentName}", request.ComponentName);

            try
            {
                // Validate request
                if (!_codeGenerator.ValidateRequest(request))
                {
                    _logger.LogWarning("Invalid request for component {ComponentName}", request.ComponentName);
                    return new WebCodeGenerationResult
                    {
                        Success = false,
                        Message = "Invalid request parameters",
                        Framework = request.Framework,
                        ComponentType = request.ComponentType,
                        Optimization = request.Optimization
                    };
                }

                // Generate code
                var result = await _codeGenerator.GenerateCodeAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("Web code generation completed successfully for {ComponentName}", request.ComponentName);
                    
                    // Log performance metrics
                    if (result.PerformanceMetrics.Any())
                    {
                        _logger.LogInformation("Performance metrics for {ComponentName}: {@Metrics}", 
                            request.ComponentName, result.PerformanceMetrics);
                    }

                    // Log bundle size information
                    if (result.BundleSizes.Any())
                    {
                        _logger.LogInformation("Bundle sizes for {ComponentName}: {@BundleSizes}", 
                            request.ComponentName, result.BundleSizes);
                    }

                    // Log warnings if any
                    if (result.Warnings.Any())
                    {
                        _logger.LogWarning("Warnings for {ComponentName}: {@Warnings}", 
                            request.ComponentName, result.Warnings);
                    }
                }
                else
                {
                    _logger.LogError("Web code generation failed for {ComponentName}: {Message}", 
                        request.ComponentName, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during web code generation for {ComponentName}", request.ComponentName);
                
                return new WebCodeGenerationResult
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}",
                    Framework = request.Framework,
                    ComponentType = request.ComponentType,
                    Optimization = request.Optimization
                };
            }
        }

        /// <summary>
        /// Gets available frameworks for code generation.
        /// </summary>
        /// <returns>List of supported frameworks.</returns>
        public IEnumerable<string> GetAvailableFrameworks()
        {
            return _codeGenerator.GetSupportedFrameworks();
        }

        /// <summary>
        /// Gets available component types for a specific framework.
        /// </summary>
        /// <param name="framework">The framework name.</param>
        /// <returns>List of supported component types.</returns>
        public IEnumerable<string> GetAvailableComponentTypes(string framework)
        {
            return _codeGenerator.GetSupportedComponentTypes(framework);
        }

        /// <summary>
        /// Gets available WebAssembly optimization strategies.
        /// </summary>
        /// <returns>List of available optimization strategies.</returns>
        public IEnumerable<WebAssemblyOptimization> GetAvailableOptimizations()
        {
            return _wasmOptimizer.GetAvailableOptimizations();
        }

        /// <summary>
        /// Analyzes the performance of existing code.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze.</param>
        /// <returns>Performance analysis results.</returns>
        public async Task<WebAssemblyPerformanceAnalysis> AnalyzePerformanceAsync(string sourceCode)
        {
            _logger.LogInformation("Starting performance analysis");
            return await _wasmOptimizer.AnalyzePerformanceAsync(sourceCode);
        }

        /// <summary>
        /// Estimates bundle size for existing code.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze.</param>
        /// <param name="config">The WebAssembly configuration.</param>
        /// <returns>Bundle size analysis results.</returns>
        public async Task<WebAssemblyBundleAnalysis> EstimateBundleSizeAsync(string sourceCode, WebAssemblyConfig config)
        {
            _logger.LogInformation("Starting bundle size estimation");
            return await _wasmOptimizer.EstimateBundleSizeAsync(sourceCode, config);
        }
    }
} 