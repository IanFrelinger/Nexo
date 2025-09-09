using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Platform;

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
}
