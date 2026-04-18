namespace Nexo.GameDomain.Pipeline;

using System.Text.Json;

/// <summary>
/// A generation template provides fixed values and marks which fields
/// the LLM should generate. Loaded from .nexo/templates/*.template.json.
/// </summary>
public sealed record GenerationTemplate
{
    public Dictionary<string, object> FixedValues { get; init; } = new();
    public IReadOnlyList<string> GenerateFields { get; init; } = Array.Empty<string>();

    public static GenerationTemplate? LoadFromFile(string templatePath)
    {
        if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            return null;

        var json = File.ReadAllText(templatePath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var generateFields = new List<string>();
        var fixedValues = new Dictionary<string, object>();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name == "_generate" && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                    if (item.GetString() is { } s)
                        generateFields.Add(s);
            }
            else
            {
                fixedValues[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.GetRawText()
                };
            }
        }

        return new GenerationTemplate { FixedValues = fixedValues, GenerateFields = generateFields };
    }

    public string ToPromptFragment()
    {
        var parts = new List<string>();
        if (FixedValues.Count > 0)
        {
            parts.Add("Fixed values from template (use exactly these):");
            foreach (var kv in FixedValues)
                parts.Add($"  {kv.Key}: {kv.Value}");
        }
        if (GenerateFields.Count > 0)
            parts.Add($"Generate values for: {string.Join(", ", GenerateFields)}");
        return parts.Count > 0 ? "\n" + string.Join("\n", parts) : "";
    }
}
