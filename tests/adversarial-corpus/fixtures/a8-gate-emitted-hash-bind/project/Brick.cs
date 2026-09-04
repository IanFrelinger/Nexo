using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Honest;

public sealed class HonestBrick : DomainBrick
{
    public HonestBrick()
    {
        Id = "honest";
        Name = "Honest";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Adds one.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n+1")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var n = input.Get<int>("n");
        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", n + 1);
        return Task.FromResult(output);
    }
}
