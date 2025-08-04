using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Result of platform feature detection.
    /// </summary>
    public class PlatformFeatureDetectionResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public string PlatformVersion { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public List<PlatformFeature> DetectedFeatures { get; set; } = new List<PlatformFeature>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime DetectionTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a platform feature.
    /// </summary>
    public class PlatformFeature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FeatureType Type { get; set; }
        public FeatureAvailability Availability { get; set; }
        public FeaturePriority Priority { get; set; }
        public string Version { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public List<string> SupportedPlatforms { get; set; } = new List<string>();
        public bool IsExperimental { get; set; }
        public bool IsDeprecated { get; set; }
        public string DeprecationMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of feature availability check.
    /// </summary>
    public class FeatureAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public FeatureAvailability Availability { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> AlternativeFeatures { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Mapping of feature availability across platforms.
    /// </summary>
    public class FeatureAvailabilityMapping
    {
        public Dictionary<string, Dictionary<PlatformType, FeatureAvailability>> FeatureMap { get; set; } = new Dictionary<string, Dictionary<PlatformType, FeatureAvailability>>();
        public Dictionary<PlatformType, List<string>> PlatformFeatures { get; set; } = new Dictionary<PlatformType, List<string>>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of platform capabilities detection.
    /// </summary>
    public class PlatformCapabilitiesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<PlatformCapability> Capabilities { get; set; } = new List<PlatformCapability>();
        public List<PlatformLimitation> Limitations { get; set; } = new List<PlatformLimitation>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a platform capability.
    /// </summary>
    public class PlatformCapability
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CapabilityType Type { get; set; }
        public bool IsAvailable { get; set; }
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a platform limitation.
    /// </summary>
    public class PlatformLimitation
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LimitationType Type { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Workarounds { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of fallback strategy retrieval.
    /// </summary>
    public class FallbackStrategyResult
    {
        public bool HasFallback { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public PlatformType TargetPlatform { get; set; }
        public List<FallbackOption> FallbackOptions { get; set; } = new List<FallbackOption>();
        public string RecommendedStrategy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a fallback option.
    /// </summary>
    public class FallbackOption
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FallbackType Type { get; set; }
        public double CompatibilityScore { get; set; }
        public List<string> ImplementationSteps { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of feature compatibility validation.
    /// </summary>
    public class FeatureCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public List<PlatformType> Platforms { get; set; } = new List<PlatformType>();
        public Dictionary<string, Dictionary<PlatformType, bool>> CompatibilityMatrix { get; set; } = new Dictionary<string, Dictionary<PlatformType, bool>>();
        public List<CompatibilityIssue> Issues { get; set; } = new List<CompatibilityIssue>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a compatibility issue.
    /// </summary>
    public class CompatibilityIssue
    {
        public string FeatureName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public IssueType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> Solutions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of recommended features retrieval.
    /// </summary>
    public class RecommendedFeaturesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<RecommendedFeature> RecommendedFeatures { get; set; } = new List<RecommendedFeature>();
        public List<string> AvoidedFeatures { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a recommended feature.
    /// </summary>
    public class RecommendedFeature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double RecommendationScore { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> Benefits { get; set; } = new List<string>();
        public List<string> Considerations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of feature monitoring.
    /// </summary>
    public class FeatureMonitoringResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FeatureChange> Changes { get; set; } = new List<FeatureChange>();
        public DateTime MonitoringTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a feature change.
    /// </summary>
    public class FeatureChange
    {
        public string FeatureName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public ChangeType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime ChangeTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of cache refresh operation.
    /// </summary>
    public class CacheRefreshResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CachedFeatures { get; set; }
        public int UpdatedFeatures { get; set; }
        public int RemovedFeatures { get; set; }
        public DateTime RefreshTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    #region Native API Integration Models

    /// <summary>
    /// Result of native API initialization.
    /// </summary>
    public class NativeAPIInitializationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<string> AvailableAPIs { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime InitializationTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of a native API call.
    /// </summary>
    public class NativeAPICallResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string APIName { get; set; } = string.Empty;
        public object Result { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of native API availability check.
    /// </summary>
    public class NativeAPIAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string APIName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> AlternativeAPIs { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of available APIs retrieval.
    /// </summary>
    public class AvailableAPIsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<NativeAPIInfo> AvailableAPIs { get; set; } = new List<NativeAPIInfo>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Information about a native API.
    /// </summary>
    public class NativeAPIInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public APIType Type { get; set; }
        public bool RequiresPermission { get; set; }
        public List<PermissionType> RequiredPermissions { get; set; } = new List<PermissionType>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of permission request.
    /// </summary>
    public class PermissionRequestResult
    {
        public bool IsGranted { get; set; }
        public string APIName { get; set; } = string.Empty;
        public PermissionType PermissionType { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> RequiredActions { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of permission status check.
    /// </summary>
    public class PermissionStatusResult
    {
        public bool HasPermission { get; set; }
        public string APIName { get; set; } = string.Empty;
        public PermissionStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<PermissionType> GrantedPermissions { get; set; } = new List<PermissionType>();
        public List<PermissionType> DeniedPermissions { get; set; } = new List<PermissionType>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of API handler registration.
    /// </summary>
    public class APIHandlerRegistrationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string APIName { get; set; } = string.Empty;
        public bool IsRegistered { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of API abstraction layer retrieval.
    /// </summary>
    public class APIAbstractionLayerResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> AbstractionLayer { get; set; } = new Dictionary<string, object>();
        public List<string> SupportedAPIs { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of API compatibility validation.
    /// </summary>
    public class APICompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public List<string> APIs { get; set; } = new List<string>();
        public List<PlatformType> Platforms { get; set; } = new List<PlatformType>();
        public Dictionary<string, Dictionary<PlatformType, bool>> CompatibilityMatrix { get; set; } = new Dictionary<string, Dictionary<PlatformType, bool>>();
        public List<APICompatibilityIssue> Issues { get; set; } = new List<APICompatibilityIssue>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents an API compatibility issue.
    /// </summary>
    public class APICompatibilityIssue
    {
        public string APIName { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public APICompatibilityIssueType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> Solutions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of native API disposal.
    /// </summary>
    public class NativeAPIDisposalResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DisposedAPIs { get; set; }
        public List<string> DisposedResources { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metadata for API handlers.
    /// </summary>
    public class APIHandlerMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<PlatformType> SupportedPlatforms { get; set; } = new List<PlatformType>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    #endregion

    #region Performance Optimization Models

    /// <summary>
    /// Result of performance optimization initialization.
    /// </summary>
    public class PerformanceOptimizationInitializationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<string> AvailableOptimizations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime InitializationTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance tuning profile.
    /// </summary>
    public class PerformanceTuningProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TuningProfileType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<PlatformType> SupportedPlatforms { get; set; } = new List<PlatformType>();
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Result of performance tuning application.
    /// </summary>
    public class PerformanceTuningResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public List<string> AppliedOptimizations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ApplicationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Memory optimization strategy.
    /// </summary>
    public class MemoryOptimizationStrategy
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MemoryOptimizationType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IsAggressive { get; set; }
        public List<string> TargetAreas { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of memory optimization.
    /// </summary>
    public class MemoryOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public long MemoryFreed { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public List<string> OptimizationsApplied { get; set; } = new List<string>();
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Battery optimization strategy.
    /// </summary>
    public class BatteryOptimizationStrategy
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BatteryOptimizationType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IsPowerSaving { get; set; }
        public List<string> PowerSavingFeatures { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of battery optimization.
    /// </summary>
    public class BatteryOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public double EstimatedBatterySavings { get; set; }
        public List<string> OptimizationsApplied { get; set; } = new List<string>();
        public List<string> PowerSavingFeatures { get; set; } = new List<string>();
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance monitoring configuration.
    /// </summary>
    public class PerformanceMonitoringConfig
    {
        public string Name { get; set; } = string.Empty;
        public bool EnableCPUMonitoring { get; set; } = true;
        public bool EnableMemoryMonitoring { get; set; } = true;
        public bool EnableBatteryMonitoring { get; set; } = true;
        public bool EnableNetworkMonitoring { get; set; } = false;
        public int MonitoringInterval { get; set; } = 1000; // milliseconds
        public List<string> CustomMetrics { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance monitoring operations.
    /// </summary>
    public class PerformanceMonitoringResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsMonitoring { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? StopTime { get; set; }
        public List<string> MonitoredMetrics { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance metrics data.
    /// </summary>
    public class PerformanceMetricsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CollectionTime { get; set; } = DateTime.UtcNow;
        public double CPUsage { get; set; }
        public long MemoryUsage { get; set; }
        public long AvailableMemory { get; set; }
        public double BatteryLevel { get; set; }
        public bool IsCharging { get; set; }
        public double NetworkLatency { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance analysis result.
    /// </summary>
    public class PerformanceAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
        public List<PerformanceBottleneck> Bottlenecks { get; set; } = new List<PerformanceBottleneck>();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
        public double OverallPerformanceScore { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a performance bottleneck.
    /// </summary>
    public class PerformanceBottleneck
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BottleneckType Type { get; set; }
        public string Severity { get; set; } = string.Empty;
        public double Impact { get; set; }
        public List<string> Solutions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a performance recommendation.
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecommendationType Type { get; set; }
        public double ExpectedImprovement { get; set; }
        public string Priority { get; set; } = string.Empty;
        public List<string> ImplementationSteps { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of performance recommendations.
    /// </summary>
    public class PerformanceRecommendationsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of automatic optimization.
    /// </summary>
    public class AutomaticOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> AppliedOptimizations { get; set; } = new List<string>();
        public List<string> SkippedOptimizations { get; set; } = new List<string>();
        public double PerformanceImprovement { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance optimization settings.
    /// </summary>
    public class PerformanceOptimizationSettings
    {
        public bool EnableAutomaticOptimization { get; set; } = true;
        public bool EnableMemoryOptimization { get; set; } = true;
        public bool EnableBatteryOptimization { get; set; } = true;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public int OptimizationInterval { get; set; } = 300000; // 5 minutes
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance validation.
    /// </summary>
    public class PerformanceValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ValidationWarnings { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance reset.
    /// </summary>
    public class PerformanceResetResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ResetSettings { get; set; } = new List<string>();
        public DateTime ResetTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance disposal.
    /// </summary>
    public class PerformanceDisposalResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DisposedResources { get; set; }
        public List<string> DisposedComponents { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    #endregion
} 