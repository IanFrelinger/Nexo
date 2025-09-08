using System;
using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Result of iOS code generation process.
    /// </summary>
    public class IOSCodeGenerationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public IOSGeneratedCode GeneratedCode { get; set; } = new IOSGeneratedCode();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double GenerationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Generated iOS code components.
    /// </summary>
    public class IOSGeneratedCode
    {
        public List<SwiftFile> SwiftFiles { get; set; } = new List<SwiftFile>();
        public List<SwiftUIFile> SwiftUIFiles { get; set; } = new List<SwiftUIFile>();
        public List<CoreDataFile> CoreDataFiles { get; set; } = new List<CoreDataFile>();
        public List<MetalFile> MetalFiles { get; set; } = new List<MetalFile>();
        public IOSAppConfiguration AppConfiguration { get; set; } = new IOSAppConfiguration();
        public List<IOSUIPattern> AppliedUIPatterns { get; set; } = new List<IOSUIPattern>();
        public List<IOSPerformanceOptimization> AppliedOptimizations { get; set; } = new List<IOSPerformanceOptimization>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Swift source code file.
    /// </summary>
    public class SwiftFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public SwiftFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// SwiftUI view file.
    /// </summary>
    public class SwiftUIFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public SwiftUIViewType ViewType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data model file.
    /// </summary>
    public class CoreDataFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public CoreDataFileType FileType { get; set; }
        public List<CoreDataEntity> Entities { get; set; } = new List<CoreDataEntity>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metal shader file.
    /// </summary>
    public class MetalFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MetalFileType FileType { get; set; }
        public List<MetalGraphicsFeature> Features { get; set; } = new List<MetalGraphicsFeature>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS app configuration.
    /// </summary>
    public class IOSAppConfiguration
    {
        public string AppName { get; set; } = string.Empty;
        public string BundleIdentifier { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public List<string> SupportedDevices { get; set; } = new List<string>();
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public Dictionary<string, object> InfoPlist { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data entity definition.
    /// </summary>
    public class CoreDataEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<CoreDataAttribute> Attributes { get; set; } = new List<CoreDataAttribute>();
        public List<CoreDataRelationship> Relationships { get; set; } = new List<CoreDataRelationship>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data attribute definition.
    /// </summary>
    public class CoreDataAttribute
    {
        public string Name { get; set; } = string.Empty;
        public CoreDataAttributeType Type { get; set; }
        public bool IsOptional { get; set; }
        public bool IsIndexed { get; set; }
        public object? DefaultValue { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data relationship definition.
    /// </summary>
    public class CoreDataRelationship
    {
        public string Name { get; set; } = string.Empty;
        public string DestinationEntity { get; set; } = string.Empty;
        public CoreDataRelationshipType Type { get; set; }
        public bool IsOptional { get; set; }
        public bool IsToMany { get; set; }
        public string DeleteRule { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS UI pattern definition.
    /// </summary>
    public class IOSUIPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IOSUIPatternType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// iOS performance optimization definition.
    /// </summary>
    public class IOSPerformanceOptimization
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IOSPerformanceType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public double PerformanceImpact { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metal graphics feature definition.
    /// </summary>
    public class MetalGraphicsFeature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MetalGraphicsFeatureType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Result classes for different operations
    public class CoreDataIntegrationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CoreDataFile> GeneratedFiles { get; set; } = new List<CoreDataFile>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double IntegrationScore { get; set; }
        public TimeSpan IntegrationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class MetalGraphicsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<MetalFile> GeneratedFiles { get; set; } = new List<MetalFile>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double OptimizationScore { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class IOSUIPatternResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<IOSUIPattern> GeneratedPatterns { get; set; } = new List<IOSUIPattern>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PatternScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class IOSPerformanceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<IOSPerformanceOptimization> GeneratedOptimizations { get; set; } = new List<IOSPerformanceOptimization>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PerformanceScore { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class IOSAppConfigResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public IOSAppConfiguration GeneratedConfiguration { get; set; } = new IOSAppConfiguration();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double ConfigurationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class IOSCodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ValidationWarnings { get; set; } = new List<string>();
        public double ValidationScore { get; set; }
        public TimeSpan ValidationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Options classes for different operations
    public class IOSGenerationOptions
    {
        public bool EnableSwiftUI { get; set; } = true;
        public bool EnableCoreData { get; set; } = true;
        public bool EnableMetalGraphics { get; set; } = false;
        public bool EnablePerformanceOptimization { get; set; } = true;
        public string TargetIOSVersion { get; set; } = "15.0";
        public List<string> SupportedDevices { get; set; } = new List<string> { "iPhone", "iPad" };
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class CoreDataOptions
    {
        public bool EnableCloudKit { get; set; } = false;
        public bool EnableMigration { get; set; } = true;
        public string ModelName { get; set; } = "DataModel";
        public List<string> ExcludedEntities { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class MetalGraphicsOptions
    {
        public bool EnableShaders { get; set; } = true;
        public bool EnableComputeShaders { get; set; } = false;
        public bool EnableRayTracing { get; set; } = false;
        public string ShaderLanguage { get; set; } = "Metal";
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class IOSUIPatternOptions
    {
        public bool EnableNavigationPatterns { get; set; } = true;
        public bool EnableTabBarPatterns { get; set; } = true;
        public bool EnableModalPatterns { get; set; } = true;
        public bool EnableListPatterns { get; set; } = true;
        public List<string> ExcludedPatterns { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class IOSPerformanceOptions
    {
        public bool EnableMemoryOptimization { get; set; } = true;
        public bool EnableBatteryOptimization { get; set; } = true;
        public bool EnableNetworkOptimization { get; set; } = true;
        public bool EnableUIOptimization { get; set; } = true;
        public List<string> ExcludedOptimizations { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class IOSAppConfigOptions
    {
        public string AppName { get; set; } = "GeneratedApp";
        public string BundleIdentifier { get; set; } = "com.example.generatedapp";
        public string Version { get; set; } = "1.0.0";
        public string BuildNumber { get; set; } = "1";
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class IOSValidationOptions
    {
        public bool ValidateSyntax { get; set; } = true;
        public bool ValidateSemantics { get; set; } = true;
        public bool ValidatePerformance { get; set; } = true;
        public bool ValidateSecurity { get; set; } = true;
        public List<string> ExcludedValidations { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    // Android-specific models
    public class AndroidCodeGenerationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AndroidGeneratedCode GeneratedCode { get; set; } = new AndroidGeneratedCode();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double GenerationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidGeneratedCode
    {
        public List<KotlinFile> KotlinFiles { get; set; } = new List<KotlinFile>();
        public List<ComposeFile> ComposeFiles { get; set; } = new List<ComposeFile>();
        public List<RoomFile> RoomFiles { get; set; } = new List<RoomFile>();
        public List<CoroutinesFile> CoroutinesFiles { get; set; } = new List<CoroutinesFile>();
        public AndroidAppConfiguration AppConfiguration { get; set; } = new AndroidAppConfiguration();
        public List<AndroidUIPattern> AppliedUIPatterns { get; set; } = new List<AndroidUIPattern>();
        public List<AndroidPerformanceOptimization> AppliedOptimizations { get; set; } = new List<AndroidPerformanceOptimization>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class KotlinFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public KotlinFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class ComposeFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ComposeViewType ViewType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RoomFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public RoomFileType FileType { get; set; }
        public List<RoomEntity> Entities { get; set; } = new List<RoomEntity>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class CoroutinesFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public CoroutinesFileType FileType { get; set; }
        public List<KotlinCoroutinesFeature> Features { get; set; } = new List<KotlinCoroutinesFeature>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidAppConfiguration
    {
        public string AppName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public int VersionCode { get; set; }
        public int MinSdkVersion { get; set; }
        public int TargetSdkVersion { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
        public Dictionary<string, object> Manifest { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RoomEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<RoomColumn> Columns { get; set; } = new List<RoomColumn>();
        public List<RoomRelationship> Relationships { get; set; } = new List<RoomRelationship>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RoomColumn
    {
        public string Name { get; set; } = string.Empty;
        public RoomColumnType Type { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsNullable { get; set; }
        public object? DefaultValue { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RoomRelationship
    {
        public string Name { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public RoomRelationshipType Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsToMany { get; set; }
        public string OnDelete { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidUIPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AndroidUIPatternType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidPerformanceOptimization
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AndroidPerformanceType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public double PerformanceImpact { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class KotlinCoroutinesFeature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public KotlinCoroutinesFeatureType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Android result classes
    public class RoomDatabaseResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RoomFile> GeneratedFiles { get; set; } = new List<RoomFile>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double IntegrationScore { get; set; }
        public TimeSpan IntegrationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class KotlinCoroutinesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CoroutinesFile> GeneratedFiles { get; set; } = new List<CoroutinesFile>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double OptimizationScore { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidUIPatternResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AndroidUIPattern> GeneratedPatterns { get; set; } = new List<AndroidUIPattern>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PatternScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidPerformanceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AndroidPerformanceOptimization> GeneratedOptimizations { get; set; } = new List<AndroidPerformanceOptimization>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PerformanceScore { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidAppConfigResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AndroidAppConfiguration GeneratedConfiguration { get; set; } = new AndroidAppConfiguration();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double ConfigurationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidCodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ValidationWarnings { get; set; } = new List<string>();
        public double ValidationScore { get; set; }
        public TimeSpan ValidationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Android options classes
    public class AndroidGenerationOptions
    {
        public bool EnableJetpackCompose { get; set; } = true;
        public bool EnableRoomDatabase { get; set; } = true;
        public bool EnableKotlinCoroutines { get; set; } = true;
        public bool EnablePerformanceOptimization { get; set; } = true;
        public string TargetAndroidVersion { get; set; } = "34";
        public List<string> SupportedDevices { get; set; } = new List<string> { "Phone", "Tablet", "TV" };
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class RoomDatabaseOptions
    {
        public bool EnableMigration { get; set; } = true;
        public bool EnableTesting { get; set; } = true;
        public string DatabaseName { get; set; } = "AppDatabase";
        public List<string> ExcludedEntities { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class KotlinCoroutinesOptions
    {
        public bool EnableFlow { get; set; } = true;
        public bool EnableChannels { get; set; } = false;
        public bool EnableSupervisorScope { get; set; } = true;
        public string DispatcherStrategy { get; set; } = "IO";
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidUIPatternOptions
    {
        public bool EnableNavigationPatterns { get; set; } = true;
        public bool EnableBottomNavigation { get; set; } = true;
        public bool EnableMaterialDesign { get; set; } = true;
        public bool EnableListPatterns { get; set; } = true;
        public List<string> ExcludedPatterns { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidPerformanceOptions
    {
        public bool EnableMemoryOptimization { get; set; } = true;
        public bool EnableBatteryOptimization { get; set; } = true;
        public bool EnableNetworkOptimization { get; set; } = true;
        public bool EnableUIOptimization { get; set; } = true;
        public List<string> ExcludedOptimizations { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidAppConfigOptions
    {
        public string AppName { get; set; } = "GeneratedApp";
        public string PackageName { get; set; } = "com.example.generatedapp";
        public string VersionName { get; set; } = "1.0.0";
        public int VersionCode { get; set; } = 1;
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class AndroidValidationOptions
    {
        public bool ValidateSyntax { get; set; } = true;
        public bool ValidateSemantics { get; set; } = true;
        public bool ValidatePerformance { get; set; } = true;
        public bool ValidateSecurity { get; set; } = true;
        public List<string> ExcludedValidations { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    // Web-specific models
    public class WebCodeGenerationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public WebGeneratedCode GeneratedCode { get; set; } = new WebGeneratedCode();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double GenerationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebGeneratedCode
    {
        public List<JavaScriptFile> JavaScriptFiles { get; set; } = new List<JavaScriptFile>();
        public List<TypeScriptFile> TypeScriptFiles { get; set; } = new List<TypeScriptFile>();
        public List<CSSFile> CSSFiles { get; set; } = new List<CSSFile>();
        public List<HTMLFile> HTMLFiles { get; set; } = new List<HTMLFile>();
        public List<WebAssemblyFile> WebAssemblyFiles { get; set; } = new List<WebAssemblyFile>();
        public WebAppConfiguration AppConfiguration { get; set; } = new WebAppConfiguration();
        public List<WebUIPattern> AppliedUIPatterns { get; set; } = new List<WebUIPattern>();
        public List<WebPerformanceOptimization> AppliedOptimizations { get; set; } = new List<WebPerformanceOptimization>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class JavaScriptFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public JavaScriptFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class TypeScriptFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public TypeScriptFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class CSSFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public CSSFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class HTMLFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public HTMLFileType FileType { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebAssemblyFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public WebAssemblyFileType FileType { get; set; }
        public List<WebAssemblyFeature> Features { get; set; } = new List<WebAssemblyFeature>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebAppConfiguration
    {
        public string AppName { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SupportedBrowsers { get; set; } = new List<string>();
        public List<string> Features { get; set; } = new List<string>();
        public Dictionary<string, object> Manifest { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebUIPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WebUIPatternType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebPerformanceOptimization
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WebPerformanceType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public double PerformanceImpact { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebAssemblyFeature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WebAssemblyFeatureType Type { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Web result classes
    public class WebAssemblyResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<WebAssemblyFile> GeneratedFiles { get; set; } = new List<WebAssemblyFile>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double OptimizationScore { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class ProgressiveWebAppResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<JavaScriptFile> GeneratedFiles { get; set; } = new List<JavaScriptFile>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PWAScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebUIPatternResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<WebUIPattern> GeneratedPatterns { get; set; } = new List<WebUIPattern>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PatternScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebPerformanceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<WebPerformanceOptimization> GeneratedOptimizations { get; set; } = new List<WebPerformanceOptimization>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double PerformanceScore { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebAppConfigResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public WebAppConfiguration GeneratedConfiguration { get; set; } = new WebAppConfiguration();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public double ConfigurationScore { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WebCodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ValidationWarnings { get; set; } = new List<string>();
        public double ValidationScore { get; set; }
        public TimeSpan ValidationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Web options classes
    public class WebGenerationOptions
    {
        public bool EnableReact { get; set; } = true;
        public bool EnableVue { get; set; } = false;
        public bool EnableTypeScript { get; set; } = true;
        public bool EnableWebAssembly { get; set; } = false;
        public bool EnableProgressiveWebApp { get; set; } = true;
        public bool EnablePerformanceOptimization { get; set; } = true;
        public string TargetFramework { get; set; } = "React";
        public List<string> SupportedBrowsers { get; set; } = new List<string> { "Chrome", "Firefox", "Safari", "Edge" };
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class WebAssemblyOptions
    {
        public bool EnableRust { get; set; } = true;
        public bool EnableCpp { get; set; } = false;
        public bool EnableAssemblyScript { get; set; } = false;
        public string TargetLanguage { get; set; } = "Rust";
        public List<string> ExcludedFeatures { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class ProgressiveWebAppOptions
    {
        public bool EnableServiceWorker { get; set; } = true;
        public bool EnableOfflineSupport { get; set; } = true;
        public bool EnablePushNotifications { get; set; } = false;
        public bool EnableAppManifest { get; set; } = true;
        public List<string> ExcludedFeatures { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class WebUIPatternOptions
    {
        public bool EnableComponentPatterns { get; set; } = true;
        public bool EnableStateManagement { get; set; } = true;
        public bool EnableRouting { get; set; } = true;
        public bool EnableResponsiveDesign { get; set; } = true;
        public List<string> ExcludedPatterns { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class WebPerformanceOptions
    {
        public bool EnableCodeSplitting { get; set; } = true;
        public bool EnableLazyLoading { get; set; } = true;
        public bool EnableCaching { get; set; } = true;
        public bool EnableMinification { get; set; } = true;
        public List<string> ExcludedOptimizations { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class WebAppConfigOptions
    {
        public string AppName { get; set; } = "GeneratedWebApp";
        public string AppVersion { get; set; } = "1.0.0";
        public string Description { get; set; } = "Generated web application";
        public List<string> RequiredFeatures { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    public class WebValidationOptions
    {
        public bool ValidateSyntax { get; set; } = true;
        public bool ValidateSemantics { get; set; } = true;
        public bool ValidatePerformance { get; set; } = true;
        public bool ValidateAccessibility { get; set; } = true;
        public List<string> ExcludedValidations { get; set; } = new List<string>();
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }
} 