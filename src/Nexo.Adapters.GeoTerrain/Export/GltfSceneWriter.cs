using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Linq;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Export;

public sealed record GltfSceneNode
{
    public required string Name { get; init; }
    public required MeshData Mesh { get; init; }
    public required GltfMaterialSpec Material { get; init; }
}

public sealed record GltfSceneWriteResult
{
    public required string GltfJson { get; init; }
    public required ReadOnlyMemory<byte> BinBytes { get; init; }
}

/// <summary>
/// Minimal deterministic glTF 2.0 scene writer (single buffer, multiple nodes/meshes).
/// </summary>
public static class GltfSceneWriter
{
    public static GltfSceneWriteResult Write(IReadOnlyList<GltfSceneNode> nodes, Uri binUri)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(binUri);
#else
        if (nodes is null) throw new ArgumentNullException(nameof(nodes));
        if (binUri is null) throw new ArgumentNullException(nameof(binUri));
#endif
        if (nodes.Count == 0) throw new ArgumentException("At least one node is required.", nameof(nodes));

        var bin = new MemoryStream(capacity: 1024 * 1024);
        var bufferViews = new List<(int offset, int length, int target)>();
        var accessors = new List<AccessorDef>();
        var meshes = new List<MeshDef>();
        var materials = new List<GltfMaterialSpec>();

        // For each node, append its buffers and create accessors.
        for (var ni = 0; ni < nodes.Count; ni++)
        {
            var n = nodes[ni];
            var mesh = n.Mesh;
            if (mesh.Vertices.Count == 0 || mesh.Indices.Count == 0) continue;

            var hasNormals = mesh.Normals != null && mesh.Normals.Count == mesh.Vertices.Count;
            var hasUv = mesh.TexCoords != null && mesh.TexCoords.Count == mesh.Vertices.Count;
            var useU16 = mesh.Vertices.Count <= 65535;

            var materialIndex = materials.Count;
            materials.Add(n.Material);

            // positions
            Align4(bin);
            var posOffset = (int)bin.Position;
            var min = new float[] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
            var max = new float[] { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                var v = mesh.Vertices[i];
                WriteF32(bin, v.X); WriteF32(bin, v.Y); WriteF32(bin, v.Z);
                if (v.X < min[0]) min[0] = v.X; if (v.Y < min[1]) min[1] = v.Y; if (v.Z < min[2]) min[2] = v.Z;
                if (v.X > max[0]) max[0] = v.X; if (v.Y > max[1]) max[1] = v.Y; if (v.Z > max[2]) max[2] = v.Z;
            }
            var posLen = (int)bin.Position - posOffset;
            var posView = bufferViews.Count;
            bufferViews.Add((posOffset, posLen, 34962));
            var posAcc = accessors.Count;
            accessors.Add(new AccessorDef { BufferView = posView, ComponentType = 5126, Count = mesh.Vertices.Count, Type = "VEC3", Min = min, Max = max });

            int normalAcc = -1;
            if (hasNormals)
            {
                Align4(bin);
                var no = (int)bin.Position;
                for (var i = 0; i < mesh.Normals!.Count; i++)
                {
                    var vv = mesh.Normals[i];
                    WriteF32(bin, vv.X); WriteF32(bin, vv.Y); WriteF32(bin, vv.Z);
                }
                var nl = (int)bin.Position - no;
                var view = bufferViews.Count;
                bufferViews.Add((no, nl, 34962));
                normalAcc = accessors.Count;
                accessors.Add(new AccessorDef { BufferView = view, ComponentType = 5126, Count = mesh.Vertices.Count, Type = "VEC3" });
            }

            int uvAcc = -1;
            if (hasUv)
            {
                Align4(bin);
                var uo = (int)bin.Position;
                for (var i = 0; i < mesh.TexCoords!.Count; i++)
                {
                    var t = mesh.TexCoords[i];
                    WriteF32(bin, t.X); WriteF32(bin, t.Y);
                }
                var ul = (int)bin.Position - uo;
                var view = bufferViews.Count;
                bufferViews.Add((uo, ul, 34962));
                uvAcc = accessors.Count;
                accessors.Add(new AccessorDef { BufferView = view, ComponentType = 5126, Count = mesh.Vertices.Count, Type = "VEC2" });
            }

            // indices
            Align4(bin);
            var io = (int)bin.Position;
            if (useU16)
            {
                for (var i = 0; i < mesh.Indices.Count; i++) WriteU16(bin, (ushort)mesh.Indices[i]);
                Align4(bin);
            }
            else
            {
                for (var i = 0; i < mesh.Indices.Count; i++) WriteU32(bin, (uint)mesh.Indices[i]);
            }
            var il = (int)bin.Position - io;
            var idxView = bufferViews.Count;
            bufferViews.Add((io, il, 34963));
            var idxAcc = accessors.Count;
            accessors.Add(new AccessorDef { BufferView = idxView, ComponentType = useU16 ? 5123 : 5125, Count = mesh.Indices.Count, Type = "SCALAR" });

            meshes.Add(new MeshDef
            {
                Name = n.Name,
                PositionAccessor = posAcc,
                NormalAccessor = normalAcc,
                UvAccessor = uvAcc,
                IndicesAccessor = idxAcc,
                MaterialIndex = materialIndex
            });
        }

