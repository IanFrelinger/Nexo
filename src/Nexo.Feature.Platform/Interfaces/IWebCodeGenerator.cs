using Nexo.Feature.AI.Models;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Platform.Interfaces
{
    /// <summary>
    /// Interface for generating web code with modern framework optimizations.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.3: Web Implementation.
    /// </summary>
    public interface IWebCodeGenerator
    {
        /// <summary>
        /// Generates React code from standardized application logic.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic to convert</param>
        /// <param name="webOptions">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web code generation result</returns>
        Task<WebCodeGenerationResult> GenerateReactCodeAsync(
            StandardizedApplicationLogic applicationLogic,
            WebGenerationOptions webOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Vue.js code from standardized application logic.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic to convert</param>
        /// <param name="webOptions">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web code generation result</returns>
        Task<WebCodeGenerationResult> GenerateVueCodeAsync(
            StandardizedApplicationLogic applicationLogic,
            WebGenerationOptions webOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates WebAssembly performance optimization for the web code.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="wasmOptions">WebAssembly configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>WebAssembly optimization result</returns>
        Task<WebAssemblyResult> CreateWebAssemblyOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            WebAssemblyOptions wasmOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates progressive web app features.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="pwaOptions">Progressive Web App configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Progressive Web App features result</returns>
        Task<ProgressiveWebAppResult> GenerateProgressiveWebAppFeaturesAsync(
            StandardizedApplicationLogic applicationLogic,
            ProgressiveWebAppOptions pwaOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates web UI patterns for the application.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="uiPatternOptions">UI pattern configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web UI patterns generation result</returns>
        Task<WebUIPatternResult> GenerateWebUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            WebUIPatternOptions uiPatternOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates web performance optimization for the generated code.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="performanceOptions">Performance optimization configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web performance optimization result</returns>
        Task<WebPerformanceResult> CreateWebPerformanceOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            WebPerformanceOptions performanceOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates web app configuration.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="configOptions">App configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web app configuration result</returns>
        Task<WebAppConfigResult> GenerateWebAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            WebAppConfigOptions configOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the generated web code.
        /// </summary>
        /// <param name="webCode">The generated web code to validate</param>
        /// <param name="validationOptions">Validation configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web code validation result</returns>
        Task<WebCodeValidationResult> ValidateWebCodeAsync(
            WebGeneratedCode webCode,
            WebValidationOptions validationOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported web UI patterns.
        /// </summary>
        /// <returns>List of supported web UI patterns</returns>
        IEnumerable<WebUIPattern> GetSupportedWebUIPatterns();

        /// <summary>
        /// Gets supported web performance optimizations.
        /// </summary>
        /// <returns>List of supported web performance optimizations</returns>
        IEnumerable<WebPerformanceOptimization> GetSupportedWebPerformanceOptimizations();

        /// <summary>
        /// Gets supported WebAssembly features.
        /// </summary>
        /// <returns>List of supported WebAssembly features</returns>
        IEnumerable<WebAssemblyFeature> GetSupportedWebAssemblyFeatures();
    }
} 