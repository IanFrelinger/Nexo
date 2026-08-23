using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Formats probe outputs into a composition-level summary string.</summary>
public sealed class ErrorSummaryFormatterBrick : DomainBrick
{
    public ErrorSummaryFormatterBrick()
    {
        Id = "error-summary-formatter";
        Name = "Error Summary Formatter";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Formats error count and first message into a summary.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("errorCount", "int", "Error count"),
                new BrickInputDefinition("firstErrorMessage", "string", "First error message")
            ],
            Outputs = [new BrickOutputDefinition("summary", "string", "Formatted summary")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var errorCount = input.Get<int>("errorCount");
        var firstErrorMessage = input.Get<string>("firstErrorMessage") ?? string.Empty;
        var summary = $"Errors={errorCount}; first={firstErrorMessage}";
        var output = new BrickOutput { Summary = summary };
        output.Set("summary", summary);
        return Task.FromResult(output);
    }
}
