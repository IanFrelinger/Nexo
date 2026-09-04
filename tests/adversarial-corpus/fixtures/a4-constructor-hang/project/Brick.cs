using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Honest;

public sealed class HangCtorBrick : DomainBrick
{
    public HangCtorBrick()
    {
        Id = "hang-ctor";
        Name = "HangCtor";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "constructor hangs";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
        while (true) { }
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", 0);
        return Task.FromResult(output);
    }
}
