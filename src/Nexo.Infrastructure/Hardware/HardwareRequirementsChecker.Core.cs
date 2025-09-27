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
    /// Core hardware requirements checking functionality
    /// </summary>
    public partial class HardwareRequirementsChecker
    {
        /// <inheritdoc />
        public async Task<SystemCapabilities> CheckSystemCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Checking system capabilities");

                var capabilities = new SystemCapabilities
                {
                    CheckedAt = DateTime.UtcNow
                };

                // Get system information
                await GetSystemInformationAsync(capabilities);
                
                // Assess capabilities
                await AssessCapabilitiesAsync(capabilities);
                
                // Generate recommendations
                await GenerateRecommendationsAsync(capabilities);

                _logger.LogInformation("System capabilities checked. Overall: {Level}, Can Run Nexo: {CanRun}",
                    capabilities.OverallCapability, capabilities.CanRunNexo);

                return capabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system capabilities");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<HardwareRequirements> GetHardwareRequirementsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting hardware requirements");

                var requirements = new HardwareRequirements
                {
                    // Memory requirements (in bytes)
                    MinimumMemoryBytes = 8L * 1024 * 1024 * 1024, // 8GB
                    RecommendedMemoryBytes = 16L * 1024 * 1024 * 1024, // 16GB
                    MaximumMemoryBytes = 64L * 1024 * 1024 * 1024, // 64GB
                    
                    // CPU requirements
                    MinimumCpuCores = 4,
                    RecommendedCpuCores = 8,
                    MinimumCpuFrequencyGhz = 2.0,
                    RecommendedCpuFrequencyGhz = 3.0,
                    
                    // Storage requirements
                    MinimumStorageBytes = 10L * 1024 * 1024 * 1024, // 10GB
                    RecommendedStorageBytes = 50L * 1024 * 1024 * 1024, // 50GB
                    MaximumStorageBytes = 500L * 1024 * 1024 * 1024, // 500GB
                    
                    // GPU requirements (optional)
                    GpuRequired = false,
                    GpuMemoryBytes = 4L * 1024 * 1024 * 1024, // 4GB
                    
                    // Network requirements
                    NetworkRequired = false,
                    MinimumBandwidthBps = 1L * 1024 * 1024, // 1 Mbps
                    RecommendedBandwidthBps = 10L * 1024 * 1024, // 10 Mbps
                    
                    // Operating system requirements
                    SupportedOperatingSystems = new List<string>
                    {
                        "Windows 10", "Windows 11",
                        "macOS 10.15", "macOS 11", "macOS 12", "macOS 13", "macOS 14",
                        "Ubuntu 18.04", "Ubuntu 20.04", "Ubuntu 22.04",
                        "CentOS 7", "CentOS 8", "RHEL 7", "RHEL 8"
                    },
                    
                    SupportedArchitectures = new List<string>
                    {
                        "x64", "arm64"
                    }
                };

                // Add performance tiers
                requirements.PerformanceTiers = CreatePerformanceTiers();
                
                // Add cloud fallback options
                requirements.CloudFallbackOptions = await GetCloudFallbackOptionsAsync(cancellationToken);

                return await Task.FromResult(requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hardware requirements");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanRunPerformanceTierAsync(PerformanceTier tier, CancellationToken cancellationToken = default)
        {
            try
            {
                var capabilities = await CheckSystemCapabilitiesAsync(cancellationToken);
                var requirements = tier.Requirements;

                // Check memory
                if (capabilities.AvailableMemoryBytes < requirements.MinimumMemoryBytes)
                    return false;

                // Check CPU
                if (capabilities.CpuCores < requirements.MinimumCpuCores)
                    return false;

                if (capabilities.CpuFrequencyGhz < requirements.MinimumCpuFrequencyGhz)
                    return false;

                // Check storage
                if (capabilities.AvailableStorageBytes < requirements.MinimumStorageBytes)
                    return false;

                // Check GPU if required
                if (requirements.GpuRequired && !capabilities.HasGpu)
                    return false;

                if (requirements.GpuRequired && capabilities.GpuMemoryBytes < requirements.GpuMemoryBytes)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if performance tier can run");
                return false;
            }
        }
    }
}
