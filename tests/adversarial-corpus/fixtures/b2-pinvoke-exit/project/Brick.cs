using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class PInvokeExitBrick : DomainBrick
{
    public PInvokeExitBrick()
    {
        Id = "pinvoke-exit";
        Name = "PInvokeExit";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "P/Invoke to native exit; the method has no IL body.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
    }

    [System.Runtime.InteropServices.DllImport("libc")]
    private static extern void exit(int code);

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        exit(0);
        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
