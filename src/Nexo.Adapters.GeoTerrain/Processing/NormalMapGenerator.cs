using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nexo.Adapters.GeoTerrain.Processing;

/// <summary>
/// Generates normal maps from height maps or elevation data.
/// Useful for adding surface detail to terrain without increasing geometry complexity.
/// </summary>
public static class NormalMapGenerator
{
    /// <summary>
    /// Generates a normal map from a height map (grayscale image where brightness = height).
    /// </summary>
    public static byte[] GenerateFromHeightMap(byte[] heightMapBytes, float strength = 1.0f)
    {
        if (heightMapBytes == null || heightMapBytes.Length == 0)
            throw new ArgumentException("Height map is required.", nameof(heightMapBytes));

        using var heightMap = Image.Load<Rgba32>(heightMapBytes);
        using var normalMap = new Image<Rgba32>(heightMap.Width, heightMap.Height);

        normalMap.Mutate(ctx =>
        {
            for (var y = 0; y < heightMap.Height; y++)
            {
                for (var x = 0; x < heightMap.Width; x++)
                {
                    // Sample neighboring heights
                    var hL = GetHeight(heightMap, x - 1, y);
                    var hR = GetHeight(heightMap, x + 1, y);
                    var hD = GetHeight(heightMap, x, y - 1);
                    var hU = GetHeight(heightMap, x, y + 1);

                    // Calculate normal from height differences
                    var dx = (hL - hR) * strength;
                    var dy = (hD - hU) * strength;
                    var dz = 1.0f;

                    // Normalize
                    var length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (length > 0.0001f)
                    {
                        dx /= length;
                        dy /= length;
                        dz /= length;
                    }

                    // Convert to RGB (normal maps typically store X,Y,Z in R,G,B with 0.5 offset)
                    var r = (byte)((dx * 0.5f + 0.5f) * 255);
                    var g = (byte)((dy * 0.5f + 0.5f) * 255);
                    var b = (byte)((dz * 0.5f + 0.5f) * 255);

                    normalMap[x, y] = new Rgba32(r, g, b, 255);
                }
            }
        });

        using var ms = new MemoryStream();
        normalMap.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static float GetHeight(Image<Rgba32> img, int x, int y)
    {
        // Clamp coordinates
        x = Math.Max(0, Math.Min(img.Width - 1, x));
        y = Math.Max(0, Math.Min(img.Height - 1, y));

        var pixel = img[x, y];
        // Use grayscale value as height
        return (pixel.R + pixel.G + pixel.B) / (3.0f * 255.0f);
    }
}
