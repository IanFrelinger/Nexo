using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Application.Services.Iteration.Strategies;

namespace Nexo.Core.Application.Services.Iteration.Components
{
    /// <summary>
    /// Handles strategy registration and management for iteration strategies.
    /// </summary>
    public class StrategyRegistry
    {
        private readonly ILogger _logger;

        public StrategyRegistry(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RegisterStrategy<T>(List<IIterationStrategy<object>> strategies, IIterationStrategy<T> strategy)
        {
            try
            {
                if (strategy == null)
                {
                    _logger.LogWarning("Attempted to register null strategy");
                    return;
                }

                if (HasStrategy(strategies, strategy))
                {
                    _logger.LogWarning("Strategy {StrategyId} is already registered", strategy.StrategyId);
                    return;
                }

                var wrapper = new StrategyWrapper<T>(strategy);
                strategies.Add(wrapper);
                
                _logger.LogInformation("Registered strategy {StrategyId} for type {Type}", 
                    strategy.StrategyId, typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering strategy {StrategyId}", strategy?.StrategyId);
            }
        }

        public void UnregisterStrategy<T>(List<IIterationStrategy<object>> strategies, IIterationStrategy<T> strategy)
        {
            try
            {
                if (strategy == null)
                {
                    _logger.LogWarning("Attempted to unregister null strategy");
                    return;
                }

                var wrapperToRemove = strategies
                    .OfType<StrategyWrapper<T>>()
                    .FirstOrDefault(w => w.Unwrap().StrategyId == strategy.StrategyId);

                if (wrapperToRemove != null)
                {
                    strategies.Remove(wrapperToRemove);
                    _logger.LogInformation("Unregistered strategy {StrategyId}", strategy.StrategyId);
                }
                else
                {
                    _logger.LogWarning("Strategy {StrategyId} not found for unregistration", strategy.StrategyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering strategy {StrategyId}", strategy?.StrategyId);
            }
        }

        public IEnumerable<IIterationStrategy<T>> GetAvailableStrategies<T>(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                return strategies
                    .OfType<StrategyWrapper<T>>()
                    .Select(w => w.Unwrap())
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available strategies for type {Type}", typeof(T).Name);
                return new List<IIterationStrategy<T>>();
            }
        }

        public bool HasStrategy<T>(List<IIterationStrategy<object>> strategies, IIterationStrategy<T> strategy)
        {
            try
            {
                if (strategy == null)
                    return false;

                return strategies
                    .OfType<StrategyWrapper<T>>()
                    .Any(w => w.Unwrap().StrategyId == strategy.StrategyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if strategy exists");
                return false;
            }
        }

        public void RegisterDefaultStrategies(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                _logger.LogInformation("Registering default iteration strategies");

                // Register basic strategies
                RegisterBasicStrategies(strategies);
                
                // Register performance strategies
                RegisterPerformanceStrategies(strategies);
                
                // Register memory strategies
                RegisterMemoryStrategies(strategies);

                _logger.LogInformation("Registered {Count} default strategies", strategies.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering default strategies");
            }
        }

        private void RegisterBasicStrategies(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                // Register simple foreach strategy
                var foreachStrategy = new SimpleForeachStrategy<object>();
                strategies.Add(foreachStrategy);

                // Register LINQ strategy
                var linqStrategy = new LinqIterationStrategy<object>();
                strategies.Add(linqStrategy);

                _logger.LogDebug("Registered basic strategies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering basic strategies");
            }
        }

        private void RegisterPerformanceStrategies(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                // Register parallel strategy
                var parallelStrategy = new ParallelIterationStrategy<object>();
                strategies.Add(parallelStrategy);

                // Register async strategy
                var asyncStrategy = new AsyncIterationStrategy<object>();
                strategies.Add(asyncStrategy);

                _logger.LogDebug("Registered performance strategies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering performance strategies");
            }
        }

        private void RegisterMemoryStrategies(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                // Register memory-efficient strategy
                var memoryStrategy = new MemoryEfficientIterationStrategy<object>();
                strategies.Add(memoryStrategy);

                // Register streaming strategy
                var streamingStrategy = new StreamingIterationStrategy<object>();
                strategies.Add(streamingStrategy);

                _logger.LogDebug("Registered memory strategies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering memory strategies");
            }
        }

        public void ClearStrategies(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                var count = strategies.Count;
                strategies.Clear();
                _logger.LogInformation("Cleared {Count} strategies", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing strategies");
            }
        }

        public int GetStrategyCount(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                return strategies.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting strategy count");
                return 0;
            }
        }

        public IEnumerable<string> GetStrategyIds(List<IIterationStrategy<object>> strategies)
        {
            try
            {
                return strategies.Select(s => s.StrategyId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting strategy IDs");
                return new List<string>();
            }
        }
    }
}
