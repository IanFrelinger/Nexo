using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Platform;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for Web code generation.
    /// Generates web applications with React/Vue, WebAssembly, and PWA features.
    /// </summary>
    public interface IWebCodeGenerator
    {
        /// <summary>
        /// Generates web application code from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate code from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web generation result</returns>
        Task<WebGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates React/Vue components from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate components from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of web components</returns>
        Task<IEnumerable<WebComponent>> GenerateComponentsAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates state management code from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate state management from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web state management configuration</returns>
        Task<WebStateManagement> GenerateStateManagementAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates API layer from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate API layer from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web API layer configuration</returns>
        Task<WebApiLayer> GenerateApiLayerAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates WebAssembly modules from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate WebAssembly from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of WebAssembly modules</returns>
        Task<IEnumerable<WebAssemblyModule>> GenerateWebAssemblyModulesAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates PWA configuration from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate PWA from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PWA configuration</returns>
        Task<PWAConfiguration> GeneratePWAConfigurationAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates service workers from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate service workers from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of service workers</returns>
        Task<IEnumerable<ServiceWorker>> GenerateServiceWorkersAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates build configuration from application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate build configuration from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Web build configuration</returns>
        Task<WebBuildConfiguration> GenerateBuildConfigurationAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates unit tests for web code.
        /// </summary>
        /// <param name="applicationLogic">The application logic to generate tests from</param>
        /// <param name="options">Web-specific generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of web tests</returns>
        Task<IEnumerable<WebTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default);
    }
}
