using System.Globalization;
using System.Text;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Export;

/// <summary>
/// Basic FBX writer for mesh data.
/// Generates ASCII FBX format (FBX 7.4 ASCII).
/// </summary>
public static class FbxWriter
{
    public static string Write(MeshData mesh, string meshName = "TerrainMesh")
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
#endif
        if (mesh.Vertices is null) throw new ArgumentException("MeshData.Vertices must be non-null.", nameof(mesh));
        if (mesh.Indices is null) throw new ArgumentException("MeshData.Indices must be non-null.", nameof(mesh));

        var sb = new StringBuilder(capacity: mesh.Vertices.Count * 64);
        var inv = CultureInfo.InvariantCulture;

        // FBX Header
        sb.AppendLine("; FBX 7.4.0 project file");
        sb.AppendLine("; Created by Nexo.Adapters.GeoTerrain");
        sb.AppendLine();
        sb.AppendLine("FBXHeaderExtension:  {");
        sb.AppendLine("\tFBXHeaderVersion: 1003");
        sb.AppendLine("\tFBXVersion: 7400");
        sb.AppendLine("}");
        sb.AppendLine();

        // Global Settings
        sb.AppendLine("GlobalSettings:  {");
        sb.AppendLine("\tVersion: 1000");
        sb.AppendLine("\tProperties70:  {");
        sb.AppendLine("\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1");
        sb.AppendLine("\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
        sb.AppendLine("\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2");
        sb.AppendLine("\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
        sb.AppendLine("\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0");
        sb.AppendLine("\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
        sb.AppendLine("\t\tP: \"OriginalUpAxis\", \"int\", \"Integer\", \"\",1");
        sb.AppendLine("\t\tP: \"OriginalUpAxisSign\", \"int\", \"Integer\", \"\",1");
        sb.AppendLine("\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",1");
        sb.AppendLine("\t}");
        sb.AppendLine("}");
        sb.AppendLine();

        // Definitions
        sb.AppendLine("Definitions:  {");
        sb.AppendLine("\tVersion: 100");
        sb.AppendLine("\tCount: 2");
        sb.AppendLine("\tObjectType: \"Geometry\" {");
        sb.AppendLine("\t\tCount: 1");
        sb.AppendLine("\t}");
        sb.AppendLine("\tObjectType: \"Model\" {");
        sb.AppendLine("\t\tCount: 1");
        sb.AppendLine("\t}");
        sb.AppendLine("}");
        sb.AppendLine();

        // Objects
        sb.AppendLine("Objects:  {");

        // Geometry (Mesh)
        sb.AppendLine("\tGeometry: ").Append(GetFbxId(1)).AppendLine(", \"Geometry::").Append(meshName).AppendLine("\", \"Mesh\" {");
        sb.AppendLine("\t\tVertices: *").Append(mesh.Vertices.Count * 3).AppendLine(" {");
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            sb.Append("\ta:").Append(v.X.ToString("G9", inv)).Append(',')
              .Append(v.Y.ToString("G9", inv)).Append(',')
              .Append(v.Z.ToString("G9", inv));
            if (i < mesh.Vertices.Count - 1) sb.Append(',');
        }
        sb.AppendLine();
        sb.AppendLine("\t\t}");

        // Polygon Vertex Index (negative for end of polygon)
        sb.AppendLine("\t\tPolygonVertexIndex: *").Append(mesh.Indices.Count).AppendLine(" {");
        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var a = mesh.Indices[i];
            var b = mesh.Indices[i + 1];
            var c = mesh.Indices[i + 2];
            sb.Append("\ta:").Append(a).Append(',').Append(b).Append(',').Append(-(c + 1));
            if (i < mesh.Indices.Count - 3) sb.Append(',');
        }
        sb.AppendLine();
        sb.AppendLine("\t\t}");

        // Edges (empty for now)
        sb.AppendLine("\t\tEdges: *0 {");
        sb.AppendLine("\t\t}");

        // Layer Element Normal
        if (mesh.Normals != null && mesh.Normals.Count == mesh.Vertices.Count)
        {
            sb.AppendLine("\t\tLayerElementNormal: 0 {");
            sb.AppendLine("\t\t\tVersion: 101");
            sb.AppendLine("\t\t\tName: \"\"");
            sb.AppendLine("\t\t\tMappingInformationType: \"ByVertice\"");
            sb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");
            sb.AppendLine("\t\t\tNormals: *").Append(mesh.Normals.Count * 3).AppendLine(" {");
            for (var i = 0; i < mesh.Normals.Count; i++)
            {
                var n = mesh.Normals[i];
                sb.Append("\t\t\t\ta:").Append(n.X.ToString("G9", inv)).Append(',')
                  .Append(n.Y.ToString("G9", inv)).Append(',')
                  .Append(n.Z.ToString("G9", inv));
                if (i < mesh.Normals.Count - 1) sb.Append(',');
            }
            sb.AppendLine();
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
        }

        // Layer Element UV
        if (mesh.TexCoords != null && mesh.TexCoords.Count == mesh.Vertices.Count)
        {
            sb.AppendLine("\t\tLayerElementUV: 0 {");
            sb.AppendLine("\t\t\tVersion: 101");
            sb.AppendLine("\t\t\tName: \"UVChannel_1\"");
            sb.AppendLine("\t\t\tMappingInformationType: \"ByVertice\"");
            sb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");
            sb.AppendLine("\t\t\tUV: *").Append(mesh.TexCoords.Count * 2).AppendLine(" {");
            for (var i = 0; i < mesh.TexCoords.Count; i++)
            {
                var t = mesh.TexCoords[i];
                sb.Append("\t\t\t\ta:").Append(t.X.ToString("G9", inv)).Append(',')
                  .Append(t.Y.ToString("G9", inv));
                if (i < mesh.TexCoords.Count - 1) sb.Append(',');
            }
            sb.AppendLine();
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
        }

        sb.AppendLine("\t\tLayer: 0 {");
        sb.AppendLine("\t\t\tVersion: 100");
        sb.AppendLine("\t\t\tLayerElement:  {");
        sb.AppendLine("\t\t\t\tType: \"LayerElementNormal\"");
        sb.AppendLine("\t\t\t\tTypedIndex: 0");
        sb.AppendLine("\t\t\t}");
        if (mesh.TexCoords != null)
        {
            sb.AppendLine("\t\t\tLayerElement:  {");
            sb.AppendLine("\t\t\t\tType: \"LayerElementUV\"");
            sb.AppendLine("\t\t\t\tTypedIndex: 0");
            sb.AppendLine("\t\t\t}");
        }
        sb.AppendLine("\t\t}");
        sb.AppendLine("\t}");

        // Model
        sb.AppendLine("\tModel: ").Append(GetFbxId(2)).AppendLine(", \"Model::").Append(meshName).AppendLine("\", \"Mesh\" {");
        sb.AppendLine("\t\tVersion: 232");
        sb.AppendLine("\t\tProperties70:  {");
        sb.AppendLine("\t\t\tP: \"RotationActive\", \"bool\", \"\", \"\",1");
        sb.AppendLine("\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
        sb.AppendLine("\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
        sb.AppendLine("\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
        sb.AppendLine("\t\t}");
        sb.AppendLine("\t\tShading: T");
        sb.AppendLine("\t\tCulling: \"CullingOff\"");
        sb.AppendLine("\t}");
        sb.AppendLine("}");
        sb.AppendLine();

        // Connections
        sb.AppendLine("Connections:  {");
        sb.Append("\tC: \"OO\",").Append(GetFbxId(2)).Append(',').AppendLine(GetFbxId(1));
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetFbxId(long id) => id.ToString(CultureInfo.InvariantCulture);
}
