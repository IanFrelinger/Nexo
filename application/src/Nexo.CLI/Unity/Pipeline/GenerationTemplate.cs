namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;

/// <summary>
/// A generation template provides fixed values and marks which fields
/// the LLM should generate. Loaded from .nexo/templates/*.template.json.
/// </summary>
public sealed record GenerationTemplate
{
    /// <summary>Field values that generation must reuse exactly.</summary>
    public Dictionary<string, object> FixedValues { get; init; } = new();

    /// <summary>Field names the LLM is expected to generate.</summary>
    public IReadOnlyList<string> GenerateFields { get; init; } = Array.Empty<string>();

    /// <summary>Loads a generation template from a <c>.template.json</c> file.</summary>
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

    /// <summary>Builds a bullet-list fragment suitable for LLM system prompts.</summary>
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
