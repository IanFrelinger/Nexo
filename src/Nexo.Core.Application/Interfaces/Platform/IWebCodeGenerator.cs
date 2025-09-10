using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    // Platform-specific models for Web code generation
    public class WebGenerationOptions
    {
        public string ProjectName { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = "net8.0";
        public bool UseReact { get; set; } = true;
        public bool UseVue { get; set; } = false;
        public bool UseAngular { get; set; } = false;
        public bool UseStateManagement { get; set; } = true;
        public bool UseApiLayer { get; set; } = true;
        public bool UseWebAssembly { get; set; } = false;
        public bool UsePWA { get; set; } = false;
        public bool UseServiceWorker { get; set; } = false;
        public bool UseBuildConfiguration { get; set; } = true;
        public bool UseTest { get; set; } = true;
    }

    public class WebGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> GeneratedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class WebComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class WebStateManagement
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class WebApiLayer
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class WebAssemblyModule
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class PWAConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ServiceWorker
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class WebBuildConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class WebTest
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // Additional Web models
    public class WebStore
    {
        public string StoreName { get; set; } = string.Empty;
        public string StoreType { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public List<string> Reducers { get; set; } = new();
        public Dictionary<string, object> State { get; set; } = new();
    }

    public class WebAction
    {
        public string ActionName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public Dictionary<string, object> Payload { get; set; } = new();
    }

    public class WebReducer
    {
        public string ReducerName { get; set; } = string.Empty;
        public string StateType { get; set; } = string.Empty;
        public List<string> ActionTypes { get; set; } = new();
        public string ReducerFunction { get; set; } = string.Empty;
    }

    public class WebApiService
    {
        public string ServiceName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> Endpoints { get; set; } = new();
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class WebApiClient
    {
        public string ClientName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> Methods { get; set; } = new();
        public Dictionary<string, string> Configuration { get; set; } = new();
    }

    public class WebApiType
    {
        public string TypeName { get; set; } = string.Empty;
        public string TypeDefinition { get; set; } = string.Empty;
        public List<string> Properties { get; set; } = new();
    }

    public class PWAManifest
    {
        public string AppName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StartUrl { get; set; } = string.Empty;
        public string ThemeColor { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public List<PWAIcon> Icons { get; set; } = new();
    }

    public class PWAIcon
    {
        public string Src { get; set; } = string.Empty;
        public string Sizes { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class PWASplashScreen
    {
        public string Src { get; set; } = string.Empty;
        public string Sizes { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class WebPackageJson
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Dependencies { get; set; } = new();
        public Dictionary<string, string> DevDependencies { get; set; } = new();
        public Dictionary<string, string> Scripts { get; set; } = new();
    }

    public class WebWebpackConfig
    {
        public string ConfigName { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public List<string> EntryPoints { get; set; } = new();
        public Dictionary<string, object> Plugins { get; set; } = new();
    }

    public class WebTypeScriptConfig
    {
        public string ConfigName { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public List<string> Include { get; set; } = new();
        public List<string> Exclude { get; set; } = new();
    }
}
