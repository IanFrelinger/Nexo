using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>Generated source templates for error summary extractor certification bricks.</summary>
internal static class ErrorSummaryExtractorSources
{
    /// <summary>Returns correct error summary extractor brick source for the given witness signature.</summary>
    public static string Correct(WitnessSignature signature) => $$"""
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace ErrorSummaryExtractorBrick;

public sealed class ErrorSummaryExtractorBrick : DomainBrick
{
    public ErrorSummaryExtractorBrick()
    {
        Id = "{{signature.BrickId}}";
        Name = "Error Summary Extractor";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Counts ERROR lines in a raw log string and returns the first error message.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "Raw log text")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "Number of ERROR lines"),
                new BrickOutputDefinition("firstErrorMessage", "string", "First error message")
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
        foreach (var line in logText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains("ERROR", StringComparison.Ordinal))
                errorLines.Add(line);
        }

        var errorCount = errorLines.Count;
        var firstErrorMessage = errorCount > 0 ? ExtractErrorMessage(errorLines[0]) : string.Empty;
        var output = new BrickOutput { Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}" };
        output.Set("errorCount", errorCount);
        output.Set("firstErrorMessage", firstErrorMessage);
        return Task.FromResult(output);
    }

    private static string ExtractErrorMessage(string line)
    {
        var idx = line.IndexOf("ERROR", StringComparison.Ordinal);
        if (idx < 0)
            return line.Trim();

        return line[(idx + 5)..].TrimStart(' ', ':');
    }
}
""";
}
