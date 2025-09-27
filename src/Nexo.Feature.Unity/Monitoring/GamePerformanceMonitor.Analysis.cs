using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Monitoring
{
    public partial class GamePerformanceMonitor
    {
        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var valueList = values.ToList();
            if (!valueList.Any()) return 0;
            
            var mean = valueList.Average();
            var variance = valueList.Sum(v => Math.Pow(v - mean, 2)) / valueList.Count;
            return Math.Sqrt(variance);
        }
        
        private IEnumerable<PerformanceTrend> AnalyzePerformanceTrends(List<GamePerformanceSnapshot> snapshots)
        {
            var trends = new List<PerformanceTrend>();
            
            if (snapshots.Count < 2) return trends;
            
            // Analyze frame rate trend
            var frameRateTrend = AnalyzeMetricTrend(snapshots, s => s.FrameRate, "FrameRate");
            if (frameRateTrend != null) trends.Add(frameRateTrend);
            
            // Analyze memory usage trend
            var memoryTrend = AnalyzeMetricTrend(snapshots, s => s.MemoryUsage, "MemoryUsage");
            if (memoryTrend != null) trends.Add(memoryTrend);
            
            // Analyze CPU time trend
            var cpuTrend = AnalyzeMetricTrend(snapshots, s => s.CpuTime, "CpuTime");
            if (cpuTrend != null) trends.Add(cpuTrend);
            
            return trends;
        }
        
        private PerformanceTrend? AnalyzeMetricTrend(List<GamePerformanceSnapshot> snapshots, Func<GamePerformanceSnapshot, double> metricSelector, string metricName)
        {
            if (snapshots.Count < 2) return null;
            
            var values = snapshots.Select(metricSelector).ToList();
            var firstHalf = values.Take(values.Count / 2).Average();
            var secondHalf = values.Skip(values.Count / 2).Average();
            
            var trendDirection = secondHalf > firstHalf ? TrendDirection.Increasing : TrendDirection.Decreasing;
            var trendStrength = Math.Abs(secondHalf - firstHalf) / firstHalf;
            
            return new PerformanceTrend
            {
                MetricName = metricName,
                Direction = trendDirection,
                Strength = trendStrength,
                StartValue = firstHalf,
                EndValue = secondHalf
            };
        }
        
        private IEnumerable<CriticalPerformanceEvent> IdentifyCriticalEvents(List<GamePerformanceSnapshot> snapshots)
        {
            var events = new List<CriticalPerformanceEvent>();
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.FrameRate < 15) // Critical frame rate drop
                {
                    events.Add(new CriticalPerformanceEvent
                    {
                        Timestamp = snapshot.Timestamp,
                        EventType = "Critical Frame Rate Drop",
                        Severity = PerformanceIssueSeverity.Critical,
                        Description = $"Frame rate dropped to {snapshot.FrameRate:F1} FPS",
                        Snapshot = snapshot
                    });
                }
                
                if (snapshot.MemoryUsage > 2L * 1024 * 1024 * 1024) // > 2GB memory usage
                {
                    events.Add(new CriticalPerformanceEvent
                    {
                        Timestamp = snapshot.Timestamp,
                        EventType = "High Memory Usage",
                        Severity = PerformanceIssueSeverity.High,
                        Description = $"Memory usage reached {snapshot.MemoryUsage / 1024 / 1024 / 1024:F1} GB",
                        Snapshot = snapshot
                    });
                }
                
                if (snapshot.GarbageCollectionTime > 10) // > 10ms GC time
                {
                    events.Add(new CriticalPerformanceEvent
                    {
                        Timestamp = snapshot.Timestamp,
                        EventType = "High Garbage Collection",
                        Severity = PerformanceIssueSeverity.High,
                        Description = $"GC time reached {snapshot.GarbageCollectionTime:F1}ms",
                        Snapshot = snapshot
                    });
                }
            }
            
            return events;
        }
        
        private async Task<IEnumerable<OptimizationOpportunity>> IdentifyOptimizationOpportunities(List<GamePerformanceSnapshot> snapshots)
        {
            var opportunities = new List<OptimizationOpportunity>();
            
            // Frame rate optimization opportunities
            var lowFrameRateSnapshots = snapshots.Where(s => s.FrameRate < 30).ToList();
            if (lowFrameRateSnapshots.Any())
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    Type = "Frame Rate Optimization",
                    Description = "Frame rate consistently below 30 FPS",
                    Impact = OptimizationImpact.High,
                    EstimatedImprovement = 0.3,
                    Recommendations = new[] { "Optimize rendering pipeline", "Reduce draw calls", "Implement LOD groups" }
                });
            }
            
            // Memory optimization opportunities
            var highMemorySnapshots = snapshots.Where(s => s.MemoryUsage > 1024 * 1024 * 1024).ToList(); // > 1GB
            if (highMemorySnapshots.Any())
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    Type = "Memory Optimization",
                    Description = "High memory usage detected",
                    Impact = OptimizationImpact.Medium,
                    EstimatedImprovement = 0.2,
                    Recommendations = new[] { "Optimize texture sizes", "Implement object pooling", "Reduce asset quality" }
                });
            }
            
            // GC optimization opportunities
            var highGCSnapshots = snapshots.Where(s => s.GarbageCollectionTime > 5).ToList(); // > 5ms GC
            if (highGCSnapshots.Any())
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    Type = "Garbage Collection Optimization",
                    Description = "High garbage collection time detected",
                    Impact = OptimizationImpact.High,
                    EstimatedImprovement = 0.4,
                    Recommendations = new[] { "Avoid allocations in Update", "Use object pooling", "Cache frequently used objects" }
                });
            }
            
            return opportunities;
        }
        
        private IEnumerable<PlatformInsight> GeneratePlatformInsights(List<GamePerformanceSnapshot> snapshots)
        {
            var insights = new List<PlatformInsight>();
            
            // Analyze performance patterns
            var averageFrameRate = snapshots.Average(s => s.FrameRate);
            var frameRateVariance = CalculateStandardDeviation(snapshots.Select(s => s.FrameRate));
            
            insights.Add(new PlatformInsight
            {
                Platform = "Current Platform",
                InsightType = "Performance Stability",
                Description = $"Average frame rate: {averageFrameRate:F1} FPS, Variance: {frameRateVariance:F1}",
                Recommendation = frameRateVariance > 10 ? "Improve frame rate stability" : "Frame rate is stable"
            });
            
            // Memory usage insights
            var averageMemory = snapshots.Average(s => s.MemoryUsage);
            var maxMemory = snapshots.Max(s => s.MemoryUsage);
            
            insights.Add(new PlatformInsight
            {
                Platform = "Current Platform",
                InsightType = "Memory Usage",
                Description = $"Average memory: {averageMemory / 1024 / 1024:F1} MB, Peak: {maxMemory / 1024 / 1024:F1} MB",
                Recommendation = maxMemory > 1024 * 1024 * 1024 ? "Optimize memory usage" : "Memory usage is acceptable"
            });
            
            return insights;
        }
    }
}
