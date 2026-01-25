using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;

namespace Nexo.SDK.Estimation;

/// <summary>
/// Utility for calculating actual memory footprint of data structures.
/// </summary>
public static class MemoryCalculator
{
    /// <summary>
    /// Calculates the actual memory footprint of an ElevationGrid.
    /// </summary>
    public static long CalculateElevationGridMemory(ElevationGrid grid)
    {
        if (grid == null) return 0;
        
        // float[] heightsMeters: width * height * 4 bytes
        var heightsBytes = (long)grid.Width * grid.Height * 4;
        
        // Object overhead: ~100 bytes
        var objectOverhead = 100L;
        
        return heightsBytes + objectOverhead;
    }

    /// <summary>
    /// Calculates the actual memory footprint of a MeshData.
    /// </summary>
    public static long CalculateMeshMemory(MeshData mesh)
    {
        if (mesh == null) return 0;

        // Vertices: count * 12 bytes (Vector3 = 3 floats)
        var verticesBytes = (long)mesh.Vertices.Count * 12;
        
        // Indices: count * 4 bytes (int)
        var indicesBytes = (long)mesh.Indices.Count * 4;
        
        // Normals: count * 12 bytes (if present)
        var normalsBytes = mesh.Normals != null ? (long)mesh.Normals.Count * 12 : 0;
        
        // TexCoords: count * 8 bytes (if present)
        var texCoordsBytes = mesh.TexCoords != null ? (long)mesh.TexCoords.Count * 8 : 0;
        
        // List overhead: ~50 bytes per list
        var listOverhead = 200L;
        
        return verticesBytes + indicesBytes + normalsBytes + texCoordsBytes + listOverhead;
    }

    /// <summary>
    /// Calculates the actual memory footprint of a GeoFeatureSet.
    /// </summary>
    public static long CalculateVectorFeaturesMemory(GeoFeatureSet features)
    {
        if (features == null || features.Features == null) return 0;

        long totalBytes = 0;
        
        foreach (var feature in features.Features)
        {
            // Feature overhead: ~200 bytes (Id string, Kind, Properties dictionary)
            totalBytes += 200;
            
            // Geometry memory depends on type
            if (feature.Geometry != null)
            {
                // Rough estimate: count vertices in geometry
                // This is approximate - actual geometry types vary
                totalBytes += EstimateGeometryMemory(feature.Geometry);
            }
            
            // Properties: estimate ~100 bytes per feature
            if (feature.Properties != null)
            {
                totalBytes += feature.Properties.Count * 50; // ~50 bytes per property
            }
        }
        
        return totalBytes;
    }

    /// <summary>
    /// Estimates memory for a geometry object.
    /// </summary>
    private static long EstimateGeometryMemory(GeoVector.Geometry.IGeoGeometry geometry)
    {
        // This is a rough estimate - actual implementation would need to inspect geometry type
        // For polygons: vertices * 8 bytes (Vector2)
        // For polylines: vertices * 8 bytes
        // Conservative estimate: 1000 bytes per geometry
        return 1000;
    }

    /// <summary>
    /// Calculates the actual memory footprint of downloaded tile data.
    /// </summary>
    public static long CalculateTileDownloadMemory(byte[] tileData)
    {
        return tileData?.Length ?? 0;
    }
}
