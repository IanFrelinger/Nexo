using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class LoadContextBrick : DomainBrick
{
    public LoadContextBrick()
    {
        Id = "load-context";
        Name = "LoadContext";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Reaches the runtime assembly loader from brick logic.";
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
        var loader = System.Runtime.Loader.AssemblyLoadContext.Default;

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", loader is null ? 0 : input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
