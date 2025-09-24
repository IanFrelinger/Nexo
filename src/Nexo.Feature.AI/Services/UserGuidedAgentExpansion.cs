using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Manages user-guided expansion of agent capabilities
    /// </summary>
    public class UserGuidedAgentExpansion : IUserGuidedAgentExpansion
    {
        private readonly ILogger<UserGuidedAgentExpansion> _logger;
        private readonly IAgentCapabilityRegistry _capabilityRegistry;
        private readonly IAgentLearningSystem _learningSystem;
        private readonly IUserFeedbackProcessor _feedbackProcessor;

        public UserGuidedAgentExpansion(
            ILogger<UserGuidedAgentExpansion> logger,
            IAgentCapabilityRegistry capabilityRegistry,
            IAgentLearningSystem learningSystem,
            IUserFeedbackProcessor feedbackProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _capabilityRegistry = capabilityRegistry ?? throw new ArgumentNullException(nameof(capabilityRegistry));
            _learningSystem = learningSystem ?? throw new ArgumentNullException(nameof(learningSystem));
            _feedbackProcessor = feedbackProcessor ?? throw new ArgumentNullException(nameof(feedbackProcessor));
        }

        public async Task<ExpansionResult> ExpandAgentCapabilitiesAsync(AgentExpansionRequest request)
        {
            _logger.LogInformation("Expanding agent capabilities for: {AgentId}", request.AgentId);

            try
            {
                // Analyze current agent capabilities
                var currentCapabilities = await _capabilityRegistry.GetAgentCapabilitiesAsync(request.AgentId);
                
                // Identify capability gaps
                var capabilityGaps = await IdentifyCapabilityGapsAsync(currentCapabilities, request.DesiredCapabilities);
                
                // Generate expansion plan
                var expansionPlan = await GenerateExpansionPlanAsync(capabilityGaps, request.Constraints);
                
                // Execute expansion plan
                var expansionResult = await ExecuteExpansionPlanAsync(request.AgentId, expansionPlan);
                
                // Learn from expansion process
                await LearnFromExpansionAsync(request, expansionResult);
                
                return expansionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expanding agent capabilities for: {AgentId}", request.AgentId);
                throw;
            }
        }

        public async Task<CapabilityAssessment> AssessExpansionFeasibilityAsync(AgentExpansionRequest request)
        {
            _logger.LogDebug("Assessing expansion feasibility for: {AgentId}", request.AgentId);

            var currentCapabilities = await _capabilityRegistry.GetAgentCapabilitiesAsync(request.AgentId);
            var capabilityGaps = await IdentifyCapabilityGapsAsync(currentCapabilities, request.DesiredCapabilities);
            
            var feasibility = new CapabilityAssessment
            {
                IsFeasible = true,
                EstimatedComplexity = CalculateExpansionComplexity(capabilityGaps),
                RequiredResources = EstimateRequiredResources(capabilityGaps),
                PotentialConflicts = await IdentifyPotentialConflictsAsync(currentCapabilities, request.DesiredCapabilities),
                RecommendedApproach = await RecommendExpansionApproachAsync(capabilityGaps, request.Constraints)
            };

            return feasibility;
        }

        public async Task<ExpansionProgress> TrackExpansionProgressAsync(string agentId, string expansionId)
        {
            _logger.LogDebug("Tracking expansion progress for agent: {AgentId}, expansion: {ExpansionId}", agentId, expansionId);

            var progress = await _learningSystem.GetExpansionProgressAsync(agentId, expansionId);
            
            return new ExpansionProgress
            {
                AgentId = agentId,
                ExpansionId = expansionId,
                CurrentPhase = progress.CurrentPhase,
                CompletionPercentage = progress.CompletionPercentage,
                EstimatedTimeRemaining = progress.EstimatedTimeRemaining,
                Issues = progress.Issues,
                NextSteps = progress.NextSteps
            };
        }

        public async Task<ExpansionResult> RollbackExpansionAsync(string agentId, string expansionId)
        {
            _logger.LogInformation("Rolling back expansion for agent: {AgentId}, expansion: {ExpansionId}", agentId, expansionId);

            try
            {
                // Get expansion details
                var expansion = await _learningSystem.GetExpansionDetailsAsync(agentId, expansionId);
                
                // Rollback changes
                var rollbackResult = await _learningSystem.RollbackExpansionAsync(agentId, expansionId);
                
                // Restore previous capabilities
                await _capabilityRegistry.RestoreAgentCapabilitiesAsync(agentId, expansion.PreviousCapabilities);
                
                return new ExpansionResult
                {
                    Success = true,
                    AgentId = agentId,
                    ExpansionId = expansionId,
                    NewCapabilities = expansion.PreviousCapabilities,
                    RollbackSuccessful = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back expansion for agent: {AgentId}, expansion: {ExpansionId}", agentId, expansionId);
                throw;
            }
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

        private async Task<ExpansionResult> ExecuteExpansionPlanAsync(string agentId, ExpansionPlan plan)
        {
            _logger.LogInformation("Executing expansion plan for agent: {AgentId}", agentId);

            var result = new ExpansionResult
            {
                AgentId = agentId,
                ExpansionId = plan.PlanId,
                Success = true,
                NewCapabilities = new List<AgentCapability>(),
                ExecutionLog = new List<ExpansionStep>()
            };

            foreach (var phase in plan.Phases)
            {
                _logger.LogDebug("Executing expansion phase: {PhaseName}", phase.Name);

                var phaseResult = await ExecuteExpansionPhaseAsync(agentId, phase);
                
                if (phaseResult.Success)
                {
                    result.NewCapabilities.AddRange(phaseResult.NewCapabilities);
                    result.ExecutionLog.AddRange(phaseResult.ExecutionLog);
                }
                else
                {
                    result.Success = false;
                    result.ExecutionLog.Add(new ExpansionStep
                    {
                        StepId = Guid.NewGuid().ToString(),
                        PhaseId = phase.PhaseId,
                        Status = ExpansionStepStatus.Failed,
                        ErrorMessage = phaseResult.ErrorMessage,
                        Timestamp = DateTime.UtcNow
                    });
                    break;
                }
            }

            return result;
        }

        private async Task<PhaseResult> ExecuteExpansionPhaseAsync(string agentId, ExpansionPhase phase)
        {
            var phaseResult = new PhaseResult
            {
                PhaseId = phase.PhaseId,
                Success = true,
                NewCapabilities = new List<AgentCapability>(),
                ExecutionLog = new List<ExpansionStep>()
            };

            foreach (var gap in phase.Gaps)
            {
                try
                {
                    _logger.LogDebug("Addressing capability gap: {GapType} for {CapabilityType}", gap.GapType, gap.Type);

                    var step = new ExpansionStep
                    {
                        StepId = Guid.NewGuid().ToString(),
                        PhaseId = phase.PhaseId,
                        GapId = gap.Type.ToString(),
                        Status = ExpansionStepStatus.InProgress,
                        Timestamp = DateTime.UtcNow
                    };

                    // Address the capability gap
                    var capability = await AddressCapabilityGapAsync(agentId, gap);
                    
                    if (capability != null)
                    {
                        phaseResult.NewCapabilities.Add(capability);
                        step.Status = ExpansionStepStatus.Completed;
                    }
                    else
                    {
                        step.Status = ExpansionStepStatus.Failed;
                        step.ErrorMessage = $"Failed to address capability gap: {gap.Type}";
                    }

                    step.CompletedAt = DateTime.UtcNow;
                    phaseResult.ExecutionLog.Add(step);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error addressing capability gap: {GapType} for {CapabilityType}", gap.GapType, gap.Type);
                    
                    phaseResult.Success = false;
                    phaseResult.ErrorMessage = ex.Message;
                    
                    phaseResult.ExecutionLog.Add(new ExpansionStep
                    {
                        StepId = Guid.NewGuid().ToString(),
                        PhaseId = phase.PhaseId,
                        GapId = gap.Type.ToString(),
                        Status = ExpansionStepStatus.Failed,
                        ErrorMessage = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            return phaseResult;
        }

        private async Task<AgentCapability> AddressCapabilityGapAsync(string agentId, CapabilityGap gap)
        {
            // This would integrate with the actual agent learning system
            // For now, we'll create a placeholder capability
            return new AgentCapability
            {
                Type = gap.Type,
                Level = CapabilityLevel.Intermediate,
                AcquiredAt = DateTime.UtcNow,
                Confidence = 0.8,
                Description = $"Acquired {gap.Type} capability through expansion"
            };
        }

        private async Task LearnFromExpansionAsync(AgentExpansionRequest request, ExpansionResult result)
        {
            // Learn from the expansion process to improve future expansions
            await _learningSystem.RecordExpansionExperienceAsync(request, result);
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
            return EstimatePhaseResources(gaps);
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

    /// <summary>
    /// Interface for user-guided agent expansion
    /// </summary>
    public interface IUserGuidedAgentExpansion
    {
        Task<ExpansionResult> ExpandAgentCapabilitiesAsync(AgentExpansionRequest request);
        Task<CapabilityAssessment> AssessExpansionFeasibilityAsync(AgentExpansionRequest request);
        Task<ExpansionProgress> TrackExpansionProgressAsync(string agentId, string expansionId);
        Task<ExpansionResult> RollbackExpansionAsync(string agentId, string expansionId);
    }
}
