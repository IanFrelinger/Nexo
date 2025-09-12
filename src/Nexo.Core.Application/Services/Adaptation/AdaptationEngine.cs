using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Core adaptation engine that orchestrates real-time system improvements
/// </summary>
public class AdaptationEngine : IAdaptationEngine, IHostedService
{
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IUserFeedbackCollector _feedbackCollector;
    private readonly IEnvironmentDetector _environmentDetector;
    private readonly IAdaptationStrategyRegistry _strategyRegistry;
    private readonly IAdaptationLearningSystem _learningSystem;
    private readonly ILogger<AdaptationEngine> _logger;
    private readonly IAdaptationDataStore _dataStore;
    
    private Timer? _adaptationTimer;
    private readonly ConcurrentQueue<AdaptationTrigger> _pendingAdaptations = new();
    private volatile bool _isAdapting = false;
    private volatile AdaptationEngineStatus _engineStatus = AdaptationEngineStatus.Stopped;
    private readonly SemaphoreSlim _adaptationSemaphore = new(1, 1);
    
    public AdaptationEngine(
        IPerformanceMonitor performanceMonitor,
        IUserFeedbackCollector feedbackCollector,
        IEnvironmentDetector environmentDetector,
        IAdaptationStrategyRegistry strategyRegistry,
        IAdaptationLearningSystem learningSystem,
        ILogger<AdaptationEngine> logger,
        IAdaptationDataStore dataStore)
    {
        _performanceMonitor = performanceMonitor;
        _feedbackCollector = feedbackCollector;
        _environmentDetector = environmentDetector;
        _strategyRegistry = strategyRegistry;
        _learningSystem = learningSystem;
        _logger = logger;
        _dataStore = dataStore;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return StartAdaptationAsync(cancellationToken);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopAdaptationAsync(cancellationToken);
    }
    
        public async Task StartAdaptationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Nexo Adaptation Engine");
        _engineStatus = AdaptationEngineStatus.Starting;
        
        try
        {
            // Start continuous monitoring
            _adaptationTimer = new Timer(ProcessAdaptations, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            
            // Set up event listeners
            _performanceMonitor.OnPerformanceDegradation += HandlePerformanceDegradation;
            _feedbackCollector.OnNegativeFeedback += HandleNegativeFeedback;
            _environmentDetector.OnEnvironmentChange += HandleEnvironmentChange;
            
            _engineStatus = AdaptationEngineStatus.Running;
            _logger.LogInformation("Nexo Adaptation Engine started successfully");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _engineStatus = AdaptationEngineStatus.Error;
            _logger.LogError(ex, "Failed to start adaptation engine");
            throw;
        }
    }
    
    public async Task StopAdaptationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Nexo Adaptation Engine");
        _engineStatus = AdaptationEngineStatus.Stopping;
        
        try
        {
            // Stop the timer
            _adaptationTimer?.Dispose();
            _adaptationTimer = null;
            
            // Remove event listeners
            _performanceMonitor.OnPerformanceDegradation -= HandlePerformanceDegradation;
            _feedbackCollector.OnNegativeFeedback -= HandleNegativeFeedback;
            _environmentDetector.OnEnvironmentChange -= HandleEnvironmentChange;
            
            // Wait for any ongoing adaptations to complete
            await _adaptationSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Process any remaining pending adaptations
                await ProcessPendingAdaptations();
            }
            finally
            {
                _adaptationSemaphore.Release();
            }
            
