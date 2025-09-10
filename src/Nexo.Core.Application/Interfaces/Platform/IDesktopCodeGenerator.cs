using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    // Platform-specific models for Desktop code generation
    public class DesktopGenerationOptions
    {
        public string ProjectName { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = "net8.0";
        public bool UseWPF { get; set; } = true;
        public bool UseWinUI { get; set; } = false;
        public bool UseAvalonia { get; set; } = false;
        public bool UseDataAccess { get; set; } = true;
        public bool UseConfiguration { get; set; } = true;
        public bool UsePlatformSpecificCode { get; set; } = true;
        public bool UseBuildConfiguration { get; set; } = true;
        public bool UseTest { get; set; } = true;
    }

    public class DesktopGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> GeneratedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class DesktopUIComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DesktopViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DesktopService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DesktopDataAccess
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DesktopConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class PlatformSpecificCode
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DesktopBuildConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DesktopTest
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // Additional Desktop models
    public class DesktopEntity
    {
        public string EntityName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public List<DesktopEntityField> Fields { get; set; } = new();
        public List<string> Indexes { get; set; } = new();
        public List<string> Constraints { get; set; } = new();
    }

    public class DesktopEntityField
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
    }

    public class DesktopRepository
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public List<string> QueryMethods { get; set; } = new();
        public List<string> InsertMethods { get; set; } = new();
        public List<string> UpdateMethods { get; set; } = new();
        public List<string> DeleteMethods { get; set; } = new();
    }

    public class DesktopDatabaseContext
    {
        public string ContextName { get; set; } = string.Empty;
        public List<string> DbSets { get; set; } = new();
        public List<string> Configurations { get; set; } = new();
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class DesktopAppSettings
    {
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, string> Settings { get; set; } = new();
    }

    public class DesktopDependencyInjection
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ImplementationType { get; set; } = string.Empty;
        public string Lifetime { get; set; } = string.Empty;
    }

    public class DesktopLoggingConfiguration
    {
        public string LogLevel { get; set; } = string.Empty;
        public List<string> LogProviders { get; set; } = new();
        public Dictionary<string, string> LogSettings { get; set; } = new();
    }

    public class DesktopProjectFile
    {
        public string ProjectName { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
        public List<string> PackageReferences { get; set; } = new();
        public List<string> ProjectReferences { get; set; } = new();
    }

    public class DesktopSolutionFile
    {
        public string SolutionName { get; set; } = string.Empty;
        public List<string> Projects { get; set; } = new();
        public List<string> Configurations { get; set; } = new();
    }

    public class DesktopBuildScript
    {
        public string ScriptName { get; set; } = string.Empty;
        public string ScriptType { get; set; } = string.Empty;
        public string ScriptContent { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
    }
}
