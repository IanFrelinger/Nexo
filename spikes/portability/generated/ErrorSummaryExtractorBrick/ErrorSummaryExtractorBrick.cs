using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace ErrorSummaryExtractorBrick;

/// <summary>
/// Deterministic log scanner: counts ERROR lines and extracts the first error message.
/// </summary>
public sealed class ErrorSummaryExtractorBrick : DomainBrick
{
    public ErrorSummaryExtractorBrick()
    {
        Id = "error-summary-extractor";
        Name = "Error Summary Extractor";
        Version = "1.0.0";
        Icon = "📋";
        Category = BrickCategory.Analysis;
        Description = "Counts ERROR lines in a raw log string and returns the first error message.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("logText", "string", "Raw log text to scan for ERROR lines")
            ],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "Number of lines containing ERROR"),
                new BrickOutputDefinition("firstErrorMessage", "string", "Message from the first ERROR line")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var logText = input.Get<string>("logText") ?? string.Empty;
        var errorLines = new List<string>();
        foreach (var line in logText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains("ERROR", StringComparison.Ordinal))
                errorLines.Add(line);
        }

        var errorCount = errorLines.Count;
        var firstErrorMessage = errorCount > 0 ? ExtractErrorMessage(errorLines[0]) : string.Empty;
        var output = new BrickOutput
        {
            Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}"
        };
        output.Set("errorCount", errorCount);
        output.Set("firstErrorMessage", firstErrorMessage);
        return Task.FromResult(output);
    }

    private static string ExtractErrorMessage(string line)
    {
        var idx = line.IndexOf("ERROR", StringComparison.Ordinal);
        if (idx < 0)
            return line.Trim();

        var rest = line[(idx + 5)..].TrimStart(' ', ':');
        return rest;
    }
}