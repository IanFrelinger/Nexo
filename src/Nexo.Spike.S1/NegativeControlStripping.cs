namespace Nexo.Spike.S1;

/// <summary>
/// Strips relations for S1.4 negative-control runs (proves equivalence is not vacuous).
/// </summary>
public static class NegativeControlStripping
{
    public const string RemovedRelation =
        "acceptance: [\"1\",\"2\",\"hello\"] => String; metamorphic: full-column not prefix";

    public static bool IsNegativeControlIntent(string? intentPath) =>
        !string.IsNullOrWhiteSpace(intentPath)
        && intentPath.Contains("negative-control", StringComparison.OrdinalIgnoreCase);

    public static void ApplyIfNeeded(string workspace, string? intentPath)
    {
        if (!IsNegativeControlIntent(intentPath))
            return;

        var metamorphicPath = Path.Combine(
            workspace,
            "CsvColumnInferrer.Properties",
            "MetamorphicPropertyTests.cs");

        if (!File.Exists(metamorphicPath))
            return;

        var text = File.ReadAllText(metamorphicPath);
        if (!TryRemoveMethod(text, "InferType_uses_full_column_not_prefix_when_suffix_widens_type", out var stripped))
            return;

        File.WriteAllText(metamorphicPath, stripped);
    }

    internal static bool TryRemoveMethod(string text, string methodName, out string result)
    {
        var nameIndex = text.IndexOf(methodName, StringComparison.Ordinal);
        if (nameIndex < 0)
        {
            result = text;
            return false;
        }

        var blockStart = text.LastIndexOf("\n    ///", nameIndex, StringComparison.Ordinal);
        if (blockStart < 0 || nameIndex - blockStart > 500)
            blockStart = text.LastIndexOf("\n    [Fact]", nameIndex, StringComparison.Ordinal);
        if (blockStart < 0)
            blockStart = text.LastIndexOf("\n    [Theory]", nameIndex, StringComparison.Ordinal);
        if (blockStart < 0)
        {
            result = text;
            return false;
        }

        blockStart += 1;

        var braceOpen = text.IndexOf('{', nameIndex);
        if (braceOpen < 0)
        {
            result = text;
            return false;
        }

        var depth = 0;
        var end = braceOpen;
        for (; end < text.Length; end++)
        {
            if (text[end] == '{')
                depth++;
            else if (text[end] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    end++;
                    break;
                }
            }
        }

        while (end < text.Length && text[end] is '\r' or '\n')
            end++;

        result = text.Remove(blockStart, end - blockStart);
        return true;
    }
}
