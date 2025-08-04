using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Interfaces
{
    /// <summary>
    /// Interface for detecting platform-specific features and capabilities.
    /// Part of Epic 6.2: Platform-Specific Feature Integration, Story 6.2.1: Platform Feature Detection.
    /// </summary>
    public interface IPlatformFeatureDetector
    {
        /// <summary>
        /// Detects all available features for the current platform.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Platform feature detection result</returns>
        Task<PlatformFeatureDetectionResult> DetectPlatformFeaturesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects features for a specific platform.
        /// </summary>
        /// <param name="platformType">The platform type to detect features for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Platform feature detection result</returns>
        Task<PlatformFeatureDetectionResult> DetectFeaturesForPlatformAsync(PlatformType platformType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a specific feature is available on the current platform.
        /// </summary>
        /// <param name="featureName">The name of the feature to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature availability result</returns>
        Task<FeatureAvailabilityResult> CheckFeatureAvailabilityAsync(string featureName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the feature availability mapping for all supported platforms.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature availability mapping</returns>
        Task<FeatureAvailabilityMapping> GetFeatureAvailabilityMappingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects platform-specific capabilities and limitations.
        /// </summary>
        /// <param name="platformType">The platform type to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Platform capabilities result</returns>
        Task<PlatformCapabilitiesResult> DetectPlatformCapabilitiesAsync(PlatformType platformType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the fallback strategy for when a feature is not available.
        /// </summary>
        /// <param name="featureName">The feature name</param>
        /// <param name="targetPlatform">The target platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Fallback strategy result</returns>
        Task<FallbackStrategyResult> GetFallbackStrategyAsync(string featureName, PlatformType targetPlatform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates feature compatibility across platforms.
        /// </summary>
        /// <param name="features">The features to validate</param>
        /// <param name="platforms">The platforms to check against</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature compatibility validation result</returns>
        Task<FeatureCompatibilityResult> ValidateFeatureCompatibilityAsync(IEnumerable<string> features, IEnumerable<PlatformType> platforms, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recommended features for a specific platform based on its capabilities.
        /// </summary>
        /// <param name="platformType">The platform type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recommended features result</returns>
        Task<RecommendedFeaturesResult> GetRecommendedFeaturesAsync(PlatformType platformType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors platform feature changes and updates the detection cache.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature monitoring result</returns>
        Task<FeatureMonitoringResult> MonitorFeatureChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes the platform feature detection cache.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cache refresh result</returns>
        Task<CacheRefreshResult> RefreshFeatureCacheAsync(CancellationToken cancellationToken = default);
    }
} 