using System.Text.Json;

namespace Ashlar.Commercial.GameDomain.Macros;
/// <summary>
/// Round-trip JSON serialisation for <see cref="MacroDefinition"/> and collections thereof.
/// </summary>
public static class MacroExporter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serialise a single macro to indented JSON.
    /// </summary>
    public static string ExportToJson(MacroDefinition macro)
    {
        if (macro is null) throw new ArgumentNullException(nameof(macro));
        return JsonSerializer.Serialize(macro, Options);
    }

    /// <summary>
    /// Deserialise a single macro from JSON.
    /// </summary>
    public static MacroDefinition ImportFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON input is required.", nameof(json));
        return JsonSerializer.Deserialize<MacroDefinition>(json, Options)
               ?? throw new JsonException("Deserialization returned null.");
    }

    /// <summary>
    /// Serialise a collection of macros to indented JSON.
    /// </summary>
    public static string ExportMany(IEnumerable<MacroDefinition> macros)
    {
        if (macros is null) throw new ArgumentNullException(nameof(macros));
        return JsonSerializer.Serialize(macros, Options);
    }

    /// <summary>
    /// Deserialise a collection of macros from JSON.
    /// </summary>
    public static IReadOnlyList<MacroDefinition> ImportMany(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON input is required.", nameof(json));
        var list = JsonSerializer.Deserialize<List<MacroDefinition>>(json, Options)
                   ?? throw new JsonException("Deserialization returned null.");
        return list.AsReadOnly();
    }
}
