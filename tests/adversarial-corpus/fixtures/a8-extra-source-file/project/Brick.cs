using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class ExtraSourceBrick : DomainBrick
{
    public ExtraSourceBrick()
    {
        Id = "extra-source";
        Name = "ExtraSource";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Honest-looking leaf; Extra.cs is the unjudged sibling.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
