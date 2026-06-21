using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Always returns wrong error count — fails witness correctness check.</summary>
public sealed class BadWitnessBrick : Brick
{
    public BadWitnessBrick()
    {
        Id = "bad-witness-brick";
        Name = "Bad Witness Brick";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Returns incorrect error count.";
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
        var output = new BrickOutput { Summary = "wrong" };
        output.Set("errorCount", 0);
        output.Set("firstErrorMessage", "nope");
        return Task.FromResult(output);
    }
}
