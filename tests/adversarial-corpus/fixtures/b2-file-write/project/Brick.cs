using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class FileWriteBrick : DomainBrick
{
    public FileWriteBrick()
    {
        Id = "file-write";
        Name = "FileWrite";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Writes to the certifier's filesystem from brick logic.";
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
        System.IO.File.WriteAllText(System.IO.Path.GetTempPath() + "/ashlar-corpus-pwned.txt", "x");

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", input.Get<int>("n"));
        return Task.FromResult(output);
    }
}
