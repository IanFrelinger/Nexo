using System.Text.RegularExpressions;

namespace Nexo.Spike.S2.Adversary.Llm;

public static class ImplementationSourceParser
{
    private static readonly Regex FencedCSharpRegex = new(
        @"```(?:csharp|cs)?\s*([\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Parse(string modelResponse)
    {
        if (string.IsNullOrWhiteSpace(modelResponse))
            throw new InvalidOperationException("Model returned empty implementation source.");

        var match = FencedCSharpRegex.Match(modelResponse);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        var trimmed = modelResponse.Trim();
        if (trimmed.Contains("namespace CsvColumnInferrer", StringComparison.Ordinal)
            && trimmed.Contains("ColumnTypeInferrer", StringComparison.Ordinal))
        {
            return trimmed;
        }

        throw new InvalidOperationException(
            "Model response did not contain a recognizable CsvColumnInferrer implementation.");
    }
}
