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
    /// Interface for generating native iOS code with platform-specific optimizations.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.1: iOS Native Implementation.
    /// </summary>
    public interface IIOSCodeGenerator
    {
        /// <summary>
        /// Generates Swift UI code from standardized application logic.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic to convert</param>
        /// <param name="iosOptions">iOS-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iOS code generation result</returns>
        Task<IOSCodeGenerationResult> GenerateSwiftUICodeAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Integrates Core Data for data persistence.
        /// </summary>
        /// <param name="applicationLogic">The application logic to add Core Data to</param>
        /// <param name="coreDataOptions">Core Data configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Core Data integration result</returns>
        Task<CoreDataIntegrationResult> IntegrateCoreDataAsync(
            StandardizedApplicationLogic applicationLogic,
            CoreDataOptions coreDataOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates Metal graphics optimizations for iOS.
        /// </summary>
        /// <param name="applicationLogic">The application logic to add Metal optimizations to</param>
        /// <param name="metalOptions">Metal graphics configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Metal graphics optimization result</returns>
        Task<MetalGraphicsResult> CreateMetalGraphicsOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            MetalGraphicsOptions metalOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates iOS-specific UI patterns and components.
        /// </summary>
        /// <param name="applicationLogic">The application logic to add iOS UI patterns to</param>
        /// <param name="uiPatternOptions">iOS UI pattern configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iOS UI pattern generation result</returns>
        Task<IOSUIPatternResult> GenerateIOSUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSUIPatternOptions uiPatternOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates iOS-specific performance optimizations.
        /// </summary>
        /// <param name="applicationLogic">The application logic to optimize for iOS</param>
        /// <param name="performanceOptions">iOS performance configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iOS performance optimization result</returns>
        Task<IOSPerformanceResult> CreateIOSPerformanceOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSPerformanceOptions performanceOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates iOS app configuration and setup files.
        /// </summary>
        /// <param name="applicationLogic">The application logic to create iOS app configuration for</param>
        /// <param name="configOptions">iOS app configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iOS app configuration generation result</returns>
        Task<IOSAppConfigResult> GenerateIOSAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSAppConfigOptions configOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the generated iOS code.
        /// </summary>
        /// <param name="iosCode">The generated iOS code to validate</param>
        /// <param name="validationOptions">iOS code validation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iOS code validation result</returns>
        Task<IOSCodeValidationResult> ValidateIOSCodeAsync(
            IOSGeneratedCode iosCode,
            IOSValidationOptions validationOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported iOS UI patterns.
        /// </summary>
        /// <returns>List of supported iOS UI patterns</returns>
        IEnumerable<IOSUIPattern> GetSupportedIOSUIPatterns();

        /// <summary>
        /// Gets supported iOS performance optimizations.
        /// </summary>
        /// <returns>List of supported iOS performance optimizations</returns>
        IEnumerable<IOSPerformanceOptimization> GetSupportedIOSPerformanceOptimizations();

        /// <summary>
        /// Gets supported Metal graphics features.
        /// </summary>
        /// <returns>List of supported Metal graphics features</returns>
        IEnumerable<MetalGraphicsFeature> GetSupportedMetalGraphicsFeatures();
    }
} 