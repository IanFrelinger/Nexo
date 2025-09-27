using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Core platform feature detection models
    /// </summary>
    public partial class PlatformFeatureDetectionResult
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
    public partial class PlatformFeature
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
    public partial class FeatureAvailabilityResult
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
    public partial class FeatureAvailabilityMapping
    {
        public Dictionary<string, Dictionary<PlatformType, FeatureAvailability>> FeatureMap { get; set; } = new Dictionary<string, Dictionary<PlatformType, FeatureAvailability>>();
        public Dictionary<PlatformType, List<string>> PlatformFeatures { get; set; } = new Dictionary<PlatformType, List<string>>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of platform capabilities detection.
    /// </summary>
    public partial class PlatformCapabilitiesResult
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
    public partial class PlatformCapability
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
    public partial class PlatformLimitation
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
    public partial class FallbackStrategyResult
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
    public partial class FallbackOption
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
    public partial class FeatureCompatibilityResult
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
    public partial class CompatibilityIssue
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
    public partial class RecommendedFeaturesResult
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
    public partial class RecommendedFeature
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
    public partial class FeatureMonitoringResult
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
    public partial class FeatureChange
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
    public partial class CacheRefreshResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CachedFeatures { get; set; }
        public int UpdatedFeatures { get; set; }
        public int RemovedFeatures { get; set; }
        public DateTime RefreshTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
