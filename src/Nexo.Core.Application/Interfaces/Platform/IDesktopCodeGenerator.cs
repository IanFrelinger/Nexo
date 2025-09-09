using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Platform;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for Desktop code generation.
    /// Generates native desktop applications for Windows, Mac, and Linux.
    /// </summary>
    public interface IDesktopCodeGenerator
    {
        /// <summary>
        /// Generates desktop application code from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate code from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Desktop generation result</returns>
        Task<DesktopGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates UI components from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate UI from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of desktop UI components</returns>
        Task<IEnumerable<DesktopUIComponent>> GenerateUIComponentsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates ViewModels from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate ViewModels from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of desktop ViewModels</returns>
        Task<IEnumerable<DesktopViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates services from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate services from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of desktop services</returns>
        Task<IEnumerable<DesktopService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates data access layer from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate data access from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Desktop data access configuration</returns>
        Task<DesktopDataAccess> GenerateDataAccessAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates configuration from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate configuration from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Desktop configuration</returns>
        Task<DesktopConfiguration> GenerateConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates platform-specific code from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate platform code from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of platform-specific code</returns>
        Task<IEnumerable<PlatformSpecificCode>> GeneratePlatformSpecificAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates build configuration from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate build configuration from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Desktop build configuration</returns>
        Task<DesktopBuildConfiguration> GenerateBuildConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates unit tests for desktop code.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate tests from</param>
        /// <param name="options">Desktop-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of desktop tests</returns>
        Task<IEnumerable<DesktopTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default);
    }
}
