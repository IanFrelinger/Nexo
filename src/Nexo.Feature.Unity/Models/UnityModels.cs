using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Unity.Models
{
    /// <summary>
    /// Represents a complete Unity project analysis
    /// </summary>
    public class UnityProjectAnalysis
    {
        public string ProjectPath { get; set; } = string.Empty;
        public DateTime AnalysisTimestamp { get; set; }
        public UnityProjectStructure ProjectStructure { get; set; } = new();
        public UnityScriptAnalysis ScriptAnalysis { get; set; } = new();
        public UnityAssetAnalysis AssetAnalysis { get; set; } = new();
        public UnitySceneAnalysis SceneAnalysis { get; set; } = new();
        public IEnumerable<PerformanceRecommendation> PerformanceRecommendations { get; set; } = new List<PerformanceRecommendation>();
        public IEnumerable<IterationOptimizationOpportunity> IterationOptimizations { get; set; } = new List<IterationOptimizationOpportunity>();
        public GameplayData? GameplayData { get; set; }
    }
    
    /// <summary>
    /// Represents Unity project structure analysis
    /// </summary>
    public class UnityProjectStructure
    {
        public string UnityVersion { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public IEnumerable<string> Scripts { get; set; } = new List<string>();
        public IEnumerable<string> Scenes { get; set; } = new List<string>();
        public IEnumerable<string> Assets { get; set; } = new List<string>();
        public IEnumerable<string> Packages { get; set; } = new List<string>();
        public ProjectSettings ProjectSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Represents Unity script analysis
    /// </summary>
    public class UnityScriptAnalysis
    {
        public IEnumerable<UnityScript> Scripts { get; set; } = new List<UnityScript>();
        public IEnumerable<PerformanceIssue> PerformanceIssues { get; set; } = new List<PerformanceIssue>();
        public IEnumerable<CodeQualityIssue> CodeQualityIssues { get; set; } = new List<CodeQualityIssue>();
        public IEnumerable<IterationPattern> IterationPatterns { get; set; } = new List<IterationPattern>();
    }
    
    /// <summary>
    /// Represents a Unity script
    /// </summary>
    public class UnityScript
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public IEnumerable<UnityScriptMethod> Methods { get; set; } = new List<UnityScriptMethod>();
        public IEnumerable<UnityScriptField> Fields { get; set; } = new List<UnityScriptField>();
        public IEnumerable<UnityScriptProperty> Properties { get; set; } = new List<UnityScriptProperty>();
        public bool InheritsFromMonoBehaviour { get; set; }
        public bool InheritsFromScriptableObject { get; set; }
        public int LinesOfCode { get; set; }
        public int CyclomaticComplexity { get; set; }
    }
    
    /// <summary>
    /// Represents a Unity script method
    /// </summary>
    public class UnityScriptMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public IEnumerable<string> Parameters { get; set; } = new List<string>();
        public string Body { get; set; } = string.Empty;
        public bool IsUnityLifecycleMethod { get; set; }
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public int LineNumber { get; set; }
        public int LinesOfCode { get; set; }
    }
    
    /// <summary>
    /// Represents a Unity script field
    /// </summary>
    public class UnityScriptField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public bool HasSerializeFieldAttribute { get; set; }
        public int LineNumber { get; set; }
    }
    
    /// <summary>
    /// Represents a Unity script property
    /// </summary>
    public class UnityScriptProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public bool IsPublic { get; set; }
        public int LineNumber { get; set; }
    }
    
    /// <summary>
    /// Represents an iteration pattern found in Unity scripts
    /// </summary>
    public class IterationPattern
    {
        public string Code { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public bool UsesGetComponent { get; set; }
        public bool UsesForeach { get; set; }
        public bool UsesLINQ { get; set; }
        public string CollectionType { get; set; } = string.Empty;
        public int IterationCount { get; set; }
        public double EstimatedPerformanceImpact { get; set; }
    }
    
    /// <summary>
    /// Represents an iteration optimization opportunity
    /// </summary>
    public class IterationOptimizationOpportunity
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CurrentPattern { get; set; } = string.Empty;
        public string OptimizedPattern { get; set; } = string.Empty;
        public double EstimatedPerformanceGain { get; set; }
        public IEnumerable<string> UnitySpecificOptimization { get; set; } = new List<string>();
        public OptimizationPriority Priority { get; set; }
    }
    
    /// <summary>
    /// Represents Unity asset analysis
    /// </summary>
    public class UnityAssetAnalysis
    {
        public IEnumerable<UnityAsset> Assets { get; set; } = new List<UnityAsset>();
        public IEnumerable<TextureOptimizationOpportunity> TextureOptimizations { get; set; } = new List<TextureOptimizationOpportunity>();
        public IEnumerable<AudioOptimizationOpportunity> AudioOptimizations { get; set; } = new List<AudioOptimizationOpportunity>();
        public long TotalAssetSize { get; set; }
        public long OptimizableAssetSize { get; set; }
    }
    
    /// <summary>
    /// Represents a Unity asset
    /// </summary>
    public class UnityAsset
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ImportSettings { get; set; } = string.Empty;
        public bool IsOptimized { get; set; }
        public IEnumerable<string> OptimizationOpportunities { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents a texture optimization opportunity
    /// </summary>
    public class TextureOptimizationOpportunity
    {
        public string AssetPath { get; set; } = string.Empty;
        public int CurrentWidth { get; set; }
        public int CurrentHeight { get; set; }
        public int RecommendedWidth { get; set; }
        public int RecommendedHeight { get; set; }
        public string CurrentFormat { get; set; } = string.Empty;
        public string RecommendedFormat { get; set; } = string.Empty;
        public long CurrentSize { get; set; }
        public long OptimizedSize { get; set; }
        public double SizeReduction { get; set; }
    }
    
    /// <summary>
    /// Represents an audio optimization opportunity
    /// </summary>
    public class AudioOptimizationOpportunity
    {
        public string AssetPath { get; set; } = string.Empty;
        public string CurrentFormat { get; set; } = string.Empty;
        public string RecommendedFormat { get; set; } = string.Empty;
        public int CurrentQuality { get; set; }
        public int RecommendedQuality { get; set; }
        public long CurrentSize { get; set; }
        public long OptimizedSize { get; set; }
        public double SizeReduction { get; set; }
    }
    
    /// <summary>
    /// Represents Unity scene analysis
    /// </summary>
    public class UnitySceneAnalysis
    {
        public IEnumerable<UnityScene> Scenes { get; set; } = new List<UnityScene>();
        public IEnumerable<RenderingOptimizationOpportunity> RenderingOptimizations { get; set; } = new List<RenderingOptimizationOpportunity>();
        public int TotalGameObjects { get; set; }
        public int TotalComponents { get; set; }
        public int TotalDrawCalls { get; set; }
        public int OptimizableDrawCalls { get; set; }
    }
    
    /// <summary>
    /// Represents a Unity scene
    /// </summary>
    public class UnityScene
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GameObjects { get; set; }
        public int Components { get; set; }
        public int DrawCalls { get; set; }
        public int Triangles { get; set; }
        public int Vertices { get; set; }
        public IEnumerable<string> OptimizationOpportunities { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents a rendering optimization opportunity
    /// </summary>
    public class RenderingOptimizationOpportunity
    {
        public string ScenePath { get; set; } = string.Empty;
        public string GameObjectName { get; set; } = string.Empty;
        public string OptimizationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double EstimatedPerformanceGain { get; set; }
        public OptimizationPriority Priority { get; set; }
    }
    
    /// <summary>
    /// Represents Unity performance profiling session
    /// </summary>
    public class UnityProfilingSession
    {
        public string SessionId { get; set; } = string.Empty;
        public UnityProfilingConfiguration Configuration { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public ProfilingSessionStatus Status { get; set; }
    }
    
    /// <summary>
    /// Represents Unity profiling configuration
    /// </summary>
    public class UnityProfilingConfiguration
    {
        public TimeSpan TargetFrameTime { get; set; } = TimeSpan.FromMilliseconds(16.67); // 60 FPS
        public TimeSpan SamplingInterval { get; set; } = TimeSpan.FromMilliseconds(100);
        public bool EnableMemoryProfiling { get; set; } = true;
        public bool EnableRenderingProfiling { get; set; } = true;
        public bool EnableScriptProfiling { get; set; } = true;
        public bool EnablePhysicsProfiling { get; set; } = true;
    }
    
    /// <summary>
    /// Represents Unity performance profile
    /// </summary>
    public class UnityPerformanceProfile
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<UnityFrameData> FrameData { get; set; } = new();
        public UnityPerformanceAnalysis Analysis { get; set; } = new();
        public IEnumerable<UnityOptimizationRecommendation> OptimizationRecommendations { get; set; } = new List<UnityOptimizationRecommendation>();
    }
    
    /// <summary>
    /// Represents Unity frame data
    /// </summary>
    public class UnityFrameData
    {
        public DateTime Timestamp { get; set; }
        public double FrameRate { get; set; }
        public double FrameTime { get; set; }
        public double CpuTime { get; set; }
        public double GpuTime { get; set; }
        public long MemoryUsage { get; set; }
        public double GarbageCollectionTime { get; set; }
        public int DrawCalls { get; set; }
        public int BatchedDrawCalls { get; set; }
        public int Triangles { get; set; }
        public int Vertices { get; set; }
        public int ActivePlayerCount { get; set; }
        public string CurrentGameState { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents Unity performance analysis
    /// </summary>
    public class UnityPerformanceAnalysis
    {
        public double AverageFrameRate { get; set; }
        public double MinFrameRate { get; set; }
        public double MaxFrameRate { get; set; }
        public double TargetFrameRate { get; set; }
        public double AverageFrameTime { get; set; }
        public double AverageCpuTime { get; set; }
        public double AverageGpuTime { get; set; }
        public long AverageMemoryUsage { get; set; }
        public double GcAllocationsPerSecond { get; set; }
        public double AcceptableGCAllocationRate { get; set; }
        public PlatformProfile PlatformProfile { get; set; } = new();
        public IEnumerable<PerformanceIssue> Issues { get; set; } = new List<PerformanceIssue>();
    }
    
    /// <summary>
    /// Represents a Unity optimization recommendation
    /// </summary>
    public class UnityOptimizationRecommendation
    {
        public UnityOptimizationType Type { get; set; }
        public OptimizationPriority Priority { get; set; }
        public string Description { get; set; } = string.Empty;
        public IEnumerable<string> SpecificActions { get; set; } = new List<string>();
        public double EstimatedImprovement { get; set; }
        public IEnumerable<UnityBuildTarget> TargetPlatforms { get; set; } = new List<UnityBuildTarget>();
    }
    
    /// <summary>
    /// Represents Unity build request
    /// </summary>
    public class UnityBuildRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public IEnumerable<UnityBuildTarget> TargetPlatforms { get; set; } = new List<UnityBuildTarget>();
        public UnityBuildSettings BuildSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Represents Unity build settings
    /// </summary>
    public class UnityBuildSettings
    {
        public ScriptingImplementation ScriptingBackend { get; set; }
        public ApiCompatibilityLevel ApiCompatibilityLevel { get; set; }
        public CodeOptimization CodeOptimization { get; set; }
        public StrippingLevel StrippingLevel { get; set; }
        public TextureImporterFormat TextureCompression { get; set; }
        public int MaxTextureSize { get; set; }
        public AudioCompressionFormat AudioCompression { get; set; }
        public AndroidArchitecture AndroidArchitecture { get; set; }
        public iOSTargetDevice iOSArchitecture { get; set; }
        public bool MetalEditorSupport { get; set; }
    }
    
    /// <summary>
    /// Represents Unity build optimization
    /// </summary>
    public class UnityBuildOptimization
    {
        public UnityBuildSettings OriginalBuildSettings { get; set; } = new();
        public IEnumerable<UnityBuildTarget> TargetPlatforms { get; set; } = new List<UnityBuildTarget>();
        public Dictionary<UnityBuildTarget, PlatformBuildOptimization> PlatformOptimizations { get; set; } = new();
        public IEnumerable<AssetOptimization> AssetOptimizations { get; set; } = new List<AssetOptimization>();
        public IEnumerable<BuildSizeOptimization> BuildSizeOptimizations { get; set; } = new List<BuildSizeOptimization>();
    }
    
    /// <summary>
    /// Represents platform-specific build optimization
    /// </summary>
    public class PlatformBuildOptimization
    {
        public UnityBuildTarget Platform { get; set; }
        public UnityBuildSettings OptimizedSettings { get; set; } = new();
        public IEnumerable<string> AppliedOptimizations { get; set; } = new List<string>();
        public long EstimatedSizeReduction { get; set; }
        public double EstimatedPerformanceImprovement { get; set; }
    }
    
    /// <summary>
    /// Represents asset optimization
    /// </summary>
    public class AssetOptimization
    {
        public string AssetPath { get; set; } = string.Empty;
        public string OptimizationType { get; set; } = string.Empty;
        public long OriginalSize { get; set; }
        public long OptimizedSize { get; set; }
        public double SizeReduction { get; set; }
        public IEnumerable<UnityBuildTarget> ApplicablePlatforms { get; set; } = new List<UnityBuildTarget>();
    }
    
    /// <summary>
    /// Represents build size optimization
    /// </summary>
    public class BuildSizeOptimization
    {
        public string OptimizationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long EstimatedSizeReduction { get; set; }
        public IEnumerable<UnityBuildTarget> ApplicablePlatforms { get; set; } = new List<UnityBuildTarget>();
    }
    
    /// <summary>
    /// Represents project settings
    /// </summary>
    public class ProjectSettings
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BundleIdentifier { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public IEnumerable<string> EnabledModules { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents a performance issue
    /// </summary>
    public class PerformanceIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public PerformanceIssueSeverity Severity { get; set; }
        public double EstimatedImpact { get; set; }
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents a code quality issue
    /// </summary>
    public class CodeQualityIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public CodeQualitySeverity Severity { get; set; }
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents a performance recommendation
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double EstimatedImprovement { get; set; }
        public OptimizationPriority Priority { get; set; }
        public IEnumerable<string> Actions { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents gameplay data
    /// </summary>
    public class GameplayData
    {
        public string GameType { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public double AverageWinRate { get; set; }
        public double SkillVariance { get; set; }
        public IEnumerable<string> PopularStrategies { get; set; } = new List<string>();
        public IEnumerable<string> UnderusedStrategies { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents platform profile
    /// </summary>
    public class PlatformProfile
    {
        public bool IsMobile { get; set; }
        public bool IsConsole { get; set; }
        public bool IsPC { get; set; }
        public bool IsWeb { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public IEnumerable<UnityBuildTarget> GetOptimizationTargets()
        {
            var targets = new List<UnityBuildTarget>();
            if (IsMobile) targets.AddRange(new[] { UnityBuildTarget.Android, UnityBuildTarget.iOS });
            if (IsPC) targets.Add(UnityBuildTarget.StandaloneWindows64);
            if (IsWeb) targets.Add(UnityBuildTarget.WebGL);
            return targets;
        }
    }
    
    /// <summary>
    /// Represents Unity optimization evaluation
    /// </summary>
    public class UnityOptimizationEvaluation
    {
        public bool HasOptimization { get; set; }
        public string OptimizedCode { get; set; } = string.Empty;
        public IEnumerable<string> UnityOptimizations { get; set; } = new List<string>();
        public double EstimatedGain { get; set; }
    }
    
    // Enums
    public enum UnityBuildTarget
    {
        Android,
        iOS,
        WebGL,
        StandaloneWindows64,
        StandaloneOSX,
        StandaloneLinux64
    }
    
    public enum ScriptingImplementation
    {
        Mono,
        IL2CPP
    }
    
    public enum ApiCompatibilityLevel
    {
        NET_Standard_2_0,
        NET_Standard_2_1,
        NET_Framework
    }
    
    public enum CodeOptimization
    {
        Debug,
        Release
    }
    
    public enum StrippingLevel
    {
        Disabled,
        Minimal,
        Low,
        Medium,
        High
    }
    
    public enum TextureImporterFormat
    {
        ASTC_4x4,
        ASTC_6x6,
        ASTC_8x8,
        ETC2_RGBA8,
        DXT5,
        BC7
    }
    
    public enum AudioCompressionFormat
    {
        PCM,
        Vorbis,
        MP3,
        AAC
    }
    
    public enum AndroidArchitecture
    {
        ARMv7,
        ARM64,
        ARMv7AndARM64
    }
    
    public enum iOSTargetDevice
    {
        iPhoneOnly,
        iPadOnly,
        DeviceAndSimulator
    }
    
    public enum UnityOptimizationType
    {
        FrameRate,
        Memory,
        BuildSize,
        Rendering,
        Physics,
        Audio
    }
    
    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum PerformanceIssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum CodeQualitySeverity
    {
        Info,
        Warning,
        Error
    }
    
    public enum ProfilingSessionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed
    }
}
