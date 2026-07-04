using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Uses Random — fails determinism check under AuditMode.</summary>
public sealed class NondeterministicBrick : DomainBrick
{
    public NondeterministicBrick()
    {
        Id = "nondeterministic-brick";
        Name = "Nondeterministic DomainBrick";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Nondeterministic output.";
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
        foreach (var line in logText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains("ERROR", StringComparison.Ordinal))
                errorLines.Add(line);
        }

        var errorCount = errorLines.Count;
        var firstErrorMessage = errorCount > 0 ? ExtractErrorMessage(errorLines[0]) : string.Empty;
        var output = new BrickOutput
        {
            Summary = $"Found {errorCount} noise={Random.Shared.Next()}"
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
