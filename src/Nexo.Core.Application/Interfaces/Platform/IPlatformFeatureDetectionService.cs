using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for platform feature detection service.
    /// Detects platform capabilities and maps feature availability.
    /// </summary>
    public interface IPlatformFeatureDetectionService
    {
        /// <summary>
        /// Detects platform capabilities for a given platform.
        /// </summary>
        /// <param name="platform">The platform to detect capabilities for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Platform capabilities</returns>
        Task<PlatformCapabilities> DetectCapabilitiesAsync(
            string platform,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps feature availability across platforms.
        /// </summary>
        /// <param name="features">The features to map</param>
        /// <param name="platforms">The platforms to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature availability map</returns>
        Task<FeatureAvailabilityMap> MapFeatureAvailabilityAsync(
            IEnumerable<string> features,
            IEnumerable<string> platforms,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates feature compatibility across platforms.
        /// </summary>
        /// <param name="features">The features to validate</param>
        /// <param name="platforms">The platforms to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature compatibility report</returns>
        Task<FeatureCompatibilityReport> ValidateFeatureCompatibilityAsync(
            IEnumerable<string> features,
            IEnumerable<string> platforms,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets platform-specific recommendations for features.
        /// </summary>
        /// <param name="features">The features to get recommendations for</param>
        /// <param name="targetPlatform">The target platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Platform recommendations</returns>
        Task<PlatformRecommendations> GetPlatformRecommendationsAsync(
            IEnumerable<string> features,
            string targetPlatform,
            CancellationToken cancellationToken = default);
    }

    // Platform-specific models for Platform Feature Detection
    public class PlatformCapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<string> SupportedFeatures { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class FeatureAvailabilityMap
    {
        public string FeatureName { get; set; } = string.Empty;
        public Dictionary<string, bool> PlatformSupport { get; set; } = new();
        public List<string> SupportedPlatforms { get; set; } = new();
    }

    public class FeatureCompatibilityReport
    {
        public bool IsCompatible { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> CompatibleFeatures { get; set; } = new();
        public List<string> IncompatibleFeatures { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class PlatformRecommendations
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> RecommendedFeatures { get; set; } = new();
        public List<string> AvoidFeatures { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
    }

    // Additional Platform Feature Detection models
    public class UICapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> SupportedUI { get; set; } = new();
        public List<string> SupportedLayouts { get; set; } = new();
        public List<string> SupportedThemes { get; set; } = new();
    }

    public class DataCapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> SupportedDatabases { get; set; } = new();
        public List<string> SupportedStorage { get; set; } = new();
        public List<string> SupportedCaching { get; set; } = new();
    }

    public class NetworkCapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> SupportedProtocols { get; set; } = new();
        public List<string> SupportedAuth { get; set; } = new();
        public List<string> SupportedAPIs { get; set; } = new();
    }

    public class HardwareCapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> SupportedProcessors { get; set; } = new();
        public List<string> SupportedMemory { get; set; } = new();
        public List<string> SupportedStorage { get; set; } = new();
    }

    public class SecurityCapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> SupportedEncryption { get; set; } = new();
        public List<string> SupportedAuth { get; set; } = new();
        public List<string> SupportedSecurity { get; set; } = new();
    }

    public class PerformanceCapabilities
    {
        public string Platform { get; set; } = string.Empty;
        public List<string> SupportedOptimizations { get; set; } = new();
        public List<string> SupportedCaching { get; set; } = new();
        public List<string> SupportedMonitoring { get; set; } = new();
    }

    public class FeatureAvailability
    {
        public string FeatureName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
    }

    public class FeatureRecommendation
    {
        public string FeatureName { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public List<string> Alternatives { get; set; } = new();
    }

    public class CompatibilityIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> AffectedFeatures { get; set; } = new();
        public List<string> Solutions { get; set; } = new();
    }
}
