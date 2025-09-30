using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Interface for ComfyUI texture generation adapter.
    /// Provides offline image generation capabilities for game assets.
    /// </summary>
    public interface ITextureGenAdapter : IAdapter
    {
        /// <summary>
        /// Generates a texture set for the specified prompt and style.
        /// </summary>
        /// <param name="prompt">Text prompt for image generation</param>
        /// <param name="style">Art style for the generation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated texture set</returns>
        Task<TextureSet> GeneratePackAsync(string prompt, string style, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates a single texture with specific parameters.
        /// </summary>
        /// <param name="prompt">Text prompt for image generation</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        /// <param name="style">Art style for the generation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated texture</returns>
        Task<Texture> GenerateTextureAsync(string prompt, int width, int height, string style, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates multiple textures for a game slice.
        /// </summary>
        /// <param name="requirements">Asset requirements to generate textures for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated texture collection</returns>
        Task<TextureCollection> GenerateForAssetsAsync(IReadOnlyList<AssetRequirement> requirements, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Represents a set of related textures.
    /// </summary>
    public sealed record TextureSet
    {
        /// <summary>
        /// Unique identifier for this texture set.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the texture set.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the texture set.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// List of textures in this set.
        /// </summary>
        public IReadOnlyList<Texture> Textures { get; init; } = Array.Empty<Texture>();
        
        /// <summary>
        /// Art style of the texture set.
        /// </summary>
        public string Style { get; init; } = "";
        
        /// <summary>
        /// Generation prompt used.
        /// </summary>
        public string Prompt { get; init; } = "";
        
        /// <summary>
        /// Timestamp when the set was generated.
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents a single texture.
    /// </summary>
    public sealed record Texture
    {
        /// <summary>
        /// Unique identifier for this texture.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the texture.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the texture.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// File path to the texture file.
        /// </summary>
        public string FilePath { get; init; } = "";
        
        /// <summary>
        /// Width of the texture in pixels.
        /// </summary>
        public int Width { get; init; }
        
        /// <summary>
        /// Height of the texture in pixels.
        /// </summary>
        public int Height { get; init; }
        
        /// <summary>
        /// Format of the texture (PNG, JPG, etc.).
        /// </summary>
        public string Format { get; init; } = "PNG";
        
        /// <summary>
        /// Size of the texture file in bytes.
        /// </summary>
        public long FileSizeBytes { get; init; }
        
        /// <summary>
        /// Generation prompt used for this texture.
        /// </summary>
        public string Prompt { get; init; } = "";
        
        /// <summary>
        /// Art style of the texture.
        /// </summary>
        public string Style { get; init; } = "";
        
        /// <summary>
        /// Timestamp when the texture was generated.
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents a collection of textures for a game slice.
    /// </summary>
    public sealed record TextureCollection
    {
        /// <summary>
        /// Unique identifier for this collection.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the collection.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the collection.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// List of texture sets in this collection.
        /// </summary>
        public IReadOnlyList<TextureSet> TextureSets { get; init; } = Array.Empty<TextureSet>();
        
        /// <summary>
        /// Total number of textures in the collection.
        /// </summary>
        public int TotalTextures { get; init; }
        
        /// <summary>
        /// Total size of all textures in bytes.
        /// </summary>
        public long TotalSizeBytes { get; init; }
        
        /// <summary>
        /// Timestamp when the collection was generated.
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
}
