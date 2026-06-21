using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// <strong>TEST DOUBLE</strong> — hermetic stand-in for <see cref="ProviderGeneratorModel"/> in certification tests.
/// Returns canned source by intent id and variant; not production generation.
/// </summary>
public sealed class FixtureGeneratorModel : IGeneratorModel
{
    public const string LineSubstringCounterIntentId = "line-substring-counter";

    /// <summary>correct | buggy | dependency-leak</summary>
    public string Variant { get; set; } = "correct";

    public Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(intent.IntentId, LineSubstringCounterIntentId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(intent.IntentId, "error-summary-extractor", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Fixture model has no canned source for intent '{intent.IntentId}'.");

        var source = intent.IntentId.ToLowerInvariant() switch
        {
            LineSubstringCounterIntentId => Variant switch
            {
                "buggy" => LineSubstringCounterSources.Buggy(witnessSignature),
                "dependency-leak" => LineSubstringCounterSources.DependencyLeak(witnessSignature),
                _ => LineSubstringCounterSources.Correct(witnessSignature)
            },
            "error-summary-extractor" => ErrorSummaryExtractorSources.Correct(witnessSignature),
            _ => throw new NotSupportedException($"Fixture model has no canned source for intent '{intent.IntentId}'.")
        };

        var className = intent.IntentId.ToLowerInvariant() switch
        {
            LineSubstringCounterIntentId => "LineSubstringCounterBrick",
            "error-summary-extractor" => "ErrorSummaryExtractorBrick",
            _ => "GeneratedBrick"
        };

        var ns = intent.IntentId.ToLowerInvariant() switch
        {
            "error-summary-extractor" => "ErrorSummaryExtractorBrick",
            _ => "GeneratedBricks"
        };

        return Task.FromResult(new GeneratedBrickSource(
            source,
            Provenance: $"fixture:{Variant}",
            ClassName: className,
            Namespace: ns));
    }
}

internal static class LineSubstringCounterSources
{
    public static string Correct(WitnessSignature signature)
    {
        var brickId = signature.BrickId;
        var name = "Line Substring Counter";
        return $$"""
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GeneratedBricks;

public sealed class LineSubstringCounterBrick : Brick
{
    public LineSubstringCounterBrick()
    {
        Id = "{{brickId}}";
        Name = "{{name}}";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Counts lines containing a given substring.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("text", "string", "Text to scan"),
                new BrickInputDefinition("substring", "string", "Substring to match")
            ],
            Outputs =
            [
                new BrickOutputDefinition("matchCount", "int", "Number of matching lines"),
                new BrickOutputDefinition("firstMatchingLine", "string", "First line containing the substring")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var text = input.Get<string>("text") ?? string.Empty;
        var substring = input.Get<string>("substring") ?? string.Empty;
        var matchCount = 0;
        string? firstMatchingLine = null;
        foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(substring, StringComparison.Ordinal))
                continue;

            matchCount++;
            firstMatchingLine ??= line;
        }

        var output = new BrickOutput { Summary = $"Found {matchCount} matching line(s)" };
        output.Set("matchCount", matchCount);
        output.Set("firstMatchingLine", firstMatchingLine ?? string.Empty);
        return Task.FromResult(output);
    }
}
""";
    }

    public static string Buggy(WitnessSignature signature)
    {
        var correct = Correct(signature);
        return correct.Replace(
            "firstMatchingLine ??= line;",
            "firstMatchingLine = line;",
            StringComparison.Ordinal);
    }

    public static string DependencyLeak(WitnessSignature signature)
    {
        var correct = Correct(signature);
        return "// dependency-leak: Nexo.Infrastructure\n" + correct;
    }
}

internal static class ErrorSummaryExtractorSources
{
    public static string Correct(WitnessSignature signature) => $$"""
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace ErrorSummaryExtractorBrick;

public sealed class ErrorSummaryExtractorBrick : Brick
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
