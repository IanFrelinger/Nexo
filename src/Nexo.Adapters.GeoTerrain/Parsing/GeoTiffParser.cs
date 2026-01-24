using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Parsing;

/// <summary>
/// Basic parser for GeoTIFF elevation data.
/// 
/// Note: Full GeoTIFF support requires reading TIFF tags for georeferencing.
/// This implementation assumes:
/// - Single-band or grayscale TIFF (elevation in pixel values)
/// - Bounds provided separately or inferred from filename
/// - Simple scaling (pixel value * scale + offset = meters)
/// </summary>
public static class GeoTiffParser
{
    /// <summary>
    /// Parses a GeoTIFF image into an ElevationGrid.
    /// </summary>
    /// <param name="imageBytes">TIFF file bytes</param>
    /// <param name="bounds">Geographic bounds (if not embedded in TIFF tags)</param>
    /// <param name="scale">Scale factor: pixelValue * scale + offset = meters (default: 1.0)</param>
    /// <param name="offset">Offset: pixelValue * scale + offset = meters (default: 0.0)</param>
    /// <param name="noDataValue">Pixel value representing no data (default: -32768 or NaN)</param>
    public static ElevationGrid Parse(
        byte[] imageBytes,
        GeoBounds? bounds = null,
        float scale = 1.0f,
        float offset = 0.0f,
        float? noDataValue = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(imageBytes);
#else
        if (imageBytes is null) throw new ArgumentNullException(nameof(imageBytes));
#endif

        using var image = Image.Load<L16>(imageBytes); // 16-bit grayscale

        var width = image.Width;
        var height = image.Height;

        if (width < 2 || height < 2)
            throw new InvalidOperationException("GeoTIFF must be at least 2x2 pixels.");

        var heights = new float[width * height];
        var nodata = 0;
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;

        image.ProcessPixelRows(accessor =>
        {
            var hi = 0;
            for (var row = 0; row < accessor.Height; row++)
            {
                var span = accessor.GetRowSpan(row);
                for (var col = 0; col < accessor.Width; col++)
                {
                    var pixelValue = span[col].PackedValue;
                    var meters = (pixelValue * scale) + offset;

                    if (noDataValue.HasValue && Math.Abs(meters - noDataValue.Value) < 0.0001f)
                    {
                        heights[hi++] = float.NaN;
                        nodata++;
                    }
                    else
                    {
                        heights[hi++] = meters;
                        if (meters < min) min = meters;
                        if (meters > max) max = meters;
                    }
                }
            }
        });

        if (float.IsPositiveInfinity(min)) min = 0f;
        if (float.IsNegativeInfinity(max)) max = 0f;

        // If bounds not provided, create a default (caller should provide proper bounds)
        var gridBounds = bounds ?? new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MaxLatitude = new Latitude(1),
            MinLongitude = new Longitude(0),
            MaxLongitude = new Longitude(1)
        };

        // Assume square pixels for now (can be enhanced to read from TIFF tags)
        var cellSize = 1.0; // Default - should be read from GeoTIFF tags
        var spacing = new GridSpacing(cellSize, cellSize);

        return new ElevationGrid(width, height, gridBounds, spacing, heights);
    }

    /// <summary>
    /// Attempts to extract bounds from GeoTIFF tags (simplified - full implementation would parse all GeoTIFF tags).
    /// Returns null if bounds cannot be determined.
    /// </summary>
    public static GeoBounds? TryExtractBounds(byte[] imageBytes)
    {
        // TODO: Full GeoTIFF tag parsing would go here.
        // For now, return null to indicate bounds must be provided.
        return null;
    }
}
