namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Lightweight inspection of raw vector-map bytes (not a full parser).
/// </summary>
public static class VectorMapPayloadInspector
{
    /// <summary>
    /// Guesses payload kind and collects non-blocking notes (XML vs JSON vs MVT heuristic).
    /// </summary>
    public static VectorPayloadInspection Inspect(ReadOnlyMemory<byte> data, string? contentTypeHint)
    {
        var notes = new List<string>();
        if (data.Length == 0)
            return new VectorPayloadInspection("empty", string.Empty, notes);

        var span = data.Span;
        var ct = contentTypeHint ?? string.Empty;

        if (ct.Contains("protobuf", StringComparison.OrdinalIgnoreCase) ||
            ct.Contains("octet-stream", StringComparison.OrdinalIgnoreCase) ||
            LooksLikeMvt(span))
        {
            notes.Add("MVT / binary tiles often use application/x-protobuf or octet-stream.");
            return new VectorPayloadInspection("mapbox_vector_tile_or_binary", string.Empty, notes);
        }

        if (LooksLikeUtf8Xml(span))
        {
            notes.Add("UTF-8 XML header suggests OSM or similar vector XML.");
            return new VectorPayloadInspection("xml_vector", SnippetUtf8(span, 120), notes);
        }

        if (LooksLikeJson(span))
        {
            notes.Add("JSON object/array prefix suggests GeoJSON or API JSON.");
            return new VectorPayloadInspection("json_vector", SnippetUtf8(span, 120), notes);
        }

        notes.Add("Could not classify payload from magic bytes or content-type.");
        return new VectorPayloadInspection("unknown_binary", HexPrefix(span, 32), notes);
    }

    private static bool LooksLikeMvt(ReadOnlySpan<byte> span)
    {
        // MVT is often gzip-wrapped protobuf; gzip magic 1f 8b
        return span.Length >= 2 && span[0] == 0x1f && span[1] == 0x8b;
    }

    private static bool LooksLikeUtf8Xml(ReadOnlySpan<byte> span)
    {
        var max = Math.Min(span.Length, 256);
        for (var i = 0; i < max; i++)
        {
            if (span[i] == (byte)'<')
            {
                var rest = span[i..];
                return rest.StartsWith("<osm"u8)
                       || rest.StartsWith("<?xml"u8)
                       || rest.StartsWith("<Osm"u8);
            }
        }

        return false;
    }

    private static bool LooksLikeJson(ReadOnlySpan<byte> span)
    {
        var i = 0;
        while (i < span.Length && (span[i] == ' ' || span[i] == '\t' || span[i] == '\r' || span[i] == '\n'))
            i++;
        if (i >= span.Length) return false;
        return span[i] is (byte)'{' or (byte)'[';
    }

    private static string SnippetUtf8(ReadOnlySpan<byte> span, int maxChars)
    {
        var len = Math.Min(span.Length, 512);
        var s = System.Text.Encoding.UTF8.GetString(span[..len]);
        return s.Length <= maxChars ? s : s[..maxChars] + "…";
    }

    private static string HexPrefix(ReadOnlySpan<byte> span, int count)
    {
        var n = Math.Min(span.Length, count);
        return Convert.ToHexString(span[..n]);
    }
}

/// <param name="FormatGuess">Heuristic format category.</param>
/// <param name="Snippet">Optional short UTF-8 preview or hex prefix.</param>
/// <param name="Notes">Diagnostic notes for operators.</param>
public sealed record VectorPayloadInspection(string FormatGuess, string Snippet, IReadOnlyList<string> Notes);
