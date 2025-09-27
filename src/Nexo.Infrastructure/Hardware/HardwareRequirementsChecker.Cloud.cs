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
    /// Cloud fallback and cost estimation functionality
    /// </summary>
    public partial class HardwareRequirementsChecker
    {
        /// <inheritdoc />
        public async Task<List<CloudFallbackOption>> GetCloudFallbackOptionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting cloud fallback options");

                var options = new List<CloudFallbackOption>
                {
                    new CloudFallbackOption
                    {
                        Name = "Azure Standard B2s",
                        Description = "2 vCPUs, 4GB RAM, 8GB SSD",
                        Provider = CloudProvider.Azure,
                        Region = "East US",
                        Requirements = new HardwareRequirements
                        {
                            MinimumMemoryBytes = 4L * 1024 * 1024 * 1024,
                            MinimumCpuCores = 2,
                            MinimumStorageBytes = 8L * 1024 * 1024 * 1024
                        },
                        Pricing = new PricingModel
                        {
                            HourlyRate = 0.084,
                            MonthlyRate = 60.48,
                            Currency = "USD"
                        },
                        Features = new List<string> { "Basic AI models", "Standard performance" },
                        IsAvailable = true,
                        SetupInstructions = "Create Azure VM with B2s instance type"
                    },
                    new CloudFallbackOption
                    {
                        Name = "AWS t3.medium",
                        Description = "2 vCPUs, 4GB RAM, EBS storage",
                        Provider = CloudProvider.AWS,
                        Region = "us-east-1",
                        Requirements = new HardwareRequirements
                        {
                            MinimumMemoryBytes = 4L * 1024 * 1024 * 1024,
                            MinimumCpuCores = 2,
                            MinimumStorageBytes = 20L * 1024 * 1024 * 1024
                        },
                        Pricing = new PricingModel
                        {
                            HourlyRate = 0.0416,
                            MonthlyRate = 30.00,
                            Currency = "USD"
                        },
                        Features = new List<string> { "Basic AI models", "Pay-as-you-go" },
                        IsAvailable = true,
                        SetupInstructions = "Launch EC2 instance with t3.medium instance type"
                    },
                    new CloudFallbackOption
                    {
                        Name = "Google Cloud e2-standard-2",
                        Description = "2 vCPUs, 8GB RAM, 10GB SSD",
                        Provider = CloudProvider.GoogleCloud,
                        Region = "us-central1",
                        Requirements = new HardwareRequirements
                        {
                            MinimumMemoryBytes = 8L * 1024 * 1024 * 1024,
                            MinimumCpuCores = 2,
                            MinimumStorageBytes = 10L * 1024 * 1024 * 1024
                        },
                        Pricing = new PricingModel
                        {
                            HourlyRate = 0.067,
                            MonthlyRate = 48.24,
                            Currency = "USD"
                        },
                        Features = new List<string> { "Better AI models", "Sustained use discounts" },
                        IsAvailable = true,
                        SetupInstructions = "Create Compute Engine instance with e2-standard-2 machine type"
                    }
                };

                return await Task.FromResult(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cloud fallback options");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<CloudFallbackOption?> GetRecommendedCloudOptionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var capabilities = await CheckSystemCapabilitiesAsync(cancellationToken);
                var options = await GetCloudFallbackOptionsAsync(cancellationToken);

                // If system can't run Nexo locally, recommend cloud option
                if (!capabilities.CanRunNexo)
                {
                    return options
                        .Where(opt => opt.IsAvailable)
                        .OrderBy(opt => opt.Pricing.MonthlyRate)
                        .FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended cloud option");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, double>> EstimateCloudCostsAsync(int hoursPerMonth, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = await GetCloudFallbackOptionsAsync(cancellationToken);
                var costs = new Dictionary<string, double>();

                foreach (var option in options.Where(opt => opt.IsAvailable))
                {
                    var monthlyCost = option.Pricing.HourlyRate * hoursPerMonth;
                    costs[option.Name] = monthlyCost;
                }

                return await Task.FromResult(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating cloud costs");
                throw;
            }
        }
    }
}
