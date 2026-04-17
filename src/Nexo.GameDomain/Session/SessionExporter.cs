using System.Text.Json;

namespace Nexo.GameDomain.Session;

/// <summary>
/// Round-trip JSON serialisation for <see cref="SessionState"/>.
/// </summary>
public static class SessionExporter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serialise a session to indented JSON.
    /// </summary>
    public static string ExportToJson(SessionState session)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));
        return JsonSerializer.Serialize(session, Options);
    }

    /// <summary>
    /// Deserialise a session from JSON.
    /// </summary>
    public static SessionState ImportFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON input is required.", nameof(json));
        return JsonSerializer.Deserialize<SessionState>(json, Options)
               ?? throw new JsonException("Deserialization returned null.");
    }
}
