using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Entities = Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Application.Services.Iteration.Components;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Orchestrator for iteration strategy selection that delegates to specialized strategy selection components.
/// </summary>
public partial class IterationStrategySelector : IIterationStrategySelector
{
    private readonly List<IIterationStrategy<object>> _strategies = new();
    private RuntimeEnvironmentProfile _environmentProfile = new(); // Default profile
    private readonly ILogger<IterationStrategySelector> _logger;
    private readonly StrategyCompatibilityChecker _compatibilityChecker;
    private readonly StrategyScorer _scorer;
    private readonly StrategyRegistry _strategyRegistry;
    private readonly StrategyFallbackHandler _fallbackHandler;

    public IterationStrategySelector(ILogger<IterationStrategySelector> logger)
    {
        _logger = logger;
        _compatibilityChecker = new StrategyCompatibilityChecker(_logger);
        _scorer = new StrategyScorer(_logger);
        _strategyRegistry = new StrategyRegistry(_logger);
        _fallbackHandler = new StrategyFallbackHandler(_logger);
        
        RegisterDefaultStrategies();
    }

    public IIterationStrategy<T> SelectStrategy<T>(IterationContext context)
    {
        var compatibleStrategies = _compatibilityChecker.FindCompatibleStrategies<T>(_strategies, context);
        
        if (!compatibleStrategies.Any())
        {
            _logger.LogWarning("No compatible strategies found, using fallback");
            return _fallbackHandler.GetFallbackStrategy<T>();
        }

        var scoredStrategies = _scorer.ScoreStrategies(compatibleStrategies, context);
        return scoredStrategies.FirstOrDefault()?.Strategy ?? _fallbackHandler.GetFallbackStrategy<T>();
    }

    public async Task<IIterationStrategy<T>> SelectStrategyAsync<T>(IterationContext context)
    {
        var compatibleStrategies = await _compatibilityChecker.FindCompatibleStrategiesAsync<T>(_strategies, context);
        
        if (!compatibleStrategies.Any())
        {
            _logger.LogWarning("No compatible strategies found, using fallback");
            return _fallbackHandler.GetFallbackStrategy<T>();
        }

        var scoredStrategies = await _scorer.ScoreStrategiesAsync(compatibleStrategies, context);
        return scoredStrategies.FirstOrDefault()?.Strategy ?? _fallbackHandler.GetFallbackStrategy<T>();
    }

    public void RegisterStrategy<T>(IIterationStrategy<T> strategy)
    {
        _strategyRegistry.RegisterStrategy(_strategies, strategy);
    }

    public void UnregisterStrategy<T>(IIterationStrategy<T> strategy)
    {
        _strategyRegistry.UnregisterStrategy(_strategies, strategy);
    }

    public IEnumerable<IIterationStrategy<T>> GetAvailableStrategies<T>()
    {
        return _strategyRegistry.GetAvailableStrategies<T>(_strategies);
    }

    public void SetEnvironmentProfile(RuntimeEnvironmentProfile profile)
    {
        _environmentProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        _logger.LogInformation("Environment profile updated to {Platform}", profile.CurrentPlatform);
    }

    public RuntimeEnvironmentProfile GetEnvironmentProfile()
    {
        return _environmentProfile;
    }

    public bool IsStrategyCompatible<T>(IIterationStrategy<T> strategy, IterationContext context)
    {
        return _compatibilityChecker.IsStrategyCompatible(strategy, context);
    }

    public double CalculateStrategyScore<T>(IIterationStrategy<T> strategy, IterationContext context)
    {
        return _scorer.CalculateStrategyScore(strategy, context);
    }

    public void ClearStrategies()
    {
        _strategies.Clear();
        _logger.LogInformation("All strategies cleared");
    }

    public int GetStrategyCount()
    {
        return _strategies.Count;
    }

    public bool HasStrategy<T>(IIterationStrategy<T> strategy)
    {
        return _strategyRegistry.HasStrategy(_strategies, strategy);
    }

    private void RegisterDefaultStrategies()
    {
        _strategyRegistry.RegisterDefaultStrategies(_strategies);
    }
}
