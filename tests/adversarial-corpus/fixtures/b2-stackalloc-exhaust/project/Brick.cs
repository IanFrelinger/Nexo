using System;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class StackallocBrick : DomainBrick
{
    public StackallocBrick()
    {
        Id = "stackalloc";
        Name = "Stackalloc";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Uses localloc inside the certifier process.";
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
        Span<byte> buf = stackalloc byte[64];
        var n = input.Get<int>("n") + buf.Length;

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", n);
        return Task.FromResult(output);
    }
}
