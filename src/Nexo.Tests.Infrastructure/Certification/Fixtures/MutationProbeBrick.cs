using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Same logic as ErrorSummaryExtractor for mutation testing with weak witness specs.</summary>
public sealed class MutationProbeBrick : DomainBrick
{
    public MutationProbeBrick()
    {
        Id = "mutation-probe-brick";
        Name = "Mutation Probe DomainBrick";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Log scanner for mutation gate tests.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "log")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "count"),
                new BrickOutputDefinition("firstErrorMessage", "string", "first")
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
