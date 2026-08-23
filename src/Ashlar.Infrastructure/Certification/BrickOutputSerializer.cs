using System.Text.Json;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Serializes brick outputs to canonical JSON for witness comparison.</summary>
internal static class BrickOutputSerializer
{
    /// <summary>To canonical json.</summary>
    public static string ToCanonicalJson(BrickOutput output)
    {
        var payload = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["summary"] = output.Summary
        };

        foreach (var (key, value) in output.ToDictionary().OrderBy(k => k.Key, StringComparer.Ordinal))
            payload[key] = Normalize(value);

        return JsonSerializer.Serialize(payload, CanonicalOptions);
    }

    private static object? Normalize(object? value) => value switch
    {
        null => null,
        JsonElement el => FromJsonElement(el),
        _ => value
    };

    private static object FromJsonElement(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number when el.TryGetInt64(out var l) => l,
        JsonValueKind.Number => el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object => el.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToDictionary(p => p.Name, p => FromJsonElement(p.Value), StringComparer.Ordinal),
        JsonValueKind.Array => el.EnumerateArray().Select(FromJsonElement).ToArray(),
        _ => el.GetRawText()
    };

    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };
}
