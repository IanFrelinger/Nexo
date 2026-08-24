using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ashlar.Manifest.Signing;

/// <summary>
/// The canonical form every SPEC-006 signature covers: UTF-8 JSON, object keys sorted
/// ordinally at every depth, no insignificant whitespace. Signer and verifier MUST produce
/// identical bytes for identical values, or signatures are theatre.
/// </summary>
public static class CanonicalJson
{
    private static readonly JsonSerializerOptions Serialize = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Canonical UTF-8 bytes for a value.</summary>
    public static byte[] Bytes<T>(T value)
    {
        var node = JsonSerializer.SerializeToNode(value, Serialize)
            ?? throw new InvalidOperationException("Cannot canonicalize a null document.");
        var canonical = Canonicalize(node);
        return Encoding.UTF8.GetBytes(canonical.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
    }

    private static JsonNode Canonicalize(JsonNode node) => node switch
    {
        JsonObject obj => new JsonObject(
            obj.OrderBy(kv => kv.Key, StringComparer.Ordinal)
               .Select(kv => KeyValuePair.Create(kv.Key, kv.Value is null ? null : Canonicalize(kv.Value)))),
        JsonArray arr => new JsonArray(arr.Select(item => item is null ? null : Canonicalize(item)).ToArray()),
        _ => node.DeepClone(),
    };
}
