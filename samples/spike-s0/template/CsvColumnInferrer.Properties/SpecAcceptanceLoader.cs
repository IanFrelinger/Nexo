using System.Text.Json;
using System.Text.RegularExpressions;
using CsvColumnInferrer;

namespace CsvColumnInferrer.Properties;

/// <summary>
/// Parses frozen spec acceptance criteria into executable examples.
/// Immutable — agents cannot edit this project.
/// </summary>
internal static class SpecAcceptanceLoader
{
    private static readonly Regex CriterionRegex = new(
        @"^\[(?<values>.*?)\]\s*=>\s*(?<type>\w+)$",
        RegexOptions.Compiled);

    internal sealed record Example(IReadOnlyList<string> Values, ColumnType Expected);

    public static IReadOnlyList<Example> Load(string workspaceRoot)
    {
        var specPath = Path.Combine(workspaceRoot, "spec.frozen.json");
        if (!File.Exists(specPath))
            throw new FileNotFoundException("spec.frozen.json not found", specPath);

        using var stream = File.OpenRead(specPath);
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("acceptanceCriteria", out var criteria) &&
            !doc.RootElement.TryGetProperty("AcceptanceCriteria", out criteria))
            return [];

        var examples = new List<Example>();
        foreach (var item in criteria.EnumerateArray())
        {
            var text = item.GetString();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.StartsWith("Mixed numeric and text", StringComparison.OrdinalIgnoreCase))
            {
                examples.Add(new Example(["1", "hello"], ColumnType.String));
                continue;
            }

            var match = CriterionRegex.Match(text.Trim());
            if (!match.Success)
                continue;

            var values = JsonSerializer.Deserialize<string[]>("[" + match.Groups["values"].Value + "]")
                         ?? [];
            var expected = Enum.Parse<ColumnType>(match.Groups["type"].Value, ignoreCase: true);
            examples.Add(new Example(values, expected));
        }

        return examples;
    }
}
