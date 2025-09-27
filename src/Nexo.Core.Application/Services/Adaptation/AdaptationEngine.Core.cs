using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Core adaptation processing methods for AdaptationEngine.
/// </summary>
public partial class AdaptationEngine
{
    private async void ProcessAdaptations(object? state)
    {
        if (_isAdapting) return;
        
        await _adaptationSemaphore.WaitAsync();
        try
        {
            _isAdapting = true;
            
            // Collect current system state
            var systemState = await CollectSystemState();
            
            // Analyze adaptation needs
            var adaptationNeeds = await AnalyzeAdaptationNeeds(systemState);
            
            if (adaptationNeeds.Any())
            {
                _logger.LogInformation("Identified {Count} adaptation needs", adaptationNeeds.Count());
                
                // Execute adaptations
                await ExecuteAdaptations(adaptationNeeds);
                
                // Learn from adaptation results
                foreach (var need in adaptationNeeds)
                {
                    // TODO: Create proper AdaptationRecord and PerformanceMetrics
                    // await _learningSystem.LearnFromAdaptationAsync(record, metrics);
                }
            }
            
            // Process any pending manual triggers
            await ProcessPendingAdaptations();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during adaptation processing");
        }
        finally
        {
            _isAdapting = false;
            _adaptationSemaphore.Release();
        }
    }
    
    private async Task ProcessAdaptationImmediately(AdaptationContext context)
    {
        await _adaptationSemaphore.WaitAsync();
        try
        {
            var systemState = context.Context ?? await CollectSystemState();
            var adaptationNeed = new AdaptationNeed
            {
                Type = DetermineAdaptationType(context.Trigger),
                Trigger = context.Trigger,
                Priority = context.Priority,
                Context = systemState,
                Description = context.Description ?? $"Manual trigger: {context.Trigger}"
            };
            
            await ExecuteAdaptations(new[] { adaptationNeed });
        }
        finally
        {
            _adaptationSemaphore.Release();
        }
    }
    
