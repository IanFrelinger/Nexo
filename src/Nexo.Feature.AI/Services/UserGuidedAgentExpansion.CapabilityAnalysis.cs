using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Capability gap identification and assessment functionality
    /// </summary>
    public partial class UserGuidedAgentExpansion
    {
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
    }
}
