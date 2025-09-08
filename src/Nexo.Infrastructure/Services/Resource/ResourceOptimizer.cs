using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Infrastructure.Services.Resource
{
    /// <summary>
    /// Resource optimizer for adaptive performance tuning and resource management.
    /// </summary>
    public class ResourceOptimizer : IResourceOptimizer
    {
        private readonly ILogger<ResourceOptimizer> _logger;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IResourceManager _resourceManager;
        private readonly Dictionary<string, OptimizationRule> _optimizationRules;
        private readonly List<OptimizationHistory> _optimizationHistory;

        public ResourceOptimizer(
            ILogger<ResourceOptimizer> logger,
            IResourceMonitor resourceMonitor,
            IResourceManager resourceManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            
            _optimizationRules = new Dictionary<string, OptimizationRule>();
            _optimizationHistory = [];

            InitializeDefaultRules();
            _logger.LogInformation("Resource optimizer initialized");
        }

        /// <summary>
        /// Performs adaptive performance tuning based on current resource usage.
        /// </summary>
        public async Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogDebug("Starting resource optimization");

                var systemResourceUsage = await _resourceMonitor.GetResourceUsageAsync(cancellationToken);
                var optimizationResult = new OptimizationResult
                {
                    Timestamp = DateTime.UtcNow,
                    Recommendations = []
                };

                // Apply optimization rules
                foreach (var rule in _optimizationRules.Values)
                {
                    if (!await ShouldApplyRuleAsync(rule, systemResourceUsage)) continue;
                    var recommendation = await ApplyOptimizationRuleAsync(rule, systemResourceUsage);
                    optimizationResult.Recommendations.Add(recommendation);
                }

                // Convert SystemResourceUsage to ResourceUsage for history
                var resourceUsage = ConvertToResourceUsage(systemResourceUsage);
                
                // Record optimization history
                var history = new OptimizationHistory
                {
                    Timestamp = DateTime.UtcNow,
                    ResourceUsage = resourceUsage,
                    Recommendations = optimizationResult.Recommendations
                };
                _optimizationHistory.Add(history);

                // Keep only recent history (last 100 entries)
                if (_optimizationHistory.Count > 100)
                {
                    _optimizationHistory.RemoveRange(0, _optimizationHistory.Count - 100);
                }

                _logger.LogInformation("Resource optimization completed with {Count} recommendations", 
                    optimizationResult.Recommendations.Count);

                return optimizationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resource optimization");
                return new OptimizationResult
                {
                    Timestamp = DateTime.UtcNow,
                    Recommendations = new List<OptimizationRecommendation>()
                };
            }
        }

        /// <summary>
        /// Adds a custom optimization rule.
        /// </summary>
        public void AddOptimizationRule(string ruleId, OptimizationRule rule)
        {
            if (string.IsNullOrEmpty(ruleId))
                throw new ArgumentException("Rule ID cannot be null or empty", nameof(ruleId));

            _optimizationRules[ruleId] = rule ?? throw new ArgumentNullException(nameof(rule));
            _logger.LogDebug("Added optimization rule: {RuleId}", ruleId);
        }

        /// <summary>
        /// Removes an optimization rule.
        /// </summary>
        public void RemoveOptimizationRule(string ruleId)
        {
            if (_optimizationRules.Remove(ruleId))
            {
                _logger.LogDebug("Removed optimization rule: {RuleId}", ruleId);
            }
        }

        /// <summary>
        /// Gets optimization history for analysis.
        /// </summary>
        public IEnumerable<OptimizationHistory> GetOptimizationHistory()
        {
            return _optimizationHistory.AsReadOnly();
        }

        /// <summary>
        /// Performs resource-aware pipeline scheduling and throttling.
        /// </summary>
        public async Task<ThrottlingResult> CalculateThrottlingAsync(
            PipelineExecutionRequest request, 
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var resourceUsage = await _resourceMonitor.GetResourceUsageAsync(cancellationToken);
                var throttlingResult = new ThrottlingResult
                {
                    ShouldThrottle = false,
                    ThrottlingLevel = ThrottlingLevel.None,
                    RecommendedDelay = TimeSpan.Zero
                };

                switch (resourceUsage.CpuUsagePercentage)
                {
                    // Check CPU usage
                    case > 90:
                        throttlingResult.ShouldThrottle = true;
                        throttlingResult.ThrottlingLevel = ThrottlingLevel.High;
                        throttlingResult.RecommendedDelay = TimeSpan.FromSeconds(5);
                        break;
                    case > 75:
                        throttlingResult.ShouldThrottle = true;
                        throttlingResult.ThrottlingLevel = ThrottlingLevel.Medium;
                        throttlingResult.RecommendedDelay = TimeSpan.FromSeconds(2);
                        break;
                    case > 60:
                        throttlingResult.ShouldThrottle = true;
                        throttlingResult.ThrottlingLevel = ThrottlingLevel.Low;
                        throttlingResult.RecommendedDelay = TimeSpan.FromSeconds(1);
                        break;
                }

                // Check memory usage
                if (!(resourceUsage.Memory.UsagePercentage > 85)) return throttlingResult;
                throttlingResult.ShouldThrottle = true;
                throttlingResult.ThrottlingLevel = ThrottlingLevel.High;
                throttlingResult.RecommendedDelay = TimeSpan.FromSeconds(10);

                return throttlingResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating throttling");
                return new ThrottlingResult
                {
                    ShouldThrottle = false,
                    ThrottlingLevel = ThrottlingLevel.None,
                    RecommendedDelay = TimeSpan.Zero
                };
            }
        }

        private void InitializeDefaultRules()
        {
            // CPU optimization rule
            AddOptimizationRule("cpu_high_usage", new OptimizationRule
            {
                Name = "High CPU Usage",
                Description = "Optimize when CPU usage is high",
                Condition = usage => Task.FromResult(usage.UtilizationByType.TryGetValue(ResourceType.CPU, out var cpuUsage) && cpuUsage > 80),
                Action = _ => Task.FromResult(new OptimizationRecommendation
                {
                    Type = "CPU_OPTIMIZATION",
                    Message = "Consider reducing concurrent operations or increasing CPU allocation",
                    Impact = "High",
                    Priority = 1
                })
            });

            // Memory optimization rule
            AddOptimizationRule("memory_high_usage", new OptimizationRule
            {
                Name = "High Memory Usage",
                Description = "Optimize when memory usage is high",
                Condition = usage => Task.FromResult(usage.UtilizationByType.TryGetValue(ResourceType.Memory, out var memoryUsage) && memoryUsage > 85),
                Action = _ => Task.FromResult(new OptimizationRecommendation
                {
                    Type = "MEMORY_OPTIMIZATION",
                    Message = "Consider garbage collection or reducing memory-intensive operations",
                    Impact = "High",
                    Priority = 1
                })
            });

            // Storage optimization rule
            AddOptimizationRule("storage_low_space", new OptimizationRule
            {
                Name = "Low Storage Space",
                Description = "Optimize when storage space is low",
                Condition = (usage) => Task.FromResult(usage.UtilizationByType.TryGetValue(ResourceType.Storage, out var storageUsage) && storageUsage > 90),
                Action = _ => Task.FromResult(new OptimizationRecommendation
                {
                    Type = "STORAGE_OPTIMIZATION",
                    Message = "Consider cleaning up temporary files or increasing storage space",
                    Impact = "Medium",
                    Priority = 2
                })
            });
        }

        private async Task<bool> ShouldApplyRuleAsync(
            OptimizationRule rule, 
            SystemResourceUsage usage)
        {
            try
            {
                // Convert SystemResourceUsage to ResourceUsage for the rule
                var resourceUsage = ConvertToResourceUsage(usage);
                return await rule.Condition(resourceUsage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating optimization rule condition: {RuleName}", rule.Name);
                return false;
            }
        }

        private async Task<OptimizationRecommendation?> ApplyOptimizationRuleAsync(
            OptimizationRule rule, 
            SystemResourceUsage usage)
        {
            try
            {
                // Convert SystemResourceUsage to ResourceUsage for the rule
                var resourceUsage = ConvertToResourceUsage(usage);
                return await rule.Action(resourceUsage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying optimization rule: {RuleName}", rule.Name);
                return null;
            }
        }

        private ResourceUsage ConvertToResourceUsage(SystemResourceUsage systemUsage)
        {
            return new ResourceUsage
            {
                AllocatedByType = new Dictionary<ResourceType, long>
                {
                    { ResourceType.CPU, (long)(systemUsage.CpuUsagePercentage * 100) },
                    { ResourceType.Memory, systemUsage.Memory.UsedBytes },
                    { ResourceType.Storage, systemUsage.Disk.UsedBytes }
                },
                AvailableByType = new Dictionary<ResourceType, long>
                {
                    { ResourceType.CPU, (long)((100 - systemUsage.CpuUsagePercentage) * 100) },
                    { ResourceType.Memory, systemUsage.Memory.AvailableBytes },
                    { ResourceType.Storage, systemUsage.Disk.AvailableBytes }
                },
                UtilizationByType = new Dictionary<ResourceType, double>
                {
                    { ResourceType.CPU, systemUsage.CpuUsagePercentage },
                    { ResourceType.Memory, systemUsage.Memory.UsagePercentage },
                    { ResourceType.Storage, systemUsage.Disk.UsagePercentage }
                },
                ActiveAllocations = new List<ResourceAllocation>(),
                Timestamp = systemUsage.Timestamp
            };
        }
    }
} 