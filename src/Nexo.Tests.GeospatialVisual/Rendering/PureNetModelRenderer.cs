using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using JeremyAnsel.Media.WavefrontObj;
using ImageSharpColor = SixLabors.ImageSharp.Color;

namespace Nexo.Tests.GeospatialVisual.Rendering;

/// <summary>
/// Pure .NET 3D model renderer using ImageSharp.
/// Works on all platforms including Unity (.NET Standard 2.0 compatible).
/// Replaces Playwright-based rendering for maximum portability.
/// </summary>
public static class PureNetModelRenderer
{
    /// <summary>
    /// Renders an OBJ file to a PNG image using pure .NET.
    /// </summary>
    public static byte[] RenderObjToImage(
        string objFilePath,
        int width = 1920,
        int height = 1080,
        RenderStyle style = RenderStyle.Wireframe)
    {
        if (!File.Exists(objFilePath))
            throw new FileNotFoundException($"OBJ file not found: {objFilePath}");

        // Parse OBJ file
        var objFile = ObjFile.FromFile(objFilePath);

        // Calculate bounding box and center
        var (bounds, center) = CalculateBounds(objFile);

        // Create image
        using var image = new Image<Rgba32>(width, height);
        
        // Fill background - set all pixels directly
        var bgColor = new Rgba32(26, 26, 26);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = bgColor;
            }
        }

        // Project and render
        var scale = CalculateScale(bounds, width, height);
        var offset = new Vector2(width / 2f, height / 2f);

        if (style == RenderStyle.Wireframe)
        {
            RenderWireframe(image, objFile, center, scale, offset);
        }
        else
        {
            RenderSolid(image, objFile, center, scale, offset, bounds);
        }

        // Save to memory
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static (BoundingBox bounds, Vector3 center) CalculateBounds(ObjFile objFile)
    {
        if (objFile.Vertices == null || objFile.Vertices.Count == 0)
            return (new BoundingBox { Min = Vector3.Zero, Max = Vector3.Zero }, Vector3.Zero);

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var vertex in objFile.Vertices)
        {
            var v = new Vector3((float)vertex.Position.X, (float)vertex.Position.Y, (float)vertex.Position.Z);
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        var center = (min + max) / 2f;
        return (new BoundingBox { Min = min, Max = max }, center);
    }

    private static float CalculateScale(BoundingBox bounds, int width, int height)
    {
        var size = bounds.Max - bounds.Min;
        var maxDim = Math.Max(Math.Max(size.X, size.Y), size.Z);
        if (maxDim <= 0) return 1f;

        // Scale to fit 80% of viewport
        var viewportSize = Math.Min(width, height) * 0.8f;
        return viewportSize / maxDim;
    }

    private static void RenderWireframe(
        Image<Rgba32> image,
        ObjFile objFile,
        Vector3 center,
        float scale,
        Vector2 offset)
    {
        var color = ImageSharpColor.FromRgb(136, 136, 136); // Gray wireframe

        if (objFile.Faces == null) return;

        foreach (var face in objFile.Faces)
        {
            if (face.Vertices == null || face.Vertices.Count < 3) continue;

            var vertices = new List<Vector2>();
            foreach (var fv in face.Vertices)
            {
                var vertexIndex = fv.Vertex - 1; // OBJ uses 1-based indexing
                if (vertexIndex < 0 || vertexIndex >= objFile.Vertices.Count) continue;
                var v = objFile.Vertices[vertexIndex];
                var worldPos = new Vector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z) - center;
                var screenPos = ProjectToScreen(worldPos, scale, offset);
                vertices.Add(screenPos);
            }

            // Draw edges
            for (int i = 0; i < vertices.Count; i++)
            {
                var p1 = vertices[i];
                var p2 = vertices[(i + 1) % vertices.Count];
                DrawLine(image, p1, p2, color);
            }
        }
    }

    private static void RenderSolid(
        Image<Rgba32> image,
        ObjFile objFile,
        Vector3 center,
        float scale,
        Vector2 offset,
        BoundingBox bounds)
    {
        // For solid rendering, we'll use a simple depth-sorted approach
        // Sort faces by average Z depth (back to front)
        if (objFile.Faces == null) return;

        var facesWithDepth = new List<(ObjFace face, float depth)>();
        foreach (var face in objFile.Faces)
        {
            if (face.Vertices == null || face.Vertices.Count < 3) continue;

            float avgZ = 0;
            int count = 0;
            foreach (var fv in face.Vertices)
            {
                var vertexIndex = fv.Vertex - 1; // OBJ uses 1-based indexing
                if (vertexIndex < 0 || vertexIndex >= objFile.Vertices.Count) continue;
                var v = objFile.Vertices[vertexIndex];
                var worldPos = new Vector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z) - center;
                avgZ += worldPos.Z;
                count++;
            }
            if (count > 0)
            {
                facesWithDepth.Add((face, avgZ / count));
            }
        }

        // Sort by depth (back to front)
        facesWithDepth.Sort((a, b) => a.depth.CompareTo(b.depth));

        // Render each face
        foreach (var (face, depth) in facesWithDepth)
        {
            var vertices = new List<Vector2>();
            foreach (var fv in face.Vertices)
            {
                var vertexIndex = fv.Vertex - 1; // OBJ uses 1-based indexing
                if (vertexIndex < 0 || vertexIndex >= objFile.Vertices.Count) continue;
                var v = objFile.Vertices[vertexIndex];
                var worldPos = new Vector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z) - center;
                var screenPos = ProjectToScreen(worldPos, scale, offset);
                vertices.Add(screenPos);
            }

            if (vertices.Count >= 3)
            {
                // Calculate lighting based on normal (simple flat shading)
                var normal = CalculateFaceNormal(face, objFile, center);
                var lightDir = Vector3.Normalize(new Vector3(0.5f, 1f, 0.5f));
                var intensity = Math.Max(0.3f, Vector3.Dot(normal, lightDir));
                var baseR = 136;
                var baseG = 136;
                var baseB = 136;
                var r = (byte)(baseR * intensity);
                var g = (byte)(baseG * intensity);
                var b = (byte)(baseB * intensity);
                var shadedColor = ImageSharpColor.FromRgb(r, g, b);

                // Draw filled polygon
                DrawFilledPolygon(image, vertices, shadedColor);
            }
        }
    }

    private static Vector2 ProjectToScreen(Vector3 worldPos, float scale, Vector2 offset)
    {
        // Simple orthographic projection (top-down with slight isometric angle)
        var x = worldPos.X * scale + offset.X;
        var y = -(worldPos.Y * 0.866f + worldPos.Z * 0.5f) * scale + offset.Y; // Isometric projection
        return new Vector2(x, y);
    }

    private static Vector3 CalculateFaceNormal(ObjFace face, ObjFile objFile, Vector3 center)
    {
        if (face.Vertices == null || face.Vertices.Count < 3) return Vector3.UnitY;

        var indices = face.Vertices
            .Select(fv => fv.Vertex - 1) // OBJ uses 1-based indexing
            .Where(idx => idx >= 0 && idx < objFile.Vertices.Count)
            .Take(3)
            .Select(idx => objFile.Vertices[idx])
            .ToList();

        if (indices.Count < 3) return Vector3.UnitY;

        var v0 = new Vector3((float)indices[0].Position.X, (float)indices[0].Position.Y, (float)indices[0].Position.Z) - center;
        var v1 = new Vector3((float)indices[1].Position.X, (float)indices[1].Position.Y, (float)indices[1].Position.Z) - center;
        var v2 = new Vector3((float)indices[2].Position.X, (float)indices[2].Position.Y, (float)indices[2].Position.Z) - center;

        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var normal = Vector3.Cross(edge1, edge2);
        if (normal.LengthSquared() > 0.0001f)
        {
            return Vector3.Normalize(normal);
        }
        return Vector3.UnitY;
    }

    private static void DrawLine(Image<Rgba32> image, Vector2 p1, Vector2 p2, ImageSharpColor color)
    {
        var x0 = (int)Math.Round(p1.X);
        var y0 = (int)Math.Round(p1.Y);
        var x1 = (int)Math.Round(p2.X);
        var y1 = (int)Math.Round(p2.Y);

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        
        var pixelColor = new Rgba32(color.R, color.G, color.B, color.A);

        while (true)
        {
            if (x0 >= 0 && x0 < image.Width && y0 >= 0 && y0 < image.Height)
            {
                image[x0, y0] = pixelColor;
            }

            if (x0 == x1 && y0 == y1) break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawFilledPolygon(Image<Rgba32> image, List<Vector2> vertices, ImageSharpColor color)
    {
        if (vertices.Count < 3) return;

        // Simple scanline fill
        var minY = (int)vertices.Min(v => v.Y);
        var maxY = (int)vertices.Max(v => v.Y);
        minY = Math.Max(0, minY);
        maxY = Math.Min(image.Height - 1, maxY);

        for (int y = minY; y <= maxY; y++)
        {
            var intersections = new List<float>();
            for (int i = 0; i < vertices.Count; i++)
            {
                var p1 = vertices[i];
                var p2 = vertices[(i + 1) % vertices.Count];

                if ((p1.Y <= y && p2.Y > y) || (p2.Y <= y && p1.Y > y))
                {
                    var t = (y - p1.Y) / (p2.Y - p1.Y);
                    var x = p1.X + t * (p2.X - p1.X);
                    intersections.Add(x);
                }
            }

            intersections.Sort();
            for (int i = 0; i < intersections.Count - 1; i += 2)
            {
                var xStart = (int)Math.Round(intersections[i]);
                var xEnd = (int)Math.Round(intersections[i + 1]);
                xStart = Math.Max(0, xStart);
                xEnd = Math.Min(image.Width - 1, xEnd);

                var pixelColor = new Rgba32(color.R, color.G, color.B, color.A);
                for (int x = xStart; x <= xEnd; x++)
                {
                    if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                    {
                        image[x, y] = pixelColor;
                    }
                }
            }
        }
    }

    private struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;
    }
}

/// <summary>
/// Rendering style for 3D models.
/// </summary>
public enum RenderStyle
{
    /// <summary>
    /// Wireframe rendering (edges only).
    /// </summary>
    Wireframe,

    /// <summary>
    /// Solid rendering with flat shading.
    /// </summary>
    Solid
}
