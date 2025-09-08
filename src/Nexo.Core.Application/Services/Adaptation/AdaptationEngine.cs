using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

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
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartAdaptationAsync(cancellationToken);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopAdaptationAsync(cancellationToken);
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
        
        await Task.CompletedTask;
    }
    
    public void RegisterAdaptationStrategy(IAdaptationStrategy strategy)
    {
        _strategyRegistry.RegisterStrategy(strategy);
        _logger.LogInformation("Registered adaptation strategy: {StrategyId}", strategy.StrategyId);
    }
    
    public async Task<AdaptationStatus> GetAdaptationStatusAsync()
    {
        var activeAdaptations = await _dataStore.GetActiveAdaptationsAsync();
        var recentImprovements = await _dataStore.GetRecentImprovementsAsync(TimeSpan.FromHours(24));
        var totalAdaptations = await _dataStore.GetTotalAdaptationsCountAsync();
        var overallEffectiveness = await _dataStore.GetOverallEffectivenessAsync();
        
        return new AdaptationStatus
        {
            EngineStatus = _engineStatus,
            ActiveAdaptations = activeAdaptations,
            RecentImprovements = recentImprovements,
            LastAdaptationTime = activeAdaptations.Any() ? activeAdaptations.Max(a => a.AppliedAt) : DateTime.MinValue,
            TotalAdaptationsApplied = totalAdaptations,
            OverallEffectiveness = overallEffectiveness
        };
    }
    
    public async Task<IEnumerable<AdaptationRecord>> GetRecentAdaptationsAsync(TimeSpan timeWindow)
    {
        return await _dataStore.GetRecentAdaptationsAsync(timeWindow);
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
                await _learningSystem.RecordAdaptationResultsAsync(adaptationNeeds);
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
        return new SystemState
        {
            PerformanceMetrics = await _performanceMonitor.GetCurrentMetricsAsync(),
            EnvironmentProfile = await _environmentDetector.GetCurrentEnvironmentAsync(),
            RecentFeedback = await _feedbackCollector.GetRecentFeedbackAsync(TimeSpan.FromMinutes(5)),
            ResourceUtilization = await GetResourceUtilizationAsync(),
            ActiveWorkloads = await GetActiveWorkloadsAsync(),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private async Task<IEnumerable<AdaptationNeed>> AnalyzeAdaptationNeeds(SystemState systemState)
    {
        var needs = new List<AdaptationNeed>();
        
        // Performance-based adaptations
        if (systemState.PerformanceMetrics.RequiresOptimization)
        {
            needs.Add(new AdaptationNeed
            {
                Type = AdaptationType.PerformanceOptimization,
                Trigger = AdaptationTrigger.PerformanceDegradation,
                Priority = MapSeverityToPriority(systemState.PerformanceMetrics.Severity),
                Context = systemState,
                Description = $"Performance degradation detected: {systemState.PerformanceMetrics.Severity}"
            });
        }
        
        // Resource-based adaptations
        if (systemState.ResourceUtilization.IsConstrained)
        {
            needs.Add(new AdaptationNeed
            {
                Type = AdaptationType.ResourceOptimization,
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
                Type = AdaptationType.UserExperienceOptimization,
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
                Type = AdaptationType.EnvironmentOptimization,
                Trigger = AdaptationTrigger.EnvironmentChange,
                Priority = AdaptationPriority.Medium,
                Context = systemState,
                Description = "Environment change detected"
            });
        }
        
        return needs.OrderByDescending(n => n.Priority);
    }
    
    private async Task ExecuteAdaptations(IEnumerable<AdaptationNeed> adaptationNeeds)
    {
        foreach (var need in adaptationNeeds)
        {
            var strategies = _strategyRegistry.GetStrategiesForAdaptationType(need.Type);
            
            foreach (var strategy in strategies.OrderByDescending(s => s.GetPriority(need.Context)))
            {
                try
                {
                    if (await strategy.CanHandleAsync(need))
                    {
                        var result = await strategy.ExecuteAdaptationAsync(need);
                        
                        if (result.IsSuccessful)
                        {
                            _logger.LogInformation("Successfully applied adaptation {AdaptationType} using strategy {StrategyId}",
                                need.Type, strategy.StrategyId);
                            
                            await RecordSuccessfulAdaptation(need, strategy, result);
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
            WasSuccessful = result.IsSuccessful,
            EffectivenessScore = result.EstimatedImprovement
        };
        
        await _dataStore.RecordAdaptationAsync(record);
        
        // Record applied adaptations
        foreach (var adaptation in result.AppliedAdaptations)
        {
            await _dataStore.RecordAppliedAdaptationAsync(adaptation);
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
    
    private async Task<ResourceUtilization> GetResourceUtilizationAsync()
    {
        // This would integrate with actual system resource monitoring
        return new ResourceUtilization
        {
            CpuUsage = 0.0, // Would be populated from actual monitoring
            MemoryUsage = 0.0,
            DiskUsage = 0.0,
            NetworkUsage = 0.0,
            IsConstrained = false,
            ConstraintType = ResourceConstraintType.None
        };
    }
    
    private async Task<IEnumerable<ActiveWorkload>> GetActiveWorkloadsAsync()
    {
        // This would integrate with actual workload monitoring
        return Enumerable.Empty<ActiveWorkload>();
    }
    
    private void HandlePerformanceDegradation(object? sender, PerformanceDegradationEventArgs e)
    {
        _logger.LogWarning("Performance degradation detected: {Severity}", e.Severity);
        _pendingAdaptations.Enqueue(AdaptationTrigger.PerformanceDegradation);
    }
    
    private void HandleNegativeFeedback(object? sender, NegativeFeedbackEventArgs e)
    {
        _logger.LogWarning("Negative feedback received: {Severity}", e.Feedback.Severity);
        _pendingAdaptations.Enqueue(AdaptationTrigger.UserFeedback);
    }
    
    private void HandleEnvironmentChange(object? sender, EnvironmentChangeEventArgs e)
    {
        _logger.LogInformation("Environment change detected: {ChangeType}", e.ChangeType);
        _pendingAdaptations.Enqueue(AdaptationTrigger.EnvironmentChange);
    }
    
    private AdaptationType DetermineAdaptationType(AdaptationTrigger trigger)
    {
        return trigger switch
        {
            AdaptationTrigger.PerformanceDegradation => AdaptationType.PerformanceOptimization,
            AdaptationTrigger.ResourceConstraint => AdaptationType.ResourceOptimization,
            AdaptationTrigger.UserFeedback => AdaptationType.UserExperienceOptimization,
            AdaptationTrigger.EnvironmentChange => AdaptationType.EnvironmentOptimization,
            _ => AdaptationType.PerformanceOptimization
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