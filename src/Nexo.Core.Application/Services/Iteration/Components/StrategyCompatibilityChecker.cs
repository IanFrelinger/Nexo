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
    /// Handles strategy compatibility checking for iteration strategies.
    /// </summary>
    public class StrategyCompatibilityChecker
    {
        private readonly ILogger _logger;

        public StrategyCompatibilityChecker(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<IIterationStrategy<T>> FindCompatibleStrategies<T>(List<IIterationStrategy<object>> strategies, IterationContext context)
        {
            try
            {
                var compatibleStrategies = strategies
                    .Where(s => s is IIterationStrategy<T> || (s is StrategyWrapper<T> wrapper && wrapper.CanHandleType<T>()))
                    .Where(s => IsPlatformCompatible(s.PlatformCompatibility, context.EnvironmentProfile.CurrentPlatform))
                    .Select(s => s is IIterationStrategy<T> direct ? direct : ((StrategyWrapper<T>)s).Unwrap())
                    .ToList();

                _logger.LogDebug("Found {Count} compatible strategies for type {Type}", compatibleStrategies.Count, typeof(T).Name);
                return compatibleStrategies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding compatible strategies for type {Type}", typeof(T).Name);
                return new List<IIterationStrategy<T>>();
            }
        }

        public async Task<IEnumerable<IIterationStrategy<T>>> FindCompatibleStrategiesAsync<T>(List<IIterationStrategy<object>> strategies, IterationContext context)
        {
            try
            {
                var compatibleStrategies = strategies
                    .Where(s => s is IIterationStrategy<T> || (s is StrategyWrapper<T> wrapper && wrapper.CanHandleType<T>()))
                    .Where(s => IsPlatformCompatible(s.PlatformCompatibility, context.EnvironmentProfile.CurrentPlatform))
                    .Select(s => s is IIterationStrategy<T> direct ? direct : ((StrategyWrapper<T>)s).Unwrap())
                    .ToList();

                _logger.LogDebug("Found {Count} compatible strategies for type {Type}", compatibleStrategies.Count, typeof(T).Name);
                return await Task.FromResult(compatibleStrategies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding compatible strategies for type {Type}", typeof(T).Name);
                return new List<IIterationStrategy<T>>();
            }
        }

        public bool IsStrategyCompatible<T>(IIterationStrategy<T> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null)
                    return false;

                if (context?.EnvironmentProfile == null)
                    return false;

                return IsPlatformCompatible(strategy.PlatformCompatibility, context.EnvironmentProfile.CurrentPlatform);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking strategy compatibility");
                return false;
            }
        }

        public bool IsPlatformCompatible(PlatformCompatibility compatibility, PlatformType targetPlatform)
        {
            try
            {
                if (compatibility == null)
                    return false;

                return targetPlatform switch
                {
                    PlatformType.Android => compatibility.SupportsAndroid,
                    PlatformType.iOS => compatibility.SupportsIOS,
                    PlatformType.Windows => compatibility.SupportsWindows,
                    PlatformType.macOS => compatibility.SupportsMacOS,
                    PlatformType.Linux => compatibility.SupportsLinux,
                    PlatformType.Web => compatibility.SupportsWeb,
                    PlatformType.Unknown => true, // Unknown platform is compatible with all
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking platform compatibility");
                return false;
            }
        }

        public bool IsMemoryCompatible(IIterationStrategy<object> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null || context?.EnvironmentProfile == null)
                    return false;

                var availableMemory = context.EnvironmentProfile.AvailableMemory;
                var requiredMemory = strategy.MemoryRequirements?.EstimatedMemoryUsage ?? 0;

                return availableMemory >= requiredMemory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking memory compatibility");
                return false;
            }
        }

        public bool IsPerformanceCompatible(IIterationStrategy<object> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null || context?.EnvironmentProfile == null)
                    return false;

                var targetPerformance = context.EnvironmentProfile.TargetPerformance;
                var strategyPerformance = strategy.PerformanceCharacteristics?.PerformanceRating ?? PerformanceRating.Medium;

                return IsPerformanceRatingCompatible(strategyPerformance, targetPerformance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking performance compatibility");
                return false;
            }
        }

        public bool IsFeatureCompatible(IIterationStrategy<object> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null || context?.EnvironmentProfile == null)
                    return false;

                var requiredFeatures = strategy.RequiredFeatures ?? new List<string>();
                var availableFeatures = context.EnvironmentProfile.AvailableFeatures ?? new List<string>();

                return requiredFeatures.All(feature => availableFeatures.Contains(feature));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feature compatibility");
                return false;
            }
        }

        public bool IsDataSizeCompatible(IIterationStrategy<object> strategy, IterationContext context)
        {
            try
            {
                if (strategy == null || context?.DataProfile == null)
                    return false;

                var dataSize = context.DataProfile.EstimatedDataSize;
                var maxDataSize = strategy.DataSizeLimitations?.MaxDataSize ?? long.MaxValue;
                var minDataSize = strategy.DataSizeLimitations?.MinDataSize ?? 0;

                return dataSize >= minDataSize && dataSize <= maxDataSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking data size compatibility");
                return false;
            }
        }

        private bool IsPerformanceRatingCompatible(PerformanceRating strategyRating, PerformanceRating targetRating)
        {
            return strategyRating switch
            {
                PerformanceRating.Low => targetRating == PerformanceRating.Low,
                PerformanceRating.Medium => targetRating == PerformanceRating.Low || targetRating == PerformanceRating.Medium,
                PerformanceRating.High => true, // High performance strategies are compatible with all targets
                _ => false
            };
        }
    }
}
