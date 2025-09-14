using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for hardware requirements checking
    /// </summary>
    public interface IHardwareRequirementsChecker
    {
        /// <summary>
        /// Checks if the current system meets hardware requirements
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>System capabilities assessment</returns>
        Task<SystemCapabilities> CheckSystemCapabilitiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets hardware requirements for different performance tiers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Hardware requirements</returns>
        Task<HardwareRequirements> GetHardwareRequirementsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available cloud fallback options
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of cloud fallback options</returns>
        Task<List<CloudFallbackOption>> GetCloudFallbackOptionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Recommends the best performance tier for the current system
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recommended performance tier</returns>
        Task<PerformanceTier?> GetRecommendedPerformanceTierAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Recommends the best cloud fallback option
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recommended cloud fallback option</returns>
        Task<CloudFallbackOption?> GetRecommendedCloudOptionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a specific performance tier can run on the current system
        /// </summary>
        /// <param name="tier">Performance tier to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the tier can run</returns>
        Task<bool> CanRunPerformanceTierAsync(PerformanceTier tier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets optimization recommendations for the current system
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of optimization recommendations</returns>
        Task<List<CapabilityRecommendation>> GetOptimizationRecommendationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Estimates the cost of running Nexo on different cloud providers
        /// </summary>
        /// <param name="hoursPerMonth">Hours per month of usage</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cost estimates for different providers</returns>
        Task<Dictionary<string, double>> EstimateCloudCostsAsync(int hoursPerMonth, CancellationToken cancellationToken = default);
    }
}
