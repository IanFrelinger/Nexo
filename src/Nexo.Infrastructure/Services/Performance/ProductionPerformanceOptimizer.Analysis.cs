using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Caching;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Performance
{
    /// <summary>
    /// Analysis and calculation methods for ProductionPerformanceOptimizer.
    /// </summary>
    public partial class ProductionPerformanceOptimizer
    {
        private double CalculateCacheImprovement(CachePerformanceMetrics metrics)
        {
            // Calculate potential improvement based on current metrics
            var hitRate = metrics.HitCount / Math.Max(metrics.HitCount + metrics.MissCount, 1);
            if (hitRate < 0.7) return 0.3; // 30% improvement
            if (hitRate < 0.8) return 0.2; // 20% improvement
            if (hitRate < 0.9) return 0.1; // 10% improvement
            return 0.05; // 5% improvement
        }

        private PerformanceTrend CalculateTrend(List<double> values)
        {
            if (values.Count < 2) return PerformanceTrend.Stable;
            
            var firstHalf = values.Take(values.Count / 2).Average();
            var secondHalf = values.Skip(values.Count / 2).Average();
            
            var change = (secondHalf - firstHalf) / firstHalf;
            
            if (change > 0.1) return PerformanceTrend.Improving;
            if (change < -0.1) return PerformanceTrend.Degrading;
            return PerformanceTrend.Stable;
        }

        private PerformanceTrend CalculateOverallTrend(PerformanceTrends trends)
        {
            var trendsList = new List<PerformanceTrend>
            {
                trends.CacheHitRateTrend,
                trends.AIResponseTimeTrend,
                trends.MemoryUsageTrend
            };
            
            var improvingCount = trendsList.Count(t => t == PerformanceTrend.Improving);
            var degradingCount = trendsList.Count(t => t == PerformanceTrend.Degrading);
            
            if (improvingCount > degradingCount) return PerformanceTrend.Improving;
            if (degradingCount > improvingCount) return PerformanceTrend.Degrading;
            return PerformanceTrend.Stable;
        }
    }
}
