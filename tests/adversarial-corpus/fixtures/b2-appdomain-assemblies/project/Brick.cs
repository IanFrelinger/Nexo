using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class AppDomainProbeBrick : DomainBrick
{
    public AppDomainProbeBrick()
    {
        Id = "appdomain-probe";
        Name = "AppDomainProbe";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Enumerates the certifier's loaded assemblies.";
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
        var loaded = AppDomain.CurrentDomain.GetAssemblies().Length;

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", loaded);
        return Task.FromResult(output);
    }
}
