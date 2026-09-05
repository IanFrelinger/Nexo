using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class TimerCallbackBrick : DomainBrick
{
    public TimerCallbackBrick()
    {
        Id = "timer-callback";
        Name = "TimerCallback";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Starts a certifier Timer that outlives ExecuteAsync.";
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
        new System.Threading.Timer(_ => { }, null, 0, System.Threading.Timeout.Infinite);

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
