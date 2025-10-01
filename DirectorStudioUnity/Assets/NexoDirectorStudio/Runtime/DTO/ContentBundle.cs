using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a bundle of content assets generated for the game slice.
    /// This includes ScriptableObjects, prefabs, and other Unity assets.
    /// </summary>
    public sealed record ContentBundle
    {
        /// <summary>
        /// Unique identifier for this content bundle.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The interaction graph this bundle was generated from.
        /// </summary>
        public string InteractionGraphId { get; init; } = string.Empty;
        
        /// <summary>
        /// All generated assets in this bundle.
        /// </summary>
        public IReadOnlyList<AssetData> Assets { get; init; } = Array.Empty<AssetData>();
        
        /// <summary>
        /// Addressables group configuration.
        /// </summary>
        public AddressablesGroupData AddressablesGroup { get; init; } = new();
        
        /// <summary>
        /// Metadata about the bundle.
        /// </summary>
        public BundleMetadata Metadata { get; init; } = new();
        
        /// <summary>
        /// Seed used for deterministic generation.
        /// </summary>
        public int Seed { get; init; }
        
        /// <summary>
        /// Timestamp when the bundle was generated.
        /// </summary>
        public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Represents a single asset in the content bundle.
    /// </summary>
    public sealed record AssetData
    {
        /// <summary>
        /// Unique identifier for this asset.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of asset (e.g., "ScriptableObject", "Prefab", "Texture", "Audio").
        /// </summary>
        public string AssetType { get; init; } = string.Empty;
        
        /// <summary>
        /// Name of the asset.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Path where the asset should be stored.
        /// </summary>
        public string AssetPath { get; init; } = string.Empty;
        
        /// <summary>
        /// Data for the asset (serialized content).
        /// </summary>
        public string AssetDataJson { get; init; } = string.Empty;
        
        /// <summary>
        /// Dependencies on other assets.
        /// </summary>
        public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Import settings for the asset.
        /// </summary>
        public AssetImportSettings ImportSettings { get; init; } = new();
        
        /// <summary>
        /// Whether this asset should be included in the Addressables group.
        /// </summary>
        public bool IncludeInAddressables { get; init; } = false;
        
        /// <summary>
        /// Addressables key for this asset.
        /// </summary>
        public string AddressablesKey { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Represents Addressables group configuration.
    /// </summary>
    public sealed record AddressablesGroupData
    {
        /// <summary>
        /// Name of the Addressables group.
        /// </summary>
        public string GroupName { get; init; } = "Generated";
        
        /// <summary>
        /// Build settings for the group.
        /// </summary>
        public AddressablesBuildSettings BuildSettings { get; init; } = new();
        
        /// <summary>
        /// Asset labels for the group.
        /// </summary>
        public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();
    }
    
    /// <summary>
    /// Represents Addressables build settings.
    /// </summary>
    public sealed record AddressablesBuildSettings
    {
        /// <summary>
        /// Build target platform.
        /// </summary>
        public string BuildTarget { get; init; } = "StandaloneWindows64";
        
        /// <summary>
        /// Compression type for the group.
        /// </summary>
        public string CompressionType { get; init; } = "LZ4";
        
        /// <summary>
        /// Whether to use remote catalog.
        /// </summary>
        public bool UseRemoteCatalog { get; init; } = false;
        
        /// <summary>
        /// Remote catalog URL.
        /// </summary>
        public string RemoteCatalogUrl { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Represents bundle metadata.
    /// </summary>
    public sealed record BundleMetadata
    {
        /// <summary>
        /// Version of the bundle.
        /// </summary>
        public string Version { get; init; } = "1.0.0";
        
        /// <summary>
        /// Author of the bundle.
        /// </summary>
        public string Author { get; init; } = "Director Studio";
        
        /// <summary>
        /// Description of the bundle.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Tags for the bundle.
        /// </summary>
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Size of the bundle in bytes.
        /// </summary>
        public long SizeBytes { get; init; } = 0;
        
        /// <summary>
        /// Hash of the bundle content.
        /// </summary>
        public string ContentHash { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Represents asset import settings.
    /// </summary>
    public sealed record AssetImportSettings
    {
        /// <summary>
        /// Import settings for textures.
        /// </summary>
        public TextureImportSettings? TextureSettings { get; init; }
        
        /// <summary>
        /// Import settings for audio.
        /// </summary>
        public AudioImportSettings? AudioSettings { get; init; }
        
        /// <summary>
        /// Import settings for models.
        /// </summary>
        public ModelImportSettings? ModelSettings { get; init; }
        
        /// <summary>
        /// Whether to generate colliders for the asset.
        /// </summary>
        public bool GenerateColliders { get; init; } = false;
        
        /// <summary>
        /// Whether to generate lightmap UVs.
        /// </summary>
        public bool GenerateLightmapUVs { get; init; } = false;
    }
    
    /// <summary>
    /// Represents texture import settings.
    /// </summary>
    public sealed record TextureImportSettings
    {
        /// <summary>
        /// Texture type (e.g., "Default", "NormalMap", "Sprite").
        /// </summary>
        public string TextureType { get; init; } = "Default";
        
        /// <summary>
        /// Texture shape (e.g., "2D", "Cube").
        /// </summary>
        public string TextureShape { get; init; } = "2D";
        
        /// <summary>
        /// Maximum texture size.
        /// </summary>
        public int MaxTextureSize { get; init; } = 1024;
        
        /// <summary>
        /// Texture compression format.
        /// </summary>
        public string CompressionFormat { get; init; } = "Automatic";
        
        /// <summary>
        /// Whether to generate mipmaps.
        /// </summary>
        public bool GenerateMipmaps { get; init; } = true;
    }
    
    /// <summary>
    /// Represents audio import settings.
    /// </summary>
    public sealed record AudioImportSettings
    {
        /// <summary>
        /// Audio format (e.g., "Compressed", "Uncompressed").
        /// </summary>
        public string AudioFormat { get; init; } = "Compressed";
        
        /// <summary>
        /// Audio compression format.
        /// </summary>
        public string CompressionFormat { get; init; } = "Vorbis";
        
        /// <summary>
        /// Audio quality (0-1).
        /// </summary>
        public float Quality { get; init; } = 0.5f;
        
        /// <summary>
        /// Whether to load audio on startup.
        /// </summary>
        public bool LoadOnStartup { get; init; } = false;
    }
    
    /// <summary>
    /// Represents model import settings.
    /// </summary>
    public sealed record ModelImportSettings
    {
        /// <summary>
        /// Import scale factor.
        /// </summary>
        public float ScaleFactor { get; init; } = 1.0f;
        
        /// <summary>
        /// Whether to import materials.
        /// </summary>
        public bool ImportMaterials { get; init; } = true;
        
        /// <summary>
        /// Whether to import animations.
        /// </summary>
        public bool ImportAnimations { get; init; } = true;
        
        /// <summary>
        /// Whether to import blend shapes.
        /// </summary>
        public bool ImportBlendShapes { get; init; } = false;
    }
}
