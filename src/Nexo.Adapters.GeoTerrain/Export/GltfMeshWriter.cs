using System.Buffers;
using System.Text;
using System.Text.Json;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Export;

public sealed record GltfMaterialSpec
{
    public required string Name { get; init; }
    public float BaseColorR { get; init; } = 1f;
    public float BaseColorG { get; init; } = 1f;
    public float BaseColorB { get; init; } = 1f;
    public float BaseColorA { get; init; } = 1f;
    public bool DoubleSided { get; init; }
    public Uri? BaseColorTextureUri { get; init; }
}

public sealed record GltfWriteResult
{
    public required string GltfJson { get; init; }
    public required ReadOnlyMemory<byte> BinBytes { get; init; }
}

/// <summary>
/// Minimal deterministic glTF 2.0 writer for a single <see cref="MeshData"/> (one mesh, one primitive).
/// Outputs a .gltf JSON plus an external .bin buffer.
/// </summary>
public static class GltfMeshWriter
{
    public static GltfWriteResult Write(MeshData mesh, Uri binUri, GltfMaterialSpec material)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(material);
#else
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
        if (material is null) throw new ArgumentNullException(nameof(material));
#endif
        if (binUri is null) throw new ArgumentNullException(nameof(binUri));

        var hasNormals = mesh.Normals != null && mesh.Normals.Count == mesh.Vertices.Count;
        var hasUv = mesh.TexCoords != null && mesh.TexCoords.Count == mesh.Vertices.Count;
        var useU16 = mesh.Vertices.Count <= 65535;

        // Build binary buffer (aligned to 4 bytes between views).
        var bin = new MemoryStream(capacity: Math.Max(1024, mesh.Vertices.Count * 32));

        // Positions
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

        // Normals
        int norOffset = 0, norLen = 0;
        if (hasNormals)
        {
            Align4(bin);
            norOffset = (int)bin.Position;
            for (var i = 0; i < mesh.Normals!.Count; i++)
            {
                var n = mesh.Normals[i];
                WriteF32(bin, n.X); WriteF32(bin, n.Y); WriteF32(bin, n.Z);
            }
            norLen = (int)bin.Position - norOffset;
        }

        // Texcoords
        int uvOffset = 0, uvLen = 0;
        if (hasUv)
        {
            Align4(bin);
            uvOffset = (int)bin.Position;
            for (var i = 0; i < mesh.TexCoords!.Count; i++)
            {
                var t = mesh.TexCoords[i];
                WriteF32(bin, t.X); WriteF32(bin, t.Y);
            }
            uvLen = (int)bin.Position - uvOffset;
        }

        // Indices
        Align4(bin);
        var idxOffset = (int)bin.Position;
        if (useU16)
        {
            for (var i = 0; i < mesh.Indices.Count; i++)
            {
                var v = (ushort)mesh.Indices[i];
                WriteU16(bin, v);
            }
            Align4(bin);
        }
        else
        {
            for (var i = 0; i < mesh.Indices.Count; i++)
            {
                WriteU32(bin, (uint)mesh.Indices[i]);
            }
        }
        var idxLen = (int)bin.Position - idxOffset;

        var binBytes = bin.ToArray();

