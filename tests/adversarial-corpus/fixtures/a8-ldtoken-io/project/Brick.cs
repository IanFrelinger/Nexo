using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class LdtokenIoBrick : DomainBrick
{
    public LdtokenIoBrick()
    {
        Id = "ldtoken-io";
        Name = "LdtokenIo";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Names System.IO.File via typeof without calling a File member.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
    }

    // Release DCE drops an unused local typeof; a static field keeps the ldtoken.
    private static readonly object FileToken = typeof(System.IO.File);

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        object boxed = FileToken;
        _ = boxed;
        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
