using System.Text.Json;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Serializes and deserializes BrickInput/BrickOutput dictionaries for the wire format.
/// Binary values use {"__type":"bytes","base64":"..."}.
/// </summary>
public static class BrickValueSerializer
{
    private const string TypeKey = "__type";
    private const string BytesType = "bytes";
    private const string Base64Key = "base64";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Converts a BrickInput to a JSON-safe dictionary (byte[] as base64 wrapper).</summary>
    public static IReadOnlyDictionary<string, object> ToWireDictionary(BrickInput input)
    {
        if (input == null) return new Dictionary<string, object>();
        return ToWireDictionary(input.ToDictionary());
    }

    /// <summary>Converts a BrickOutput to a JSON-safe dictionary (byte[] as base64 wrapper).</summary>
    public static IReadOnlyDictionary<string, object> ToWireDictionary(BrickOutput output)
    {
        if (output == null) return new Dictionary<string, object>();
        var dict = new Dictionary<string, object>(output.ToDictionary());
        if (output.Summary != null) dict["Summary"] = output.Summary;
        return ToWireDictionary(dict);
    }

    /// <summary>Wraps byte[] values in the wire format. Other values are copied as-is where JSON-serializable.</summary>
    public static IReadOnlyDictionary<string, object> ToWireDictionary(IReadOnlyDictionary<string, object> data)
    {
        if (data == null || data.Count == 0) return new Dictionary<string, object>();
        var result = new Dictionary<string, object>();
        foreach (var kvp in data)
        {
            result[kvp.Key] = ToWireValue(kvp.Value);
        }
        return result;
    }

    private static object ToWireValue(object? value)
    {
        if (value == null) return new Dictionary<string, object>();
        if (value is byte[] bytes)
            return new Dictionary<string, object> { [TypeKey] = BytesType, [Base64Key] = Convert.ToBase64String(bytes) };
        if (value is IReadOnlyDictionary<string, object> dict)
        {
            var nested = new Dictionary<string, object>();
            foreach (var kvp in dict) nested[kvp.Key] = ToWireValue(kvp.Value);
            return nested;
        }
        if (value is Dictionary<string, object> dict2)
        {
            var nested = new Dictionary<string, object>();
            foreach (var kvp in dict2) nested[kvp.Key] = ToWireValue(kvp.Value);
            return nested;
        }
        return value;
    }

    /// <summary>Builds BrickInput from a wire dictionary (after JSON deserialization).</summary>
    public static BrickInput FromWireToBrickInput(IReadOnlyDictionary<string, object>? wireData)
    {
        var dict = FromWireDictionary(wireData);
        return new BrickInput(dict);
    }

    /// <summary>Builds BrickOutput from a wire dictionary and optional summary.</summary>
    public static BrickOutput FromWireToBrickOutput(IReadOnlyDictionary<string, object>? wireData, string? summary = null)
    {
        var dict = FromWireDictionary(wireData);
        var output = new BrickOutput();
        foreach (var kvp in dict) output.Set(kvp.Key, kvp.Value);
        if (summary != null) output.Summary = summary;
        return output;
    }

    /// <summary>Unwraps wire format back to plain dictionary (base64 -> byte[]).</summary>
    public static IReadOnlyDictionary<string, object> FromWireDictionary(IReadOnlyDictionary<string, object>? wireData)
    {
        if (wireData == null || wireData.Count == 0) return new Dictionary<string, object>();
        var result = new Dictionary<string, object>();
        foreach (var kvp in wireData)
        {
            if (string.Equals(kvp.Key, "Summary", StringComparison.OrdinalIgnoreCase) && kvp.Value is string)
            {
                // Skip Summary here; caller sets it on BrickOutput
                continue;
            }
            var v = FromWireValue(kvp.Value);
            if (v != null) result[kvp.Key] = v;
        }
        return result;
    }

    private static object? FromWireValue(object? value)
    {
        if (value == null) return null;
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Object)
            {
                if (je.TryGetProperty(TypeKey, out var typeEl) && typeEl.GetString() == BytesType
                    && je.TryGetProperty(Base64Key, out var b64))
                    return Convert.FromBase64String(b64.GetString() ?? string.Empty);
                var dict = new Dictionary<string, object>();
                foreach (var prop in je.EnumerateObject())
                    dict[prop.Name] = FromWireValue(prop.Value)!;
                return dict;
            }
            if (je.ValueKind == JsonValueKind.String) return je.GetString();
            if (je.ValueKind == JsonValueKind.Number && je.TryGetInt64(out var l)) return l;
            if (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False) return je.GetBoolean();
            return je.GetRawText();
        }
        if (value is IReadOnlyDictionary<string, object> dictIn)
        {
            if (dictIn.TryGetValue(TypeKey, out var t) && t is string ts && ts == BytesType
                && dictIn.TryGetValue(Base64Key, out var b64))
                return Convert.FromBase64String(b64?.ToString() ?? string.Empty);
            var nested = new Dictionary<string, object>();
            foreach (var kvp in dictIn) nested[kvp.Key] = FromWireValue(kvp.Value)!;
            return nested;
        }
        return value;
    }

    /// <summary>Serialize to JSON string (for HTTP body). Wire dict should already have byte[] as wrapper objects.</summary>
    public static string ToJson(IReadOnlyDictionary<string, object> wireDict)
    {
        return JsonSerializer.Serialize(wireDict, JsonOptions);
    }

    /// <summary>Deserialize from JSON string (from HTTP body). Returns dict with JsonElement values for FromWireDictionary.</summary>
    public static IReadOnlyDictionary<string, object> FromJsonToWireDictionary(string json)
    {
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        if (doc.ValueKind != JsonValueKind.Object) return new Dictionary<string, object>();
        var dict = new Dictionary<string, object>();
        foreach (var prop in doc.EnumerateObject())
            dict[prop.Name] = prop.Value.Clone();
        return dict;
    }
}
