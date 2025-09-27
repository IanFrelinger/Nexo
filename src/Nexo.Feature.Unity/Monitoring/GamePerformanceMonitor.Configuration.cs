using System;
using System.Collections.Generic;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Monitoring
{
    public partial class GamePerformanceMonitor
    {
        private void ConfigurePerformanceThresholds(GameMonitoringConfiguration config)
        {
            _performanceThresholds.Clear();
            
            // Frame rate threshold
            _performanceThresholds["FrameRate"] = new PerformanceThreshold
            {
                MetricName = "FrameRate",
                ThresholdValue = config.TargetFrameRate,
                ComparisonType = ComparisonType.LessThan,
                TriggerAdaptation = true,
                SendAlert = true
            };
            
            // Memory usage threshold
            _performanceThresholds["MemoryUsage"] = new PerformanceThreshold
            {
                MetricName = "MemoryUsage",
                ThresholdValue = config.MaxMemoryUsage,
                ComparisonType = ComparisonType.GreaterThan,
                TriggerAdaptation = true,
                SendAlert = true
            };
            
            // CPU time threshold
            _performanceThresholds["CpuTime"] = new PerformanceThreshold
            {
                MetricName = "CpuTime",
                ThresholdValue = config.MaxCpuTime,
                ComparisonType = ComparisonType.GreaterThan,
                TriggerAdaptation = true,
                SendAlert = false
            };
            
            // GPU time threshold
            _performanceThresholds["GpuTime"] = new PerformanceThreshold
            {
                MetricName = "GpuTime",
                ThresholdValue = config.MaxGpuTime,
                ComparisonType = ComparisonType.GreaterThan,
                TriggerAdaptation = false,
                SendAlert = true
            };
        }
        
        private double GetMetricValue(GamePerformanceSnapshot snapshot, string metricName)
        {
            return metricName switch
            {
                "FrameRate" => snapshot.FrameRate,
                "MemoryUsage" => snapshot.MemoryUsage,
                "CpuTime" => snapshot.CpuTime,
                "GpuTime" => snapshot.GpuTime,
                "GarbageCollectionTime" => snapshot.GarbageCollectionTime,
                "DrawCalls" => snapshot.DrawCalls,
                _ => 0
            };
        }
        
        private AdaptationPriority DetermineAdaptationPriority(PerformanceAnalysis analysis)
        {
            return analysis.Severity switch
            {
                PerformanceIssueSeverity.Critical => AdaptationPriority.Critical,
                PerformanceIssueSeverity.High => AdaptationPriority.High,
                PerformanceIssueSeverity.Medium => AdaptationPriority.Medium,
                _ => AdaptationPriority.Low
            };
        }
    }
}
