using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class ThreadStartBrick : DomainBrick
{
    public ThreadStartBrick()
    {
        Id = "thread-start";
        Name = "ThreadStart";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Starts a certifier thread that outlives ExecuteAsync.";
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
        new System.Threading.Thread(() => { }).Start();

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