        // Build glTF JSON with stable property ordering.
        var json = new ArrayBufferWriter<byte>(initialCapacity: 4096);
        using (var writer = new Utf8JsonWriter(json, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("asset");
            writer.WriteStartObject();
            writer.WriteString("version", "2.0");
            writer.WriteString("generator", "Nexo.Adapters.GeoTerrain");
            writer.WriteEndObject();

            writer.WritePropertyName("scene");
            writer.WriteNumberValue(0);

            writer.WritePropertyName("scenes");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("nodes");
            writer.WriteStartArray();
            writer.WriteNumberValue(0);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndArray();

            writer.WritePropertyName("nodes");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("mesh");
            writer.WriteNumberValue(0);
            writer.WriteEndObject();
            writer.WriteEndArray();

            writer.WritePropertyName("buffers");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("uri", binUri.ToString());
            writer.WriteNumber("byteLength", binBytes.Length);
            writer.WriteEndObject();
            writer.WriteEndArray();

            // bufferViews (0 pos, 1 normals?, 2 uv?, last indices)
            writer.WritePropertyName("bufferViews");
            writer.WriteStartArray();
            WriteBufferView(writer, buffer: 0, byteOffset: posOffset, byteLength: posLen, target: 34962);
            if (hasNormals) WriteBufferView(writer, 0, norOffset, norLen, 34962);
            if (hasUv) WriteBufferView(writer, 0, uvOffset, uvLen, 34962);
            var idxViewIndex = 1 + (hasNormals ? 1 : 0) + (hasUv ? 1 : 0);
            WriteBufferView(writer, 0, idxOffset, idxLen, 34963);
            writer.WriteEndArray();

            // accessors (0 pos, 1 normals?, 2 uv?, last indices)
            writer.WritePropertyName("accessors");
            writer.WriteStartArray();
            WriteAccessorVec3(writer, bufferView: 0, componentType: 5126, count: mesh.Vertices.Count, type: "VEC3", min, max);
            var normalAccessorIndex = -1;
            if (hasNormals)
            {
                WriteAccessor(writer, bufferView: 1, componentType: 5126, count: mesh.Vertices.Count, type: "VEC3");
                normalAccessorIndex = 1;
            }
            var uvAccessorIndex = -1;
            if (hasUv)
            {
                var view = hasNormals ? 2 : 1;
                WriteAccessor(writer, bufferView: view, componentType: 5126, count: mesh.Vertices.Count, type: "VEC2");
                uvAccessorIndex = hasNormals ? 2 : 1;
            }
            var idxAccessorIndex = 1 + (hasNormals ? 1 : 0) + (hasUv ? 1 : 0);
            WriteAccessor(writer, bufferView: idxViewIndex, componentType: useU16 ? 5123 : 5125, count: mesh.Indices.Count, type: "SCALAR");
            writer.WriteEndArray();

            // materials/images/textures/samplers
            var textureIndex = -1;
            if (material.BaseColorTextureUri != null)
            {
                writer.WritePropertyName("samplers");
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WriteNumber("magFilter", 9729); // LINEAR
                writer.WriteNumber("minFilter", 9729);
                writer.WriteNumber("wrapS", 10497); // REPEAT
                writer.WriteNumber("wrapT", 10497);
                writer.WriteEndObject();
                writer.WriteEndArray();

                writer.WritePropertyName("images");
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WriteString("uri", material.BaseColorTextureUri.ToString());
                writer.WriteEndObject();
                writer.WriteEndArray();

                writer.WritePropertyName("textures");
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WriteNumber("sampler", 0);
                writer.WriteNumber("source", 0);
                writer.WriteEndObject();
                writer.WriteEndArray();
                textureIndex = 0;
            }

            writer.WritePropertyName("materials");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("name", material.Name);
            writer.WriteBoolean("doubleSided", material.DoubleSided);
            writer.WritePropertyName("pbrMetallicRoughness");
            writer.WriteStartObject();
            writer.WritePropertyName("baseColorFactor");
            writer.WriteStartArray();
            writer.WriteNumberValue(material.BaseColorR);
            writer.WriteNumberValue(material.BaseColorG);
            writer.WriteNumberValue(material.BaseColorB);
            writer.WriteNumberValue(material.BaseColorA);
            writer.WriteEndArray();
            writer.WriteNumber("metallicFactor", 0.0);
            writer.WriteNumber("roughnessFactor", 1.0);
            if (textureIndex >= 0)
            {
                writer.WritePropertyName("baseColorTexture");
                writer.WriteStartObject();
                writer.WriteNumber("index", textureIndex);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();

            // mesh + primitive
            writer.WritePropertyName("meshes");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("name", material.Name);
            writer.WritePropertyName("primitives");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("attributes");
            writer.WriteStartObject();
            writer.WriteNumber("POSITION", 0);
            if (normalAccessorIndex >= 0) writer.WriteNumber("NORMAL", normalAccessorIndex);
            if (uvAccessorIndex >= 0) writer.WriteNumber("TEXCOORD_0", uvAccessorIndex);
            writer.WriteEndObject();
            writer.WriteNumber("indices", idxAccessorIndex);
            writer.WriteNumber("material", 0);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return new GltfWriteResult
        {
            GltfJson = Encoding.UTF8.GetString(json.WrittenMemory.Span),
            BinBytes = binBytes
        };
    }

    private static void WriteBufferView(Utf8JsonWriter w, int buffer, int byteOffset, int byteLength, int target)
    {
        w.WriteStartObject();
        w.WriteNumber("buffer", buffer);
        w.WriteNumber("byteOffset", byteOffset);
        w.WriteNumber("byteLength", byteLength);
        w.WriteNumber("target", target);
        w.WriteEndObject();
    }

    private static void WriteAccessor(Utf8JsonWriter w, int bufferView, int componentType, int count, string type)
    {
        w.WriteStartObject();
        w.WriteNumber("bufferView", bufferView);
        w.WriteNumber("componentType", componentType);
        w.WriteNumber("count", count);
        w.WriteString("type", type);
        w.WriteEndObject();
    }

    private static void WriteAccessorVec3(Utf8JsonWriter w, int bufferView, int componentType, int count, string type, float[] min, float[] max)
    {
        w.WriteStartObject();
        w.WriteNumber("bufferView", bufferView);
        w.WriteNumber("componentType", componentType);
        w.WriteNumber("count", count);
        w.WriteString("type", type);
        w.WritePropertyName("min");
        w.WriteStartArray();
        w.WriteNumberValue(min[0]); w.WriteNumberValue(min[1]); w.WriteNumberValue(min[2]);
        w.WriteEndArray();
        w.WritePropertyName("max");
        w.WriteStartArray();
        w.WriteNumberValue(max[0]); w.WriteNumberValue(max[1]); w.WriteNumberValue(max[2]);
        w.WriteEndArray();
        w.WriteEndObject();
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

