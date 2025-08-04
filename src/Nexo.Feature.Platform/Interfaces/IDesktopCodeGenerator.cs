using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Interfaces
{
    /// <summary>
    /// Interface for desktop code generation services.
    /// </summary>
    public interface IDesktopCodeGenerator
    {
        /// <summary>
        /// Generates desktop code based on the provided request.
        /// </summary>
        /// <param name="request">The code generation request.</param>
        /// <returns>The code generation result.</returns>
        Task<DesktopCodeGenerationResult> GenerateCodeAsync(DesktopCodeGenerationRequest request);
        
        /// <summary>
        /// Validates the code generation request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>True if the request is valid, false otherwise.</returns>
        bool ValidateRequest(DesktopCodeGenerationRequest request);
        
        /// <summary>
        /// Gets supported desktop platforms for code generation.
        /// </summary>
        /// <returns>List of supported desktop platform types.</returns>
        IEnumerable<string> GetSupportedPlatforms();
        
        /// <summary>
        /// Gets supported application types for a given platform.
        /// </summary>
        /// <param name="platform">The platform type.</param>
        /// <returns>List of supported application types.</returns>
        IEnumerable<string> GetSupportedApplicationTypes(string platform);
        
        /// <summary>
        /// Gets supported UI frameworks for a given platform.
        /// </summary>
        /// <param name="platform">The platform type.</param>
        /// <returns>List of supported UI frameworks.</returns>
        IEnumerable<string> GetSupportedUIFrameworks(string platform);
        
        /// <summary>
        /// Generates platform-specific optimizations for the given code.
        /// </summary>
        /// <param name="code">The base code to optimize.</param>
        /// <param name="platform">The target platform.</param>
        /// <param name="optimizationLevel">The optimization level to apply.</param>
        /// <returns>The optimized code.</returns>
        Task<string> OptimizeForPlatformAsync(string code, string platform, DesktopOptimizationLevel optimizationLevel);
        
        /// <summary>
        /// Generates system integration features for the given platform.
        /// </summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="features">The features to integrate.</param>
        /// <returns>The system integration code.</returns>
        Task<string> GenerateSystemIntegrationAsync(string platform, IEnumerable<string> features);
        
        /// <summary>
        /// Validates the generated code for platform compatibility.
        /// </summary>
        /// <param name="code">The code to validate.</param>
        /// <param name="platform">The target platform.</param>
        /// <returns>Validation result with any issues found.</returns>
        Task<DesktopCodeValidationResult> ValidateCodeAsync(string code, string platform);
        
        /// <summary>
        /// Generates deployment configuration for the given platform.
        /// </summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="configuration">The deployment configuration.</param>
        /// <returns>The deployment configuration files.</returns>
        Task<DesktopDeploymentConfig> GenerateDeploymentConfigAsync(string platform, DesktopDeploymentRequest configuration);
        
        /// <summary>
        /// Analyzes performance characteristics for the given platform.
        /// </summary>
        /// <param name="code">The code to analyze.</param>
        /// <param name="platform">The target platform.</param>
        /// <returns>Performance analysis results.</returns>
        Task<DesktopPerformanceAnalysis> AnalyzePerformanceAsync(string code, string platform);
        
        /// <summary>
        /// Generates native API bindings for the given platform.
        /// </summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="apis">The APIs to bind.</param>
        /// <returns>The native API bindings.</returns>
        Task<string> GenerateNativeAPIBindingsAsync(string platform, IEnumerable<string> apis);
    }
} 