using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    // Platform-specific models for Android code generation
    public class AndroidGenerationOptions
    {
        public string ProjectName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string TargetSdkVersion { get; set; } = "34";
        public string MinSdkVersion { get; set; } = "21";
        public bool UseCompose { get; set; } = true;
        public bool UseRoom { get; set; } = true;
        public bool UseHilt { get; set; } = true;
        public bool UseRetrofit { get; set; } = true;
        public bool UseCoroutines { get; set; } = true;
        public bool UseViewModel { get; set; } = true;
        public bool UseRepository { get; set; } = true;
        public bool UseService { get; set; } = true;
        public bool UseTest { get; set; } = true;
        public bool UseDependencyInjection { get; set; } = true;
        public bool UseAppConfiguration { get; set; } = true;
    }

    public class AndroidGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> GeneratedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ComposeScreen
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class RoomDatabase
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Entities { get; set; } = new();
    }

    public class AndroidViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AndroidRepository
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AndroidService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DependencyInjectionConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AndroidAppConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AndroidTest
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // Additional Android models
    public class RoomEntity
    {
        public string EntityName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public List<EntityField> Fields { get; set; } = new();
        public List<string> Indexes { get; set; } = new();
        public List<string> Constraints { get; set; } = new();
    }

    public class EntityField
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
    }

    public class RoomDAO
    {
        public string DaoName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public List<string> QueryMethods { get; set; } = new();
        public List<string> InsertMethods { get; set; } = new();
        public List<string> UpdateMethods { get; set; } = new();
        public List<string> DeleteMethods { get; set; } = new();
    }

    public class HiltModule
    {
        public string ModuleName { get; set; } = string.Empty;
        public List<string> Provides { get; set; } = new();
        public List<string> Binds { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
    }
}
