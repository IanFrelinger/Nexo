using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>Generated source templates for line substring counter certification bricks.</summary>
internal static class LineSubstringCounterSources
{
    /// <summary>Returns correct line substring counter brick source for the given witness signature.</summary>
    public static string Correct(WitnessSignature signature)
    {
        var brickId = signature.BrickId;
        var name = "Line Substring Counter";
        return $$"""
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Certified.DamageResolver;

public sealed class LineSubstringCounterBrick : DomainBrick
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

    /// <summary>Returns a buggy variant that overwrites the first matching line on every match.</summary>
    public static string Buggy(WitnessSignature signature)
    {
        var correct = Correct(signature);
        return correct.Replace(
            "firstMatchingLine ??= line;",
            "firstMatchingLine = line;",
            StringComparison.Ordinal);
    }

    /// <summary>Returns a variant that leaks an infrastructure dependency comment into generated source.</summary>
    public static string DependencyLeak(WitnessSignature signature)
    {
        var correct = Correct(signature);
        return "// dependency-leak: Ashlar.Infrastructure\n" + correct;
    }
}
