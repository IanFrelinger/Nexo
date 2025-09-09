using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Platform;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for Android native code generation.
    /// Generates native Android code with Jetpack Compose, Room, and Kotlin coroutines.
    /// </summary>
    public interface IAndroidCodeGenerator
    {
        /// <summary>
        /// Generates native Android code from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate code from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android generation result</returns>
        Task<AndroidGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Jetpack Compose UI from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate UI from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of Compose screens</returns>
        Task<IEnumerable<ComposeScreen>> GenerateJetpackComposeUIAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Room database from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate database from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Room database configuration</returns>
        Task<RoomDatabase> GenerateRoomDatabaseAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates ViewModels from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate ViewModels from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of Android ViewModels</returns>
        Task<IEnumerable<AndroidViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates repositories from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate repositories from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of Android repositories</returns>
        Task<IEnumerable<AndroidRepository>> GenerateRepositoriesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates services from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate services from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of Android services</returns>
        Task<IEnumerable<AndroidService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates dependency injection configuration.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate DI from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dependency injection configuration</returns>
        Task<DependencyInjectionConfig> GenerateDependencyInjectionAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates app configuration files.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate configuration from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Android app configuration</returns>
        Task<AndroidAppConfiguration> GenerateAppConfigurationAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates unit tests for Android code.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate tests from</param>
        /// <param name="options">Android-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of Android tests</returns>
        Task<IEnumerable<AndroidTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default);
    }
}
