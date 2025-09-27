using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Expansion plan generation and resource estimation functionality
    /// </summary>
    public partial class UserGuidedAgentExpansion
    {
        private async Task<ExpansionPlan> GenerateExpansionPlanAsync(List<CapabilityGap> gaps, ExpansionConstraints constraints)
        {
            var plan = new ExpansionPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Phases = new List<ExpansionPhase>()
            };

            // Group gaps by severity and type
            var highPriorityGaps = gaps.Where(g => g.Severity >= CapabilityGapSeverity.High).ToList();
            var mediumPriorityGaps = gaps.Where(g => g.Severity == CapabilityGapSeverity.Medium).ToList();
            var lowPriorityGaps = gaps.Where(g => g.Severity == CapabilityGapSeverity.Low).ToList();

            // Create phases based on priority
            if (highPriorityGaps.Any())
            {
                plan.Phases.Add(new ExpansionPhase
                {
                    PhaseId = Guid.NewGuid().ToString(),
                    Name = "High Priority Capabilities",
                    Priority = ExpansionPriority.High,
                    Gaps = highPriorityGaps,
                    EstimatedDuration = TimeSpan.FromHours(2),
                    RequiredResources = EstimatePhaseResources(highPriorityGaps)
                });
            }

            if (mediumPriorityGaps.Any())
            {
                plan.Phases.Add(new ExpansionPhase
                {
                    PhaseId = Guid.NewGuid().ToString(),
                    Name = "Medium Priority Capabilities",
                    Priority = ExpansionPriority.Medium,
                    Gaps = mediumPriorityGaps,
                    EstimatedDuration = TimeSpan.FromHours(1),
                    RequiredResources = EstimatePhaseResources(mediumPriorityGaps)
                });
            }

            if (lowPriorityGaps.Any())
            {
                plan.Phases.Add(new ExpansionPhase
                {
                    PhaseId = Guid.NewGuid().ToString(),
                    Name = "Low Priority Capabilities",
                    Priority = ExpansionPriority.Low,
                    Gaps = lowPriorityGaps,
                    EstimatedDuration = TimeSpan.FromMinutes(30),
                    RequiredResources = EstimatePhaseResources(lowPriorityGaps)
                });
            }

            return plan;
        }

        private ResourceRequirements EstimatePhaseResources(List<CapabilityGap> gaps)
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

        private ResourceRequirements EstimateRequiredResources(List<CapabilityGap> gaps)
        {
            return EstimatePhaseResources(gaps);
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
