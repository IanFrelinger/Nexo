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
    /// Interface for generating native Android code with platform-specific optimizations.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.2: Android Native Implementation.
    /// </summary>
    public interface IAndroidCodeGenerator
    {
        /// <summary>
        /// Generates Jetpack Compose code from standardized application logic.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic to convert</param>
        /// <param name="androidOptions">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android code generation result</returns>
        Task<AndroidCodeGenerationResult> GenerateJetpackComposeCodeAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Integrates Room database with the generated Android code.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="roomOptions">Room database configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Room database integration result</returns>
        Task<RoomDatabaseResult> IntegrateRoomDatabaseAsync(
            StandardizedApplicationLogic applicationLogic,
            RoomDatabaseOptions roomOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates Kotlin coroutines optimization for the Android code.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="coroutinesOptions">Kotlin coroutines configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Kotlin coroutines optimization result</returns>
        Task<KotlinCoroutinesResult> CreateKotlinCoroutinesOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            KotlinCoroutinesOptions coroutinesOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Android UI patterns for the application.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="uiPatternOptions">UI pattern configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android UI patterns generation result</returns>
        Task<AndroidUIPatternResult> GenerateAndroidUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidUIPatternOptions uiPatternOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates Android performance optimization for the generated code.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="performanceOptions">Performance optimization configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android performance optimization result</returns>
        Task<AndroidPerformanceResult> CreateAndroidPerformanceOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidPerformanceOptions performanceOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Android app configuration.
        /// </summary>
        /// <param name="applicationLogic">The standardized application logic</param>
        /// <param name="configOptions">App configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android app configuration result</returns>
        Task<AndroidAppConfigResult> GenerateAndroidAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidAppConfigOptions configOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the generated Android code.
        /// </summary>
        /// <param name="androidCode">The generated Android code to validate</param>
        /// <param name="validationOptions">Validation configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android code validation result</returns>
        Task<AndroidCodeValidationResult> ValidateAndroidCodeAsync(
            AndroidGeneratedCode androidCode,
            AndroidValidationOptions validationOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported Android UI patterns.
        /// </summary>
        /// <returns>List of supported Android UI patterns</returns>
        IEnumerable<AndroidUIPattern> GetSupportedAndroidUIPatterns();

        /// <summary>
        /// Gets supported Android performance optimizations.
        /// </summary>
        /// <returns>List of supported Android performance optimizations</returns>
        IEnumerable<AndroidPerformanceOptimization> GetSupportedAndroidPerformanceOptimizations();

        /// <summary>
        /// Gets supported Kotlin coroutines features.
        /// </summary>
        /// <returns>List of supported Kotlin coroutines features</returns>
        IEnumerable<KotlinCoroutinesFeature> GetSupportedKotlinCoroutinesFeatures();
    }
} 