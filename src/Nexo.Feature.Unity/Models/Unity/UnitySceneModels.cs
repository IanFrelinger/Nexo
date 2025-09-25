using System;
using System.Collections.Generic;

namespace Nexo.Feature.Unity.Models.Unity
{
    /// <summary>
    /// Represents Unity scene analysis
    /// </summary>
    public class UnitySceneAnalysis
    {
        public IEnumerable<UnityScene> Scenes { get; set; } = new List<UnityScene>();
        public IEnumerable<GameObjectAnalysis> GameObjectAnalyses { get; set; } = new List<GameObjectAnalysis>();
        public IEnumerable<ComponentAnalysis> ComponentAnalyses { get; set; } = new List<ComponentAnalysis>();
        public SceneOptimizationAnalysis OptimizationAnalysis { get; set; } = new();
        public ScenePerformanceAnalysis PerformanceAnalysis { get; set; } = new();
    }

    /// <summary>
    /// Represents a Unity scene
    /// </summary>
    public class UnityScene
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsMainScene { get; set; }
        public bool IsInBuildSettings { get; set; }
        public int BuildIndex { get; set; }
        public IEnumerable<UnityGameObject> GameObjects { get; set; } = new List<UnityGameObject>();
        public IEnumerable<UnityLight> Lights { get; set; } = new List<UnityLight>();
        public IEnumerable<UnityCamera> Cameras { get; set; } = new List<UnityCamera>();
        public IEnumerable<UnityRenderer> Renderers { get; set; } = new List<UnityRenderer>();
        public SceneSettings SceneSettings { get; set; } = new();
        public long SceneSize { get; set; }
        public DateTime LastModified { get; set; }
        public int GameObjectCount { get; set; }
        public int ComponentCount { get; set; }
        public int LightCount { get; set; }
        public int CameraCount { get; set; }
        public int RendererCount { get; set; }
    }

    /// <summary>
    /// Represents a Unity GameObject
    /// </summary>
    public class UnityGameObject
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Layer { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsStatic { get; set; }
        public IEnumerable<UnityComponent> Components { get; set; } = new List<UnityComponent>();
        public IEnumerable<UnityGameObject> Children { get; set; } = new List<UnityGameObject>();
        public UnityGameObject? Parent { get; set; }
        public UnityTransform Transform { get; set; } = new();
        public string PrefabSource { get; set; } = string.Empty;
        public bool IsPrefabInstance { get; set; }
        public bool IsPrefabVariant { get; set; }
        public bool HasMissingScript { get; set; }
        public int ComponentCount { get; set; }
        public int ChildCount { get; set; }
        public int HierarchyDepth { get; set; }
    }

    /// <summary>
    /// Represents a Unity Component
    /// </summary>
    public class UnityComponent
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsScript { get; set; }
        public string ScriptPath { get; set; } = string.Empty;
        public IEnumerable<string> SerializedFields { get; set; } = new List<string>();
        public IEnumerable<string> PublicFields { get; set; } = new List<string>();
        public IEnumerable<string> ReferencedAssets { get; set; } = new List<string>();
        public bool HasMissingReferences { get; set; }
        public int MissingReferenceCount { get; set; }
        public string ComponentCategory { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; }
        public bool IsCustomScript { get; set; }
        public bool IsThirdParty { get; set; }
    }

    /// <summary>
    /// Represents a Unity Transform
    /// </summary>
    public class UnityTransform
    {
        public Vector3 Position { get; set; } = new();
        public Vector3 Rotation { get; set; } = new();
        public Vector3 Scale { get; set; } = new();
        public Vector3 LocalPosition { get; set; } = new();
        public Vector3 LocalRotation { get; set; } = new();
        public Vector3 LocalScale { get; set; } = new();
        public bool HasNonUniformScale { get; set; }
        public bool HasZeroScale { get; set; }
        public bool HasNegativeScale { get; set; }
        public bool IsIdentity { get; set; }
        public double DistanceFromOrigin { get; set; }
        public string TransformType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a Unity Light
    /// </summary>
    public class UnityLight
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float Intensity { get; set; }
        public Color Color { get; set; } = new();
        public float Range { get; set; }
        public float SpotAngle { get; set; }
        public string ShadowType { get; set; } = string.Empty;
        public float ShadowStrength { get; set; }
        public string RenderMode { get; set; } = string.Empty;
        public string CullingMask { get; set; } = string.Empty;
        public bool IsRealtime { get; set; }
        public bool IsBaked { get; set; }
        public bool IsMixed { get; set; }
        public bool CastsShadows { get; set; }
        public string LightmapBakeType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a Unity Camera
    /// </summary>
    public class UnityCamera
    {
        public string Name { get; set; } = string.Empty;
        public string ClearFlags { get; set; } = string.Empty;
        public Color BackgroundColor { get; set; } = new();
        public string CullingMask { get; set; } = string.Empty;
        public string Projection { get; set; } = string.Empty;
        public float FieldOfView { get; set; }
        public float OrthographicSize { get; set; }
        public float NearClipPlane { get; set; }
        public float FarClipPlane { get; set; }
        public int Depth { get; set; }
        public string RenderingPath { get; set; } = string.Empty;
        public string TargetTexture { get; set; } = string.Empty;
        public bool UseOcclusion { get; set; }
        public bool AllowHDR { get; set; }
        public bool AllowMSAA { get; set; }
        public bool AllowDynamicResolution { get; set; }
    }

    /// <summary>
    /// Represents a Unity Renderer
    /// </summary>
    public class UnityRenderer
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public IEnumerable<string> Materials { get; set; } = new List<string>();
        public bool CastShadows { get; set; }
        public bool ReceiveShadows { get; set; }
        public string LightProbeUsage { get; set; } = string.Empty;
        public string ReflectionProbeUsage { get; set; } = string.Empty;
        public string SortingLayer { get; set; } = string.Empty;
        public int SortingOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsStatic { get; set; }
        public string MotionVectorGenerationMode { get; set; } = string.Empty;
        public int MaterialCount { get; set; }
        public int SubMeshCount { get; set; }
        public int VertexCount { get; set; }
        public int TriangleCount { get; set; }
    }

    /// <summary>
    /// Represents scene settings
    /// </summary>
    public class SceneSettings
    {
        public string LightingSettings { get; set; } = string.Empty;
        public string RenderSettings { get; set; } = string.Empty;
        public string FogSettings { get; set; } = string.Empty;
        public string AmbientLighting { get; set; } = string.Empty;
        public string Skybox { get; set; } = string.Empty;
        public bool HasLightmaps { get; set; }
        public bool HasReflectionProbes { get; set; }
        public bool HasLightProbes { get; set; }
        public bool HasOcclusionCulling { get; set; }
        public string OcclusionCullingSettings { get; set; } = string.Empty;
        public string NavigationSettings { get; set; } = string.Empty;
        public bool HasNavMesh { get; set; }
        public string PhysicsSettings { get; set; } = string.Empty;
        public string Audio3DSettings { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents GameObject analysis
    /// </summary>
    public class GameObjectAnalysis
    {
        public string GameObjectName { get; set; } = string.Empty;
        public IEnumerable<string> OptimizationOpportunities { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceIssues { get; set; } = new List<string>();
        public IEnumerable<string> MemoryIssues { get; set; } = new List<string>();
        public IEnumerable<string> QualityIssues { get; set; } = new List<string>();
        public double PerformanceScore { get; set; }
        public double OptimizationPotential { get; set; }
        public string AnalysisCategory { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int ComplexityScore { get; set; }
        public bool RequiresAttention { get; set; }
        public DateTime AnalysisTime { get; set; }
    }

    /// <summary>
    /// Represents Component analysis
    /// </summary>
    public class ComponentAnalysis
    {
        public string ComponentType { get; set; } = string.Empty;
        public string GameObjectName { get; set; } = string.Empty;
        public IEnumerable<string> OptimizationOpportunities { get; set; } = new List<string>();
        public IEnumerable<string> ConfigurationIssues { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceImpacts { get; set; } = new List<string>();
        public double EfficiencyScore { get; set; }
        public string UsagePattern { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public bool IsOptimal { get; set; }
        public bool HasIssues { get; set; }
        public string AnalysisResult { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; }
    }

    /// <summary>
    /// Represents scene optimization analysis
    /// </summary>
    public class SceneOptimizationAnalysis
    {
        public IEnumerable<string> OptimizationOpportunities { get; set; } = new List<string>();
        public IEnumerable<string> DrawCallOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> LightingOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> TextureOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> GeometryOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> ScriptOptimizations { get; set; } = new List<string>();
        public double OptimizationPotential { get; set; }
        public int OptimizableObjectCount { get; set; }
        public string OptimizationPriority { get; set; } = string.Empty;
        public long PotentialMemorySavings { get; set; }
        public double PotentialPerformanceGain { get; set; }
        public IEnumerable<string> PlatformSpecificOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> QualityOptimizations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents scene performance analysis
    /// </summary>
    public class ScenePerformanceAnalysis
    {
        public int DrawCallCount { get; set; }
        public int BatchCount { get; set; }
        public long TriangleCount { get; set; }
        public long VertexCount { get; set; }
        public int LightCount { get; set; }
        public int ShadowCastingLightCount { get; set; }
        public long TextureMemoryUsage { get; set; }
        public long MeshMemoryUsage { get; set; }
        public long AudioMemoryUsage { get; set; }
        public double EstimatedFrameTime { get; set; }
        public double EstimatedFPS { get; set; }
        public string PerformanceRating { get; set; } = string.Empty;
        public IEnumerable<string> PerformanceBottlenecks { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceRecommendations { get; set; } = new List<string>();
        public string TargetPlatform { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a Vector3
    /// </summary>
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    /// <summary>
    /// Represents a Color
    /// </summary>
    public class Color
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }
    }
}
