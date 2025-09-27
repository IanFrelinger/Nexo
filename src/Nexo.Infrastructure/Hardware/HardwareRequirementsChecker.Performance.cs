using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.Infrastructure.Hardware
{
    /// <summary>
    /// Performance tier and recommendation functionality
    /// </summary>
    public partial class HardwareRequirementsChecker
    {
        /// <inheritdoc />
        public async Task<PerformanceTier?> GetRecommendedPerformanceTierAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var capabilities = await CheckSystemCapabilitiesAsync(cancellationToken);
                var requirements = await GetHardwareRequirementsAsync(cancellationToken);

                // Find the best tier based on current capabilities
                var recommendedTier = requirements.PerformanceTiers
                    .Where(tier => CanRunPerformanceTierAsync(tier, cancellationToken).Result)
                    .OrderByDescending(tier => tier.Level)
                    .FirstOrDefault();

                return recommendedTier;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended performance tier");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<CapabilityRecommendation>> GetOptimizationRecommendationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var capabilities = await CheckSystemCapabilitiesAsync(cancellationToken);
                var recommendations = new List<CapabilityRecommendation>();

                // Memory recommendations
                if (capabilities.AvailableMemoryBytes < 8L * 1024 * 1024 * 1024)
                {
                    recommendations.Add(new CapabilityRecommendation
                    {
                        Type = RecommendationType.HardwareUpgrade,
                        Title = "Upgrade Memory",
                        Description = "Add more RAM to improve performance",
                        Implementation = "Install additional RAM modules",
                        Priority = 1,
                        ImpactScore = 8.0,
                        Cost = 100.0,
                        CostDescription = "Cost of additional RAM"
                    });
                }

                // CPU recommendations
                if (capabilities.CpuCores < 4)
                {
                    recommendations.Add(new CapabilityRecommendation
                    {
                        Type = RecommendationType.HardwareUpgrade,
                        Title = "Upgrade CPU",
                        Description = "Upgrade to a CPU with more cores",
                        Implementation = "Replace CPU with higher core count model",
                        Priority = 2,
                        ImpactScore = 7.0,
                        Cost = 300.0,
                        CostDescription = "Cost of CPU upgrade"
                    });
                }

                // Storage recommendations
                if (capabilities.AvailableStorageBytes < 50L * 1024 * 1024 * 1024)
                {
                    recommendations.Add(new CapabilityRecommendation
                    {
                        Type = RecommendationType.HardwareUpgrade,
                        Title = "Add Storage",
                        Description = "Add more storage space",
                        Implementation = "Install additional SSD storage",
                        Priority = 3,
                        ImpactScore = 5.0,
                        Cost = 80.0,
                        CostDescription = "Cost of additional storage"
                    });
                }

                // Cloud migration recommendations
                if (!capabilities.CanRunNexo)
                {
                    recommendations.Add(new CapabilityRecommendation
                    {
                        Type = RecommendationType.CloudMigration,
                        Title = "Use Cloud Fallback",
                        Description = "Run Nexo on cloud infrastructure",
                        Implementation = "Set up cloud instance with recommended specs",
                        Priority = 1,
                        ImpactScore = 10.0,
                        Cost = 30.0,
                        CostDescription = "Monthly cloud hosting cost"
                    });
                }

                return await Task.FromResult(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization recommendations");
                throw;
            }
        }

        private List<PerformanceTier> CreatePerformanceTiers()
        {
            return new List<PerformanceTier>
            {
                new PerformanceTier
                {
                    Name = "Minimal",
                    Description = "Basic functionality with limited AI capabilities",
                    Level = PerformanceLevel.Minimal,
                    Requirements = new HardwareRequirements
                    {
                        MinimumMemoryBytes = 4L * 1024 * 1024 * 1024,
                        MinimumCpuCores = 2,
                        MinimumStorageBytes = 10L * 1024 * 1024 * 1024
                    },
                    EstimatedCostPerHour = 0.0,
                    Features = new List<string> { "Basic tool generation", "Simple AI models" },
                    IsRecommended = false
                },
                new PerformanceTier
                {
                    Name = "Basic",
                    Description = "Standard functionality with good AI capabilities",
                    Level = PerformanceLevel.Basic,
                    Requirements = new HardwareRequirements
                    {
                        MinimumMemoryBytes = 8L * 1024 * 1024 * 1024,
                        MinimumCpuCores = 4,
                        MinimumStorageBytes = 20L * 1024 * 1024 * 1024
                    },
                    EstimatedCostPerHour = 0.0,
                    Features = new List<string> { "Full tool generation", "Standard AI models", "Quality analysis" },
                    IsRecommended = true
                },
                new PerformanceTier
                {
                    Name = "High Performance",
                    Description = "Enhanced functionality with advanced AI capabilities",
                    Level = PerformanceLevel.High,
                    Requirements = new HardwareRequirements
                    {
                        MinimumMemoryBytes = 16L * 1024 * 1024 * 1024,
                        MinimumCpuCores = 8,
                        MinimumStorageBytes = 50L * 1024 * 1024 * 1024
                    },
                    EstimatedCostPerHour = 0.0,
                    Features = new List<string> { "Advanced tool generation", "Large AI models", "Real-time analysis" },
                    IsRecommended = false
                }
            };
        }
    }
}
