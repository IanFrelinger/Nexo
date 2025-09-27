using System;

namespace NexoDoomGame
{
    /// <summary>
    /// Agent configuration for dynamic behavior using existing Nexo system
    /// </summary>
    [System.Serializable]
    public class AgentConfiguration
    {
        [Header("Nexo Agent Settings")]
        public string aiMode = "OFF"; // OFF, HYBRID, EMBEDDED
        public bool enableImageGeneration = true;
        public bool enableCodeGeneration = true;
        public bool enableAssetGeneration = true;
        
        [Header("Script Generation Settings")]
        public bool includeComments = true;
        public bool includeErrorHandling = true;
        public bool includeLogging = true;
        public string codeStyle = "Unity";
        
        [Header("Asset Generation Settings")]
        public int textureResolution = 512;
        public string textureFormat = "PNG";
        public bool generateNormalMaps = true;
        public bool generateSpecularMaps = true;
        
        [Header("Performance Settings")]
        public int maxConcurrentGenerations = 3;
        public float generationTimeout = 30f;
        public bool enableCaching = true;
    }
    
    /// <summary>
    /// Generated asset data structure
    /// </summary>
    [System.Serializable]
    public class GeneratedAsset
    {
        public AssetType Type;
        public string Name;
        public string Content;
        public DateTime GeneratedAt;
        public string FilePath;
    }
    
    /// <summary>
    /// Asset types enum
    /// </summary>
    public enum AssetType
    {
        Script,
        Texture,
        Model,
        Audio,
        Prefab,
        Scene
    }
    
    /// <summary>
    /// Game specification data structure
    /// </summary>
    public class GameSpecification
    {
        public string GameType { get; set; } = "";
        public string ArtStyle { get; set; } = "";
        public string[] ColorPalette { get; set; } = Array.Empty<string>();
        public string[] EnemyTypes { get; set; } = Array.Empty<string>();
        public string[] WeaponTypes { get; set; } = Array.Empty<string>();
        public int LevelCount { get; set; }
        public int TargetFPS { get; set; }
        public string[] RequiredScripts { get; set; } = Array.Empty<string>();
        public string[] RequiredAssets { get; set; } = Array.Empty<string>();
    }
    
    /// <summary>
    /// Extended GameSpecification with asset prompts
    /// </summary>
    public static class GameSpecificationExtensions
    {
        public static string[] GetAssetPrompts(this GameSpecification spec)
        {
            return new[]
            {
                $"Dark sci-fi wall texture, {spec.ArtStyle} style, {string.Join(" and ", spec.ColorPalette)} colors",
                $"Industrial floor texture, {spec.ArtStyle} style, weathered and worn",
                $"Weapon icon for {string.Join(" and ", spec.WeaponTypes)}, {spec.ArtStyle} style",
                $"Enemy sprite for {string.Join(" and ", spec.EnemyTypes)}, {spec.ArtStyle} style",
                $"UI element for health bar, {spec.ArtStyle} style, retro-futuristic",
                $"UI element for ammo counter, {spec.ArtStyle} style, digital display",
                $"Blood splatter decal, realistic effect, dark red color",
                $"Muzzle flash effect, {spec.ArtStyle} style, bright orange",
                $"Explosion effect, {spec.ArtStyle} style, dramatic lighting",
                $"Environmental decal, {spec.ArtStyle} style, industrial damage"
            };
        }
    }
}
