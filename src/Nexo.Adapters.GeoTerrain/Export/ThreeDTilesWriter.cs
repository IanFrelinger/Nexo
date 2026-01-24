using System.Text;
using System.Text.Json;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Export;

/// <summary>
/// Basic 3D Tiles writer for Cesium format.
/// Generates tileset.json and b3dm (Batched 3D Model) tiles.
/// </summary>
public static class ThreeDTilesWriter
{
    /// <summary>
    /// Creates a 3D Tiles tileset.json file.
    /// </summary>
    public static string CreateTilesetJson(
        GeoBounds bounds,
        string baseUrl = "./",
        int maxZoom = 18)
    {
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));

        bounds.Validate();

        var centerLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) / 2.0;
        var centerLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) / 2.0;
        var centerHeight = 0.0;

        var tileset = new
        {
            asset = new
            {
                version = "1.0"
            },
            geometricError = 500.0,
            root = new
            {
                boundingVolume = new
                {
                    region = new[]
                    {
                        bounds.MinLongitude.Degrees * Math.PI / 180.0,
                        bounds.MinLatitude.Degrees * Math.PI / 180.0,
                        bounds.MaxLongitude.Degrees * Math.PI / 180.0,
                        bounds.MaxLatitude.Degrees * Math.PI / 180.0,
                        0.0,
                        1000.0
                    }
                },
                geometricError = 500.0,
                refine = "REPLACE",
                content = new
                {
                    uri = $"{baseUrl}0.b3dm"
                }
            }
        };

        return JsonSerializer.Serialize(tileset, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Creates a b3dm (Batched 3D Model) tile from glTF data.
    /// </summary>
    public static byte[] CreateB3dmTile(string gltfJson, ReadOnlyMemory<byte> binBytes)
    {
        if (string.IsNullOrWhiteSpace(gltfJson))
            throw new ArgumentException("glTF JSON is required.", nameof(gltfJson));

        var featureTableJson = "{\"BATCH_LENGTH\":0}";
        var featureTableBytes = Encoding.UTF8.GetBytes(featureTableJson);
        var gltfJsonBytes = Encoding.UTF8.GetBytes(gltfJson);

        // Calculate sizes
        var featureTableJsonSize = Align8(featureTableBytes.Length);
        var featureTableBinarySize = 0; // No binary feature table
        var gltfJsonSize = Align8(gltfJsonBytes.Length);
        var gltfBinarySize = Align8(binBytes.Length);

        var totalSize = 28 + featureTableJsonSize + featureTableBinarySize + gltfJsonSize + gltfBinarySize;

        var result = new byte[totalSize];
        var offset = 0;

        // B3DM header
        WriteU32(result, offset, 0x6D643362); // 'b3dm'
        offset += 4;
        WriteU32(result, offset, 1); // version
        offset += 4;
        WriteU32(result, offset, (uint)totalSize); // total length
        offset += 4;
        WriteU32(result, offset, (uint)featureTableJsonSize); // feature table JSON length
        offset += 4;
        WriteU32(result, offset, (uint)featureTableBinarySize); // feature table binary length
        offset += 4;
        WriteU32(result, offset, (uint)gltfJsonSize); // glTF JSON length
        offset += 4;
        WriteU32(result, offset, (uint)gltfBinarySize); // glTF binary length
        offset += 4;

        // Feature table JSON
        Buffer.BlockCopy(featureTableBytes, 0, result, offset, featureTableBytes.Length);
        offset += featureTableJsonSize;

        // Feature table binary (empty)
        // offset += featureTableBinarySize;

        // glTF JSON
        Buffer.BlockCopy(gltfJsonBytes, 0, result, offset, gltfJsonBytes.Length);
        offset += gltfJsonSize;

        // glTF binary
        binBytes.Span.CopyTo(result.AsSpan(offset, binBytes.Length));
        // Remaining bytes are already 0

        return result;
    }

    private static int Align8(int n) => (n + 7) & ~7;

    private static void WriteU32(byte[] dst, int offset, uint v)
    {
        dst[offset + 0] = (byte)(v & 0xFF);
        dst[offset + 1] = (byte)((v >> 8) & 0xFF);
        dst[offset + 2] = (byte)((v >> 16) & 0xFF);
        dst[offset + 3] = (byte)((v >> 24) & 0xFF);
    }
}
