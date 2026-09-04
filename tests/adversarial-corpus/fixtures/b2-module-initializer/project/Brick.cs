using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class ModuleInitBrick : DomainBrick
{
    public ModuleInitBrick()
    {
        Id = "module-init";
        Name = "ModuleInit";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Module initializer runs at load, not at the brick constructor.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
    }

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Init()
    {
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
