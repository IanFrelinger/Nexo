using System;
using System.Collections.Generic;

namespace Nexo.Feature.Unity.Models.Unity
{
    /// <summary>
    /// Represents a performance recommendation
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string ImplementationGuide { get; set; } = string.Empty;
        public double EstimatedImprovement { get; set; }
        public string TargetPlatform { get; set; } = string.Empty;
        public string PerformanceMetric { get; set; } = string.Empty;
        public bool IsAutomatable { get; set; }
        public string AutomationScript { get; set; } = string.Empty;
        public string ValidationMethod { get; set; } = string.Empty;
        public DateTime RecommendationDate { get; set; }
    }

    /// <summary>
    /// Represents an iteration optimization opportunity
    /// </summary>
    public class IterationOptimizationOpportunity
    {
        public string Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CurrentApproach { get; set; } = string.Empty;
        public string OptimizedApproach { get; set; } = string.Empty;
        public double EstimatedSpeedup { get; set; }
        public string IterationPattern { get; set; } = string.Empty;
        public int EstimatedIterationCount { get; set; }
        public string DataStructure { get; set; } = string.Empty;
        public string AlgorithmComplexity { get; set; } = string.Empty;
        public string OptimizationTechnique { get; set; } = string.Empty;
        public bool RequiresRefactoring { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
        public string ValidationApproach { get; set; } = string.Empty;
        public DateTime IdentificationDate { get; set; }
    }

    /// <summary>
    /// Represents Unity performance metrics
    /// </summary>
    public class UnityPerformanceMetrics
    {
        public float FrameRate { get; set; }
        public float FrameTime { get; set; }
        public long MemoryUsage { get; set; }
        public long TextureMemory { get; set; }
        public long MeshMemory { get; set; }
        public long AudioMemory { get; set; }
        public long ScriptMemory { get; set; }
        public int DrawCalls { get; set; }
        public int Batches { get; set; }
        public long Triangles { get; set; }
        public long Vertices { get; set; }
        public float CPUUsage { get; set; }
        public float GPUUsage { get; set; }
        public float BatteryUsage { get; set; }
        public float ThermalState { get; set; }
    }

    /// <summary>
    /// Represents Unity performance analysis
    /// </summary>
    public class UnityPerformanceAnalysis
    {
        public UnityPerformanceMetrics Metrics { get; set; } = new();
        public IEnumerable<PerformanceBottleneck> Bottlenecks { get; set; } = new List<PerformanceBottleneck>();
        public IEnumerable<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
        public IEnumerable<IterationOptimizationOpportunity> IterationOptimizations { get; set; } = new List<IterationOptimizationOpportunity>();
        public PerformanceProfile Profile { get; set; } = new();
        public string TargetPlatform { get; set; } = string.Empty;
        public string PerformanceRating { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public DateTime AnalysisTime { get; set; }
    }

    /// <summary>
    /// Represents a performance bottleneck
    /// </summary>
    public class PerformanceBottleneck
    {
        public string Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public double Impact { get; set; }
        public string Cause { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public string DetectionMethod { get; set; } = string.Empty;
        public string MitigationStrategy { get; set; } = string.Empty;
        public string PreventionStrategy { get; set; } = string.Empty;
        public DateTime DetectedTime { get; set; }
    }

    /// <summary>
    /// Represents a performance profile
    /// </summary>
    public class PerformanceProfile
    {
        public string ProfileName { get; set; } = string.Empty;
        public string TargetPlatform { get; set; } = string.Empty;
        public string TargetQuality { get; set; } = string.Empty;
        public float TargetFrameRate { get; set; }
        public long TargetMemoryUsage { get; set; }
        public int TargetDrawCalls { get; set; }
        public string OptimizationStrategy { get; set; } = string.Empty;
        public IEnumerable<string> EnabledOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> DisabledFeatures { get; set; } = new List<string>();
        public string QualitySettings { get; set; } = string.Empty;
        public string RenderingSettings { get; set; } = string.Empty;
        public string PhysicsSettings { get; set; } = string.Empty;
        public string AudioSettings { get; set; } = string.Empty;
        public DateTime ProfileCreated { get; set; }
    }

    /// <summary>
    /// Represents Unity optimization settings
    /// </summary>
    public class UnityOptimizationSettings
    {
        public bool EnableBatching { get; set; }
        public bool EnableInstancing { get; set; }
        public bool EnableOcclusion { get; set; }
        public bool EnableLOD { get; set; }
        public bool EnableCulling { get; set; }
        public string TextureCompression { get; set; } = string.Empty;
        public string AudioCompression { get; set; } = string.Empty;
        public string MeshCompression { get; set; } = string.Empty;
        public string AnimationCompression { get; set; } = string.Empty;
        public string ShaderVariantStripping { get; set; } = string.Empty;
        public string CodeStripping { get; set; } = string.Empty;
        public string BuildOptimization { get; set; } = string.Empty;
        public IEnumerable<string> OptimizationFlags { get; set; } = new List<string>();
        public IEnumerable<string> PlatformSettings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents Unity profiling data
    /// </summary>
    public class UnityProfilingData
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public IEnumerable<ProfilingFrame> Frames { get; set; } = new List<ProfilingFrame>();
        public ProfilingSummary Summary { get; set; } = new();
        public IEnumerable<ProfilingMarker> Markers { get; set; } = new List<ProfilingMarker>();
        public string Platform { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string UnityVersion { get; set; } = string.Empty;
        public string BuildConfiguration { get; set; } = string.Empty;
        public long DataSize { get; set; }
    }

    /// <summary>
    /// Represents a profiling frame
    /// </summary>
    public class ProfilingFrame
    {
        public int FrameNumber { get; set; }
        public float FrameTime { get; set; }
        public float CPUTime { get; set; }
        public float GPUTime { get; set; }
        public float RenderTime { get; set; }
        public float ScriptTime { get; set; }
        public float PhysicsTime { get; set; }
        public float AudioTime { get; set; }
        public float UITime { get; set; }
        public long MemoryUsage { get; set; }
        public int DrawCalls { get; set; }
        public int Batches { get; set; }
        public long Triangles { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents profiling summary
    /// </summary>
    public class ProfilingSummary
    {
        public float AverageFrameTime { get; set; }
        public float MinFrameTime { get; set; }
        public float MaxFrameTime { get; set; }
        public float AverageFrameRate { get; set; }
        public float MinFrameRate { get; set; }
        public float MaxFrameRate { get; set; }
        public long AverageMemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        public int AverageDrawCalls { get; set; }
        public int PeakDrawCalls { get; set; }
        public float CPUUsagePercent { get; set; }
        public float GPUUsagePercent { get; set; }
        public int TotalFrames { get; set; }
        public int DroppedFrames { get; set; }
    }

    /// <summary>
    /// Represents a profiling marker
    /// </summary>
    public class ProfilingMarker
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public float Duration { get; set; }
        public int ThreadId { get; set; }
        public string ThreadName { get; set; } = string.Empty;
        public string CallStack { get; set; } = string.Empty;
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public string MarkerType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long MemoryAllocated { get; set; }
        public int CallCount { get; set; }
    }
}
