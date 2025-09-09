using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for iOS native code generation.
    /// Part of Phase 6 platform-specific implementation.
    /// </summary>
    public interface IIOSCodeGenerator
    {
        /// <summary>
        /// Generates native iOS code from application logic.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate code from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>iOS generation result.</returns>
        Task<iOSGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Swift UI views from application logic.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate views from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of Swift UI views.</returns>
        Task<IEnumerable<SwiftUIView>> GenerateSwiftUIViewsAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Core Data models from application logic.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate models from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of Core Data models.</returns>
        Task<IEnumerable<CoreDataModel>> GenerateCoreDataModelsAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates ViewModels from application logic.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate ViewModels from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of Swift ViewModels.</returns>
        Task<IEnumerable<SwiftViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates services from application logic.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate services from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of Swift services.</returns>
        Task<IEnumerable<SwiftService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Metal shaders for graphics optimization.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate shaders from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of Metal shaders.</returns>
        Task<IEnumerable<MetalShader>> GenerateMetalShadersAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates app configuration files.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate configuration from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>iOS app configuration.</returns>
        Task<iOSAppConfiguration> GenerateAppConfigurationAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates unit tests for iOS code.
        /// </summary>
        /// <param name="applicationLogic">Application logic to generate tests from.</param>
        /// <param name="options">iOS generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of Swift tests.</returns>
        Task<IEnumerable<SwiftTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// iOS generation options.
    /// </summary>
    public class iOSGenerationOptions
    {
        public bool GenerateViews { get; set; } = true;
        public bool GenerateDataModels { get; set; } = true;
        public bool GenerateViewModels { get; set; } = true;
        public bool GenerateServices { get; set; } = true;
        public bool GenerateMetalShaders { get; set; } = false;
        public bool GenerateAppConfiguration { get; set; } = true;
        public bool GenerateTests { get; set; } = true;
        public bool UseSwiftUI { get; set; } = true;
        public bool UseCoreData { get; set; } = true;
        public bool UseCombine { get; set; } = true;
        public bool UseAsyncAwait { get; set; } = true;
        public string MinimumiOSVersion { get; set; } = "15.0";
        public string TargetiOSVersion { get; set; } = "17.0";
        public bool IncludeAccessibility { get; set; } = true;
        public bool IncludeDarkMode { get; set; } = true;
        public bool IncludeLocalization { get; set; } = true;
    }

    /// <summary>
    /// iOS generation result.
    /// </summary>
    public class iOSGenerationResult
    {
        public string ApplicationName { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public iOSGenerationOptions Options { get; set; } = new();
        
        public IEnumerable<SwiftUIView> Views { get; set; } = new List<SwiftUIView>();
        public IEnumerable<CoreDataModel> DataModels { get; set; } = new List<CoreDataModel>();
        public IEnumerable<SwiftViewModel> ViewModels { get; set; } = new List<SwiftViewModel>();
        public IEnumerable<SwiftService> Services { get; set; } = new List<SwiftService>();
        public IEnumerable<MetalShader> MetalShaders { get; set; } = new List<MetalShader>();
        public iOSAppConfiguration? AppConfiguration { get; set; }
        public IEnumerable<SwiftTest> Tests { get; set; } = new List<SwiftTest>();
    }

    /// <summary>
    /// Swift UI view.
    /// </summary>
    public class SwiftUIView
    {
        public string Name { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Core Data model.
    /// </summary>
    public class CoreDataModel
    {
        public string Name { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Swift ViewModel.
    /// </summary>
    public class SwiftViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Swift service.
    /// </summary>
    public class SwiftService
    {
        public string Name { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Metal shader.
    /// </summary>
    public class MetalShader
    {
        public string Name { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// iOS app configuration.
    /// </summary>
    public class iOSAppConfiguration
    {
        public string AppName { get; set; } = string.Empty;
        public string BundleIdentifier { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public string MinimumiOSVersion { get; set; } = string.Empty;
        public string TargetiOSVersion { get; set; } = string.Empty;
        public string[] SupportedOrientations { get; set; } = Array.Empty<string>();
        public string[] RequiredCapabilities { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> InfoPlistSettings { get; set; } = new();
        public Dictionary<string, string> BuildSettings { get; set; } = new();
    }

    /// <summary>
    /// Swift test.
    /// </summary>
    public class SwiftTest
    {
        public string Name { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #region Application Logic Models

    /// <summary>
    /// Application logic model.
    /// </summary>
    public class ApplicationLogic
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Feature> Features { get; set; } = new();
        public List<Entity> Entities { get; set; } = new();
        public List<Service> Services { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
    }

    /// <summary>
    /// Feature model.
    /// </summary>
    public class Feature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public bool RequiresCamera { get; set; }
        public bool RequiresLocation { get; set; }
        public bool RequiresPushNotifications { get; set; }
        public bool RequiresBluetooth { get; set; }
        public bool RequiresMicrophone { get; set; }
        public bool RequiresGraphics { get; set; }
        public string GraphicsRequirements { get; set; } = string.Empty;
    }

    /// <summary>
    /// Entity model.
    /// </summary>
    public class Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Property> Properties { get; set; } = new();
    }

    /// <summary>
    /// Property model.
    /// </summary>
    public class Property
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// Service model.
    /// </summary>
    public class Service
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Method> Methods { get; set; } = new();
    }

    /// <summary>
    /// Method model.
    /// </summary>
    public class Method
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<Parameter> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Parameter model.
    /// </summary>
    public class Parameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Business rule model.
    /// </summary>
    public class BusinessRule
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
    }

    #endregion
}
