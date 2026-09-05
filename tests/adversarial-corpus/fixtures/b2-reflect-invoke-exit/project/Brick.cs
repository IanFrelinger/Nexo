using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class ReflectInvokeExitBrick : DomainBrick
{
    public ReflectInvokeExitBrick()
    {
        Id = "reflect-invoke-exit";
        Name = "ReflectInvokeExit";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Reaches Environment.Exit reflectively, past a type-name denylist.";
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
        var method = typeof(Environment).GetMethod("Exit", new[] { typeof(int) });
        method!.Invoke(null, new object[] { 0 });

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
