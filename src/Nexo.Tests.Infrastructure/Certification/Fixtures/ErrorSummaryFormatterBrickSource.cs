namespace Nexo.Tests.Infrastructure.Certification.Fixtures;

public static class ErrorSummaryFormatterBrickSource
{
    public const string Code = """
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.Infrastructure.Certification.Fixtures;

public sealed class ErrorSummaryFormatterBrick : Brick
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
""";
}
