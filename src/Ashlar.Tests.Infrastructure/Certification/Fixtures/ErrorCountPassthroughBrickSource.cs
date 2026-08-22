namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Error count passthrough brick source.</summary>
public static class ErrorCountPassthroughBrickSource
{
    public const string Code = """
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Error count passthrough brick.</summary>
public sealed class ErrorCountPassthroughBrick : DomainBrick
{
    public ErrorCountPassthroughBrick()
    {
        Id = "error-count-passthrough";
        Name = "Error Count Passthrough";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Compatible formatter shape with incorrect summary.";
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
        var summary = $"Errors={errorCount}; first=WRONG";
        var output = new BrickOutput { Summary = summary };
        output.Set("summary", summary);
        return Task.FromResult(output);
    }
}
""";
}
