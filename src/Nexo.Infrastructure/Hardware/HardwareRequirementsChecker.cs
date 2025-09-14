using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.Infrastructure.Hardware
{
    /// <summary>
    /// Service for checking hardware requirements and providing cloud fallback options
    /// </summary>
    public class HardwareRequirementsChecker : IHardwareRequirementsChecker
    {
        private readonly ILogger<HardwareRequirementsChecker> _logger;

        public HardwareRequirementsChecker(ILogger<HardwareRequirementsChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

        #region Private Methods

        private async Task GetSystemInformationAsync(SystemCapabilities capabilities)
        {
            try
            {
                // Get memory information
                capabilities.AvailableMemoryBytes = GC.GetTotalMemory(false);
                
                // Get CPU information
                capabilities.CpuCores = Environment.ProcessorCount;
                
                // Get CPU frequency (simplified)
                capabilities.CpuFrequencyGhz = GetCpuFrequency();
                
                // Get storage information
                capabilities.AvailableStorageBytes = GetAvailableStorage();
                
                // Get operating system information
                capabilities.OperatingSystem = Environment.OSVersion.ToString();
                capabilities.Architecture = RuntimeInformation.OSArchitecture.ToString();
                
                // Get GPU information
                capabilities.HasGpu = HasGpu();
                if (capabilities.HasGpu)
                {
                    capabilities.GpuModel = GetGpuModel();
                    capabilities.GpuMemoryBytes = GetGpuMemory();
                }
                
                // Get network information
                capabilities.HasNetworkConnection = HasNetworkConnection();
                capabilities.BandwidthBps = GetBandwidth();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting system information, using defaults");
            }
        }

        private async Task AssessCapabilitiesAsync(SystemCapabilities capabilities)
        {
            try
            {
                var requirements = await GetHardwareRequirementsAsync();
                
                // Assess overall capability
                var memoryScore = AssessMemoryCapability(capabilities.AvailableMemoryBytes, requirements);
                var cpuScore = AssessCpuCapability(capabilities.CpuCores, capabilities.CpuFrequencyGhz, requirements);
                var storageScore = AssessStorageCapability(capabilities.AvailableStorageBytes, requirements);
                
                capabilities.OverallCapability = (CapabilityLevel)Math.Min(Math.Min(memoryScore, cpuScore), storageScore);
                
                // Determine if can run Nexo
                capabilities.CanRunNexo = capabilities.OverallCapability >= CapabilityLevel.Basic;
                capabilities.CanRunWithCloudFallback = true; // Always possible with cloud
                
                // Generate issues
                GenerateCapabilityIssues(capabilities, requirements);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error assessing capabilities");
            }
        }

        private async Task GenerateRecommendationsAsync(SystemCapabilities capabilities)
        {
            try
            {
                capabilities.Recommendations = await GetOptimizationRecommendationsAsync();
                
                // Set recommended tier and cloud option
                capabilities.RecommendedTier = await GetRecommendedPerformanceTierAsync();
                capabilities.RecommendedCloudOption = await GetRecommendedCloudOptionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating recommendations");
            }
        }

        private int AssessMemoryCapability(long availableMemory, HardwareRequirements requirements)
        {
            if (availableMemory >= requirements.RecommendedMemoryBytes)
                return (int)CapabilityLevel.Excellent;
            if (availableMemory >= requirements.MinimumMemoryBytes)
                return (int)CapabilityLevel.Good;
            if (availableMemory >= requirements.MinimumMemoryBytes / 2)
                return (int)CapabilityLevel.Basic;
            return (int)CapabilityLevel.Insufficient;
        }

        private int AssessCpuCapability(int cores, double frequency, HardwareRequirements requirements)
        {
            var coreScore = cores >= requirements.RecommendedCpuCores ? 5 : 
                           cores >= requirements.MinimumCpuCores ? 3 : 1;
            var freqScore = frequency >= requirements.RecommendedCpuFrequencyGhz ? 5 :
                           frequency >= requirements.MinimumCpuFrequencyGhz ? 3 : 1;
            
            return Math.Min(coreScore, freqScore);
        }

        private int AssessStorageCapability(long availableStorage, HardwareRequirements requirements)
        {
            if (availableStorage >= requirements.RecommendedStorageBytes)
                return (int)CapabilityLevel.Excellent;
            if (availableStorage >= requirements.MinimumStorageBytes)
                return (int)CapabilityLevel.Good;
            if (availableStorage >= requirements.MinimumStorageBytes / 2)
                return (int)CapabilityLevel.Basic;
            return (int)CapabilityLevel.Insufficient;
        }

        private void GenerateCapabilityIssues(SystemCapabilities capabilities, HardwareRequirements requirements)
        {
            // Memory issues
            if (capabilities.AvailableMemoryBytes < requirements.MinimumMemoryBytes)
            {
                capabilities.Issues.Add(new CapabilityIssue
                {
                    Type = IssueType.Memory,
                    Severity = IssueSeverity.High,
                    Title = "Insufficient Memory",
                    Description = "System does not meet minimum memory requirements",
                    CurrentValue = FormatBytes(capabilities.AvailableMemoryBytes),
                    RequiredValue = FormatBytes(requirements.MinimumMemoryBytes),
                    FixSuggestion = "Add more RAM or use cloud fallback"
                });
            }

            // CPU issues
            if (capabilities.CpuCores < requirements.MinimumCpuCores)
            {
                capabilities.Issues.Add(new CapabilityIssue
                {
                    Type = IssueType.Cpu,
                    Severity = IssueSeverity.High,
                    Title = "Insufficient CPU Cores",
                    Description = "System does not meet minimum CPU core requirements",
                    CurrentValue = capabilities.CpuCores.ToString(),
                    RequiredValue = requirements.MinimumCpuCores.ToString(),
                    FixSuggestion = "Upgrade CPU or use cloud fallback"
                });
            }

            // Storage issues
            if (capabilities.AvailableStorageBytes < requirements.MinimumStorageBytes)
            {
                capabilities.Issues.Add(new CapabilityIssue
                {
                    Type = IssueType.Storage,
                    Severity = IssueSeverity.Medium,
                    Title = "Insufficient Storage",
                    Description = "System does not meet minimum storage requirements",
                    CurrentValue = FormatBytes(capabilities.AvailableStorageBytes),
                    RequiredValue = FormatBytes(requirements.MinimumStorageBytes),
                    FixSuggestion = "Add more storage or use cloud fallback"
                });
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

        private double GetCpuFrequency()
        {
            try
            {
                // Simplified CPU frequency detection
                return 2.5; // Default assumption
            }
            catch
            {
                return 2.0;
            }
        }

        private long GetAvailableStorage()
        {
            try
            {
                var drive = new DriveInfo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return 100L * 1024 * 1024 * 1024; // Assume 100GB available
            }
        }

        private bool HasGpu()
        {
            try
            {
                // Simplified GPU detection
                return false; // Assume no GPU for now
            }
            catch
            {
                return false;
            }
        }

        private string? GetGpuModel()
        {
            return null; // Not implemented
        }

        private long? GetGpuMemory()
        {
            return null; // Not implemented
        }

        private bool HasNetworkConnection()
        {
            try
            {
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return true; // Assume network is available
            }
        }

        private long GetBandwidth()
        {
            return 10L * 1024 * 1024; // Assume 10 Mbps
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        #endregion
    }
}
