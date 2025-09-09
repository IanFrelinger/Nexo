using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation.Strategies;

/// <summary>
/// Interface for adaptation strategies that can be applied to improve system performance
/// </summary>
public interface IAdaptationStrategy
{
    /// <summary>
    /// Unique identifier for this strategy
    /// </summary>
    string StrategyId { get; }
    
    /// <summary>
    /// The type of adaptation this strategy handles
    /// </summary>
    AdaptationType SupportedAdaptationType { get; }
    
    /// <summary>
    /// Execute the adaptation strategy
    /// </summary>
    Task<AdaptationResult> ExecuteAdaptationAsync(AdaptationNeed need);
    
    /// <summary>
    /// Get the priority of this strategy for the given system state
    /// </summary>
    int GetPriority(SystemState systemState);
    
    /// <summary>
    /// Check if this strategy can handle the given adaptation need
    /// </summary>
    Task<bool> CanHandleAsync(AdaptationNeed need);
    
    /// <summary>
    /// Get a description of what this strategy does
    /// </summary>
    string GetDescription();
    
    /// <summary>
    /// Get estimated improvement factor for this strategy
    /// </summary>
    double GetEstimatedImprovementFactor(AdaptationNeed need);
}

/// <summary>
/// Registry for managing adaptation strategies
/// </summary>
public interface IAdaptationStrategyRegistry
{
    /// <summary>
    /// Register a new adaptation strategy
    /// </summary>
    void RegisterStrategy(IAdaptationStrategy strategy);
    
    /// <summary>
    /// Get all strategies that support the given adaptation type
    /// </summary>
    IEnumerable<IAdaptationStrategy> GetStrategiesForAdaptationType(AdaptationType type);
    
    /// <summary>
    /// Get all registered strategies
    /// </summary>
    IEnumerable<IAdaptationStrategy> GetAllStrategies();
    
    /// <summary>
    /// Remove a strategy by ID
    /// </summary>
    bool RemoveStrategy(string strategyId);
}

/// <summary>
/// Implementation of the adaptation strategy registry
/// </summary>
public class AdaptationStrategyRegistry : IAdaptationStrategyRegistry
{
    private readonly Dictionary<string, IAdaptationStrategy> _strategies = new();
    private readonly Dictionary<AdaptationType, List<IAdaptationStrategy>> _strategiesByType = new();
    private readonly object _lock = new();
    
    public void RegisterStrategy(IAdaptationStrategy strategy)
    {
        lock (_lock)
        {
            _strategies[strategy.StrategyId] = strategy;
            
            if (!_strategiesByType.ContainsKey(strategy.SupportedAdaptationType))
            {
                _strategiesByType[strategy.SupportedAdaptationType] = new List<IAdaptationStrategy>();
            }
            
            _strategiesByType[strategy.SupportedAdaptationType].Add(strategy);
        }
    }
    
    public IEnumerable<IAdaptationStrategy> GetStrategiesForAdaptationType(AdaptationType type)
    {
        lock (_lock)
        {
            return _strategiesByType.ContainsKey(type) 
                ? _strategiesByType[type].ToList() 
                : Enumerable.Empty<IAdaptationStrategy>();
        }
    }
    
    public IEnumerable<IAdaptationStrategy> GetAllStrategies()
    {
        lock (_lock)
        {
            return _strategies.Values.ToList();
        }
    }
    
    public bool RemoveStrategy(string strategyId)
    {
        lock (_lock)
        {
            if (_strategies.TryGetValue(strategyId, out var strategy))
            {
                _strategies.Remove(strategyId);
                
                if (_strategiesByType.ContainsKey(strategy.SupportedAdaptationType))
                {
                    _strategiesByType[strategy.SupportedAdaptationType].Remove(strategy);
                }
                
                return true;
            }
            
            return false;
        }
    }
    
}