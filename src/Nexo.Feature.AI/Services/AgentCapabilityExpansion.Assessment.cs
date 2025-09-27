using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Capability assessment and analysis functionality
    /// </summary>
    public partial class AgentCapabilityExpansion
    {
        public async Task<CapabilityAssessment> AssessExpansionFeasibilityAsync(AgentExpansionRequest request)
        {
            _logger.LogDebug("Assessing expansion feasibility for: {AgentId}", request.AgentId);

            var currentCapabilities = await _capabilityRegistry.GetAgentCapabilitiesAsync(request.AgentId);
            var capabilityGaps = await IdentifyCapabilityGapsAsync(currentCapabilities, request.DesiredCapabilities);
            
            var assessment = new CapabilityAssessment
            {
                IsFeasible = true,
                EstimatedComplexity = CalculateExpansionComplexity(capabilityGaps),
                RequiredResources = EstimateRequiredResources(capabilityGaps),
                PotentialConflicts = await IdentifyPotentialConflictsAsync(currentCapabilities, request.DesiredCapabilities),
                RecommendedApproach = await RecommendExpansionApproachAsync(capabilityGaps, request.Constraints)
            };

            return assessment;
        }

        private async Task<List<CapabilityGap>> IdentifyCapabilityGapsAsync(AgentCapabilities currentCapabilities, List<DesiredCapability> desiredCapabilities)
        {
            var gaps = new List<CapabilityGap>();

            foreach (var desired in desiredCapabilities)
            {
                var current = currentCapabilities.Capabilities.FirstOrDefault(c => c.Type == desired.Type);
                
                if (current == null)
                {
                    // Completely missing capability
                    gaps.Add(new CapabilityGap
                    {
                        Type = desired.Type,
                        GapType = CapabilityGapType.Missing,
                        Severity = CapabilityGapSeverity.High,
                        Description = $"Missing {desired.Type} capability"
                    });
                }
                else if (current.Level < desired.Level)
                {
                    // Insufficient capability level
                    gaps.Add(new CapabilityGap
                    {
                        Type = desired.Type,
                        GapType = CapabilityGapType.Insufficient,
                        Severity = CalculateGapSeverity(current.Level, desired.Level),
                        Description = $"{desired.Type} capability level {current.Level} is insufficient for required level {desired.Level}"
                    });
                }
            }

            return gaps;
        }

        private CapabilityGapSeverity CalculateGapSeverity(CapabilityLevel current, CapabilityLevel desired)
        {
            var levelDifference = (int)desired - (int)current;
            
            return levelDifference switch
            {
                1 => CapabilityGapSeverity.Low,
                2 => CapabilityGapSeverity.Medium,
                3 => CapabilityGapSeverity.High,
                _ => CapabilityGapSeverity.Critical
            };
        }

        private ExpansionComplexity CalculateExpansionComplexity(List<CapabilityGap> gaps)
        {
            var highSeverityGaps = gaps.Count(g => g.Severity >= CapabilityGapSeverity.High);
            var mediumSeverityGaps = gaps.Count(g => g.Severity == CapabilityGapSeverity.Medium);
            var lowSeverityGaps = gaps.Count(g => g.Severity == CapabilityGapSeverity.Low);

            if (highSeverityGaps > 3) return ExpansionComplexity.VeryHigh;
            if (highSeverityGaps > 1 || mediumSeverityGaps > 3) return ExpansionComplexity.High;
            if (mediumSeverityGaps > 1 || lowSeverityGaps > 3) return ExpansionComplexity.Medium;
            return ExpansionComplexity.Low;
        }

        private ResourceRequirements EstimateRequiredResources(List<CapabilityGap> gaps)
        {
            var totalMemory = gaps.Sum(g => EstimateGapMemoryRequirement(g));
            var totalProcessing = gaps.Sum(g => EstimateGapProcessingRequirement(g));
            var totalStorage = gaps.Sum(g => EstimateGapStorageRequirement(g));

            return new ResourceRequirements
            {
                MemoryUsage = totalMemory,
                ProcessingPower = (ProcessingPower)Math.Min((int)ProcessingPower.High, (int)totalProcessing),
                StorageSpace = totalStorage
            };
        }

        private int EstimateGapMemoryRequirement(CapabilityGap gap)
        {
            return gap.Type switch
            {
                CapabilityType.MaterialGeneration => 1024 * 1024, // 1MB
                CapabilityType.TextureGeneration => 2048 * 1024, // 2MB
                CapabilityType.ShaderGeneration => 512 * 1024,   // 512KB
                CapabilityType.ModelGeneration => 4096 * 1024,  // 4MB
                _ => 256 * 1024 // 256KB default
            };
        }

        private ProcessingPower EstimateGapProcessingRequirement(CapabilityGap gap)
        {
            return gap.Type switch
            {
                CapabilityType.MaterialGeneration => ProcessingPower.Medium,
                CapabilityType.TextureGeneration => ProcessingPower.High,
                CapabilityType.ShaderGeneration => ProcessingPower.Medium,
                CapabilityType.ModelGeneration => ProcessingPower.High,
                _ => ProcessingPower.Low
            };
        }

        private int EstimateGapStorageRequirement(CapabilityGap gap)
        {
            return gap.Type switch
            {
                CapabilityType.MaterialGeneration => 10 * 1024 * 1024, // 10MB
                CapabilityType.TextureGeneration => 50 * 1024 * 1024,  // 50MB
                CapabilityType.ShaderGeneration => 5 * 1024 * 1024,    // 5MB
                CapabilityType.ModelGeneration => 100 * 1024 * 1024,   // 100MB
                _ => 1 * 1024 * 1024 // 1MB default
            };
        }

        private async Task<List<PotentialConflict>> IdentifyPotentialConflictsAsync(AgentCapabilities currentCapabilities, List<DesiredCapability> desiredCapabilities)
        {
            var conflicts = new List<PotentialConflict>();

            // Check for conflicts between desired capabilities
            foreach (var desired in desiredCapabilities)
            {
                var conflicting = desiredCapabilities.FirstOrDefault(d => 
                    d != desired && 
                    IsConflictingCapability(desired.Type, d.Type));

                if (conflicting != null)
                {
                    conflicts.Add(new PotentialConflict
                    {
                        Type = ConflictType.CapabilityConflict,
                        Severity = ConflictSeverity.Medium,
                        Description = $"Capability {desired.Type} conflicts with {conflicting.Type}",
                        AffectedCapabilities = new[] { desired.Type, conflicting.Type }
                    });
                }
            }

            return conflicts;
        }

        private bool IsConflictingCapability(CapabilityType type1, CapabilityType type2)
        {
            // Define capability conflicts
            var conflicts = new Dictionary<CapabilityType, List<CapabilityType>>
            {
                { CapabilityType.MaterialGeneration, new List<CapabilityType> { CapabilityType.ShaderGeneration } },
                { CapabilityType.TextureGeneration, new List<CapabilityType> { CapabilityType.ModelGeneration } }
            };

            return conflicts.ContainsKey(type1) && conflicts[type1].Contains(type2);
        }

        private async Task<ExpansionApproach> RecommendExpansionApproachAsync(List<CapabilityGap> gaps, ExpansionConstraints constraints)
        {
            var complexity = CalculateExpansionComplexity(gaps);
            
            return complexity switch
            {
                ExpansionComplexity.Low => ExpansionApproach.Incremental,
                ExpansionComplexity.Medium => ExpansionApproach.Phased,
                ExpansionComplexity.High => ExpansionApproach.Careful,
                ExpansionComplexity.VeryHigh => ExpansionApproach.Experimental,
                _ => ExpansionApproach.Incremental
            };
        }
    }
}
