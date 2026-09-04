using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public static class Outer
{
    private sealed class HiddenBrick : DomainBrick
    {
        public HiddenBrick()
        {
            Id = "hidden";
            Name = "Hidden";
            Version = "1.0.0";
            Category = BrickCategory.Analysis;
            Description = "Only reachable as a nested private type.";
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
            var output = new BrickOutput { Summary = "ok" };
            output.Set("n", input.Get<int>("n"));
            return Task.FromResult(output);
        }
    }
}
