using System;
using System.Collections.Generic;

namespace Nexo.Feature.Unity.Models.Unity
{
    /// <summary>
    /// Represents Unity asset analysis
    /// </summary>
    public class UnityAssetAnalysis
    {
        public IEnumerable<UnityAsset> Assets { get; set; } = new List<UnityAsset>();
        public IEnumerable<TextureAsset> Textures { get; set; } = new List<TextureAsset>();
        public IEnumerable<ModelAsset> Models { get; set; } = new List<ModelAsset>();
        public IEnumerable<AudioAsset> AudioClips { get; set; } = new List<AudioAsset>();
        public IEnumerable<MaterialAsset> Materials { get; set; } = new List<MaterialAsset>();
        public IEnumerable<ShaderAsset> Shaders { get; set; } = new List<ShaderAsset>();
        public IEnumerable<AnimationAsset> Animations { get; set; } = new List<AnimationAsset>();
        public IEnumerable<PrefabAsset> Prefabs { get; set; } = new List<PrefabAsset>();
        public IEnumerable<FontAsset> Fonts { get; set; } = new List<FontAsset>();
        public IEnumerable<ScriptableObjectAsset> ScriptableObjects { get; set; } = new List<ScriptableObjectAsset>();
        public AssetUsageAnalysis UsageAnalysis { get; set; } = new();
        public AssetOptimizationAnalysis OptimizationAnalysis { get; set; } = new();
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
        public DateTime LastModified { get; set; }
        public string GUID { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public bool IsInResourcesFolder { get; set; }
        public bool IsInStreamingAssetsFolder { get; set; }
        public bool IsAddressable { get; set; }
        public string AssetBundle { get; set; } = string.Empty;
        public IEnumerable<string> Dependencies { get; set; } = new List<string>();
        public IEnumerable<string> References { get; set; } = new List<string>();
        public string ImportSettings { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a texture asset
    /// </summary>
    public class TextureAsset : UnityAsset
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Compression { get; set; } = string.Empty;
        public bool MipmapEnabled { get; set; }
        public string FilterMode { get; set; } = string.Empty;
        public string WrapMode { get; set; } = string.Empty;
        public int MaxTextureSize { get; set; }
        public string TextureType { get; set; } = string.Empty;
        public bool IsReadable { get; set; }
        public bool GenerateMipmaps { get; set; }
        public string AlphaSource { get; set; } = string.Empty;
        public bool AlphaIsTransparency { get; set; }
        public string SpriteMode { get; set; } = string.Empty;
        public int PixelsPerUnit { get; set; }
    }

    /// <summary>
    /// Represents a 3D model asset
    /// </summary>
    public class ModelAsset : UnityAsset
    {
        public int VertexCount { get; set; }
        public int TriangleCount { get; set; }
        public int MaterialCount { get; set; }
        public int AnimationCount { get; set; }
        public string ModelFormat { get; set; } = string.Empty;
        public bool ImportMaterials { get; set; }
        public bool ImportAnimation { get; set; }
        public bool ImportBlendShapes { get; set; }
        public bool ImportVisibility { get; set; }
        public bool ImportCameras { get; set; }
        public bool ImportLights { get; set; }
        public string MeshCompression { get; set; } = string.Empty;
        public bool OptimizeMesh { get; set; }
        public bool GenerateColliders { get; set; }
        public string NormalImportMode { get; set; } = string.Empty;
        public string TangentImportMode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an audio asset
    /// </summary>
    public class AudioAsset : UnityAsset
    {
        public float Duration { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public string AudioFormat { get; set; } = string.Empty;
        public string CompressionFormat { get; set; } = string.Empty;
        public float CompressionQuality { get; set; }
        public bool LoadInBackground { get; set; }
        public bool Preload { get; set; }
        public string LoadType { get; set; } = string.Empty;
        public bool Force2Mono { get; set; }
        public bool Normalize { get; set; }
        public bool LoadInMemory { get; set; }
        public string AudioClipType { get; set; } = string.Empty;
        public bool Ambisonic { get; set; }
    }

    /// <summary>
    /// Represents a material asset
    /// </summary>
    public class MaterialAsset : UnityAsset
    {
        public string ShaderName { get; set; } = string.Empty;
        public IEnumerable<string> TextureProperties { get; set; } = new List<string>();
        public IEnumerable<string> ColorProperties { get; set; } = new List<string>();
        public IEnumerable<string> FloatProperties { get; set; } = new List<string>();
        public IEnumerable<string> VectorProperties { get; set; } = new List<string>();
        public string RenderQueue { get; set; } = string.Empty;
        public string RenderType { get; set; } = string.Empty;
        public bool EnableInstancing { get; set; }
        public bool DoubleSidedGI { get; set; }
        public string MaterialType { get; set; } = string.Empty;
        public string BlendMode { get; set; } = string.Empty;
        public string CullMode { get; set; } = string.Empty;
        public string ZWrite { get; set; } = string.Empty;
        public string ZTest { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a shader asset
    /// </summary>
    public class ShaderAsset : UnityAsset
    {
        public string ShaderType { get; set; } = string.Empty;
        public IEnumerable<string> ShaderVariants { get; set; } = new List<string>();
        public IEnumerable<string> ShaderProperties { get; set; } = new List<string>();
        public IEnumerable<string> ShaderKeywords { get; set; } = new List<string>();
        public string RenderPipeline { get; set; } = string.Empty;
        public string ShaderModel { get; set; } = string.Empty;
        public bool SupportsInstancing { get; set; }
        public bool SupportsShadows { get; set; }
        public bool SupportsLighting { get; set; }
        public string ShaderLanguage { get; set; } = string.Empty;
        public string ShaderCompiler { get; set; } = string.Empty;
        public int PassCount { get; set; }
        public int PropertyCount { get; set; }
        public int KeywordCount { get; set; }
    }

    /// <summary>
    /// Represents an animation asset
    /// </summary>
    public class AnimationAsset : UnityAsset
    {
        public float Duration { get; set; }
        public int FrameRate { get; set; }
        public int KeyframeCount { get; set; }
        public IEnumerable<string> AnimatedProperties { get; set; } = new List<string>();
        public string AnimationType { get; set; } = string.Empty;
        public bool IsLooping { get; set; }
        public string WrapMode { get; set; } = string.Empty;
        public string CompressionType { get; set; } = string.Empty;
        public float CompressionRatio { get; set; }
        public bool OptimizeGameObjects { get; set; }
        public string AnimationClipType { get; set; } = string.Empty;
        public bool HasRootMotion { get; set; }
        public bool HasEvents { get; set; }
        public int EventCount { get; set; }
    }

    /// <summary>
    /// Represents a prefab asset
    /// </summary>
    public class PrefabAsset : UnityAsset
    {
        public int ComponentCount { get; set; }
        public int ChildCount { get; set; }
        public IEnumerable<string> ComponentTypes { get; set; } = new List<string>();
        public IEnumerable<string> ScriptTypes { get; set; } = new List<string>();
        public string PrefabType { get; set; } = string.Empty;
        public bool IsPrefabVariant { get; set; }
        public string BasePrefabPath { get; set; } = string.Empty;
        public bool HasMissingScripts { get; set; }
        public bool HasMissingReferences { get; set; }
        public int MissingScriptCount { get; set; }
        public int MissingReferenceCount { get; set; }
        public string PrefabCategory { get; set; } = string.Empty;
        public bool IsNestedPrefab { get; set; }
        public int NestingLevel { get; set; }
    }

    /// <summary>
    /// Represents a font asset
    /// </summary>
    public class FontAsset : UnityAsset
    {
        public string FontType { get; set; } = string.Empty;
        public int FontSize { get; set; }
        public string FontStyle { get; set; } = string.Empty;
        public IEnumerable<string> SupportedCharacters { get; set; } = new List<string>();
        public bool IncludeFontData { get; set; }
        public string FontRenderingMode { get; set; } = string.Empty;
        public string HintingMode { get; set; } = string.Empty;
        public bool UseKerning { get; set; }
        public string CharacterSet { get; set; } = string.Empty;
        public int AtlasWidth { get; set; }
        public int AtlasHeight { get; set; }
        public string FontFormat { get; set; } = string.Empty;
        public bool IsDynamic { get; set; }
        public bool HasFallbackFonts { get; set; }
    }

    /// <summary>
    /// Represents a ScriptableObject asset
    /// </summary>
    public class ScriptableObjectAsset : UnityAsset
    {
        public string ScriptType { get; set; } = string.Empty;
        public IEnumerable<string> SerializedFields { get; set; } = new List<string>();
        public IEnumerable<string> ReferencedAssets { get; set; } = new List<string>();
        public string ScriptableObjectType { get; set; } = string.Empty;
        public bool IsEditorOnly { get; set; }
        public bool IsConfigurationData { get; set; }
        public bool IsGameData { get; set; }
        public string DataCategory { get; set; } = string.Empty;
        public int FieldCount { get; set; }
        public int ReferenceCount { get; set; }
        public string SerializationMode { get; set; } = string.Empty;
        public bool HasCircularReferences { get; set; }
        public bool IsSharedData { get; set; }
    }

    /// <summary>
    /// Represents asset usage analysis
    /// </summary>
    public class AssetUsageAnalysis
    {
        public IEnumerable<string> UnusedAssets { get; set; } = new List<string>();
        public IEnumerable<string> DuplicateAssets { get; set; } = new List<string>();
        public IEnumerable<string> LargeAssets { get; set; } = new List<string>();
        public IEnumerable<string> MissingAssets { get; set; } = new List<string>();
        public IEnumerable<string> UncompressedAssets { get; set; } = new List<string>();
        public long TotalAssetSize { get; set; }
        public long UnusedAssetSize { get; set; }
        public long DuplicateAssetSize { get; set; }
        public int AssetCount { get; set; }
        public int UnusedAssetCount { get; set; }
        public int DuplicateAssetCount { get; set; }
        public int MissingAssetCount { get; set; }
        public double AssetUtilizationRate { get; set; }
        public double StorageEfficiency { get; set; }
    }

    /// <summary>
    /// Represents asset optimization analysis
    /// </summary>
    public class AssetOptimizationAnalysis
    {
        public IEnumerable<string> OptimizationOpportunities { get; set; } = new List<string>();
        public IEnumerable<string> CompressionRecommendations { get; set; } = new List<string>();
        public IEnumerable<string> ResolutionRecommendations { get; set; } = new List<string>();
        public IEnumerable<string> FormatRecommendations { get; set; } = new List<string>();
        public long PotentialSavings { get; set; }
        public double OptimizationPotential { get; set; }
        public int OptimizableAssetCount { get; set; }
        public string OptimizationPriority { get; set; } = string.Empty;
        public IEnumerable<string> PlatformSpecificOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> QualityOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> MemoryOptimizations { get; set; } = new List<string>();
        public IEnumerable<string> LoadTimeOptimizations { get; set; } = new List<string>();
    }
}
