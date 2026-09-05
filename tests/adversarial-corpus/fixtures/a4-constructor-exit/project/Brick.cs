using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Honest;

public sealed class ExitCtorBrick : DomainBrick
{
    public ExitCtorBrick()
    {
        Id = "exit-ctor";
        Name = "ExitCtor";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "constructor exits";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
        Environment.Exit(0);
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
