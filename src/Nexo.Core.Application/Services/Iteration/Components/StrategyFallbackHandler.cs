using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Application.Services.Iteration.Strategies;

namespace Nexo.Core.Application.Services.Iteration.Components
{
    /// <summary>
    /// Handles fallback strategy selection for iteration strategies.
    /// </summary>
    public partial class StrategyFallbackHandler
    {
        private readonly ILogger _logger;

        public StrategyFallbackHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IIterationStrategy<T> GetFallbackStrategy<T>()
        {
            try
            {
                _logger.LogWarning("Using fallback strategy for type {Type}", typeof(T).Name);
                return new SimpleForeachStrategy<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fallback strategy for type {Type}", typeof(T).Name);
                return CreateEmergencyFallbackStrategy<T>();
            }
        }

        public IIterationStrategy<T> GetFallbackStrategy<T>(IterationContext context)
        {
            try
            {
                if (context?.EnvironmentProfile == null)
                {
                    _logger.LogWarning("No context provided, using basic fallback strategy");
                    return new SimpleForeachStrategy<T>();
                }

                var platform = context.EnvironmentProfile.CurrentPlatform;
                var performance = context.EnvironmentProfile.TargetPerformance;
                var memory = context.EnvironmentProfile.AvailableMemory;

                return SelectContextualFallbackStrategy<T>(platform, performance, memory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contextual fallback strategy");
                return new SimpleForeachStrategy<T>();
            }
        }

        public IIterationStrategy<T> GetFallbackStrategy<T>(string reason)
        {
            try
            {
                _logger.LogWarning("Using fallback strategy for type {Type}, reason: {Reason}", 
                    typeof(T).Name, reason);
                return new SimpleForeachStrategy<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fallback strategy with reason");
                return CreateEmergencyFallbackStrategy<T>();
            }
        }

        public IIterationStrategy<T> GetFallbackStrategy<T>(Exception exception)
        {
            try
            {
                _logger.LogWarning(exception, "Using fallback strategy for type {Type} due to exception", 
                    typeof(T).Name);
                return new SimpleForeachStrategy<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fallback strategy due to exception");
                return CreateEmergencyFallbackStrategy<T>();
            }
        }

        public bool IsFallbackStrategy<T>(IIterationStrategy<T> strategy)
        {
            try
            {
                if (strategy == null)
                    return false;

                return strategy is SimpleForeachStrategy<T> || 
                       strategy.StrategyId.Contains("Fallback") ||
                       strategy.StrategyId.Contains("Default");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if strategy is fallback");
                return false;
            }
        }

        public IEnumerable<IIterationStrategy<T>> GetAvailableFallbackStrategies<T>()
        {
            try
            {
                var fallbackStrategies = new List<IIterationStrategy<T>>
                {
                    new SimpleForeachStrategy<T>(),
                    new LinqIterationStrategy<T>()
                };

                _logger.LogDebug("Available fallback strategies for type {Type}: {Count}", 
                    typeof(T).Name, fallbackStrategies.Count);

                return fallbackStrategies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available fallback strategies");
                return new List<IIterationStrategy<T>> { new SimpleForeachStrategy<T>() };
            }
        }

        public IIterationStrategy<T> GetBestFallbackStrategy<T>(IterationContext context)
        {
            try
            {
                var availableStrategies = GetAvailableFallbackStrategies<T>();
                var bestStrategy = availableStrategies.FirstOrDefault();

                if (bestStrategy == null)
                {
                    _logger.LogError("No fallback strategies available, creating emergency fallback");
                    return CreateEmergencyFallbackStrategy<T>();
                }

                _logger.LogDebug("Selected best fallback strategy {StrategyId} for type {Type}", 
                    bestStrategy.StrategyId, typeof(T).Name);

                return bestStrategy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting best fallback strategy");
                return CreateEmergencyFallbackStrategy<T>();
            }
        }

        private IIterationStrategy<T> SelectContextualFallbackStrategy<T>(PlatformType platform, PerformanceRating performance, long memory)
        {
            try
            {
                // Select strategy based on context
                if (memory < 100 * 1024 * 1024) // Less than 100MB
                {
                    _logger.LogDebug("Low memory detected, using memory-efficient fallback strategy");
                    return new MemoryEfficientIterationStrategy<T>();
                }

                if (performance == PerformanceRating.High)
                {
                    _logger.LogDebug("High performance required, using performance fallback strategy");
                    return new ParallelIterationStrategy<T>();
                }

                if (platform == PlatformType.Web)
                {
                    _logger.LogDebug("Web platform detected, using web-compatible fallback strategy");
                    return new SimpleForeachStrategy<T>();
                }

                _logger.LogDebug("Using default fallback strategy");
                return new SimpleForeachStrategy<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting contextual fallback strategy");
                return new SimpleForeachStrategy<T>();
            }
        }

        private IIterationStrategy<T> CreateEmergencyFallbackStrategy<T>()
        {
            try
            {
                _logger.LogCritical("Creating emergency fallback strategy for type {Type}", typeof(T).Name);
                
                // Create a minimal strategy that should always work
                return new SimpleForeachStrategy<T>();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to create emergency fallback strategy");
                
                // Last resort - return null and let the caller handle it
                return null;
            }
        }

        public void LogFallbackUsage<T>(IIterationStrategy<T> strategy, string reason)
        {
            try
            {
                _logger.LogInformation("Fallback strategy {StrategyId} used for type {Type}, reason: {Reason}", 
                    strategy?.StrategyId, typeof(T).Name, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging fallback usage");
            }
        }

        public void LogFallbackUsage<T>(IIterationStrategy<T> strategy, Exception exception)
        {
            try
            {
                _logger.LogInformation(exception, "Fallback strategy {StrategyId} used for type {Type} due to exception", 
                    strategy?.StrategyId, typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging fallback usage with exception");
            }
        }
    }
}