    private async Task<SystemState> CollectSystemState()
    {
        var domainMetrics = await _performanceMonitor.GetCurrentMetricsAsync();
        var domainEnvironment = await _environmentDetector.GetCurrentEnvironmentAsync();
        var domainFeedback = await _feedbackCollector.GetRecentFeedbackAsync(TimeSpan.FromMinutes(5));
        
        return new SystemState
        {
            PerformanceMetrics = new PerformanceMetrics
            {
                CpuUsage = domainMetrics.CpuUsage,
                MemoryUsage = domainMetrics.MemoryUsage,
                NetworkLatency = domainMetrics.NetworkLatency,
                ResponseTime = domainMetrics.ResponseTime,
                Throughput = domainMetrics.Throughput,
                ErrorRate = domainMetrics.ErrorRate,
                Severity = domainMetrics.Severity,
                RequiresOptimization = domainMetrics.RequiresOptimization
            },
            EnvironmentProfile = new EnvironmentProfile
            {
                EnvironmentId = domainEnvironment.EnvironmentId,
                EnvironmentName = domainEnvironment.EnvironmentName,
                PlatformType = domainEnvironment.PlatformType,
                CpuCores = domainEnvironment.CpuCores,
                AvailableMemoryMB = domainEnvironment.AvailableMemoryMB,
                FrameworkVersion = domainEnvironment.FrameworkVersion,
                Context = domainEnvironment.Context
            },
            RecentFeedback = domainFeedback.Select(f => new UserFeedback
            {
                Id = f.Id,
                Type = f.Type,
                Rating = f.Rating,
                Message = f.Message,
                Timestamp = f.Timestamp,
                Context = f.Context
            }),
            ResourceUtilization = await GetResourceUtilizationAsync(),
            ActiveWorkloads = await GetActiveWorkloadsAsync(),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private Task<IEnumerable<AdaptationNeed>> AnalyzeAdaptationNeeds(SystemState systemState)
    {
        var needs = new List<AdaptationNeed>();
        
        // Performance-based adaptations
        if (systemState.PerformanceMetrics.RequiresOptimization)
        {
            needs.Add(new AdaptationNeed
            {
                Type = Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.PerformanceOptimization,
                Trigger = AdaptationTrigger.PerformanceDegradation,
                Priority = MapSeverityToPriority(ConvertAlertSeverityToPerformanceSeverity(systemState.PerformanceMetrics.Severity)),
                Context = systemState,
                Description = $"Performance degradation detected: {systemState.PerformanceMetrics.Severity}"
            });
        }
        
        // Resource-based adaptations
        if (systemState.ResourceUtilization.IsConstrained)
        {
            needs.Add(new AdaptationNeed
            {
                Type = Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.ResourceOptimization,
                Trigger = AdaptationTrigger.ResourceConstraint,
                Priority = AdaptationPriority.High,
                Context = systemState,
                Description = $"Resource constraint detected: {systemState.ResourceUtilization.ConstraintType}"
            });
        }
        
        // Feedback-based adaptations
        if (systemState.RecentFeedback.Any(f => f.Severity >= FeedbackSeverity.High))
        {
            needs.Add(new AdaptationNeed
            {
                Type = Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.UserExperienceOptimization,
                Trigger = AdaptationTrigger.UserFeedback,
                Priority = AdaptationPriority.Medium,
                Context = systemState,
                Description = "High severity user feedback received"
            });
        }
        
        // Environment-based adaptations
        if (systemState.EnvironmentProfile.HasChanged)
        {
            needs.Add(new AdaptationNeed
            {
                Type = Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.EnvironmentOptimization,
                Trigger = AdaptationTrigger.EnvironmentChange,
                Priority = AdaptationPriority.Medium,
                Context = systemState,
                Description = "Environment change detected"
            });
        }
        
        return Task.FromResult(needs.OrderByDescending(n => n.Priority).AsEnumerable());
    }
    
    private async Task ExecuteAdaptations(IEnumerable<AdaptationNeed> adaptationNeeds)
    {
        foreach (var need in adaptationNeeds)
        {
            var strategies = _strategyRegistry.GetStrategiesForAdaptationType(need.Type);
            
            foreach (var strategyObj in strategies.OrderByDescending(s => ((Strategies.IAdaptationStrategy)s).GetPriority(new SystemState())))
            {
                var strategy = (Strategies.IAdaptationStrategy)strategyObj;
                try
                {
                    if (await strategy.CanHandleAsync(need))
                    {
                        var result = await strategy.ExecuteAdaptationAsync(need);
                        
                        if (result.IsSuccessful)
                        {
                            _logger.LogInformation("Successfully applied adaptation {AdaptationType} using strategy {StrategyId}",
                                need.Type, strategy.StrategyId);
                            
                            await RecordSuccessfulAdaptation(need, (IAdaptationStrategy)strategy, result);
                            break; // Success, move to next need
                        }
                        else
                        {
                            _logger.LogWarning("Adaptation strategy {StrategyId} failed for {AdaptationType}: {Error}",
                                strategy.StrategyId, need.Type, result.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Adaptation strategy {StrategyId} failed for {AdaptationType}",
                        strategy.StrategyId, need.Type);
                }
            }
        }
    }
    
    private async Task RecordSuccessfulAdaptation(AdaptationNeed need, IAdaptationStrategy strategy, AdaptationResult result)
    {
        var record = new AdaptationRecord
        {
            Id = Guid.NewGuid().ToString(),
            Type = need.Type,
            Trigger = need.Trigger,
            AppliedAt = DateTime.UtcNow,
            StrategyId = strategy.StrategyId,
            Success = result.IsSuccessful,
            EffectivenessScore = result.EstimatedImprovement
        };
        
        await _dataStore.StoreAdaptationAsync(record);
        
        // Record applied adaptations
        foreach (var adaptation in result.AppliedAdaptations)
        {
            var adaptationRecord = new AdaptationRecord
            {
                Id = Guid.NewGuid().ToString(),
                Type = need.Type,
                Success = true,
                AppliedAt = DateTime.UtcNow,
                StrategyId = strategy.StrategyId
            };
            await _dataStore.StoreAdaptationAsync(adaptationRecord);
        }
    }
    
    private async Task ProcessPendingAdaptations()
    {
        while (_pendingAdaptations.TryDequeue(out var trigger))
        {
            var context = new AdaptationContext
            {
                Trigger = trigger,
                Priority = AdaptationPriority.Medium,
                Description = $"Pending adaptation trigger: {trigger}"
            };
            
            await ProcessAdaptationImmediately(context);
        }
    }
}