        var binBytes = bin.ToArray();

        // Build glTF JSON.
        var json = new ArrayBufferWriter<byte>(initialCapacity: 8192);
        using (var w = new Utf8JsonWriter(json, new JsonWriterOptions { Indented = false }))
        {
            w.WriteStartObject();

            w.WritePropertyName("asset");
            w.WriteStartObject();
            w.WriteString("version", "2.0");
            w.WriteString("generator", "Nexo.Adapters.GeoTerrain");
            w.WriteEndObject();

            w.WritePropertyName("scene");
            w.WriteNumberValue(0);

            w.WritePropertyName("scenes");
            w.WriteStartArray();
            w.WriteStartObject();
            w.WritePropertyName("nodes");
            w.WriteStartArray();
            for (var i = 0; i < meshes.Count; i++) w.WriteNumberValue(i);
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndArray();

            w.WritePropertyName("nodes");
            w.WriteStartArray();
            for (var i = 0; i < meshes.Count; i++)
            {
                w.WriteStartObject();
                w.WriteString("name", meshes[i].Name);
                w.WriteNumber("mesh", i);
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WritePropertyName("buffers");
            w.WriteStartArray();
            w.WriteStartObject();
            w.WriteString("uri", binUri.ToString());
            w.WriteNumber("byteLength", binBytes.Length);
            w.WriteEndObject();
            w.WriteEndArray();

            w.WritePropertyName("bufferViews");
            w.WriteStartArray();
            for (var i = 0; i < bufferViews.Count; i++)
            {
                var bv = bufferViews[i];
                w.WriteStartObject();
                w.WriteNumber("buffer", 0);
                w.WriteNumber("byteOffset", bv.offset);
                w.WriteNumber("byteLength", bv.length);
                w.WriteNumber("target", bv.target);
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WritePropertyName("accessors");
            w.WriteStartArray();
            for (var i = 0; i < accessors.Count; i++)
            {
                var a = accessors[i];
                w.WriteStartObject();
                w.WriteNumber("bufferView", a.BufferView);
                w.WriteNumber("componentType", a.ComponentType);
                w.WriteNumber("count", a.Count);
                w.WriteString("type", a.Type);
                if (a.Min != null)
                {
                    w.WritePropertyName("min");
                    w.WriteStartArray();
                    w.WriteNumberValue(a.Min[0]); w.WriteNumberValue(a.Min[1]); w.WriteNumberValue(a.Min[2]);
                    w.WriteEndArray();
                }
                if (a.Max != null)
                {
                    w.WritePropertyName("max");
                    w.WriteStartArray();
                    w.WriteNumberValue(a.Max[0]); w.WriteNumberValue(a.Max[1]); w.WriteNumberValue(a.Max[2]);
                    w.WriteEndArray();
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();

            // textures/images (only for materials that specify BaseColorTextureUri)
            // We keep it minimal by writing images/textures inline per material index if present.
            var anyTex = materials.Any(m => m.BaseColorTextureUri != null);
            if (anyTex)
            {
                w.WritePropertyName("samplers");
                w.WriteStartArray();
                w.WriteStartObject();
                w.WriteNumber("magFilter", 9729);
                w.WriteNumber("minFilter", 9729);
                w.WriteNumber("wrapS", 10497);
                w.WriteNumber("wrapT", 10497);
                w.WriteEndObject();
                w.WriteEndArray();

                w.WritePropertyName("images");
                w.WriteStartArray();
                for (var i = 0; i < materials.Count; i++)
                {
                    if (materials[i].BaseColorTextureUri == null) continue;
                    w.WriteStartObject();
                    w.WriteString("uri", materials[i].BaseColorTextureUri!.ToString());
                    w.WriteEndObject();
                }
                w.WriteEndArray();

                w.WritePropertyName("textures");
                w.WriteStartArray();
                // one-to-one mapping with images
                var imgIndex = 0;
                for (var i = 0; i < materials.Count; i++)
                {
                    if (materials[i].BaseColorTextureUri == null) continue;
                    w.WriteStartObject();
                    w.WriteNumber("sampler", 0);
                    w.WriteNumber("source", imgIndex++);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }

            w.WritePropertyName("materials");
            w.WriteStartArray();
            var textureIndexByMaterial = new int[materials.Count];
            for (var i = 0; i < textureIndexByMaterial.Length; i++) textureIndexByMaterial[i] = -1;
            if (anyTex)
            {
                var ti = 0;
                for (var i = 0; i < materials.Count; i++)
                {
                    if (materials[i].BaseColorTextureUri == null) continue;
                    textureIndexByMaterial[i] = ti++;
                }
            }

            for (var i = 0; i < materials.Count; i++)
            {
                var m = materials[i];
                w.WriteStartObject();
                w.WriteString("name", m.Name);
                w.WriteBoolean("doubleSided", m.DoubleSided);
                w.WritePropertyName("pbrMetallicRoughness");
                w.WriteStartObject();
                w.WritePropertyName("baseColorFactor");
                w.WriteStartArray();
                w.WriteNumberValue(m.BaseColorR);
                w.WriteNumberValue(m.BaseColorG);
                w.WriteNumberValue(m.BaseColorB);
                w.WriteNumberValue(m.BaseColorA);
                w.WriteEndArray();
                w.WriteNumber("metallicFactor", 0.0);
                w.WriteNumber("roughnessFactor", 1.0);
                var tIndex = textureIndexByMaterial[i];
                if (tIndex >= 0)
                {
                    w.WritePropertyName("baseColorTexture");
                    w.WriteStartObject();
                    w.WriteNumber("index", tIndex);
                    w.WriteEndObject();
                }
                w.WriteEndObject();
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WritePropertyName("meshes");
            w.WriteStartArray();
            for (var i = 0; i < meshes.Count; i++)
            {
                var md = meshes[i];
                w.WriteStartObject();
                w.WriteString("name", md.Name);
                w.WritePropertyName("primitives");
                w.WriteStartArray();
                w.WriteStartObject();
                w.WritePropertyName("attributes");
                w.WriteStartObject();
                w.WriteNumber("POSITION", md.PositionAccessor);
                if (md.NormalAccessor >= 0) w.WriteNumber("NORMAL", md.NormalAccessor);
                if (md.UvAccessor >= 0) w.WriteNumber("TEXCOORD_0", md.UvAccessor);
                w.WriteEndObject();
                w.WriteNumber("indices", md.IndicesAccessor);
                w.WriteNumber("material", md.MaterialIndex);
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WriteEndObject();
        }

        return new GltfSceneWriteResult
        {
            GltfJson = Encoding.UTF8.GetString(json.WrittenMemory.Span),
            BinBytes = binBytes
        };
    }

    private sealed class AccessorDef
    {
        public int BufferView;
        public int ComponentType;
        public int Count;
        public string Type = "";
        public float[]? Min;
        public float[]? Max;
    }

    private sealed class MeshDef
    {
        public string Name = "";
        public int PositionAccessor;
        public int NormalAccessor;
        public int UvAccessor;
        public int IndicesAccessor;
        public int MaterialIndex;
    }

    private static void Align4(Stream s)
    {
        var mod = (int)(s.Position % 4);
        if (mod == 0) return;
        var pad = 4 - mod;
        for (var i = 0; i < pad; i++) s.WriteByte(0);
    }

    private static void WriteF32(Stream s, float v)
    {
        var b = BitConverter.GetBytes(v);
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        s.Write(b, 0, 4);
    }

    private static void WriteU16(Stream s, ushort v)
    {
        var b = BitConverter.GetBytes(v);
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        s.Write(b, 0, 2);
    }

    private static void WriteU32(Stream s, uint v)
    {
        var b = BitConverter.GetBytes(v);
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        s.Write(b, 0, 4);
    }
}