            _engineStatus = AdaptationEngineStatus.Stopped;
            _logger.LogInformation("Nexo Adaptation Engine stopped successfully");
        }
        catch (Exception ex)
        {
            _engineStatus = AdaptationEngineStatus.Error;
            _logger.LogError(ex, "Error stopping adaptation engine");
            throw;
        }
    }
    
    public async Task TriggerAdaptationAsync(AdaptationContext context)
    {
        _logger.LogInformation("Triggering adaptation: {Trigger} with priority {Priority}", 
            context.Trigger, context.Priority);
        
        _pendingAdaptations.Enqueue(context.Trigger);
        
        // Process immediately for high priority adaptations
        if (context.Priority >= AdaptationPriority.High)
        {
            await ProcessAdaptationImmediately(context);
        }
    }
    
    public void RegisterAdaptationStrategy(IAdaptationStrategy strategy)
    {
        _strategyRegistry.RegisterStrategy(strategy.StrategyId, strategy);
        _logger.LogInformation("Registered adaptation strategy: {StrategyId}", strategy.StrategyId);
    }
    
    public async Task<AdaptationStatus> GetAdaptationStatusAsync()
    {
        var activeAdaptations = await _dataStore.GetActiveAdaptationsAsync();
        var recentImprovements = await _dataStore.GetRecentImprovementsAsync(24);
        var totalAdaptations = await _dataStore.GetTotalAdaptationsCountAsync();
        var overallEffectiveness = await _dataStore.GetOverallEffectivenessAsync();
        
        return new AdaptationStatus
        {
            EngineStatus = _engineStatus,
            ActiveAdaptations = activeAdaptations.Select(a => new AppliedAdaptation
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                Description = a.Description,
                AppliedAt = a.Timestamp,
                EstimatedImprovementFactor = a.EffectivenessScore
            }),
            RecentImprovements = recentImprovements.Select(a => new AdaptationImprovement
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                Description = a.Description,
                AppliedAt = a.AppliedAt,
                ImprovementFactor = a.ImprovementPercentage
            }),
            LastAdaptationTime = activeAdaptations.Any() ? activeAdaptations.Max(a => a.AppliedAt) : DateTime.MinValue,
            TotalAdaptationsApplied = totalAdaptations,
            OverallEffectiveness = overallEffectiveness
        };
    }
    
    public Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.AdaptationRecord>> GetRecentAdaptationsAsync(TimeSpan timeWindow)
    {
        // Convert TimeSpan to count - assume 1 adaptation per hour for simplicity
        var count = Math.Max(1, (int)timeWindow.TotalHours);
        return _dataStore.GetRecentAdaptationsAsync(count);
    }
    
    public Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.AdaptationRecord>> GetRecentAdaptationsAsync(int count = 10)
    {
        return _dataStore.GetRecentAdaptationsAsync(count);
    }
    
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
    
    private Task<ResourceUtilization> GetResourceUtilizationAsync()
    {
        // This would integrate with actual system resource monitoring
        return Task.FromResult(new ResourceUtilization
        {
            CpuUsage = 0.0, // Would be populated from actual monitoring
            MemoryUsage = 0.0,
            DiskUsage = 0.0,
            NetworkUsage = 0.0,
            IsConstrained = false,
            ConstraintType = ResourceConstraintType.None
        });
    }
    
    private Task<IEnumerable<ActiveWorkload>> GetActiveWorkloadsAsync()
    {
        // This would integrate with actual workload monitoring
        return Task.FromResult(Enumerable.Empty<ActiveWorkload>());
    }
    
    private void HandlePerformanceDegradation(object? sender, PerformanceDegradationEventArgs e)
    {
        _logger.LogWarning("Performance degradation detected: {Severity}", e.Severity);
        _pendingAdaptations.Enqueue(AdaptationTrigger.PerformanceDegradation);
    }
    
    private void HandleNegativeFeedback(object? sender, Nexo.Core.Domain.Entities.Infrastructure.NegativeFeedbackEventArgs e)
    {
        _logger.LogWarning("Negative feedback received: {Severity}", e.Feedback.Severity);
        _pendingAdaptations.Enqueue(AdaptationTrigger.UserFeedback);
    }
    
    private void HandleEnvironmentChange(object? sender, Nexo.Core.Domain.Interfaces.Infrastructure.EnvironmentChangeEventArgs e)
    {
        _logger.LogInformation("Environment change detected: {ChangeType}", e.ChangeType);
        _pendingAdaptations.Enqueue(AdaptationTrigger.EnvironmentChange);
    }
    
    private AdaptationType DetermineAdaptationType(AdaptationTrigger trigger)
    {
        return trigger switch
        {
            AdaptationTrigger.PerformanceDegradation => Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.PerformanceOptimization,
            AdaptationTrigger.ResourceConstraint => Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.ResourceOptimization,
            AdaptationTrigger.UserFeedback => Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.UserExperienceOptimization,
            AdaptationTrigger.EnvironmentChange => Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.EnvironmentOptimization,
            _ => Nexo.Core.Domain.Entities.Infrastructure.AdaptationType.PerformanceOptimization
        };
    }
    
    private AdaptationPriority MapSeverityToPriority(PerformanceSeverity severity)
    {
        return severity switch
        {
            PerformanceSeverity.Critical => AdaptationPriority.Critical,
            PerformanceSeverity.High => AdaptationPriority.High,
            PerformanceSeverity.Medium => AdaptationPriority.Medium,
            _ => AdaptationPriority.Low
        };
    }
    
    private PerformanceSeverity ConvertAlertSeverityToPerformanceSeverity(Nexo.Core.Domain.Interfaces.Infrastructure.AlertSeverity alertSeverity)
    {
        return alertSeverity switch
        {
            Nexo.Core.Domain.Interfaces.Infrastructure.AlertSeverity.Critical => PerformanceSeverity.Critical,
            Nexo.Core.Domain.Interfaces.Infrastructure.AlertSeverity.High => PerformanceSeverity.High,
            Nexo.Core.Domain.Interfaces.Infrastructure.AlertSeverity.Medium => PerformanceSeverity.Medium,
            Nexo.Core.Domain.Interfaces.Infrastructure.AlertSeverity.Low => PerformanceSeverity.Low,
            _ => PerformanceSeverity.Low
        };
    }
    
    public void Dispose()
    {
        _adaptationTimer?.Dispose();
        _adaptationSemaphore?.Dispose();
    }
}

// Event argument classes
public class PerformanceDegradationEventArgs : EventArgs
{
    public PerformanceSeverity Severity { get; set; }
    public PerformanceMetrics Metrics { get; set; } = new();
}

public class NegativeFeedbackEventArgs : EventArgs
{
    public UserFeedback Feedback { get; set; } = new();
}

public class EnvironmentChangeEventArgs : EventArgs
{
    public string ChangeType { get; set; } = string.Empty;
    public EnvironmentProfile NewProfile { get; set; } = new();
    public EnvironmentProfile PreviousProfile { get; set; } = new();
}