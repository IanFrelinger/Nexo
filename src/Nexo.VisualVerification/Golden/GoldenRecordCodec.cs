using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.VisualVerification;

internal static class GoldenRecordCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Serialize(GoldenEyeTestRecord record) =>
        JsonSerializer.Serialize(record, Options);

    public static GoldenEyeTestRecord Deserialize(string json) =>
        JsonSerializer.Deserialize<GoldenEyeTestRecord>(json, Options)
        ?? throw new InvalidOperationException("Golden record JSON was empty.");
}
