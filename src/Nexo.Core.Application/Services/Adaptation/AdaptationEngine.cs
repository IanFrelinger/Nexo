using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Core adaptation engine that orchestrates real-time system improvements
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class AdaptationEngine : IAdaptationEngine, IHostedService
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