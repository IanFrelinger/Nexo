using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.Infrastructure.Hardware
{
    /// <summary>
    /// Capability assessment and analysis functionality
    /// </summary>
    public partial class HardwareRequirementsChecker
    {
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
    }
}
