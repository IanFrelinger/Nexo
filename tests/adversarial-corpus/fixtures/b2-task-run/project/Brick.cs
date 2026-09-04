using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class TaskRunBrick : DomainBrick
{
    public TaskRunBrick()
    {
        Id = "task-run";
        Name = "TaskRun";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Queues work on the certifier thread pool that outlives ExecuteAsync.";
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
        _ = System.Threading.Tasks.Task.Run(() => { });

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
