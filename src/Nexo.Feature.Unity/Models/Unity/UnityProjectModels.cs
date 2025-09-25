using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Unity.Models.Unity
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
    /// Represents Unity project settings
    /// </summary>
    public class ProjectSettings
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BundleIdentifier { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public string TargetPlatform { get; set; } = string.Empty;
        public string ScriptingBackend { get; set; } = string.Empty;
        public string ApiCompatibilityLevel { get; set; } = string.Empty;
        public bool StripEngineCode { get; set; }
        public bool ManagedStrippingLevel { get; set; }
        public string ColorSpace { get; set; } = string.Empty;
        public string GraphicsAPI { get; set; } = string.Empty;
        public string RenderingPath { get; set; } = string.Empty;
        public string QualityLevel { get; set; } = string.Empty;
        public string PhysicsEngine { get; set; } = string.Empty;
        public string AudioSystem { get; set; } = string.Empty;
        public string InputSystem { get; set; } = string.Empty;
        public string XRSupport { get; set; } = string.Empty;
        public string VRSupport { get; set; } = string.Empty;
        public string ARSupport { get; set; } = string.Empty;
        public string PlatformSettings { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents Unity gameplay data
    /// </summary>
    public class GameplayData
    {
        public string GameplayType { get; set; } = string.Empty;
        public string GameplayMechanics { get; set; } = string.Empty;
        public string GameplayFeatures { get; set; } = string.Empty;
        public string GameplaySystems { get; set; } = string.Empty;
        public string GameplayUI { get; set; } = string.Empty;
        public string GameplayAudio { get; set; } = string.Empty;
        public string GameplayGraphics { get; set; } = string.Empty;
        public string GameplayPerformance { get; set; } = string.Empty;
        public string GameplayOptimization { get; set; } = string.Empty;
        public string GameplayTesting { get; set; } = string.Empty;
    }
}
