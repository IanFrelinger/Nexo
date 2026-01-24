using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nexo.Adapters.GeoTerrain.Processing;

/// <summary>
/// Generates texture atlases by combining multiple textures into a single atlas.
/// Useful for reducing draw calls and improving rendering performance.
/// </summary>
public static class TextureAtlasGenerator
{
    /// <summary>
    /// Creates a texture atlas from multiple input textures.
    /// Arranges textures in a grid layout.
    /// </summary>
    public static TextureAtlasResult CreateAtlas(
        IReadOnlyList<byte[]> textures,
        int atlasSize = 2048,
        int padding = 2)
    {
        if (textures == null || textures.Count == 0)
            throw new ArgumentException("At least one texture is required.", nameof(textures));

        // Calculate grid dimensions
        var count = textures.Count;
        var cols = (int)Math.Ceiling(Math.Sqrt(count));
        var rows = (int)Math.Ceiling((double)count / cols);

        // Calculate cell size (assuming square textures)
        var cellSize = (atlasSize - padding * (cols + 1)) / cols;

        using var atlas = new Image<Rgba32>(atlasSize, atlasSize);
        var mappings = new List<TextureAtlasMapping>();

        // Initialize with transparent background by setting all pixels
        for (var y = 0; y < atlasSize; y++)
        {
            for (var x = 0; x < atlasSize; x++)
            {
                atlas[x, y] = new Rgba32(0, 0, 0, 0);
            }
        }

        for (var i = 0; i < textures.Count; i++)
        {
            using var texture = Image.Load<Rgba32>(textures[i]);
            var resized = texture.Clone();
            resized.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(cellSize, cellSize),
                Mode = ResizeMode.Max
            }));

            var col = i % cols;
            var row = i / cols;
            var x = padding + col * (cellSize + padding);
            var y = padding + row * (cellSize + padding);

            atlas.Mutate(ctx => ctx.DrawImage(resized, new Point(x, y), 1.0f));

            // Calculate normalized UV coordinates
            var uMin = (float)x / atlasSize;
            var vMin = (float)y / atlasSize;
            var uMax = (float)(x + cellSize) / atlasSize;
            var vMax = (float)(y + cellSize) / atlasSize;

            mappings.Add(new TextureAtlasMapping
            {
                Index = i,
                UMin = uMin,
                VMin = vMin,
                UMax = uMax,
                VMax = vMax
            });
        }

        using var ms = new MemoryStream();
        atlas.SaveAsPng(ms);
        var atlasBytes = ms.ToArray();

        return new TextureAtlasResult
        {
            AtlasBytes = atlasBytes,
            AtlasSize = atlasSize,
            Mappings = mappings
        };
    }
}

/// <summary>
/// Result of texture atlas generation.
/// </summary>
public sealed record TextureAtlasResult
{
    public required byte[] AtlasBytes { get; init; }
    public required int AtlasSize { get; init; }
    public required IReadOnlyList<TextureAtlasMapping> Mappings { get; init; }
}

/// <summary>
/// Mapping from original texture index to atlas UV coordinates.
/// </summary>
public sealed record TextureAtlasMapping
{
    public required int Index { get; init; }
    public required float UMin { get; init; }
    public required float VMin { get; init; }
    public required float UMax { get; init; }
    public required float VMax { get; init; }
}
