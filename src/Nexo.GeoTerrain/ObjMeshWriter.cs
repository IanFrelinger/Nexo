using System.Globalization;
using System.Text;

namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic OBJ writer (positions + optional normals).
/// </summary>
public static class ObjMeshWriter
{
    public static string Write(MeshData mesh)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
#endif
        if (mesh.Vertices is null) throw new ArgumentException("MeshData.Vertices must be non-null.", nameof(mesh));
        if (mesh.Indices is null) throw new ArgumentException("MeshData.Indices must be non-null.", nameof(mesh));

        var sb = new StringBuilder(capacity: Math.Max(1024, mesh.Vertices.Count * 32));

        sb.AppendLine("# Nexo.GeoTerrain OBJ");
        sb.AppendLine("g terrain");

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            sb.Append("v ")
              .Append(v.X.ToString("G9", CultureInfo.InvariantCulture)).Append(' ')
              .Append(v.Y.ToString("G9", CultureInfo.InvariantCulture)).Append(' ')
              .Append(v.Z.ToString("G9", CultureInfo.InvariantCulture))
              .AppendLine();
        }

        var hasNormals = mesh.Normals != null && mesh.Normals.Count == mesh.Vertices.Count;
        if (hasNormals)
        {
            for (var i = 0; i < mesh.Normals!.Count; i++)
            {
                var n = mesh.Normals[i];
                sb.Append("vn ")
                  .Append(n.X.ToString("G9", CultureInfo.InvariantCulture)).Append(' ')
                  .Append(n.Y.ToString("G9", CultureInfo.InvariantCulture)).Append(' ')
                  .Append(n.Z.ToString("G9", CultureInfo.InvariantCulture))
                  .AppendLine();
            }
        }

        // Faces are 1-based indices in OBJ.
        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var a = mesh.Indices[i] + 1;
            var b = mesh.Indices[i + 1] + 1;
            var c = mesh.Indices[i + 2] + 1;

            sb.Append("f ");
            if (hasNormals)
            {
                // v//vn (use vertex index as normal index)
                sb.Append(a).Append("//").Append(a).Append(' ')
                  .Append(b).Append("//").Append(b).Append(' ')
                  .Append(c).Append("//").Append(c).AppendLine();
            }
            else
            {
                sb.Append(a).Append(' ').Append(b).Append(' ').Append(c).AppendLine();
            }
        }

        return sb.ToString();
    }
}

