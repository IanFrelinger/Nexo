using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.Feature.Unity.Services
{
    /// <summary>
    /// Real-time Unity performance profiling and monitoring
    /// </summary>
    public class UnityPerformanceProfiler : IUnityPerformanceProfiler
    {
        private readonly IUnityProfilerAPI _profilerAPI;
        private readonly IPerformanceDataCollector _dataCollector;
        private readonly IAdaptationEngine _adaptationEngine;
        private readonly ILogger<UnityPerformanceProfiler> _logger;
        
        public UnityPerformanceProfiler(
            IUnityProfilerAPI profilerAPI,
            IPerformanceDataCollector dataCollector,
            IAdaptationEngine adaptationEngine,
            ILogger<UnityPerformanceProfiler> logger)
        {
            _profilerAPI = profilerAPI;
            _dataCollector = dataCollector;
            _adaptationEngine = adaptationEngine;
            _logger = logger;
        }
        
        public async Task<UnityPerformanceProfile> ProfileGameplayAsync(UnityProfilingSession session, TimeSpan duration)
        {
            _logger.LogInformation("Starting Unity gameplay profiling for session: {SessionId}", session.SessionId);
            
            var profile = new UnityPerformanceProfile
            {
                SessionId = session.SessionId,
                StartTime = DateTime.UtcNow,
                Duration = duration
            };
            
            // Start Unity profiler
            await _profilerAPI.StartProfilingAsync(session.Configuration);
            
            try
            {
                // Collect performance data during gameplay
                var endTime = DateTime.UtcNow.Add(duration);
                
                while (DateTime.UtcNow < endTime)
                {
                    var frameData = await _profilerAPI.GetCurrentFrameDataAsync();
                    profile.FrameData.Add(frameData);
                    
                    // Check for performance issues in real-time
                    if (frameData.FrameTime > session.Configuration.TargetFrameTime * 1.5)
                    {
                        await HandlePerformanceSpikeAsync(frameData, session);
                    }
                    
                    await Task.Delay(session.Configuration.SamplingInterval);
                }
            }
            finally
            {
                await _profilerAPI.StopProfilingAsync();
            }
            
            // Analyze collected data
            profile.Analysis = await AnalyzePerformanceData(profile.FrameData);
            
            // Generate optimization recommendations
            profile.OptimizationRecommendations = await GenerateUnityOptimizationsAsync(profile.Analysis);
            
            _logger.LogInformation("Unity gameplay profiling completed for session: {SessionId}", session.SessionId);
            
            return profile;
        }
        
        public async Task<IEnumerable<UnityOptimizationRecommendation>> GenerateUnityOptimizationsAsync(UnityPerformanceAnalysis analysis)
        {
            var recommendations = new List<UnityOptimizationRecommendation>();
            
            // Frame rate optimizations
            if (analysis.AverageFrameRate < analysis.TargetFrameRate * 0.9)
            {
                recommendations.Add(new UnityOptimizationRecommendation
                {
                    Type = UnityOptimizationType.FrameRate,
                    Priority = OptimizationPriority.High,
                    Description = "Frame rate below target - optimize rendering and iteration patterns",
                    SpecificActions = new[]
                    {
                        "Use for-loops instead of foreach in Update methods",
                        "Cache component references",
                        "Reduce draw calls through batching",
                        "Optimize texture sizes for target platform"
                    },
                    EstimatedImprovement = 0.25,
                    TargetPlatforms = analysis.PlatformProfile.GetOptimizationTargets()
                });
            }
            
            // Memory optimizations
            if (analysis.GcAllocationsPerSecond > analysis.AcceptableGCAllocationRate)
            {
                recommendations.Add(new UnityOptimizationRecommendation
                {
                    Type = UnityOptimizationType.Memory,
                    Priority = OptimizationPriority.High,
                    Description = "High garbage collection allocations detected",
                    SpecificActions = new[]
                    {
                        "Use object pooling for frequently instantiated objects",
                        "Avoid string concatenation in Update methods",
                        "Use StringBuilder for string operations",
                        "Cache array allocations outside loops"
                    },
                    EstimatedImprovement = 0.35
                });
            }
            
            // Platform-specific optimizations
            if (analysis.PlatformProfile.IsMobile)
            {
                recommendations.AddRange(GetMobileOptimizations(analysis));
            }
            
            // Rendering optimizations
            if (analysis.AverageGpuTime > analysis.AverageCpuTime * 1.5)
            {
                recommendations.Add(new UnityOptimizationRecommendation
                {
                    Type = UnityOptimizationType.Rendering,
                    Priority = OptimizationPriority.Medium,
                    Description = "GPU bottleneck detected - optimize rendering pipeline",
                    SpecificActions = new[]
                    {
                        "Reduce texture resolution",
                        "Use texture atlasing",
                        "Implement LOD groups",
                        "Optimize shader complexity"
                    },
                    EstimatedImprovement = 0.2
                });
            }
            
            return recommendations;
        }
        
        public async Task HandlePerformanceSpikeAsync(UnityFrameData frameData, UnityProfilingSession session)
        {
            _logger.LogWarning("Performance spike detected: Frame time {FrameTime}ms, Target: {TargetTime}ms", 
                frameData.FrameTime, session.Configuration.TargetFrameTime.TotalMilliseconds);
            
            // Identify the cause of performance spike
            var spikeAnalysis = await AnalyzePerformanceSpike(frameData);
            
            // Trigger real-time adaptation if possible
            if (spikeAnalysis.CanAdaptInRealTime)
            {
                await _adaptationEngine.TriggerAdaptationAsync(new AdaptationContext
                {
                    Trigger = AdaptationTrigger.UnityPerformanceSpike,
                    Priority = AdaptationPriority.High,
                    Context = new Dictionary<string, object>
                    {
                        ["FrameData"] = frameData,
                        ["SpikeAnalysis"] = spikeAnalysis,
                        ["UnitySpecific"] = true
                    }
                });
            }
            
            // Log performance spike for analysis
            await LogPerformanceSpike(frameData, spikeAnalysis);
        }
        
        private async Task<UnityPerformanceAnalysis> AnalyzePerformanceData(List<UnityFrameData> frameData)
        {
            if (!frameData.Any())
            {
                return new UnityPerformanceAnalysis();
            }
            
            var analysis = new UnityPerformanceAnalysis
            {
                AverageFrameRate = frameData.Average(f => f.FrameRate),
                MinFrameRate = frameData.Min(f => f.FrameRate),
                MaxFrameRate = frameData.Max(f => f.FrameRate),
                TargetFrameRate = 60.0, // Default target
                AverageFrameTime = frameData.Average(f => f.FrameTime),
                AverageCpuTime = frameData.Average(f => f.CpuTime),
                AverageGpuTime = frameData.Average(f => f.GpuTime),
                AverageMemoryUsage = (long)frameData.Average(f => f.MemoryUsage),
                GcAllocationsPerSecond = frameData.Average(f => f.GarbageCollectionTime),
                AcceptableGCAllocationRate = 0.1, // 10% of frame time
                PlatformProfile = new PlatformProfile { IsMobile = true }, // Default to mobile
                Issues = await IdentifyPerformanceIssues(frameData)
            };
            
            return analysis;
        }
        
        private async Task<IEnumerable<PerformanceIssue>> IdentifyPerformanceIssues(List<UnityFrameData> frameData)
        {
            var issues = new List<PerformanceIssue>();
            
            // Frame rate issues
            var lowFrameRateFrames = frameData.Where(f => f.FrameRate < 30).ToList();
            if (lowFrameRateFrames.Any())
            {
                issues.Add(new PerformanceIssue
                {
                    Type = "Low Frame Rate",
                    Description = $"{lowFrameRateFrames.Count} frames below 30 FPS",
                    Severity = PerformanceIssueSeverity.High,
                    EstimatedImpact = 0.3,
                    Recommendations = new[] { "Optimize rendering pipeline", "Reduce draw calls", "Use LOD groups" }
                });
            }
            
            // Memory issues
            var highMemoryFrames = frameData.Where(f => f.MemoryUsage > 1024 * 1024 * 1024).ToList(); // > 1GB
            if (highMemoryFrames.Any())
            {
                issues.Add(new PerformanceIssue
                {
                    Type = "High Memory Usage",
                    Description = $"{highMemoryFrames.Count} frames with memory usage > 1GB",
                    Severity = PerformanceIssueSeverity.Medium,
                    EstimatedImpact = 0.2,
                    Recommendations = new[] { "Optimize texture sizes", "Use object pooling", "Reduce asset quality" }
                });
            }
            
            // GC issues
            var highGCFrames = frameData.Where(f => f.GarbageCollectionTime > 5).ToList(); // > 5ms GC
            if (highGCFrames.Any())
            {
                issues.Add(new PerformanceIssue
                {
                    Type = "High Garbage Collection",
                    Description = $"{highGCFrames.Count} frames with GC time > 5ms",
                    Severity = PerformanceIssueSeverity.High,
                    EstimatedImpact = 0.4,
                    Recommendations = new[] { "Avoid allocations in Update", "Use object pooling", "Cache frequently used objects" }
                });
            }
            
            return issues;
        }
        
        private async Task<PerformanceSpikeAnalysis> AnalyzePerformanceSpike(UnityFrameData frameData)
        {
            var analysis = new PerformanceSpikeAnalysis
            {
                FrameTime = frameData.FrameTime,
                CpuTime = frameData.CpuTime,
                GpuTime = frameData.GpuTime,
                MemoryUsage = frameData.MemoryUsage,
                GarbageCollectionTime = frameData.GarbageCollectionTime,
                DrawCalls = frameData.DrawCalls,
                CanAdaptInRealTime = false
            };
            
            // Determine if we can adapt in real-time
            if (frameData.CpuTime > frameData.GpuTime * 1.5)
            {
                analysis.PrimaryCause = "CPU Bottleneck";
                analysis.CanAdaptInRealTime = true;
                analysis.AdaptationActions = new[] { "Reduce script complexity", "Cache component references", "Optimize iteration patterns" };
            }
            else if (frameData.GpuTime > frameData.CpuTime * 1.5)
            {
                analysis.PrimaryCause = "GPU Bottleneck";
                analysis.CanAdaptInRealTime = false; // GPU optimizations typically require build changes
                analysis.AdaptationActions = new[] { "Reduce texture quality", "Use LOD groups", "Optimize shaders" };
            }
            else if (frameData.GarbageCollectionTime > 5)
            {
                analysis.PrimaryCause = "Garbage Collection";
                analysis.CanAdaptInRealTime = true;
                analysis.AdaptationActions = new[] { "Enable object pooling", "Reduce allocations", "Cache objects" };
            }
            else
            {
                analysis.PrimaryCause = "Unknown";
                analysis.CanAdaptInRealTime = false;
            }
            
            return analysis;
        }
        
        private IEnumerable<UnityOptimizationRecommendation> GetMobileOptimizations(UnityPerformanceAnalysis analysis)
        {
            var recommendations = new List<UnityOptimizationRecommendation>();
            
            // Mobile-specific frame rate optimizations
            if (analysis.AverageFrameRate < 30)
            {
                recommendations.Add(new UnityOptimizationRecommendation
                {
                    Type = UnityOptimizationType.FrameRate,
                    Priority = OptimizationPriority.Critical,
                    Description = "Mobile frame rate optimization required",
                    SpecificActions = new[]
                    {
                        "Use IL2CPP scripting backend",
                        "Enable GPU instancing",
                        "Reduce texture resolution to 1024x1024 max",
                        "Use compressed texture formats (ASTC, ETC2)",
                        "Implement aggressive LOD system"
                    },
                    EstimatedImprovement = 0.4,
                    TargetPlatforms = new[] { UnityBuildTarget.Android, UnityBuildTarget.iOS }
                });
            }
            
            // Mobile memory optimizations
            if (analysis.AverageMemoryUsage > 512 * 1024 * 1024) // > 512MB
            {
                recommendations.Add(new UnityOptimizationRecommendation
                {
                    Type = UnityOptimizationType.Memory,
                    Priority = OptimizationPriority.High,
                    Description = "Mobile memory optimization required",
                    SpecificActions = new[]
                    {
                        "Reduce texture memory usage",
                        "Use texture streaming",
                        "Implement asset unloading",
                        "Optimize audio compression",
                        "Use texture atlasing"
                    },
                    EstimatedImprovement = 0.3,
                    TargetPlatforms = new[] { UnityBuildTarget.Android, UnityBuildTarget.iOS }
                });
            }
            
            return recommendations;
        }
        
        private async Task LogPerformanceSpike(UnityFrameData frameData, PerformanceSpikeAnalysis spikeAnalysis)
        {
            _logger.LogWarning("Performance spike logged: {Cause} at {Timestamp}, Frame time: {FrameTime}ms, " +
                "CPU: {CpuTime}ms, GPU: {GpuTime}ms, Memory: {MemoryUsage}MB, GC: {GcTime}ms",
                spikeAnalysis.PrimaryCause,
                frameData.Timestamp,
                frameData.FrameTime,
                frameData.CpuTime,
                frameData.GpuTime,
                frameData.MemoryUsage / 1024 / 1024,
                frameData.GarbageCollectionTime);
        }
    }
    
    /// <summary>
    /// Unity profiler API interface
    /// </summary>
    public interface IUnityProfilerAPI
    {
        Task StartProfilingAsync(UnityProfilingConfiguration configuration);
        Task StopProfilingAsync();
        Task<UnityFrameData> GetCurrentFrameDataAsync();
    }
    
    /// <summary>
    /// Performance data collector interface
    /// </summary>
    public interface IPerformanceDataCollector
    {
        Task CollectFrameDataAsync(UnityFrameData frameData);
        Task<IEnumerable<UnityFrameData>> GetHistoricalDataAsync(TimeSpan timeRange);
    }
    
    /// <summary>
    /// Performance spike analysis
    /// </summary>
    public class PerformanceSpikeAnalysis
    {
        public double FrameTime { get; set; }
        public double CpuTime { get; set; }
        public double GpuTime { get; set; }
        public long MemoryUsage { get; set; }
        public double GarbageCollectionTime { get; set; }
        public int DrawCalls { get; set; }
        public string PrimaryCause { get; set; } = string.Empty;
        public bool CanAdaptInRealTime { get; set; }
        public IEnumerable<string> AdaptationActions { get; set; } = new List<string>();
    }
}
