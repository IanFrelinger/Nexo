using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Application.Services.Iteration.Strategies;

namespace Nexo.Core.Application.Services.Iteration.Components
{
    /// <summary>
    /// Handles strategy scoring for iteration strategies.
    /// </summary>
    public partial class StrategyScorer
    {
        private readonly ILogger _logger;

        public StrategyScorer(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<ScoredStrategy<T>> ScoreStrategies<T>(IEnumerable<IIterationStrategy<T>> strategies, IterationContext context)
        {
            try
            {
                var scoredStrategies = strategies
                    .Select(s => new ScoredStrategy<T> { Strategy = s, Score = CalculateStrategyScore(s, context) })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                _logger.LogDebug("Strategy scores:");
                foreach (var scored in scoredStrategies)
                {
                    _logger.LogDebug("  {StrategyId}: {Score}", scored.Strategy.StrategyId, scored.Score);
                }

                return scoredStrategies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scoring strategies");
                return new List<ScoredStrategy<T>>();
            }
        }

        public async Task<IEnumerable<ScoredStrategy<T>>> ScoreStrategiesAsync<T>(IEnumerable<IIterationStrategy<T>> strategies, IterationContext context)
        {
            try
            {
                var scoredStrategies = strategies
                    .Select(s => new ScoredStrategy<T> { Strategy = s, Score = CalculateStrategyScore(s, context) })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                _logger.LogDebug("Strategy scores:");
                foreach (var scored in scoredStrategies)
                {
                    _logger.LogDebug("  {StrategyId}: {Score}", scored.Strategy.StrategyId, scored.Score);
                }

                return await Task.FromResult(scoredStrategies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scoring strategies");
                return new List<ScoredStrategy<T>>();
            }
        }

        public double CalculateStrategyScore<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null || context == null)
                    return 0.0;

                var score = 0.0;

                // Performance score (40% weight)
                score += CalculatePerformanceScore(strategy, context) * 0.4;

                // Memory efficiency score (25% weight)
                score += CalculateMemoryScore(strategy, context) * 0.25;

                // Data size compatibility score (20% weight)
                score += CalculateDataSizeScore(strategy, context) * 0.2;

                // Feature compatibility score (15% weight)
                score += CalculateFeatureScore(strategy, context) * 0.15;

                return Math.Max(0.0, Math.Min(100.0, score));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating strategy score");
                return 0.0;
            }
        }

        private double CalculatePerformanceScore<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                var targetPerformance = context.EnvironmentProfile?.TargetPerformance ?? PerformanceRating.Medium;
                var strategyPerformance = strategy.PerformanceCharacteristics?.PerformanceRating ?? PerformanceRating.Medium;

                return strategyPerformance switch
                {
                    PerformanceRating.Low => targetPerformance == PerformanceRating.Low ? 100.0 : 50.0,
                    PerformanceRating.Medium => targetPerformance == PerformanceRating.Medium ? 100.0 : 75.0,
                    PerformanceRating.High => 100.0,
                    _ => 0.0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance score");
                return 0.0;
            }
        }

        private double CalculateMemoryScore<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                var availableMemory = context.EnvironmentProfile?.AvailableMemory ?? 0;
                var requiredMemory = strategy.MemoryRequirements?.EstimatedMemoryUsage ?? 0;

                if (availableMemory == 0 || requiredMemory == 0)
                    return 50.0; // Neutral score if memory info is unavailable

                var memoryRatio = (double)availableMemory / requiredMemory;
                return Math.Min(100.0, memoryRatio * 100.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating memory score");
                return 0.0;
            }
        }

        private double CalculateDataSizeScore<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                var dataSize = context.DataProfile?.EstimatedDataSize ?? 0;
                var maxDataSize = strategy.DataSizeLimitations?.MaxDataSize ?? long.MaxValue;
                var minDataSize = strategy.DataSizeLimitations?.MinDataSize ?? 0;

                if (dataSize < minDataSize || dataSize > maxDataSize)
                    return 0.0;

                if (maxDataSize == long.MaxValue)
                    return 100.0; // No upper limit

                var sizeRatio = (double)dataSize / maxDataSize;
                return Math.Max(0.0, 100.0 - (sizeRatio * 50.0));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating data size score");
                return 0.0;
            }
        }

        private double CalculateFeatureScore<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                var requiredFeatures = strategy.RequiredFeatures ?? new List<string>();
                var availableFeatures = context.EnvironmentProfile?.AvailableFeatures ?? new List<string>();

                if (!requiredFeatures.Any())
                    return 100.0; // No requirements

                var supportedFeatures = requiredFeatures.Count(feature => availableFeatures.Contains(feature));
                return (double)supportedFeatures / requiredFeatures.Count * 100.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating feature score");
                return 0.0;
            }
        }

        public double CalculateSimpleScore<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null || context == null)
                    return 0.0;

                var dataSize = context.DataProfile?.EstimatedDataSize ?? 0;
                var isLargeData = dataSize > 10000; // Simple threshold

                // Simple scoring: prioritize performance for larger data, readability for smaller
                if (isLargeData)
                {
                    return strategy.PerformanceCharacteristics?.PerformanceRating switch
                    {
                        PerformanceRating.High => 100.0,
                        PerformanceRating.Medium => 75.0,
                        PerformanceRating.Low => 50.0,
                        _ => 25.0
                    };
                }
                else
                {
                    return strategy.PerformanceCharacteristics?.PerformanceRating switch
                    {
                        PerformanceRating.High => 75.0,
                        PerformanceRating.Medium => 100.0,
                        PerformanceRating.Low => 90.0,
                        _ => 25.0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating simple score");
                return 0.0;
            }
        }
    }

    /// <summary>
    /// Represents a scored strategy.
    /// </summary>
    public partial class ScoredStrategy<T>
    {
        public IIterationStrategy<T> Strategy { get; set; }
        public double Score { get; set; }
    }
}
